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
        var groupsInput = request.ModifierGroups.Select(g => new
        {
            name = g.Name,
            options = g.Options.Select(o => o.Name).ToList()
        }).ToList();

        var inputJson = JsonSerializer.Serialize(new
        {
            name = request.Name,
            description = request.Description,
            groups = groupsInput
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
              "groupsAlt": [
                {
                  "name": "{{targetLanguage}} group name",
                  "options": ["{{targetLanguage}} option 1", "{{targetLanguage}} option 2"]
                }
              ]
            }
            </translation>
            """;

        try
        {
            var client = _httpClientFactory.CreateClient("Anthropic");
            var model = _config["Anthropic:TranslationModel"] ?? _config["Anthropic:Model"] ?? "claude-haiku-4-5-20251001";

            var payload = new
            {
                model,
                max_tokens = 1024,
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
                return BadRequest(new { error = "Translation model did not return the expected format." });
            }

            var translationJson = text[(start + 13)..end].Trim();
            var parsed = JsonDocument.Parse(translationJson);
            var root = parsed.RootElement;

            var nameAlt = root.TryGetProperty("nameAlt", out var nv) ? nv.GetString() : null;
            var descAlt = root.TryGetProperty("descriptionAlt", out var dv) && dv.ValueKind != JsonValueKind.Null
                ? dv.GetString() : null;

            var modGroupsAlt = new List<ModifierGroupDto>();
            if (root.TryGetProperty("groupsAlt", out var gvArr) && gvArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var gv in gvArr.EnumerateArray())
                {
                    var groupName = gv.TryGetProperty("name", out var gn) ? gn.GetString() ?? "" : "";
                    var opts = new List<ModifierOptionDto>();
                    if (gv.TryGetProperty("options", out var optsArr) && optsArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var opt in optsArr.EnumerateArray())
                            opts.Add(new ModifierOptionDto { Name = opt.GetString() ?? "" });
                    }
                    modGroupsAlt.Add(new ModifierGroupDto { Name = groupName, Options = opts });
                }
            }

            return Ok(new MenuItemTranslationResponseDto
            {
                NameAlt = nameAlt,
                DescriptionAlt = descAlt,
                ModifierGroupsAlt = modGroupsAlt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Translation failed for item: {Name}", request.Name);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
