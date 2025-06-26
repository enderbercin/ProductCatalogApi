using ProductCatalogApi.Models;
using ProductCatalogApi.Repositories;
using ProductCatalogApi.ExternalClients;
using Microsoft.Extensions.Logging;
using ProductCatalogApi.Utils;

namespace ProductCatalogApi.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IFakeStoreClient _fakeStoreClient;
        private readonly ILogger<ProductService> _logger;
        private readonly Dictionary<int, string> _fakeStoreToProductCodeMapping = new();

        public ProductService(
            IProductRepository productRepository,
            IFakeStoreClient fakeStoreClient,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _fakeStoreClient = fakeStoreClient;
            _logger = logger;
        }

        public async Task<List<ProductDto>> GetAllProductsAsync()
        {
            var localProducts = await _productRepository.GetAllAsync();
            var externalProducts = await _fakeStoreClient.GetProductsAsync();

            // Fake Store'dan eklenmiş in-memory ürünleri de dahil et
            var localFakeStoreIds = localProducts.Where(p => p.FakeStoreId != null).Select(p => p.FakeStoreId).ToHashSet();

            // Fake Store'dan gelen ürünleri, in-memory'de aynı FakeStoreId ile eklenmiş olanlar hariç ekle
            var externalDtos = externalProducts
                .Where(ep => !localFakeStoreIds.Contains(ep.id))
                .Select(ep => new ProductDto
                {
                    ProductCode = $"FAKE-{ep.id}",
                    Name = ep.title ?? $"Fake Product {ep.id}",
                    Threshold = 10, // Varsayılan threshold
                    InitialStock = ep.rating?.count ?? 0,
                    CurrentStock = ep.rating?.count ?? 0,
                    CreatedAt = DateTime.UtcNow,
                    FakeStoreId = ep.id,
                    StockInRoman = RomanNumeralConverter.ToRoman(ep.rating?.count ?? 0)
                });

            var localDtos = localProducts.Select(MapToDto);

            // Hem local (in-memory) hem de dış API'den gelen ürünler birleşik olarak dönüyor
            var allDtos = localDtos.Concat(externalDtos).ToList();

            return allDtos;
        }

        public async Task<ProductDto?> GetProductByCodeAsync(string productCode)
        {
            var product = await _productRepository.GetByProductCodeAsync(productCode);
            return product != null ? MapToDto(product) : null;
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductRequest request)
        {
            // Validasyon
            if (request.InitialStock < request.Threshold)
            {
                throw new ValidationException("Initial stock cannot be less than threshold");
            }

            // ProductCode oluşturma (basit bir GUID tabanlı yaklaşım)
            var productCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            var product = new Product
            {
                ProductCode = productCode,
                Name = request.Name,
                Threshold = request.Threshold,
                InitialStock = request.InitialStock,
                CurrentStock = request.InitialStock,
                CreatedAt = DateTime.UtcNow
            };

            var createdProduct = await _productRepository.CreateAsync(product);
            _logger.LogInformation("Created new product with code: {ProductCode}", productCode);

            return MapToDto(createdProduct);
        }

        public async Task<List<ProductDto>> GetLowStockProductsAsync()
        {
            var localProducts = await _productRepository.GetAllAsync();
            var externalProducts = await _fakeStoreClient.GetProductsAsync();

            var localLowStock = localProducts.Where(p => p.CurrentStock < p.Threshold).Select(MapToDto);

            var externalLowStock = externalProducts
                .Where(ep => (ep.rating?.count ?? 0) < 10) // Varsayılan threshold
                .Select(ep => new ProductDto
                {
                    ProductCode = $"FAKE-{ep.id}",
                    Name = ep.title ?? $"Fake Product {ep.id}",
                    Threshold = 10,
                    InitialStock = ep.rating?.count ?? 0,
                    CurrentStock = ep.rating?.count ?? 0,
                    CreatedAt = DateTime.UtcNow,
                    FakeStoreId = ep.id,
                    StockInRoman = RomanNumeralConverter.ToRoman(ep.rating?.count ?? 0)
                });

            var allLowStock = localLowStock.Concat(externalLowStock.Where(ep => !localProducts.Any(lp => lp.FakeStoreId == ep.FakeStoreId))).ToList();

            return allLowStock;
        }

        public async Task<List<FakeStoreProduct>> GetExternalProductsAsync()
        {
            try
            {
                return await _fakeStoreClient.GetProductsAsync();
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(ex, "Failed to retrieve external products");
                throw;
            }
        }

        public async Task<FakeStoreProduct?> GetExternalProductByIdAsync(int id)
        {
            try
            {
                return await _fakeStoreClient.GetProductByIdAsync(id);
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(ex, "Failed to retrieve external product with ID: {Id}", id);
                throw;
            }
        }

        public async Task<ProductDto> CreateProductFromExternalAsync(int fakeStoreId)
        {
            try
            {
                // Fake Store'dan ürün bilgilerini al
                var externalProduct = await _fakeStoreClient.GetProductByIdAsync(fakeStoreId);
                if (externalProduct == null)
                {
                    throw new ValidationException($"External product with ID {fakeStoreId} not found");
                }

                // ProductCode oluştur
                var productCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

                // İç sistem ürünü oluştur
                var product = new Product
                {
                    ProductCode = productCode,
                    Name = externalProduct.title ?? $"External Product {fakeStoreId}",
                    Threshold = 10, // Varsayılan threshold
                    InitialStock = 50, // Varsayılan stok
                    CurrentStock = 50,
                    FakeStoreId = fakeStoreId,
                    CreatedAt = DateTime.UtcNow
                };

                var createdProduct = await _productRepository.CreateAsync(product);

                // Eşleştirme kaydı
                _fakeStoreToProductCodeMapping[fakeStoreId] = productCode;

                _logger.LogInformation("Created product {ProductCode} from external product {FakeStoreId}", 
                    productCode, fakeStoreId);

                return MapToDto(createdProduct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create product from external ID: {FakeStoreId}", fakeStoreId);
                throw;
            }
        }

        public async Task<string?> GetProductCodeByFakeStoreIdAsync(int fakeStoreId)
        {
            // Önce mapping'den kontrol et
            if (_fakeStoreToProductCodeMapping.TryGetValue(fakeStoreId, out var productCode))
            {
                return productCode;
            }

            // Veritabanından kontrol et
            var products = await _productRepository.GetAllAsync();
            var product = products.FirstOrDefault(p => p.FakeStoreId == fakeStoreId);
            return product?.ProductCode;
        }

        public async Task<int?> GetFakeStoreIdByProductCodeAsync(string productCode)
        {
            var product = await _productRepository.GetByProductCodeAsync(productCode);
            return product?.FakeStoreId;
        }

        private static ProductDto MapToDto(Product product)
        {
            return new ProductDto
            {
                ProductCode = product.ProductCode,
                Name = product.Name,
                Threshold = product.Threshold,
                InitialStock = product.InitialStock,
                CurrentStock = product.CurrentStock,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                FakeStoreId = product.FakeStoreId,
                StockInRoman = RomanNumeralConverter.ToRoman(product.CurrentStock > 0 ? product.CurrentStock : 0)
            };
        }
    }

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message)
        {
        }
    }
} 