using System.ComponentModel.DataAnnotations;

namespace ProductCatalogApi.Models
{
    public class Product
    {
        public string ProductCode { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [Range(0, int.MaxValue)]
        public int Threshold { get; set; }
        
        [Required]
        [Range(0, int.MaxValue)]
        public int InitialStock { get; set; }
        
        public int CurrentStock { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Fake Store API ile eşleştirme için
        public int? FakeStoreId { get; set; }
    }

    public class ProductDto
    {
        public string ProductCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Threshold { get; set; }
        public int InitialStock { get; set; }
        public int CurrentStock { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? FakeStoreId { get; set; }
        public string StockInRoman { get; set; } = string.Empty;
    }

    public class CreateProductRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [Range(0, int.MaxValue)]
        public int Threshold { get; set; }
        
        [Required]
        [Range(0, int.MaxValue)]
        public int InitialStock { get; set; }
    }
} 