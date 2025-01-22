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

        public ProductController(ApplicationDbContext context, IExternalProduct externalProduct)
        {
            _context = context;
            _externalProduct = externalProduct;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProductById(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        [HttpGet("external/{id}")]
        public async Task<ActionResult<CustomResult<ProductInfoResponse>>> GetExternalProductById(string id)
        {
            var externalProduct = await _externalProduct.GetProductByUUID(id);

            if(!externalProduct.Success)
                return NotFound(externalProduct.Message);

            return externalProduct;
        }

        [HttpGet("external")]
        public async Task<IActionResult> GetExternalProducts()
        {
            var externalProducts = await _externalProduct.GetProductPersonalList(new ListRequestRequest { limit = 100, offset = 0 });
            
            if(!externalProducts.Success)
                return BadRequest(externalProducts.Message);

            if (externalProducts.Content is null)
                return BadRequest("External request doesn't provide any results");
            
            if (externalProducts.Content.items is null)
                return BadRequest("External request doesn't provide any results");

            var missingProducts = externalProducts.Content.items.Where(x=> !_context.Products.Any(z => z.ExternalUid == x.uuid)).ToList();

            List<Product> newProducts = new List<Product>();

            foreach (var missingProduct in missingProducts)
                newProducts.Add(new Product
                {
                    ExternalUid = missingProduct.uuid,
                    Gtin = missingProduct.gtin,
                    Type = missingProduct.type,
                    Name = missingProduct.name,
                });

            if(newProducts.Count > 0)
                await _context.Products.AddRangeAsync(newProducts);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(missingProducts.Count > 0 ? $"{missingProducts.Count} products added" : "No new products");
        }

    }
}
