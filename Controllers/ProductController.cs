using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SimpleEcom.Data;
using SimpleEcom.Models;
using SimpleEcom.Repositories;

namespace SimpleEcom.Controllers
{
    public class ProductController : Controller
    { // ProductController coi như bao gồm UsersController
        private readonly IProductRepository _productRepo;
        private readonly AppDbContext _context;
        private readonly IOrderRepository _orderRepo;
        public ProductController(IProductRepository productRepo, AppDbContext context, IOrderRepository orderRepo)
        {
            _productRepo = productRepo;
            _context = context;
            _orderRepo = orderRepo;
        }
        public IActionResult Index(string category, string query, string sortOrder)
        {   // do Images thuộc bảng khác nên dùng Include
            var products = _context.Products.Include(p => p.Images).ToList();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // tìm kiếm hoặc lọc theo danh mục
            if (!string.IsNullOrEmpty(query))
            {
                products = products.Where(p => p.Name.ToLower().Contains(query.ToLower()) || p.Description.ToLower().Contains(query.ToLower())).ToList();
                category = null; // tìm sản phẩm ở bất kể danh mục nào
            }
            else if(!string.IsNullOrEmpty(category))
                products = products.Where(p => p.Category == category).ToList(); 

            // cá nhơn hóa người dùng
            if(userId != null && string.IsNullOrEmpty(category) && string.IsNullOrEmpty(query))
            {   // thuộc tính thuộc bảng khác thì dùng select
                var wishListCats = _context.WishLists.Where(w => w.UserId == userId).Select(w => w.Product.Category);

                var orderCats = _context.Orders.Where(o => o.UserId == userId && o.Status == "Hoàn thành").SelectMany(o => _context.Reviews.Where(r => r.UserId == userId).Select(r => r.Product.Category));

                var favoriteCategory = wishListCats.Concat(orderCats).GroupBy(c => c).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();

                if(favoriteCategory != null)
                {
                    products = products.OrderByDescending(p => p.Category == favoriteCategory).ToList();
                    ViewBag.Personalized = "Dành riêng cho bạn";
                    ViewBag.FavoriteCategory = favoriteCategory;
                }else 
                    products = products.OrderByDescending(p => p.Category == "Gia dụng").ToList();
            }else if (string.IsNullOrEmpty(category) && string.IsNullOrEmpty(query))
                products = products.OrderByDescending(p => p.Category == "Gia dụng").ToList();
            // sắp xếp
            if(sortOrder == "price_asc") products = products.OrderBy(p => p.Price).ToList();
            else if(sortOrder == "price_desc") 
                products = products.OrderByDescending(p => p.Price).ToList();

            ViewBag.SelectedCategory = category;
            ViewBag.CurrentQuery = query;
            ViewBag.CurrentSort = sortOrder;

            return View(products);
        }
        public IActionResult Details(int id, string sortOrder = "newest")
        {
            var product = _context.Products.Include(p => p.Images).Include(p => p.Reviews).FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Danh sách yêu thích
            ViewBag.IsLiked = _context.WishLists.Any(w => w.ProductId == id && w.UserId == userId);
            // Sản phẩm tương tự
            var relatedProducts = _productRepo.GetRelatedProducts(product.Category, id, 4);
            ViewBag.RelatedProducts = relatedProducts; // lấy 4 sản phẩm tương tự
            // Lấy điểm đánh giá trung bình
            var reviews = product.Reviews.AsQueryable(); // lấy bảng review thông qua product
            switch (sortOrder)
            {
                case "popular":
                    reviews = reviews.OrderByDescending(r => r.LikeCount).ThenByDescending(r => r.DateCreated);
                    ViewBag.CurrentSort = "popular";
                    break;
                case "newest":
                    default:
                        reviews = reviews.OrderByDescending(r => r.DateCreated);
                        ViewBag.CurrentSort = "newest";
                        break;
            }
            product.Reviews = reviews.ToList(); // gán lại cho product đó
            double averageRating = 0;
            int reviewCount = product.Reviews.Count();
            if(reviewCount > 0) averageRating = product.Reviews.Average(r => r.Rating);

            ViewBag.AverageRating = averageRating;
            ViewBag.ReviewCount = reviewCount;
            return View(product);
        }
        [Authorize]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [HttpPost]
        public IActionResult LikeReview(int reviewId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(string.IsNullOrEmpty(userId)) // kiểm tra đăng nhập
                return Json(new
                {
                    success = false,
                    message = "Bạn cần đăng nhập."
                });
            var review = _context.Reviews.Find(reviewId); // có review rồi mới like dc
            if(review == null) return Json(new {success = false});

            var existingLike = _context.ReviewLikes.FirstOrDefault(l => l.ReviewId == reviewId && l.UserId == userId);
            bool isLiked;
            if(existingLike != null)
            {
                _context.ReviewLikes.Remove(existingLike);
                review.LikeCount = Math.Max(0, review.LikeCount - 1);
                isLiked = false;
            }
            else
            {
                _context.ReviewLikes.Add(new ReviewLike
                {
                    ReviewId = reviewId,
                    UserId = userId
                });
                review.LikeCount++;
                isLiked = true;
            }
            _context.SaveChanges();
            return Json(new {
                success = true, 
                newCount = review.LikeCount,
                isLiked = isLiked // bên trái là biến mới
            });
        }
        public IActionResult MyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            CheckAndCancelExpiredOrders(userId);
            
