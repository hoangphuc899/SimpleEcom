using SimpleEcom.Models;

namespace SimpleEcom.Repositories
{
    public interface IProductRepository
    {
        IEnumerable<Product> GetProducts(string category, string query, string sortOrder);
        IEnumerable<Product> GetRelatedProducts(string category, int currentProductId, int count);
        Product GetById(int id);
        void Add(Product product);
        void Update(Product product);
        void Delete(int id);
        void Save();
    }
}