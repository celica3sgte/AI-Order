using AI_Order.Api.Filters;
using AI_Order.Api.Hubs;
using AI_Order.Api.Services;
using AI_Order.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace AI_Order.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ISquareService _squareService;
    private readonly CosmosOrderService _cosmos;
    private readonly IHubContext<OrderHub> _hub;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        ISquareService squareService,
        CosmosOrderService cosmos,
        IHubContext<OrderHub> hub,
        ILogger<OrdersController> logger)
    {
        _squareService = squareService;
        _cosmos = cosmos;
        _hub = hub;
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

            if (!string.IsNullOrEmpty(request.AspNetUserId))
            {
                var tableOrder = new TableOrder
                {
                    AspNetUserId = request.AspNetUserId,
                    TableId = request.TableId ?? "unknown",
                    CustomerName = request.CustomerName,
                    SpecialInstructions = request.SpecialInstructions,
                    LineItems = request.Order.LineItems,
                    Subtotal = request.Order.Subtotal,
                    Tax = request.Order.Tax,
                    Total = request.Order.Total,
                    Status = OrderStatus.Submitted,
                    SquareOrderId = result.SquareOrderId
                };

                await _cosmos.SaveOrderAsync(tableOrder);
                await _hub.Clients.Group(request.AspNetUserId)
                    .SendAsync("ReceiveOrderUpdate", request.AspNetUserId);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit order");
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

    // Management endpoints — server-to-server, protected by X-Management-Key header
    [HttpGet("byuser/{aspNetUserId}")]
    [RequireManagementKey]
    public async Task<IActionResult> GetOrdersByUser(string aspNetUserId)
    {
        var orders = await _cosmos.GetOrdersAsync(aspNetUserId);
        return Ok(orders);
    }

    [HttpPatch("{id}/status")]
    [RequireManagementKey]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateOrderStatusRequest dto)
    {
        var success = await _cosmos.UpdateOrderStatusAsync(id, dto.AspNetUserId, dto.Status);
        if (!success) return NotFound();
        await _hub.Clients.Group(dto.AspNetUserId).SendAsync("ReceiveOrderUpdate", dto.AspNetUserId);
        return Ok();
    }
}
