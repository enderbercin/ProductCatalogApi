using ProductCatalogApi.Models;

namespace ProductCatalogApi.Services
{
    public interface IOrderService
    {
        Task<BulkOrderResult> CheckAndPlaceOrdersAsync();
        Task<List<OrderDto>> GetAllOrdersAsync();
        Task<OrderDto?> GetOrderByIdAsync(string orderId);
    }
} 