            var myOrders = _context.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.OrderDate).ToList();
            var userReviews = _context.Reviews.Where(r => r.UserId == userId).ToList();
            ViewBag.UserReviews = userReviews;
            
            return View(myOrders);
        }
        [HttpPost]
        public IActionResult CancelOrder(int id, string reasonOption, string otherReason)
        {   // tìm khách hàng đang đăng nhập và đơn hàng đã mua tương ứng
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = _context.Orders.FirstOrDefault(o => o.Id == id && o.UserId == userId);
            var cart = JsonConvert.DeserializeObject<List<CartItem>>(order.OrderItemsJson);
            string finalReason = reasonOption == "Khác" ? otherReason : reasonOption;
            
            if(order != null && order.Status == "Chờ thanh toán")
            {
                order.Status = "Đã hủy";
                order.DateCancelled = DateTime.Now;
                order.CancellationReason = "Khách hủy vì " + (string.IsNullOrEmpty(finalReason) ? "không có lý do" : finalReason);
                _orderRepo.Save();
                foreach(var item in cart) // hoàn kho
                {
                    var product = _productRepo.GetById(item.ProductId);
                    product.Stock += item.Quantity; // hoàn lại vào kho
                    _productRepo.Update(product);
                }
                _productRepo.Save();
                TempData["Success"] = "Đã hủy đơn hàng thành công.";
            }else TempData["Error"] = "Không thể hủy đơn hàng ở trạng thái này.";

            return RedirectToAction("MyOrders");
        }
        [HttpPost]
        public IActionResult AddReview(int productId, int orderId, int rating, string comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity.Name;
            var order = _context.Orders.FirstOrDefault(o => o.Id == orderId && o.UserId == userId && o.Status == "Hoàn thành");
            if(order == null || order.DateCompleted == null)
            {
                TempData["Error"] = "Bạn không thể đánh giá sản phẩm cho đơn hàng này.";
                return RedirectToAction("MyOrders");
            }
            if((DateTime.Now - order.DateCompleted.Value).TotalDays > 7)
            {
                TempData["Error"] = "Đã quá hạn 7 ngày để đánh giá.";
                return RedirectToAction("MyOrders");
            }
            var existingReview = _context.Reviews.FirstOrDefault(r => r.ProductId == productId && r.OrderId == orderId && r.UserId == userId);
            if(existingReview != null)
            {
                if (existingReview.IsEdited)
                {
                    TempData["Error"] = "Bạn đã sửa đánh giá sản phẩm này rồi.";
                    return RedirectToAction("MyOrders");
                }
                existingReview.Rating = rating;
                existingReview.Comment = comment;
                existingReview.DateCreated = DateTime.Now;
                existingReview.IsEdited = true;

                _context.Reviews.Update(existingReview);
                TempData["Success"] = "Đã cập nhật đánh giá.";
            }
            else // chưa đánh giá thì cho đánh giá bình thường
            {
                var review = new Review
                {
                    ProductId = productId,
                    OrderId = orderId,
                    UserId = userId,
                    FullName = userName,
                    Rating = rating,
                    Comment = comment,
                    DateCreated = DateTime.Now
                };
                _context.Reviews.Add(review);
                TempData["Success"] = "Cảm ơn bạn đã đánh giá sản phẩm";
            }
            _context.SaveChanges();

            return RedirectToAction("MyOrders");
        }
        [HttpPost]
        public IActionResult ConfirmReceipt(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = _context.Orders.FirstOrDefault(o => o.Id == id && o.UserId == userId);

            if(order != null && order.Status == "Đang giao")
            {
                order.Status = "Hoàn thành";
                order.DateCompleted = DateTime.Now;
                if (!string.IsNullOrEmpty(order.OrderItemsJson))
                {
                    var items = JsonConvert.DeserializeObject<List<CartItem>>(order.OrderItemsJson);
                    foreach(var item in items)
                    {
                        var product = _context.Products.Find(item.ProductId);
                        if(product != null) product.SoldCount += item.Quantity;
                    }
                }
                _context.SaveChanges();
                TempData["Success"] = "Cảm ơn bạn đã xác nhận! Bây giờ bạn có thể đánh giá sản phẩm.";
            }else
                TempData["Error"] = "Không thể xác nhận đơn hàng này.";
            
            return RedirectToAction("MyOrders");
        }
        [HttpGet]
        public IActionResult WriteReview(int productId, int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = _context.Orders.FirstOrDefault(o => o.Id == orderId && o.UserId == userId && o.Status == "Hoàn thành");
            if(order == null) return Unauthorized();
            
            var product = _productRepo.GetById(productId);
            if(product == null) return NotFound();

            ViewBag.OrderId = orderId;
            return View(product);
        }
        [HttpPost]
        public IActionResult ReportReview(int id, string reason)
        {
            if(!User.Identity.IsAuthenticated)
                return Json(new
                {
                    success = false,
                    message = "Bạn cần đăng nhập để thực hiện báo cáo."
                });
            var review = _context.Reviews.Find(id);
            if(review == null)
                return Json(new
                {
                   success = false,
                   message = "Bình luận không tồn tại hoặc đã bị xóa." 
                });
            var report = new ReportedReview
            {
              ReviewId = id,
              Reason = reason,
              DateReported = DateTime.Now,
              IsResolved = false  
            };
            try
            {
                _context.ReportedReviews.Add(report);
                _context.SaveChanges();
                return Json(new {success = true});
            }
            catch(Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi gửi báo cáo."
                });
            }
        }
        private void CheckAndCancelExpiredOrders(string userId)
        {
            var expiredOrders = _context.Orders.Where(o => o.UserId == userId && o.Status == "Chờ thanh toán" && o.OrderDate.HasValue && o.OrderDate.Value.AddMinutes(30) < DateTime.Now).ToList();
            if (expiredOrders.Any())
            {
                foreach(var order in expiredOrders)
                {
                    order.Status = "Đã hủy";
                    order.DateCancelled = order.OrderDate?.AddMinutes(30);
                    order.CancellationReason = "Hệ thống tự động hủy do quá 30 phút chưa thanh toán.";
                    if (!string.IsNullOrEmpty(order.OrderItemsJson))
                    {
                        var items = JsonConvert.DeserializeObject<List<CartItem>>(order.OrderItemsJson);
                        foreach(var item in items)
                        {
                            var product = _context.Products.Find(item.ProductId);
                            if(product != null) product.Stock += item.Quantity;
                        }
                    }
                    if (!string.IsNullOrEmpty(order.VoucherCode))
                    {
                        var voucher = _context.Vouchers.FirstOrDefault(v => v.Code == order.VoucherCode);
                        if(voucher != null && voucher.UsedCount > 0) voucher.UsedCount--;
                    }
                }
                _context.SaveChanges();
            }
        }
    }
}