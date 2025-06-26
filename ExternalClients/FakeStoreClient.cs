using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ProductCatalogApi.ExternalClients
{
    public class FakeStoreClient : IFakeStoreClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FakeStoreClient> _logger;
        private const string BaseUrl = "https://fakestoreapi.com";

        public FakeStoreClient(HttpClient httpClient, ILogger<FakeStoreClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<FakeStoreProduct>> GetProductsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/products");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var products = JsonSerializer.Deserialize<List<FakeStoreProduct>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                _logger.LogInformation("Successfully retrieved {Count} products from Fake Store API", products?.Count ?? 0);
                return products ?? new List<FakeStoreProduct>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error retrieving products from Fake Store API");
                throw new ExternalApiException("Failed to retrieve products from external API", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing products from Fake Store API");
                throw new ExternalApiException("Failed to parse products from external API", ex);
            }
        }

        public async Task<FakeStoreProduct?> GetProductByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/products/{id}");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var product = JsonSerializer.Deserialize<FakeStoreProduct>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                _logger.LogInformation("Successfully retrieved product with ID {Id} from Fake Store API", id);
                return product;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error retrieving product with ID {Id} from Fake Store API", id);
                throw new ExternalApiException($"Failed to retrieve product with ID {id} from external API", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing product with ID {Id} from Fake Store API", id);
                throw new ExternalApiException($"Failed to parse product with ID {id} from external API", ex);
            }
        }
    }

    public class ExternalApiException : Exception
    {
        public ExternalApiException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
} 