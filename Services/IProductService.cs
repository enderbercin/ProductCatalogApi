using ProductCatalogApi.Models;
using ProductCatalogApi.ExternalClients;

namespace ProductCatalogApi.Services
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetAllProductsAsync();
        Task<ProductDto?> GetProductByCodeAsync(string productCode);
        Task<ProductDto> CreateProductAsync(CreateProductRequest request);
        Task<List<ProductDto>> GetLowStockProductsAsync();
        Task<List<FakeStoreProduct>> GetExternalProductsAsync();
        Task<FakeStoreProduct?> GetExternalProductByIdAsync(int id);
        
        // Ürün eşleştirme metodları
        Task<ProductDto> CreateProductFromExternalAsync(int fakeStoreId);
        Task<string?> GetProductCodeByFakeStoreIdAsync(int fakeStoreId);
        Task<int?> GetFakeStoreIdByProductCodeAsync(string productCode);
        Task<ProductDto?> DecreaseStockAsync(string productCode, int amount);
        Task<ProductDto?> IncreaseStockAsync(string productCode, int amount);
    }
} 