using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AI_Order.Shared.Models;

namespace AI_Order.Client.Services;

public interface IOrderingService
{
    Task<List<MenuItemDto>> GetMenuAsync(string? userId = null);
    Task<ChatResponseDto> ChatAsync(ChatRequestDto request);
    IAsyncEnumerable<string> ChatStreamAsync(ChatRequestDto request, CancellationToken ct = default);
    Task<SubmitOrderResponseDto> SubmitOrderAsync(SubmitOrderRequestDto request);
    Task<List<KitchenOrderDto>> GetKitchenOrdersAsync();
    Task<bool> CompleteOrderAsync(string orderId, int version);
}

public class OrderingService : IOrderingService
{
    private readonly HttpClient _http;

    public OrderingService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<MenuItemDto>> GetMenuAsync(string? userId = null)
    {
        var url = string.IsNullOrEmpty(userId) ? "api/menu" : $"api/menu?userId={Uri.EscapeDataString(userId)}";
        return await _http.GetFromJsonAsync<List<MenuItemDto>>(url) ?? [];
    }

    public async Task<ChatResponseDto> ChatAsync(ChatRequestDto request)
    {
        var response = await _http.PostAsJsonAsync("api/chat", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ChatResponseDto>()
               ?? new ChatResponseDto { Message = "No response" };
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(
        ChatRequestDto request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/chat/stream")
        {
            Content = content
        };

        var response = await _http.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
                continue;

            var data = line["data:".Length..].Trim();
            if (data == "[DONE]") break;

            string? text = null;
            try
            {
                var doc = JsonDocument.Parse(data);
                if (doc.RootElement.TryGetProperty("error", out var errProp))
                    throw new Exception(errProp.GetString() ?? "Unknown API error");
                text = doc.RootElement.GetProperty("text").GetString();
            }
            catch (Exception ex) when (ex is not JsonException)
            {
                throw;
            }
            catch { /* skip malformed chunks */ }

            if (!string.IsNullOrEmpty(text))
                yield return text;
        }
    }

    public async Task<SubmitOrderResponseDto> SubmitOrderAsync(SubmitOrderRequestDto request)
    {
        var response = await _http.PostAsJsonAsync("api/orders/submit", request);
        if (!response.IsSuccessStatusCode)
            return new SubmitOrderResponseDto { Success = false, Message = $"Server error: {(int)response.StatusCode}" };
        return await response.Content.ReadFromJsonAsync<SubmitOrderResponseDto>()
               ?? new SubmitOrderResponseDto { Success = false, Message = "Unknown error" };
    }

    public async Task<List<KitchenOrderDto>> GetKitchenOrdersAsync()
    {
        return await _http.GetFromJsonAsync<List<KitchenOrderDto>>("api/orders/kitchen") ?? [];
    }

    public async Task<bool> CompleteOrderAsync(string orderId, int version)
    {
        var response = await _http.PostAsync($"api/orders/{orderId}/complete?version={version}", null);
        return response.IsSuccessStatusCode;
    }
}
