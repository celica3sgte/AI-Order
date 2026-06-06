using AI_Order.Api.Services;
using AI_Order.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace AI_Order.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IClaudeService _claudeService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IClaudeService claudeService, ILogger<ChatController> logger)
    {
        _claudeService = claudeService;
        _logger = logger;
    }

    /// <summary>
    /// Non-streaming chat endpoint. Returns full response at once.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequestDto request)
    {
        try
        {
            var response = await _claudeService.ChatAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat error");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Streaming chat via Server-Sent Events. Client receives tokens as they're generated.
    /// </summary>
    [HttpPost("stream")]
    public async Task ChatStream([FromBody] ChatRequestDto request)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            await foreach (var chunk in _claudeService.ChatStreamAsync(request, HttpContext.RequestAborted))
            {
                var data = $"data: {JsonSerializer.Serialize(new { text = chunk })}\n\n";
                var bytes = Encoding.UTF8.GetBytes(data);
                await Response.Body.WriteAsync(bytes);
                await Response.Body.FlushAsync();

                if (HttpContext.RequestAborted.IsCancellationRequested)
                    break;
            }

            // Signal stream end
            var done = "data: [DONE]\n\n";
            await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(done));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat stream error");
            var error = $"data: {JsonSerializer.Serialize(new { error = ex.Message })}\n\n";
            await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(error));
        }
    }
}
