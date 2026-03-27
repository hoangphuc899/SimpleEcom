using SimpleEcom.Data;
using SimpleEcom.Models;

namespace SimpleEcom.Repositories
{
    public class OrderRepository: IOrderRepository
    {
        private readonly AppDbContext _context;
        public OrderRepository(AppDbContext context) => _context = context;
        public IEnumerable<Order> GetAll() => _context.Orders.OrderByDescending(o => o.OrderDate).ToList();
        public Order GetById(int id) => _context.Orders.Find(id);
        public void Delete(int id)
        {
            var order = _context.Orders.Find(id);
            if(order != null) _context.Orders.Remove(order);
        }
        public void Save() => _context.SaveChanges();
        public void Add(Order order) => _context.Orders.Add(order);
        public decimal GetTotalRevenue()
        {
            var validOrders = _context.Orders.Where(o => o.Status == "Hoàn thành").ToList();
            decimal total = 0;
            foreach(var order in validOrders)
                total += order.TotalAmount;

            return total;
        }
    }
}