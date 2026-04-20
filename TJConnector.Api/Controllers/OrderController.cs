using Microsoft.AspNetCore.Mvc;
using System.Text;
using TJConnector.Api.Services;
using TJConnector.Postgres;
using TJConnector.Postgres.Entities;
using TJConnector.SharedLibrary.DTOs.Forms;
using TJConnector.StateSystem.Helpers;

namespace TJConnector.Api.Controllers;

[ApiController]
[Route("api/order")]
public class OrderController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderController> _logger;
    private readonly IOrderService _orderService;

    public OrderController(
        ApplicationDbContext context,
        ILogger<OrderController> logger,
        IOrderService orderService)
    {
        _context = context;
        _logger = logger;
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CodeOrder>>> GetOrders()
    {
        var result = await _orderService.GetOrdersAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]                                                                                                                                   
    public async Task<ActionResult<CodeOrder>> GetOrderById(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);

        if (order == null)
        {
            _logger.LogWarning($"Order with ID {id} not found.");
            return NotFound();
        }

        return order;
    }

    [HttpGet("external/{id}")] 
    public async Task<ActionResult<CodeOrder>> GetExternalOrderById(int id)
    {
        var order = await _orderService.GetExternalOrderByIdAsync(id);
        if (order == null)
        {
            _logger.LogWarning($"Order with ID {id} not found or failed to fetch external info.");
            return NotFound();
        }
        return Ok(order);
    }

    [HttpPost("external/{id}/process")]
    public async Task<ActionResult<CodeOrder>> ProcessCodeEmission(int id)
    {
        var order = await _orderService.ProcessCodeEmissionAsync(id);
        if (order == null)
        {
            _logger.LogWarning($"Failed to process emission for order {id}.");
            return BadRequest("Failed to process emission for order.");
        }
        return Ok(order);
    }

    [HttpPost("external/{id}/download")]
    public async Task<ActionResult<CodeOrder>> GetCodesFromOrder(int id)
    {
        var order = await _orderService.GetCodesFromOrderAsync(id);
        if (order == null)
        {
            _logger.LogWarning($"Failed to download codes for order {id}.");
            return BadRequest("Failed to download codes for order.");
        }
        return Ok(order);
    }

    [HttpPost("{id}/download")]
    public async Task<IActionResult> DownloadOrderContent(int id, [FromQuery] string user)
    {
        var content = await _orderService.DownloadOrderContentAsync(id, user);
        if (content == null || content.OrderContent == null)
        {
            _logger.LogWarning($"Order content for ID {id} not found.");
            return NotFound();
        }

        var fileContent = string.Join(Environment.NewLine, content.OrderContent.Select(GS1CodeHelper.StripGroupSeparators));
        return File(Encoding.UTF8.GetBytes(fileContent), "text/plain", $"codes_{id}.txt");
    }

    [HttpPost]
    public async Task<ActionResult<CodeOrder>> CreateOrder([FromBody] OrderCreateForm order)
    {
        if (order == null)
        {
            _logger.LogError("Order create request body cannot be null.");
            return BadRequest("Request body is required.");
        }

        var result = await _orderService.CreateOrderAsync(order);
        if (result == null)
        {
            _logger.LogError("Failed to create order.");
            return BadRequest("Failed to create order.");
        }
        return Ok(result);
    }
}