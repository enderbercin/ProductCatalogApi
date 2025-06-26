using ProductCatalogApi.Models;

namespace ProductCatalogApi.Repositories
{
    public class InMemoryOrderRepository : IOrderRepository
    {
        private readonly List<Order> _orders = new();
        private readonly object _lock = new();

        public Task<List<Order>> GetAllAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(_orders.ToList());
            }
        }

        public Task<Order?> GetByIdAsync(string orderId)
        {
            lock (_lock)
            {
                var order = _orders.FirstOrDefault(o => o.OrderId == orderId);
                return Task.FromResult(order);
            }
        }

        public Task<Order> CreateAsync(Order order)
        {
            lock (_lock)
            {
                _orders.Add(order);
                return Task.FromResult(order);
            }
        }

        public Task<Order> UpdateAsync(Order order)
        {
            lock (_lock)
            {
                var existingOrder = _orders.FirstOrDefault(o => o.OrderId == order.OrderId);
                if (existingOrder == null)
                {
                    throw new InvalidOperationException($"Order with ID {order.OrderId} not found");
                }

                var index = _orders.IndexOf(existingOrder);
                _orders[index] = order;

                return Task.FromResult(order);
            }
        }

        public Task<List<Order>> GetByProductCodeAsync(string productCode)
        {
            lock (_lock)
            {
                var orders = _orders.Where(o => o.ProductCode == productCode).ToList();
                return Task.FromResult(orders);
            }
        }
    }
} 