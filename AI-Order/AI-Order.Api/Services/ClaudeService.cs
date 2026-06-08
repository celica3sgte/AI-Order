using System.Text;
using System.Text.Json;
using AI_Order.Shared.Models;

namespace AI_Order.Api.Services;

public interface IClaudeService
{
    Task<ChatResponseDto> ChatAsync(ChatRequestDto request);
    IAsyncEnumerable<string> ChatStreamAsync(ChatRequestDto request, CancellationToken ct = default);
}

public class ClaudeService : IClaudeService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMenuService _menuService;
    private readonly IConfiguration _config;
    private readonly ILogger<ClaudeService> _logger;

    public ClaudeService(
        IHttpClientFactory httpClientFactory,
        IMenuService menuService,
        IConfiguration config,
        ILogger<ClaudeService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _menuService = menuService;
        _config = config;
        _logger = logger;
    }

    public async Task<ChatResponseDto> ChatAsync(ChatRequestDto request)
    {
        var client = _httpClientFactory.CreateClient("Anthropic");
        var model = _config["Anthropic:Model"] ?? "claude-sonnet-4-20250514";
        var restaurantName = _config["Restaurant:Name"] ?? "Our Restaurant";
        var taxRate = _config.GetValue<decimal>("Restaurant:TaxRate", 0.0825m);

        var systemPrompt = BuildSystemPrompt(restaurantName, taxRate, request.CurrentOrder);

        var anthropicMessages = request.Messages.Select(m => new
        {
            role = m.Role,
            content = m.Content
        }).ToList();

        var payload = new
        {
            model,
            max_tokens = 1024,
            system = systemPrompt,
            messages = anthropicMessages
        };

        var json = JsonSerializer.Serialize(payload);
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var content = new ByteArrayContent(jsonBytes);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

        var response = await client.PostAsync("/v1/messages", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(responseBody);

        var messageText = parsed.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        // Try to extract order JSON if Claude embedded one
        var updatedOrder = TryExtractOrder(messageText, request.CurrentOrder);
        var cleanMessage = StripOrderJson(messageText);

        return new ChatResponseDto
        {
            Message = cleanMessage,
            UpdatedOrder = updatedOrder,
            OrderReady = messageText.Contains("[ORDER_READY]")
        };
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(
        ChatRequestDto request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("Anthropic");
        var model = _config["Anthropic:Model"] ?? "claude-sonnet-4-20250514";
        var restaurantName = _config["Restaurant:Name"] ?? "Our Restaurant";
        var taxRate = _config.GetValue<decimal>("Restaurant:TaxRate", 0.0825m);

        var systemPrompt = BuildSystemPrompt(restaurantName, taxRate, request.CurrentOrder);

        var anthropicMessages = request.Messages.Select(m => new
        {
            role = m.Role,
            content = m.Content
        }).ToList();

        var payload = new
        {
            model,
            max_tokens = 1024,
            stream = true,
            system = systemPrompt,
            messages = anthropicMessages
        };

        var json = JsonSerializer.Serialize(payload);
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var httpContent = new ByteArrayContent(jsonBytes);
        httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/messages")
        {
            Content = httpContent
        };

        var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
                continue;

            var data = line["data:".Length..].Trim();
            if (data == "[DONE]") break;

            string? delta = null;
            try
            {
                var doc = JsonDocument.Parse(data);
                var type = doc.RootElement.GetProperty("type").GetString();
                if (type == "content_block_delta")
                {
                    delta = doc.RootElement
                        .GetProperty("delta")
                        .GetProperty("text")
                        .GetString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "SSE parse skip");
            }

            if (!string.IsNullOrEmpty(delta))
                yield return delta;
        }
    }

    private string BuildSystemPrompt(string restaurantName, decimal taxRate, OrderDto? currentOrder)
    {
        var menuContext = _menuService.BuildMenuContextForClaude();
        var orderContext = BuildOrderContext(currentOrder);

        return $$"""
            You are an ordering assistant for {{restaurantName}}.
            Take orders quickly and accurately. Be brief — 1 to 2 short sentences max per reply.

            RULES:
            - No small talk. No filler phrases like "Great choice!" or "Of course!".
            - Confirm items added in one short line (e.g. "Added: Classic Burger x1.").
            - Only ask for missing info if truly required (e.g. required modifier not chosen).
            - If asked about ingredients or allergens, answer in one sentence.
            - When the order is finalized and the customer confirms, include [ORDER_READY] in your response.
            - When items are added/modified, embed the COMPLETE updated order state as JSON in your response
              wrapped in <order>...</order> tags so the UI can update in real time.
            - Always include ALL items (existing + new) in the <order> tag — never drop items already in the order.
            - Tax rate is {{taxRate:P2}}.
            - Only take orders for items that exist on the menu below.

            ORDER JSON FORMAT (include whenever order changes):
            <order>
            {
              "lineItems": [
                {
                  "menuItemId": "item-id",
                  "name": "Item Name",
                  "quantity": 1,
                  "unitPrice": 14.99,
                  "selectedModifiers": ["modifier name"],
                  "specialInstructions": null
                }
              ]
            }
            </order>

            {{orderContext}}
            {{menuContext}}
            """;
    }

    private static string BuildOrderContext(OrderDto? order)
    {
        if (order is null || order.LineItems.Count == 0)
            return string.Empty;

        var lines = order.LineItems.Select(li =>
        {
            var mods = li.SelectedModifiers.Count > 0 ? $" ({string.Join(", ", li.SelectedModifiers)})" : "";
            return $"  - {li.Name}{mods} x{li.Quantity} @ ${li.UnitPrice:F2} = ${li.LineTotal:F2}";
        });

        return $"""
            CURRENT ORDER (already added by the customer — preserve these unless the customer removes them):
            {string.Join("\n", lines)}
            Subtotal: ${order.Subtotal:F2} | Tax: ${order.Tax:F2} | Total: ${order.Total:F2}

            """;
    }

    private static OrderDto? TryExtractOrder(string message, OrderDto? currentOrder)
    {
        var start = message.IndexOf("<order>", StringComparison.OrdinalIgnoreCase);
        var end = message.IndexOf("</order>", StringComparison.OrdinalIgnoreCase);

        if (start == -1 || end == -1) return currentOrder;

        var json = message[(start + 7)..end].Trim();
        try
        {
            var parsed = JsonSerializer.Deserialize<OrderUpdatePayload>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed?.LineItems == null) return currentOrder;

            decimal subtotal = parsed.LineItems.Sum(li => li.UnitPrice * li.Quantity);
            decimal tax = subtotal * 0.0825m;

            return new OrderDto
            {
                Id = currentOrder?.Id ?? Guid.NewGuid().ToString(),
                LineItems = parsed.LineItems.Select(li => new OrderLineItemDto
                {
                    MenuItemId = li.MenuItemId,
                    Name = li.Name,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    LineTotal = li.UnitPrice * li.Quantity,
                    SelectedModifiers = li.SelectedModifiers ?? [],
                    SpecialInstructions = li.SpecialInstructions
                }).ToList(),
                Subtotal = subtotal,
                Tax = tax,
                Total = subtotal + tax,
                Status = OrderStatus.Draft
            };
        }
        catch
        {
            return currentOrder;
        }
    }

    private static string StripOrderJson(string message)
    {
        var start = message.IndexOf("<order>", StringComparison.OrdinalIgnoreCase);
        var end = message.IndexOf("</order>", StringComparison.OrdinalIgnoreCase);

        if (start == -1 || end == -1) return message;

        return (message[..start] + message[(end + 8)..]).Trim();
    }

    private class OrderUpdatePayload
    {
        public List<OrderLineItemPayload>? LineItems { get; set; }
    }

    private class OrderLineItemPayload
    {
        public string MenuItemId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public List<string>? SelectedModifiers { get; set; }
        public string? SpecialInstructions { get; set; }
    }
}
