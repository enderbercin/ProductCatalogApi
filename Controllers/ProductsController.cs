using Microsoft.AspNetCore.Mvc;
using ProductCatalogApi.Services;
using ProductCatalogApi.Models;
using ProductCatalogApi.ExternalClients;
using ProductCatalogApi.Utils;

namespace ProductCatalogApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // GET api/products
        [HttpGet]
        public async Task<IActionResult> GetCatalog()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/products/external
        [HttpGet("external")]
        public async Task<IActionResult> GetExternalProducts()
        {
            try
            {
                var products = await _productService.GetExternalProductsAsync();
                return Ok(products);
            }
            catch (ExternalApiException ex)
            {
                return StatusCode(503, $"External API error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/products/low-stock
        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStockProducts()
        {
            try
            {
                var lowStockProducts = await _productService.GetLowStockProductsAsync();
                return Ok(lowStockProducts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST api/products
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdProduct = await _productService.CreateProductAsync(request);
                return CreatedAtAction(nameof(GetProductByCode), new { productCode = createdProduct.ProductCode }, createdProduct);
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/products/{productCode}
        [HttpGet("{productCode}")]
        public async Task<IActionResult> GetProductByCode(string productCode)
        {
            try
            {
                var product = await _productService.GetProductByCodeAsync(productCode);
                if (product == null)
                {
                    return NotFound($"Product with code {productCode} not found");
                }
                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/products/{productCode}/stock-roman
        [HttpGet("{productCode}/stock-roman")]
        public async Task<IActionResult> GetProductStockInRoman(string productCode)
        {
            try
            {
                var product = await _productService.GetProductByCodeAsync(productCode);
                if (product == null)
                {
                    return NotFound($"Product with code {productCode} not found");
                }

                var romanStock = RomanNumeralConverter.ToRoman(product.CurrentStock);
                var result = new
                {
                    ProductCode = product.ProductCode,
                    ProductName = product.Name,
                    StockInNumbers = product.CurrentStock,
                    StockInRoman = romanStock
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/utils/roman/{number}
        [HttpGet("utils/roman/{number}")]
        public IActionResult ConvertToRoman(int number)
        {
            try
            {
                var roman = RomanNumeralConverter.ToRoman(number);
                var result = new
                {
                    Number = number,
                    Roman = roman
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST api/products/from-external/{fakeStoreId}
        [HttpPost("from-external/{fakeStoreId}")]
        public async Task<IActionResult> CreateProductFromExternal(int fakeStoreId)
        {
            try
            {
                var product = await _productService.CreateProductFromExternalAsync(fakeStoreId);
                return CreatedAtAction(nameof(GetProductByCode), new { productCode = product.ProductCode }, product);
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/products/mapping/fake-store/{fakeStoreId}
        [HttpGet("mapping/fake-store/{fakeStoreId}")]
        public async Task<IActionResult> GetProductCodeByFakeStoreId(int fakeStoreId)
        {
            try
            {
                var productCode = await _productService.GetProductCodeByFakeStoreIdAsync(fakeStoreId);
                if (productCode == null)
                {
                    return NotFound($"No product found for Fake Store ID {fakeStoreId}");
                }

                var result = new
                {
                    FakeStoreId = fakeStoreId,
                    ProductCode = productCode
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/products/mapping/product-code/{productCode}
        [HttpGet("mapping/product-code/{productCode}")]
        public async Task<IActionResult> GetFakeStoreIdByProductCode(string productCode)
        {
            try
            {
                var fakeStoreId = await _productService.GetFakeStoreIdByProductCodeAsync(productCode);
                if (fakeStoreId == null)
                {
                    return NotFound($"No Fake Store ID found for product code {productCode}");
                }

                var result = new
                {
                    ProductCode = productCode,
                    FakeStoreId = fakeStoreId
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
} 