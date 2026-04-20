using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TJConnector.Postgres;
using TJConnector.Postgres.Entities;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Product;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.Controllers
{
    [ApiController]
    [Route("api/product")]
    public class ProductController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IExternalProduct _externalProduct;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            ApplicationDbContext context,
            IExternalProduct externalProduct,
            ILogger<ProductController> logger)
        {
            _context = context;
            _externalProduct = externalProduct;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            try
            {
                var products = await _context.Products.ToListAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch products from local database.");
                return StatusCode(500, "An error occurred while fetching products.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProductById(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid product ID provided.");
                return BadRequest("Product ID must be greater than 0.");
            }

            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    _logger.LogWarning($"Product with ID {id} not found.");
                    return NotFound();
                }
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to fetch product with ID {id} from local database.");
                return StatusCode(500, "An error occurred while fetching the product.");
            }
        }

        [HttpGet("external/{id}")]
        public async Task<ActionResult<CustomResult<ProductInfoResponse>>> GetExternalProductById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Invalid UUID provided.");
                return BadRequest("UUID is required.");
            }

            try
            {
                var result = await _externalProduct.GetProductByUUID(id);

                if (!result.Success)
                {
                    _logger.LogWarning($"Failed to fetch external product with UUID {id}: {result.Message}");
                    return NotFound(result.Message);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to fetch external product with UUID {id}.");
                return StatusCode(500, "An error occurred while fetching the external product.");
            }
        }

        [HttpGet("external")]
        public async Task<IActionResult> GetExternalProducts()
        {
            try
            {
                var result = await _externalProduct.GetProductPersonalList(new ListRequestRequest { limit = 100, offset = 0 });

                if (!result.Success)
                {
                    _logger.LogWarning($"Failed to fetch external products: {result.Message}");
                    return BadRequest(result.Message);
                }

                if (result.Content?.items == null)
                {
                    _logger.LogWarning("No items returned from external API.");
                    return BadRequest("No items returned from external API.");
                }

                var missingProducts = result.Content.items
                    .Where(x => !_context.Products.Any(z => z.ExternalUid == x.uuid))
                    .ToList();

                var newProducts = missingProducts.Select(missingProduct => new Product
                {
                    ExternalUid = missingProduct.uuid.Value,
                    Gtin = missingProduct.gtin,
                    Type = missingProduct.type ?? 0,
                    Name = missingProduct.name,
                }).ToList();

                if (newProducts.Count > 0)
                {
                    await _context.Products.AddRangeAsync(newProducts);
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        _logger.LogError(ex, "Failed to save new products to local database.");
                        return StatusCode(500, "An error occurred while saving new products.");
                    }
                }

                return Ok(missingProducts.Count > 0 ? $"{missingProducts.Count} products added" : "No new products");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch external products.");
                return StatusCode(500, "An error occurred while fetching external products.");
            }
        }
    }
}
