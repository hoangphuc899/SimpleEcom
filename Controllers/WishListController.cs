using Microsoft.AspNetCore.Mvc;
using SimpleEcom.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SimpleEcom.Data;
using System.Security.Claims;

namespace SimpleEcom.Controllers
{
    [Authorize]
    public class WishListController: Controller
    {
        private readonly AppDbContext _context;
        public WishListController(AppDbContext context) => _context = context;
        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(string.IsNullOrEmpty(userId))
                return RedirectToPage("/Account/Login", new {area = "Identity"});
            
            var wishList = _context.WishLists.Include(w => w.Product).Where(w => w.UserId == userId).ToList();

            return View(wishList);
        }
        [HttpPost]
        public IActionResult ToggleWishList(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var item = _context.WishLists.FirstOrDefault(w => w.ProductId == productId && w.UserId == userId);

            if(item != null)
                _context.WishLists.Remove(item);
            else
                _context.WishLists.Add(new WishList {UserId = userId, ProductId = productId});
            
            _context.SaveChanges();
            return RedirectToAction("Details", "Product", new {id = productId});
        }
    }
}