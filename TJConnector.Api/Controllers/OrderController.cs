using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TJConnector.Postgres;
using TJConnector.Postgres.Entities;
using TJConnector.SharedLibrary.DTOs.Forms;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Model.ExternalRequests.MarkingCode;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.Controllers
{
    [ApiController]
    [Route("api/order")]
    public class OrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IExternalEmission _externalEmission;

        public OrderController(ApplicationDbContext context, IExternalEmission externalEmission)
        {
            _context = context;
            _externalEmission = externalEmission;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CodeOrder>>> GetOrders()
        {
            return await _context.CodeOrders.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CodeOrder>> GetOrderById(int id)
        {
            var order = await _context.CodeOrders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            return order;
        }

        [HttpPost]
        public async Task<ActionResult> CreateOrder(OrderCreateForm order)
        {
            CodeOrder localOrder = new CodeOrder()
            {
                Count = order.CodesCount,
                Description = order.Description,
                ProductId = order.ProductId,
                User = order.User,
                Status = 0,
                StatusHistoryJson = new StatusHistory[] { new StatusHistory { Status = 0, StatusDate = DateTimeOffset.UtcNow } } ,
                Type = order.Type
            }; 
            _context.CodeOrders.Add(localOrder);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            EmissionCreateRequest emissionCodesRequest = new EmissionCreateRequest()
            {
                codesCount = order.CodesCount,
                factoryUuid = order.FactoryUuid,
                markingLineUuid = order.MarkingLineUuid,
                productUuid = order.ProductUuid,
                Type = order.Type
            };

            var result = await _externalEmission.CreateCodeEmission(emissionCodesRequest);

            if (result == null) { 
                localOrder.Status = -1;
                localOrder.StatusMessage = "Blank result from state system";
                _context.Entry(localOrder).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return BadRequest(); 
            }
            if (result.Content == null) {
                localOrder.Status = -1;
                localOrder.StatusMessage = $"Blank result from state system. {result.Message}";
                _context.Entry(localOrder).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return BadRequest(result.Message); 
            }

            localOrder.Status = 1;
            localOrder.ExternalGuid = result.Content.uuid;
            _context.Entry(localOrder).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("external/{id}")]
        public async Task<ActionResult<CodeOrder>> GetExternalOrderById(string id)
        {
            var localOrder = await _context.CodeOrders.FindAsync(id);

            if (localOrder == null)
            {
                return NotFound();
            }
            if (localOrder.ExternalGuid == null)
            {
                return BadRequest("Order not sent to external system");
            }

            var externalOrder = await _externalEmission.GetEmissionInfo(localOrder.ExternalGuid.Value);

            if (externalOrder == null)
            {
                return BadRequest("Something went wrong");
            }
            if (externalOrder.Content == null)
            {
                return NotFound("No content");
            }

            localOrder.Status = externalOrder.Content.status switch
            {
                0 => -2, //saved_error
                1 => 2, //saved
                3 => 3, //executing
                4 => 4, //available
                5 => -3, //failed
                6 => 5, //done
                _ => -4 //unknown
            };
            localOrder.StatusHistoryJson.Append( 
                new StatusHistory { Status = 0, StatusDate = DateTimeOffset.UtcNow }
                );
            _context.Entry(localOrder).State = EntityState.Modified;
            _context.SaveChanges();

            return Ok(localOrder);
        }

        [HttpPost("external/{id}/process")]
        public async Task<ActionResult> ProcessCodeEmission(string id)
        {
            var localOrder = await _context.CodeOrders.FindAsync(id);
            if (localOrder == null)
                return BadRequest("Order not found");
            if (localOrder.Status != 2)
                return BadRequest("Incorrect order status");
            if (localOrder.ExternalGuid == null)
                return BadRequest("Incorrect order status");
            var response = await _externalEmission.ProcessCodeEmission(new ProcessDocument { uuids = [localOrder.ExternalGuid.Value] });
            return Ok();
        }

        [HttpPost("external/{id}/download")]
        public async Task<ActionResult> DownloadCodesFromOrder(string id)
        {
            var localOrder = await _context.CodeOrders.FindAsync(id);
            if (localOrder == null)
                return BadRequest("Order not found");
            if (localOrder.Status != 4)
                return BadRequest("Incorrect order status");
            if (localOrder.ExternalGuid == null)
                return BadRequest("Incorrect order status");
            var response = await _externalEmission.GetCodesFromEmission(new ProcessDocument { uuids = [localOrder.ExternalGuid.Value] });
            if(!response.Success)
                return BadRequest(response.Message);
            if (response.Content == null)
                return BadRequest("Lack of content");
            if (response.Content.codes == null)
                return BadRequest("Lack of content");
            _context.CodeOrdersContents.Add(new CodeOrderContent
            {
                CodeOrderId = localOrder.Id,
                OrderContent = response.Content.codes
            });

            return Ok();
        }
    }
}
