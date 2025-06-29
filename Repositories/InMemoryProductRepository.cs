using ProductCatalogApi.Models;

namespace ProductCatalogApi.Repositories
{
    public class InMemoryProductRepository : IProductRepository
    {
        private readonly List<Product> _products = new();
        private readonly object _lock = new();

        public Task<List<Product>> GetAllAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(_products.ToList());
            }
        }

        public Task<Product?> GetByProductCodeAsync(string productCode)
        {
            lock (_lock)
            {
                var product = _products.FirstOrDefault(p => p.ProductCode == productCode);
                return Task.FromResult(product);
            }
        }

        public Task<Product> CreateAsync(Product product)
        {
            lock (_lock)
            {
                _products.Add(product);
                return Task.FromResult(product);
            }
        }

        public Task<Product> UpdateAsync(Product product)
        {
            lock (_lock)
            {
                var existingProduct = _products.FirstOrDefault(p => p.ProductCode == product.ProductCode);
                if (existingProduct == null)
                {
                    throw new InvalidOperationException($"Product with code {product.ProductCode} not found");
                }

                var index = _products.IndexOf(existingProduct);
                product.UpdatedAt = DateTime.UtcNow;
                _products[index] = product;

                return Task.FromResult(product);
            }
        }

        public Task<List<Product>> GetLowStockProductsAsync()
        {
            lock (_lock)
            {
                var lowStockProducts = _products.Where(p => p.CurrentStock < p.Threshold).ToList();
                return Task.FromResult(lowStockProducts);
            }
        }

        public Task<bool> ExistsAsync(string productCode)
        {
            lock (_lock)
            {
                return Task.FromResult(_products.Any(p => p.ProductCode == productCode));
            }
        }

        public Task<Product?> DecreaseStockAsync(string productCode, int amount)
        {
            lock (_lock)
            {
                var product = _products.FirstOrDefault(p => p.ProductCode == productCode);
                if (product == null) return Task.FromResult<Product?>(null);
                product.CurrentStock = Math.Max(0, product.CurrentStock - amount);
                product.UpdatedAt = DateTime.UtcNow;
                return Task.FromResult<Product?>(product);
            }
        }

        public Task<Product?> IncreaseStockAsync(string productCode, int amount)
        {
            lock (_lock)
            {
                var product = _products.FirstOrDefault(p => p.ProductCode == productCode);
                if (product == null) return Task.FromResult<Product?>(null);
                product.CurrentStock += amount;
                product.UpdatedAt = DateTime.UtcNow;
                return Task.FromResult<Product?>(product);
            }
        }
    }
} 