using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SimpleEcom.Models;

namespace SimpleEcom.Data
{
    public class AppDbContext : IdentityDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders {get; set;}
        public DbSet<WishList> WishLists {get; set;}
        public DbSet<Review> Reviews {get; set;}
        public DbSet<Voucher> Vouchers {get; set;}
        public DbSet<ReviewLike> ReviewLikes {get; set;}
        public DbSet<ReportedReview> ReportedReviews {get; set;}
    }
}