using SimpleEcom.Data;
using SimpleEcom.Models;

namespace SimpleEcom.Repositories
{
    public class ProductRepository: IProductRepository
    {
        private readonly AppDbContext _context;
        public ProductRepository(AppDbContext context) => _context = context;
        public IEnumerable<Product> GetProducts(string category, string query, string sortOrder)
        {
            var products = _context.Products.AsQueryable();
            if(!string.IsNullOrEmpty(category))
                products = products.Where(p => p.Category.Contains(category));
            if(!string.IsNullOrEmpty(query))
                products = products.Where(p => p.Name.ToLower().Contains(query.ToLower().Trim()));
            
            switch (sortOrder)
            {
                case "price_asc": products = products.OrderBy(p => p.Price); break;
                case "price_desc": products = products.OrderByDescending(p => p.Price); break;
                default: products = products.OrderBy(p => p.Id); break;
            }
            return products.ToList();
        }
        public IEnumerable<Product> GetRelatedProducts(string category, int currentProductId, int count)
        {
            return _context.Products.Where(p => p.Category == category && p.Id != currentProductId).Take(count).ToList();
        }
        public Product GetById(int id) => _context.Products.Find(id);
        public void Add(Product product) => _context.Products.Add(product);
        public void Update (Product product) => _context.Products.Update(product);
        public void Delete(int id)
        {
            var product = _context.Products.Find(id);
            if(product != null) _context.Products.Remove(product);
        }
        public void Save() => _context.SaveChanges();
    }
}