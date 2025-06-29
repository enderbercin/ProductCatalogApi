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
        private readonly List<Product> _fakeStoreProductsInMemory = new();
        private bool _isInitialized = false;
        private readonly object _lock = new();

        public ProductService(
            IProductRepository productRepository,
            IFakeStoreClient fakeStoreClient,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _fakeStoreClient = fakeStoreClient;
            _logger = logger;
        }

        private async Task InitializeFakeStoreProductsAsync()
        {
            bool needsInit = false;
            lock (_lock)
            {
                if (!_isInitialized)
                {
                    _isInitialized = true;
                    needsInit = true;
                }
            }
            if (!needsInit)
                return;

            try
            {
                var externalProducts = await _fakeStoreClient.GetProductsAsync();
                lock (_lock)
                {
                    _fakeStoreProductsInMemory.Clear();
                    foreach (var ep in externalProducts)
                    {
                        var fakeStoreProduct = new Product
                        {
                            ProductCode = $"FAKE-{ep.id}",
                            Name = ep.title ?? $"Fake Product {ep.id}",
                            Threshold = 10, // Varsayılan threshold
                            InitialStock = ep.rating?.count ?? 0,
                            CurrentStock = ep.rating?.count ?? 0,
                            CreatedAt = DateTime.UtcNow,
                            FakeStoreId = ep.id,
                            Description = ep.description,
                            Category = ep.category,
                            Image = ep.image,
                            Price = ep.price,
                            RatingRate = ep.rating != null ? (double?)ep.rating.rate : null,
                            RatingCount = ep.rating?.count
                        };
                        _fakeStoreProductsInMemory.Add(fakeStoreProduct);
                        
                        // Fake Store ürünlerini de repository'ye ekle (eğer yoksa)
                        var existingProduct = _productRepository.GetByProductCodeAsync(fakeStoreProduct.ProductCode).Result;
                        if (existingProduct == null)
                        {
                            _productRepository.CreateAsync(fakeStoreProduct).Wait();
                        }
                    }
                    _logger.LogInformation("Initialized {Count} Fake Store products in memory and repository", _fakeStoreProductsInMemory.Count);
                }
            }
            catch (Exception ex)
            {
                lock (_lock) { _isInitialized = false; }
                _logger.LogError(ex, "Failed to initialize Fake Store products");
                throw;
            }
        }

        public async Task<List<ProductDto>> GetAllProductsAsync()
        {
            await InitializeFakeStoreProductsAsync();
            
            var localProducts = await _productRepository.GetAllAsync();
            var fakeStoreProducts = _fakeStoreProductsInMemory;

            // ProductCode'a göre grupla ve eşleştir
            var productGroups = localProducts
                .Concat(fakeStoreProducts)
                .GroupBy(p => p.ProductCode)
                .ToList();

            var allProducts = new List<ProductDto>();

            foreach (var group in productGroups)
            {
                var localProduct = group.FirstOrDefault(p => p.FakeStoreId == null);
                var fakeStoreProduct = group.FirstOrDefault(p => p.FakeStoreId != null);

                if (localProduct != null && fakeStoreProduct != null)
                {
                    // Eşleştirilmiş ürün - Fake Store'dan gelen tüm alanları, yerel ürünün stok ve threshold bilgileriyle birleştir
                    var matchedProduct = new ProductDto
                    {
                        ProductCode = fakeStoreProduct.ProductCode,
                        Name = localProduct.Name,
                        Threshold = localProduct.Threshold,
                        InitialStock = localProduct.InitialStock,
                        CurrentStock = localProduct.CurrentStock,
                        FakeStoreId = fakeStoreProduct.FakeStoreId,
                        Description = fakeStoreProduct.Description,
                        Category = fakeStoreProduct.Category,
                        Image = fakeStoreProduct.Image,
                        Price = fakeStoreProduct.Price,
                        RatingRate = fakeStoreProduct.RatingRate,
                        RatingCount = fakeStoreProduct.RatingCount,
                        IsMatched = true,
                        StockInRoman = RomanNumeralConverter.ToRoman(localProduct.CurrentStock > 0 ? localProduct.CurrentStock : 0),
                        CreatedAt = localProduct.CreatedAt,
                        UpdatedAt = localProduct.UpdatedAt
                    };
                    allProducts.Add(matchedProduct);
                }
                else if (localProduct != null)
                {
                    // Sadece yerel ürün
                    allProducts.Add(MapToDto(localProduct));
                }
                else if (fakeStoreProduct != null)
                {
                    // Sadece Fake Store ürünü
                    allProducts.Add(MapToDto(fakeStoreProduct));
                }
            }

            return allProducts.OrderBy(p => p.ProductCode).ToList();
        }

        public async Task<ProductDto?> GetProductByCodeAsync(string productCode)
        {
            await InitializeFakeStoreProductsAsync();
            
            // Önce local repository'den ara
            var localProduct = await _productRepository.GetByProductCodeAsync(productCode);
            if (localProduct != null)
            {
                return MapToDto(localProduct);
            }

            // Sonra Fake Store ürünlerinden ara
            var fakeStoreProduct = _fakeStoreProductsInMemory.FirstOrDefault(p => p.ProductCode == productCode);
            return fakeStoreProduct != null ? MapToDto(fakeStoreProduct) : null;
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductRequest request)
        {
            await InitializeFakeStoreProductsAsync();

            // Validasyon
            if (request.InitialStock < request.Threshold)
            {
                throw new ValidationException("Initial stock cannot be less than threshold");
            }

            string productCode;
            
            // Eğer Fake Store ürünü ile eşleştirme yapılacaksa
            if (request.FakeStoreProductId.HasValue)
            {
                var fakeStoreProduct = _fakeStoreProductsInMemory
                    .FirstOrDefault(p => p.FakeStoreId == request.FakeStoreProductId.Value);
                
                if (fakeStoreProduct == null)
                {
                    throw new ArgumentException($"Fake Store ürünü bulunamadı: ID {request.FakeStoreProductId.Value}");
                }
                
                // Aynı productCode'u kullan
                productCode = fakeStoreProduct.ProductCode;
                
                _logger.LogInformation("Creating product with existing Fake Store product code: {ProductCode}", productCode);
            }
            else
            {
                // Rastgele kod oluştur
                productCode = GenerateProductCode();
                _logger.LogInformation("Created new product with code: {ProductCode}", productCode);
            }

            var product = new Product
            {
                ProductCode = productCode,
                Name = request.Name,
                Threshold = request.Threshold,
                InitialStock = request.InitialStock,
                CurrentStock = request.InitialStock,
                FakeStoreId = request.FakeStoreProductId,
                CreatedAt = DateTime.UtcNow
            };

            // Eğer Fake Store ürünü ile eşleştirme yapılıyorsa, Fake Store'dan gelen alanları kopyala
            if (request.FakeStoreProductId.HasValue)
            {
                var fakeStoreProduct = _fakeStoreProductsInMemory
                    .FirstOrDefault(p => p.FakeStoreId == request.FakeStoreProductId.Value);
                
                if (fakeStoreProduct != null)
                {
                    product.Description = fakeStoreProduct.Description;
                    product.Category = fakeStoreProduct.Category;
                    product.Image = fakeStoreProduct.Image;
                    product.Price = fakeStoreProduct.Price;
                    product.RatingRate = fakeStoreProduct.RatingRate;
                    product.RatingCount = fakeStoreProduct.RatingCount;
                }
            }

            await _productRepository.CreateAsync(product);
            
            return MapToDto(product);
        }

        public async Task<List<ProductDto>> GetLowStockProductsAsync()
        {
            await InitializeFakeStoreProductsAsync();
            
            var localProducts = await _productRepository.GetAllAsync();
            var fakeStoreProducts = _fakeStoreProductsInMemory.ToList();

            // Local kritik stoklu ürünler
            var localLowStock = localProducts.Where(p => p.CurrentStock < p.Threshold).Select(MapToDto);

            // Fake Store kritik stoklu ürünler
            var fakeStoreLowStock = fakeStoreProducts
                .Where(p => p.CurrentStock < p.Threshold)
                .Select(MapToDto);

            // Local'de aynı FakeStoreId ile eklenmiş ürünleri filtrele
            var localFakeStoreIds = localProducts.Where(p => p.FakeStoreId != null).Select(p => p.FakeStoreId).ToHashSet();
            var filteredFakeStoreLowStock = fakeStoreLowStock.Where(p => !localFakeStoreIds.Contains(p.FakeStoreId)).ToList();

            var allLowStock = localLowStock.Concat(filteredFakeStoreLowStock).ToList();

            return allLowStock;
        }

        public async Task<List<FakeStoreProduct>> GetExternalProductsAsync()
        {
            try
            {
                // Doğrudan Fake Store API'den veri çek
                var externalProducts = await _fakeStoreClient.GetProductsAsync();
                return externalProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve external products from Fake Store API");
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
            await InitializeFakeStoreProductsAsync();
            
            try
            {
                // Önce in-memory'deki Fake Store ürününden al
                var fakeStoreProduct = _fakeStoreProductsInMemory.FirstOrDefault(p => p.FakeStoreId == fakeStoreId);
                if (fakeStoreProduct == null)
                {
                    // Eğer in-memory'de yoksa API'den çek
                    var externalProduct = await _fakeStoreClient.GetProductByIdAsync(fakeStoreId);
                    if (externalProduct == null)
                    {
                        throw new ValidationException($"External product with ID {fakeStoreId} not found");
                    }
                    
                    // In-memory'ye ekle
                    fakeStoreProduct = new Product
                    {
                        ProductCode = $"FAKE-{externalProduct.id}",
                        Name = externalProduct.title ?? $"Fake Product {externalProduct.id}",
                        Threshold = 10,
                        InitialStock = externalProduct.rating?.count ?? 0,
                        CurrentStock = externalProduct.rating?.count ?? 0,
                        CreatedAt = DateTime.UtcNow,
                        FakeStoreId = externalProduct.id,
                        Description = externalProduct.description,
                        Category = externalProduct.category,
                        Image = externalProduct.image,
                        Price = externalProduct.price,
                        RatingRate = externalProduct.rating != null ? (double?)externalProduct.rating.rate : null,
                        RatingCount = externalProduct.rating?.count
                    };
                    
                    lock (_lock)
                    {
                        _fakeStoreProductsInMemory.Add(fakeStoreProduct);
                    }
                }

                // ProductCode oluştur
                var productCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

                // İç sistem ürünü oluştur
                var product = new Product
                {
                    ProductCode = productCode,
                    Name = fakeStoreProduct.Name,
                    Threshold = fakeStoreProduct.Threshold,
                    InitialStock = fakeStoreProduct.InitialStock,
                    CurrentStock = fakeStoreProduct.CurrentStock,
                    FakeStoreId = fakeStoreId,
                    CreatedAt = DateTime.UtcNow,
                    Description = fakeStoreProduct.Description,
                    Category = fakeStoreProduct.Category,
                    Image = fakeStoreProduct.Image,
                    Price = fakeStoreProduct.Price,
                    RatingRate = fakeStoreProduct.RatingRate,
                    RatingCount = fakeStoreProduct.RatingCount
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
            await InitializeFakeStoreProductsAsync();
            
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
            await InitializeFakeStoreProductsAsync();
            
            var product = await _productRepository.GetByProductCodeAsync(productCode);
            if (product != null)
            {
                return product.FakeStoreId;
            }

            // Fake Store ürünlerinden de ara
            var fakeStoreProduct = _fakeStoreProductsInMemory.FirstOrDefault(p => p.ProductCode == productCode);
            return fakeStoreProduct?.FakeStoreId;
        }

        private string GenerateProductCode()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
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
                Description = product.Description,
                Category = product.Category,
                Image = product.Image,
                Price = product.Price,
                RatingRate = product.RatingRate,
                RatingCount = product.RatingCount,
                StockInRoman = RomanNumeralConverter.ToRoman(product.CurrentStock > 0 ? product.CurrentStock : 0)
            };
        }

        public async Task<ProductDto?> DecreaseStockAsync(string productCode, int amount)
        {
            var updated = await _productRepository.DecreaseStockAsync(productCode, amount);
            return updated != null ? MapToDto(updated) : null;
        }

        public async Task<ProductDto?> IncreaseStockAsync(string productCode, int amount)
        {
            var updated = await _productRepository.IncreaseStockAsync(productCode, amount);
            return updated != null ? MapToDto(updated) : null;
        }
    }

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message)
        {
        }
    }
} 