using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductCatalogApi.ExternalClients
{
    public interface IFakeStoreClient
    {
        Task<List<FakeStoreProduct>> GetProductsAsync();
        Task<FakeStoreProduct?> GetProductByIdAsync(int id);
    }

    public class FakeStoreProduct
    {
        public int id { get; set; }
        public string? title { get; set; }
        public decimal price { get; set; }
        public string? description { get; set; }
        public string? category { get; set; }
        public string? image { get; set; }
        public Rating? rating { get; set; }
    }

    public class Rating
    {
        public decimal rate { get; set; }
        public int count { get; set; }
    }
} 