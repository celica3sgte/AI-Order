using System.Text.Json;
using AI_Order.Shared.Models;
using Microsoft.Data.SqlClient;

namespace AI_Order.Api.Services;

public class SqlMenuService
{
    private readonly string? _connectionString;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public SqlMenuService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public bool IsEnabled => !string.IsNullOrEmpty(_connectionString);

    public async Task<List<MenuItemDto>> GetItemsAsync(string aspNetUserId)
    {
        if (!IsEnabled) return [];
        var results = new List<MenuItemDto>();
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT Id, Name, Description, Price, Category, IsAvailable,
                       MainImage, Image1, Image2, Image3,
                       IngredientsJson, AllergensJson, ModifierGroupsJson
                FROM MenuItems
                WHERE AspNetUserId = @userId AND IsAvailable = 1
                ORDER BY Category, Name
                """;
            cmd.Parameters.AddWithValue("@userId", aspNetUserId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                results.Add(Map(reader));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SqlMenu] GetItemsAsync failed: {ex.Message}");
        }
        return results;
    }

    public async Task<MenuItemDto?> GetItemAsync(string id, string aspNetUserId)
    {
        if (!IsEnabled) return null;
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT Id, Name, Description, Price, Category, IsAvailable,
                       MainImage, Image1, Image2, Image3,
                       IngredientsJson, AllergensJson, ModifierGroupsJson
                FROM MenuItems
                WHERE Id = @id AND AspNetUserId = @userId
                """;
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@userId", aspNetUserId);
            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? Map(reader) : null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SqlMenu] GetItemAsync failed: {ex.Message}");
            return null;
        }
    }

    private static MenuItemDto Map(SqlDataReader r) => new()
    {
        Id = r.GetString(0),
        Name = r.GetString(1),
        Description = r.IsDBNull(2) ? null : r.GetString(2),
        Price = r.GetDecimal(3),
        Category = r.GetString(4),
        IsAvailable = r.GetBoolean(5),
        MainImage = r.IsDBNull(6) ? null : r.GetString(6),
        Image1 = r.IsDBNull(7) ? null : r.GetString(7),
        Image2 = r.IsDBNull(8) ? null : r.GetString(8),
        Image3 = r.IsDBNull(9) ? null : r.GetString(9),
        Ingredients = Deserialize<List<string>>(r.GetString(10)),
        Allergens = Deserialize<List<string>>(r.GetString(11)),
        ModifierGroups = Deserialize<List<ModifierGroupDto>>(r.GetString(12)),
    };

    private static T Deserialize<T>(string json) where T : new()
    {
        try { return JsonSerializer.Deserialize<T>(json, _json) ?? new T(); }
        catch { return new T(); }
    }
}
