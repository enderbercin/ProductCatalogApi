using ProductCatalogApi.Models;
using ProductCatalogApi.Repositories;
using ProductCatalogApi.ExternalClients;
using Microsoft.Extensions.Logging;

namespace ProductCatalogApi.Services
{
    public class OrderService : IOrderService
    {
        private readonly IProductService _productService;
        private readonly IOrderRepository _orderRepository;
        private readonly IFakeStoreClient _fakeStoreClient;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IProductService productService,
            IOrderRepository orderRepository,
            IFakeStoreClient fakeStoreClient,
            ILogger<OrderService> logger)
        {
            _productService = productService;
            _orderRepository = orderRepository;
            _fakeStoreClient = fakeStoreClient;
            _logger = logger;
        }

        public async Task<BulkOrderResult> CheckAndPlaceOrdersAsync()
        {
            var result = new BulkOrderResult();
            
            try
            {
                // Kritik stoklu ürünleri al
                var lowStockProducts = await _productService.GetLowStockProductsAsync();
                _logger.LogInformation("Found {Count} products with low stock", lowStockProducts.Count);

                foreach (var product in lowStockProducts)
                {
                    try
                    {
                        var orderResult = await ProcessLowStockProductAsync(product);
                        result.Results.Add(orderResult);
                        result.TotalProcessed++;

                        if (orderResult.Success)
                        {
                            result.SuccessfulOrders++;
                        }
                        else
                        {
                            result.FailedOrders++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing low stock product: {ProductCode}", product.ProductCode);
                        result.Results.Add(new OrderResult
                        {
                            Success = false,
                            ProductCode = product.ProductCode,
                            Message = $"Error processing product: {ex.Message}"
                        });
                        result.TotalProcessed++;
                        result.FailedOrders++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk order processing");
                throw;
            }

            return result;
        }

        public async Task<List<OrderDto>> GetAllOrdersAsync()
        {
            var orders = await _orderRepository.GetAllAsync();
            return orders.Select(MapToDto).ToList();
        }

        public async Task<OrderDto?> GetOrderByIdAsync(string orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            return order != null ? MapToDto(order) : null;
        }

        private async Task<OrderResult> ProcessLowStockProductAsync(ProductDto product)
        {
            try
            {
                // Fake Store API'den ürün bilgilerini al
                var externalProduct = await _fakeStoreClient.GetProductByIdAsync(product.FakeStoreId ?? 1);
                
                if (externalProduct == null)
                {
                    return new OrderResult
                    {
                        Success = false,
                        ProductCode = product.ProductCode,
                        Message = "External product not found"
                    };
                }

                // En uygun fiyatlı satıcı seçimi (şimdilik sadece Fake Store)
                var bestPrice = externalProduct.price;
                var supplierName = "Fake Store API";

                // Sipariş oluştur
                var orderId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                var quantity = product.Threshold - product.CurrentStock + 10; // Threshold'u aşacak şekilde sipariş

                var order = new Order
                {
                    OrderId = orderId,
                    ProductCode = product.ProductCode,
                    SupplierName = supplierName,
                    Price = bestPrice,
                    Quantity = quantity,
                    Status = OrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                await _orderRepository.CreateAsync(order);

                _logger.LogInformation("Created order {OrderId} for product {ProductCode} with quantity {Quantity} at price {Price}",
                    orderId, product.ProductCode, quantity, bestPrice);

                return new OrderResult
                {
                    Success = true,
                    OrderId = orderId,
                    ProductCode = product.ProductCode,
                    Message = "Order created successfully",
                    Price = bestPrice,
                    SupplierName = supplierName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing low stock product: {ProductCode}", product.ProductCode);
                return new OrderResult
                {
                    Success = false,
                    ProductCode = product.ProductCode,
                    Message = $"Error creating order: {ex.Message}"
                };
            }
        }

        private static OrderDto MapToDto(Order order)
        {
            return new OrderDto
            {
                OrderId = order.OrderId,
                ProductCode = order.ProductCode,
                SupplierName = order.SupplierName,
                Price = order.Price,
                Quantity = order.Quantity,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                CompletedAt = order.CompletedAt
            };
        }
    }
} 