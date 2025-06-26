using ProductCatalogApi.Models;

namespace ProductCatalogApi.Repositories
{
    public interface IOrderRepository
    {
        Task<List<Order>> GetAllAsync();
        Task<Order?> GetByIdAsync(string orderId);
        Task<Order> CreateAsync(Order order);
        Task<Order> UpdateAsync(Order order);
        Task<List<Order>> GetByProductCodeAsync(string productCode);
    }
} 