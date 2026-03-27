using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SimpleEcom.Data;
using SimpleEcom.Models;
using SimpleEcom.Repositories;

namespace SimpleEcom.Controllers
{
    public class CartController : Controller
    {
        private readonly IProductRepository _productRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly ICartService _cartService; // @inject ICartService CartService
        private readonly AppDbContext _context;
        public CartController(IProductRepository productRepo, IOrderRepository orderRepo, ICartService cartService, AppDbContext context)
        {
            _productRepo = productRepo;
            _orderRepo = orderRepo;
            _cartService = cartService;
            _context = context;
        }
        public IActionResult AddToCart(int id)
        {
            var product = _productRepo.GetById(id);
            if(product == null) return NotFound();
            
            if(product.Stock <= 0)
            {
                TempData["Error"] = "Sản phẩm đã hết hàng";
                return RedirectToAction("Index", "Product");
            }
            _cartService.AddToCart(product);

            return RedirectToAction("Index");
        }
        public IActionResult Index()
        {
            var cart = _cartService.GetCart();

            var availableVouchers = _context.Vouchers.Where(v => v.IsActive == true && (v.ExpiryDate == null || v.ExpiryDate > DateTime.Now) && (v.UsageLimit == null || v.UsedCount < v.UsageLimit)).ToList();
            ViewBag.AvailableVouchers = availableVouchers;

            return View(cart);
        }
        public IActionResult RemoveFromCart(int id)
        {
            _cartService.RemoveFromCart(id);
            return RedirectToAction("Index");
        }
        [Authorize]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Checkout() // hiển thị đơn hàng
        {
            var cart = _cartService.GetCart();
            if(cart == null || !cart.Any()) 
                return RedirectToAction("Index");
            
            var model = new Order(); // tạo đối tượng order mới và lấy id người đang đăng nhập
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // nếu khách có đơn chưa thanh toán thì ko cho đặt hàng tiếp
            var pendingOrderCount = _context.Orders.Count(o => o.UserId == userId && o.Status == "Chờ thanh toán");
            if(pendingOrderCount >= 1)
            {
                TempData["Error"] = "Bạn có đơn hàng chưa thanh toán. Vui lòng hoàn tất hoặc hủy đơn cũ trước khi đặt đơn mới.";
                return RedirectToAction("Index");
            }
            // nếu có 2 đơn hủy liên tiếp thì phải đợi 60 phút mới cho đặt hàng
            var lastTwoOrders = _context.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.OrderDate).Take(2).ToList();
            if(lastTwoOrders.Count == 2 && lastTwoOrders.All(o => o.Status == "Đã hủy"))
            {   // lấy thời gian hủy của đơn gần nhất trong 2 đơn
                var lastCancelTime = lastTwoOrders.First().DateCancelled;
                if(lastCancelTime != null)
                {
                    var minutesSinceLastCancel = (DateTime.Now - lastCancelTime.Value).TotalMinutes;
                    if(minutesSinceLastCancel < 60)
                    {
                        int waitTime = 60 - (int)minutesSinceLastCancel;
                        TempData["Error"] = $"Bạn đã hủy 2 đơn liên tiếp. Vui lòng đợi {waitTime} phút nữa để đặt lại.";
                        return RedirectToAction("Index");
                    }
                }
            }
            // lưu thông tin đơn hàng trước đó để khách ko phải nhập lại thông tin
            var lastOrder = _orderRepo.GetAll().Where(o => o.UserId == userId).OrderByDescending(o => o.OrderDate).FirstOrDefault();

