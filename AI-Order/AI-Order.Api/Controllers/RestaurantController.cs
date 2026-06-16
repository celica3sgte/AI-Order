using AI_Order.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace AI_Order.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestaurantController : ControllerBase
{
    private readonly IConfiguration _config;

    public RestaurantController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings([FromQuery] string? userId = null)
    {
        var (primary, secondary) = await ReadLanguageSettingsAsync(userId);
        return Ok(new RestaurantSettingsDto
        {
            RestaurantName = _config["Restaurant:Name"] ?? "Our Restaurant",
            PrimaryLanguageCode = primary,
            SecondaryLanguageCode = secondary
        });
    }

    private async Task<(string primary, string? secondary)> ReadLanguageSettingsAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId)) return ("en", null);

        var connStr = _config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connStr)) return ("en", null);

        try
        {
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT PrimaryLanguageCode, SecondaryLanguageCode
                FROM RestaurantSettings
                WHERE AspNetUserId = @userId
                """;
            cmd.Parameters.AddWithValue("@userId", userId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var primary = reader.GetString(0);
                var secondary = reader.IsDBNull(1) ? null : reader.GetString(1);
                return (primary, secondary);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[RestaurantSettings] Read failed: {ex.Message}");
        }

        return ("en", null);
    }
}
