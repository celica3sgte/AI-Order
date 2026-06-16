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
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            return await QueryItemsAsync(conn, aspNetUserId, itemId: null);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SqlMenu] GetItemsAsync failed: {ex.Message}");
            return [];
        }
    }

    public async Task<MenuItemDto?> GetItemAsync(string id, string aspNetUserId)
    {
        if (!IsEnabled) return null;
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var items = await QueryItemsAsync(conn, aspNetUserId, itemId: id);
            return items.Count > 0 ? items[0] : null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SqlMenu] GetItemAsync failed: {ex.Message}");
            return null;
        }
    }

    private static async Task<List<MenuItemDto>> QueryItemsAsync(SqlConnection conn, string aspNetUserId, string? itemId)
    {
        await using var cmd = conn.CreateCommand();

        var whereClause = itemId is null
            ? "mi.AspNetUserId = @userId AND mi.IsAvailable = 1"
            : "mi.Id = @itemId AND mi.AspNetUserId = @userId";

        cmd.CommandText = $"""
            SELECT
                mi.Id          AS ItemId,
                mi.Name        AS ItemName,
                mi.Description,
                mi.Price,
                mi.Category,
                mi.IsAvailable,
                mi.MainImage,
                mi.Image1,
                mi.Image2,
                mi.Image3,
                mi.IngredientsJson,
                mi.IngredientsJsonAlt,
                mi.AllergensJson,
                mi.NameAlt     AS ItemNameAlt,
                mi.DescriptionAlt,
                mg.Id              AS GroupId,
                mg.Name            AS GroupName,
                mg.NameAlt         AS GroupNameAlt,
                mg.DisplayName     AS GroupDisplayName,
                mg.DisplayNameAlt  AS GroupDisplayNameAlt,
                mg.Required        AS GroupRequired,
                mimgLink.SortOrder AS GroupSortOrder,
                mo.Id          AS OptionId,
                mo.Name        AS OptionName,
                mo.NameAlt     AS OptionNameAlt,
                mo.PriceModifier,
                mo.SortOrder   AS OptionSortOrder
            FROM MenuItems mi
            LEFT JOIN MenuItemModifierGroups mimgLink ON mimgLink.MenuItemId = mi.Id
            LEFT JOIN ModifierGroups mg ON mg.Id = mimgLink.ModifierGroupId
            LEFT JOIN ModifierOptions mo ON mo.ModifierGroupId = mg.Id
            WHERE {whereClause}
            ORDER BY mi.Category, mi.Name, mimgLink.SortOrder, mo.SortOrder
            """;

        cmd.Parameters.AddWithValue("@userId", aspNetUserId);
        if (itemId is not null)
            cmd.Parameters.AddWithValue("@itemId", itemId);

        await using var reader = await cmd.ExecuteReaderAsync();

        // Get ordinals once
        int ordItemId       = reader.GetOrdinal("ItemId");
        int ordItemName     = reader.GetOrdinal("ItemName");
        int ordDescription  = reader.GetOrdinal("Description");
        int ordPrice        = reader.GetOrdinal("Price");
        int ordCategory     = reader.GetOrdinal("Category");
        int ordIsAvailable  = reader.GetOrdinal("IsAvailable");
        int ordMainImage    = reader.GetOrdinal("MainImage");
        int ordImage1       = reader.GetOrdinal("Image1");
        int ordImage2       = reader.GetOrdinal("Image2");
        int ordImage3       = reader.GetOrdinal("Image3");
        int ordIngredients    = reader.GetOrdinal("IngredientsJson");
        int ordIngredientsAlt = reader.GetOrdinal("IngredientsJsonAlt");
        int ordAllergens      = reader.GetOrdinal("AllergensJson");
        int ordItemNameAlt  = reader.GetOrdinal("ItemNameAlt");
        int ordDescAlt      = reader.GetOrdinal("DescriptionAlt");
        int ordGroupId          = reader.GetOrdinal("GroupId");
        int ordGroupName        = reader.GetOrdinal("GroupName");
        int ordGroupNameAlt     = reader.GetOrdinal("GroupNameAlt");
        int ordGroupDisplayName    = reader.GetOrdinal("GroupDisplayName");
        int ordGroupDisplayNameAlt = reader.GetOrdinal("GroupDisplayNameAlt");
        int ordRequired         = reader.GetOrdinal("GroupRequired");
        int ordOptionId     = reader.GetOrdinal("OptionId");
        int ordOptionName   = reader.GetOrdinal("OptionName");
        int ordOptionNameAlt = reader.GetOrdinal("OptionNameAlt");
        int ordPriceMod     = reader.GetOrdinal("PriceModifier");

        var itemsById  = new Dictionary<string, MenuItemDto>();
        var itemOrder  = new List<string>();
        var groupsById = new Dictionary<(string, int), ModifierGroupDto>();

        while (await reader.ReadAsync())
        {
            var id = reader.GetString(ordItemId);

            if (!itemsById.TryGetValue(id, out var item))
            {
                item = new MenuItemDto
                {
                    Id            = id,
                    Name          = reader.GetString(ordItemName),
                    Description   = reader.IsDBNull(ordDescription)  ? "" : reader.GetString(ordDescription),
                    Price         = reader.GetDecimal(ordPrice),
                    Category      = reader.GetString(ordCategory),
                    IsAvailable   = reader.GetBoolean(ordIsAvailable),
                    MainImage     = reader.IsDBNull(ordMainImage)  ? null : reader.GetString(ordMainImage),
                    Image1        = reader.IsDBNull(ordImage1)     ? null : reader.GetString(ordImage1),
                    Image2        = reader.IsDBNull(ordImage2)     ? null : reader.GetString(ordImage2),
                    Image3        = reader.IsDBNull(ordImage3)     ? null : reader.GetString(ordImage3),
                    Ingredients    = Deserialize<List<string>>(reader.GetString(ordIngredients)),
                    IngredientsAlt = Deserialize<List<string>>(reader.IsDBNull(ordIngredientsAlt) ? "[]" : reader.GetString(ordIngredientsAlt)),
                    Allergens      = Deserialize<List<string>>(reader.GetString(ordAllergens)),
                    NameAlt       = reader.IsDBNull(ordItemNameAlt) ? null : reader.GetString(ordItemNameAlt),
                    DescriptionAlt = reader.IsDBNull(ordDescAlt)   ? null : reader.GetString(ordDescAlt),
                };
                itemsById[id] = item;
                itemOrder.Add(id);
            }

            if (!reader.IsDBNull(ordGroupId))
            {
                var groupId = reader.GetInt32(ordGroupId);
                var groupKey = (id, groupId);

                if (!groupsById.TryGetValue(groupKey, out var group))
                {
                    var internalName    = reader.GetString(ordGroupName);
                    var displayName     = reader.IsDBNull(ordGroupDisplayName)    ? null : reader.GetString(ordGroupDisplayName);
                    var displayNameAlt  = reader.IsDBNull(ordGroupDisplayNameAlt) ? null : reader.GetString(ordGroupDisplayNameAlt);
                    var nameAlt         = reader.IsDBNull(ordGroupNameAlt)        ? null : reader.GetString(ordGroupNameAlt);

                    group = new ModifierGroupDto
                    {
                        Name     = displayName    ?? internalName,
                        NameAlt  = displayNameAlt ?? nameAlt,
                        Required = reader.GetBoolean(ordRequired),
                    };
                    groupsById[groupKey] = group;
                    item.ModifierGroups.Add(group);
                }

                if (!reader.IsDBNull(ordOptionId))
                {
                    group.Options.Add(new ModifierOptionDto
                    {
                        Id            = reader.GetInt32(ordOptionId).ToString(),
                        Name          = reader.GetString(ordOptionName),
                        NameAlt       = reader.IsDBNull(ordOptionNameAlt) ? null : reader.GetString(ordOptionNameAlt),
                        PriceModifier = reader.GetDecimal(ordPriceMod),
                    });
                }
            }
        }

        return itemOrder.Select(i => itemsById[i]).ToList();
    }

    private static T Deserialize<T>(string json) where T : new()
    {
        try { return JsonSerializer.Deserialize<T>(json, _json) ?? new T(); }
        catch { return new T(); }
    }
}
