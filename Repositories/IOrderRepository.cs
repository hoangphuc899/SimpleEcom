using SimpleEcom.Models;

namespace SimpleEcom.Repositories
{
    public interface IOrderRepository
    {
        IEnumerable<Order> GetAll();
        void Add(Order order);
        Order GetById(int id);
        void Delete(int id);
        void Save();
        decimal GetTotalRevenue();
    }
}