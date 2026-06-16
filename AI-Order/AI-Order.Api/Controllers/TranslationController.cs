using System.Text;
using System.Text.Json;
using AI_Order.Api.Filters;
using AI_Order.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace AI_Order.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranslationController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<TranslationController> _logger;

    public TranslationController(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<TranslationController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    [HttpPost("menu-item")]
    [RequireManagementKey]
    public async Task<IActionResult> TranslateMenuItem([FromBody] MenuItemTranslationRequestDto request)
    {
        var inputJson = JsonSerializer.Serialize(new
        {
            name = request.Name,
            description = request.Description,
            ingredients = request.Ingredients
        });

        var targetLanguage = !string.IsNullOrWhiteSpace(request.TargetLanguageName)
            ? request.TargetLanguageName
            : _config["Restaurant:SecondaryLanguage:Name"] ?? "Vietnamese";

        var prompt = $$"""
            Translate this restaurant menu item to {{targetLanguage}}.
            Return ONLY a JSON object wrapped in <translation>...</translation> tags.
            No explanation, no markdown, no extra text outside the tags.

            Input (JSON):
            {{inputJson}}

            Return this exact structure inside <translation> tags:
            <translation>
            {
              "nameAlt": "{{targetLanguage}} name",
              "descriptionAlt": "{{targetLanguage}} description or null if no description",
              "ingredientsAlt": ["{{targetLanguage}} ingredient 1", "{{targetLanguage}} ingredient 2"]
            }
            </translation>
            """;

        try
        {
            var result = await CallAnthropicAsync(prompt, 1024);
            if (result is null) return BadRequest(new { error = "Translation model did not return the expected format." });

            var root = result.Value;
            var ingredientsAlt = new List<string>();
            if (root.TryGetProperty("ingredientsAlt", out var iArr) && iArr.ValueKind == JsonValueKind.Array)
                foreach (var el in iArr.EnumerateArray())
                    ingredientsAlt.Add(el.GetString() ?? "");

            return Ok(new MenuItemTranslationResponseDto
            {
                NameAlt = root.TryGetProperty("nameAlt", out var nv) ? nv.GetString() : null,
                DescriptionAlt = root.TryGetProperty("descriptionAlt", out var dv) && dv.ValueKind != JsonValueKind.Null ? dv.GetString() : null,
                IngredientsAlt = ingredientsAlt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Translation failed for item: {Name}", request.Name);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("modifier-group")]
    [RequireManagementKey]
    public async Task<IActionResult> TranslateModifierGroup([FromBody] ModifierGroupTranslationRequestDto request)
    {
        var inputJson = JsonSerializer.Serialize(new
        {
            name = request.Name,
            options = request.OptionNames
        });

        var targetLanguage = !string.IsNullOrWhiteSpace(request.TargetLanguageName)
            ? request.TargetLanguageName
            : _config["Restaurant:SecondaryLanguage:Name"] ?? "Vietnamese";

        var prompt = $$"""
            Translate this restaurant modifier group to {{targetLanguage}}.
            Return ONLY a JSON object wrapped in <translation>...</translation> tags.
            No explanation, no markdown, no extra text outside the tags.

            Input (JSON):
            {{inputJson}}

            Return this exact structure inside <translation> tags:
            <translation>
            {
              "nameAlt": "{{targetLanguage}} group name",
              "optionsAlt": ["{{targetLanguage}} option 1", "{{targetLanguage}} option 2"]
            }
            </translation>
            """;

        try
        {
            var result = await CallAnthropicAsync(prompt, 512);
            if (result is null) return BadRequest(new { error = "Translation model did not return the expected format." });

            var root = result.Value;
            var optionsAlt = new List<string>();
            if (root.TryGetProperty("optionsAlt", out var arr) && arr.ValueKind == JsonValueKind.Array)
                foreach (var el in arr.EnumerateArray())
                    optionsAlt.Add(el.GetString() ?? "");

            return Ok(new ModifierGroupTranslationResponseDto
            {
                NameAlt = root.TryGetProperty("nameAlt", out var nv) ? nv.GetString() : null,
                OptionNamesAlt = optionsAlt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Modifier group translation failed for: {Name}", request.Name);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task<JsonElement?> CallAnthropicAsync(string prompt, int maxTokens)
    {
        var client = _httpClientFactory.CreateClient("Anthropic");
        var model = _config["Anthropic:TranslationModel"] ?? _config["Anthropic:Model"] ?? "claude-haiku-4-5-20251001";

        var payload = new
        {
            model,
            max_tokens = maxTokens,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var payloadJson = JsonSerializer.Serialize(payload);
        var httpContent = new ByteArrayContent(Encoding.UTF8.GetBytes(payloadJson));
        httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

        var response = await client.PostAsync("/v1/messages", httpContent);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";

        var start = text.IndexOf("<translation>", StringComparison.OrdinalIgnoreCase);
        var end = text.IndexOf("</translation>", StringComparison.OrdinalIgnoreCase);
        if (start == -1 || end == -1)
        {
            _logger.LogWarning("Translation response missing <translation> tags: {Text}", text);
            return null;
        }

        var translationJson = text[(start + 13)..end].Trim();
        return JsonDocument.Parse(translationJson).RootElement;
    }
}
