using ProductCatalogApi.Models;

namespace ProductCatalogApi.Repositories
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllAsync();
        Task<Product?> GetByProductCodeAsync(string productCode);
        Task<Product> CreateAsync(Product product);
        Task<Product> UpdateAsync(Product product);
        Task<List<Product>> GetLowStockProductsAsync();
        Task<bool> ExistsAsync(string productCode);
        Task<Product?> DecreaseStockAsync(string productCode, int amount);
        Task<Product?> IncreaseStockAsync(string productCode, int amount);
    }
} 