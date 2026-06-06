using AI_Order.Api.Services;
using AI_Order.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace AI_Order.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ISquareService _squareService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(ISquareService squareService, ILogger<OrdersController> logger)
    {
        _squareService = squareService;
        _logger = logger;
    }

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitOrder([FromBody] SubmitOrderRequestDto request)
    {
        if (request.Order.LineItems.Count == 0)
            return BadRequest(new { error = "Order has no items." });

        try
        {
            var result = await _squareService.CreateOrderAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit order to Square");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("kitchen")]
    public async Task<IActionResult> GetKitchenOrders()
    {
        var orders = await _squareService.GetKitchenOrdersAsync();
        return Ok(orders);
    }

    [HttpPost("{orderId}/complete")]
    public async Task<IActionResult> CompleteOrder(string orderId, [FromQuery] int version = 1)
    {
        var success = await _squareService.CompleteOrderAsync(orderId, version);
        return Ok(new { success });
    }
}
