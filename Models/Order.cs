using System.ComponentModel.DataAnnotations;

namespace ProductCatalogApi.Models
{
    public class Order
    {
        public string OrderId { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Processing,
        Completed,
        Failed
    }

    public class OrderDto
    {
        public string OrderId { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class OrderResult
    {
        public bool Success { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string? SupplierName { get; set; }
    }

    public class BulkOrderResult
    {
        public List<OrderResult> Results { get; set; } = new();
        public int TotalProcessed { get; set; }
        public int SuccessfulOrders { get; set; }
        public int FailedOrders { get; set; }
    }
} 