            if(lastOrder != null)
            {
                model.CustomerName = lastOrder.CustomerName;
                model.Phone = lastOrder.Phone;
                model.Address = lastOrder.Address;
            }
            // voucher
            var vCode = HttpContext.Session.GetString("AppliedVoucher");
            var vDiscountStr = HttpContext.Session.GetString("DiscountAmount");
            if(!string.IsNullOrEmpty(vCode) && decimal.TryParse(vDiscountStr, out decimal discount))
            {
                model.VoucherCode = vCode;
                model.DiscountAmount = discount;
            }
            return View(model);
        }
        [HttpPost]
        public IActionResult ProcessCheckout(Order order)
        {
            var cart = _cartService.GetCart();
            if(cart == null || !cart.Any())
                return RedirectToAction("Index");
            if(User.Identity.IsAuthenticated) // lấy id của người đang đăng nhập
                order.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // tính tiền chưa có khuyến mãi
            decimal trueTotal = 0;
            foreach(var item in cart)
            {
                var productInDb = _productRepo.GetById(item.ProductId);
                if(productInDb != null)
                { // lấy giá thật sự từ db
                    trueTotal += productInDb.Price * item.Quantity;
                    item.Price = productInDb.Price;
                    if(productInDb.Stock >= item.Quantity)
                    {
                        productInDb.Stock -= item.Quantity;
                        _productRepo.Update(productInDb);
                    }
                    else
                    {
                        TempData["Error"] = $"Sản phẩm {productInDb.Name} hiện không đủ số lượng chỉ còn {productInDb.Stock} món trong kho";
                        return RedirectToAction("Index");
                    }
                }
            }
            // khuyến mãi
            var vCode = HttpContext.Session.GetString("AppliedVoucher");
            var vDiscount = HttpContext.Session.GetString("DiscountAmount");
            if (!string.IsNullOrEmpty(vCode))
            {
                decimal discount = decimal.Parse(vDiscount);
                order.VoucherCode = vCode; // ghi vào csdl
                order.DiscountAmount = discount;
                trueTotal -= discount;
                var voucher = _context.Vouchers.FirstOrDefault(v => v.Code == vCode);
                if(voucher != null && voucher.UsedCount < voucher.UsageLimit)
                {
                    voucher.UsedCount++;
                    _context.Vouchers.Update(voucher);
                    _context.SaveChanges();
                }
            }
            // lưu vào database
            order.OrderItemsJson = JsonConvert.SerializeObject(cart); // lưu danh sách hàng hóa vào database
            order.TotalAmount = trueTotal;
            order.OrderDate = DateTime.Now;
            order.Status = "Chờ thanh toán";

            _orderRepo.Add(order); // tạo đơn hàng mới
            _orderRepo.Save();
            _productRepo.Save();

            _cartService.ClearCart(); // Xóa giỏ hàng sau khi mua xong
            HttpContext.Session.Remove("AppliedVoucher"); // xóa voucher sau khi dùng xong
            HttpContext.Session.Remove("DiscountAmount");

            return View(order);
        }
        [HttpPost]
        public IActionResult ConfirmPayment(int orderId)
        {
            var order = _orderRepo.GetById(orderId);
            if(order != null && order.Status == "Chờ thanh toán")
            {
                order.Status = "Chờ xác nhận";
                _orderRepo.Save(); // cập nhật rồi thì phải lưu lại
                TempData["Success"] = "Xác nhận thành công! Vui lòng đợi cửa hàng kiểm tra và giao hàng";
            }
            return RedirectToAction("MyOrders", "Product");
        }
        [HttpPost]
        public IActionResult UpdateCart(List<CartItem> items)
        {
            foreach(var item in items)
            {
                var product = _productRepo.GetById(item.ProductId);
                if(product != null && item.Quantity <= product.Stock)
                    _cartService.UpdateQuantity(item.ProductId, item.Quantity);
            }
            return RedirectToAction("Index");
        }
        public IActionResult RePayment(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = _context.Orders.FirstOrDefault(o => o.Id == orderId && o.UserId == userId && o.Status == "Chờ thanh toán");
            if(order == null)
            {
                TempData["Error"] = "Đơn hàng không tồn tại hoặc đã quá hạn thanh toán.";
                return RedirectToAction("MyOrders", "Product");
            }
            return View("ProcessCheckout", order);
        }
        [HttpPost]
        public IActionResult ApplyVoucher(string voucherCode)
        {
            var voucher = _context.Vouchers.FirstOrDefault(v => v.Code == voucherCode && v.IsActive);
            if(voucher == null) return Json(new {success = false, message = "Mã không hợp lệ"});
            if(voucher.UsedCount >= voucher.UsageLimit)
                return Json(new
                {
                   success = false, 
                   message = "Mã giảm giá này đã hết lượt sử dụng." 
                });
            var cartTotal = _cartService.GetTotal();
            decimal finalDiscount = 0;
            if (voucher.IsPercent) // giảm theo phần trăm
            {
                finalDiscount = cartTotal * (voucher.DiscountValue / 100);
                if (voucher.MaxDiscount.HasValue && finalDiscount > voucher.MaxDiscount.Value)
                    finalDiscount = voucher.MaxDiscount.Value;
            }
            else finalDiscount = voucher.DiscountValue; // giảm theo số tiền cố định
            if(cartTotal < voucher.MinOrderAmount)
            {
                return Json(new
                {
                    success = false,
                    message = $"Đơn hàng chưa đủ tối thiểu {voucher.MinOrderAmount.ToString("N0")}đ để dùng mã này."
                });
            }
            if(DateTime.Now > voucher.ExpiryDate)
            {
                return Json(new
                {
                    success = false,
                    message = $"Mã này đã hết hạn từ ngày {voucher.ExpiryDate.ToString("dd/MM/yyyy")}."
                });
            }
            HttpContext.Session.SetString("AppliedVoucher", voucherCode);
            HttpContext.Session.SetString("DiscountAmount", finalDiscount.ToString());
            
            return Json(new
            {
                success = true,
                discount = finalDiscount,
                message = voucher.IsPercent ? $"Đã áp dụng giảm {voucher.DiscountValue}%" : "Đã áp dụng mã giảm giá."
            });
        }
    }
}