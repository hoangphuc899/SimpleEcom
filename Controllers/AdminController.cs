using Microsoft.AspNetCore.Mvc;
using SimpleEcom.Data;
using Newtonsoft.Json;
using SimpleEcom.Models;
using Microsoft.EntityFrameworkCore;
using SimpleEcom.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace SimpleEcom.Controllers
{   // ngăn người dùng khi đã đăng xuất nhưng bấm nút quay lại vẫn hiện ra trang của admin
    [Authorize(Roles = "Admin")] // chỉ admin mới dc vào
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class AdminController : Controller
    {
        private readonly IProductRepository _productRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly AppDbContext _context;
        public AdminController(IProductRepository productRepo, IOrderRepository orderRepo, AppDbContext context)
        {
            _productRepo = productRepo;
            _orderRepo = orderRepo;
            _context = context;
        }
        public IActionResult Orders(string status)
        {
            var orders = _orderRepo.GetAll();
            ViewBag.TotalRevenue = _orderRepo.GetTotalRevenue();
            // Lọc đơn theo trạng thái
            if(!string.IsNullOrEmpty(status))
                orders = orders.Where(o => o.Status == status);
            
            ViewBag.CurrentStatus = status;
            
            return View(orders);
        }
        public IActionResult DeleteOrder(int id)
        {
            _orderRepo.Delete(id);
            _orderRepo.Save();
            
            return RedirectToAction("Orders");
        }
        public IActionResult CreateProduct()
        {
            return View();
        }
        [HttpPost]
        public IActionResult CreateProduct(Product product, IFormFile mainImage, List<IFormFile> subImages, IFormFile videoFile)
        {
            if (mainImage != null && mainImage.Length > 0)
                product.ImageUrl = SaveFile(mainImage);
            if(subImages != null && subImages.Count > 0)
            {
                foreach(var file in subImages)
                    product.Images.Add(new ProductImage {Url = SaveFile(file)});
            }
            if(videoFile != null && videoFile.Length > 0)
                product.VideoUrl = SaveFile(videoFile);
            
            _productRepo.Add(product);
            _productRepo.Save();
            return RedirectToAction("Products");
        }
        private string SaveFile(IFormFile file)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                file.CopyTo(stream);
            
            return "/uploads/" + fileName;
        }
        private void DeleteFileFromServer(string fileUrl)
        {
            if(string.IsNullOrEmpty(fileUrl)) return;

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileUrl.TrimStart('/')); // Xác định đường dẫn
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
        }
        public IActionResult Products(string category, string query, string sortOrder)
        {
            var products = _productRepo.GetProducts(category, query, sortOrder);

            ViewBag.SelectedCategory = category;
            ViewBag.CurrentQuery = query;
            ViewBag.CurrentSort = sortOrder;
            
            return View(products);
        }
        public IActionResult Edit(int id)
        {
            var product = _productRepo.GetById(id);
            if (product == null) return NotFound();
            return View(product);
        }
        [HttpPost]
        public IActionResult Edit(Product incomingData, IFormFile mainImage, List<IFormFile> subImages, IFormFile videoFile)
        {
            var existingProduct = _context.Products.Include(p => p.Images).FirstOrDefault(p => p.Id == incomingData.Id);
            if (existingProduct == null) return NotFound();

            existingProduct.Name = incomingData.Name;
            existingProduct.Price = incomingData.Price;
            existingProduct.Description = incomingData.Description;
            existingProduct.Category = incomingData.Category;
            existingProduct.Stock = incomingData.Stock;

            if (mainImage != null && mainImage.Length > 0)
            {   // theo đường dẫn cũ xóa ảnh trên server
                DeleteFileFromServer(existingProduct.ImageUrl);
                existingProduct.ImageUrl = SaveFile(mainImage); // ghi đè đường dẫn mới
            }
            if(subImages != null && subImages.Count > 0)
            {
                foreach(var file in subImages)
                {
                    if(file.Length > 0)
                        existingProduct.Images.Add(new ProductImage {Url = SaveFile(file)});
                }
            }
            if(videoFile != null && videoFile.Length > 0)
            {
                DeleteFileFromServer(existingProduct.VideoUrl);
                existingProduct.VideoUrl = SaveFile(videoFile);
            }
            _productRepo.Update(existingProduct);
            _productRepo.Save();

            return RedirectToAction("Products");
        }
        public IActionResult Delete(int id)
        {
            var product = _context.Products.Include(p => p.Images).FirstOrDefault(p => p.Id == id);
            if (product != null)
            {
                DeleteFileFromServer(product.ImageUrl); // Xóa ảnh
                DeleteFileFromServer(product.VideoUrl); // xóa video
                if(product.Images != null && product.Images.Any())
                { // xóa ảnh phụ
                    foreach(var img in product.Images)
                        DeleteFileFromServer(img.Url);
                }
                _productRepo.Delete(id); // Xóa bản ghi
                _productRepo.Save();
                TempData["Success"] = "Đã xóa sản phẩm và tất cả dữ liệu liên quan.";
            }
            return RedirectToAction("Products");
        }
        [HttpPost]
        public IActionResult UpdateOrderStatus(int id, string newStatus)
        {
            var order = _orderRepo.GetById(id);
            if(order == null) return NotFound();

            string oldStatus = order.Status;
            if(newStatus == "Đã hủy" && oldStatus != "Đã hủy") // admin chủ động hủy
            {
                order.DateCancelled = DateTime.Now;
                order.CancellationReason = "Hủy bởi quản trị viên (theo yêu cầu khách hàng hoặc sự cố hàng hóa).";
                var items = JsonConvert.DeserializeObject<List<CartItem>>(order.OrderItemsJson);
                foreach(var item in items)
                {
                    var product = _productRepo.GetById(item.ProductId);
                    if(product != null)
                    {
                        product.Stock += item.Quantity;
                        _productRepo.Update(product);
                    }
                }
            }
            if (!string.IsNullOrEmpty(order.VoucherCode))
            {
                var voucher = _context.Vouchers.FirstOrDefault(v => v.Code == order.VoucherCode);
                if(voucher != null && voucher.UsedCount > 0)
                {
                    voucher.UsedCount--;
                    _context.Vouchers.Update(voucher);
                }
            }
            order.Status = newStatus;
            _orderRepo.Save();
            _productRepo.Save(); // sau khi cập nhật thì phải save

            return RedirectToAction("Orders"); 
        }
        public IActionResult Vouchers()
        {
            var vouchers = _context.Vouchers.ToList();
            return View(vouchers);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateVoucher(Voucher voucher)
        {
            if (ModelState.IsValid)
            {
                _context.Vouchers.Add(voucher);
                _context.SaveChanges();
                TempData["Success"] = "Đã tạo mã giảm giá mới thành công!";
            }
            return RedirectToAction("Vouchers");
        }
        public IActionResult EditVoucher(int id)
        {
            var voucher = _context.Vouchers.Find(id);
            if(voucher == null) return NotFound();

            return View(voucher);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditVoucher(Voucher voucher)
        {
            if (ModelState.IsValid)
            {
                _context.Vouchers.Update(voucher);
                _context.SaveChanges();
                TempData["Success"] = "Đã cập nhật xong!";
                return RedirectToAction("Vouchers");
            }
            return RedirectToAction("Vouchers");
        }
        public IActionResult DeleteVoucher(int id)
        {
            var voucher = _context.Vouchers.Find(id);
            if(voucher != null)
            {
                _context.Vouchers.Remove(voucher);
                _context.SaveChanges();
            }
            return RedirectToAction("Vouchers");
        }
        public IActionResult ReportedReviews()
        {
            var reports = _context.ReportedReviews.Include(r => r.Review).OrderByDescending(r => r.DateReported).ToList();
            return View(reports);
        }
        [HttpPost]
        public IActionResult ResolveReport(int id, bool deleteComment)
        {
            var report = _context.ReportedReviews.Include(r => r.Review).FirstOrDefault(r => r.Id == id);
            if(report == null) return NotFound();

            if(deleteComment && report.Review != null)
                _context.Reviews.Remove(report.Review);
            
            report.IsResolved = true;
            _context.SaveChanges();
            TempData["Success"] = "Đã xử lý báo cáo thành công!";
            return RedirectToAction("ReportedReviews");
        }
    }
}