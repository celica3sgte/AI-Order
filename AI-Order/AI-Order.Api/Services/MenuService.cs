using AI_Order.Shared.Models;

namespace AI_Order.Api.Services;

public interface IMenuService
{
    Task<List<MenuItemDto>> GetMenuAsync(string? userId = null);
    Task<MenuItemDto?> GetMenuItemAsync(string id, string? userId = null);
    Task<string> BuildMenuContextForClaudeAsync(string? userId = null);
}

public class MenuService : IMenuService
{
    private readonly SqlMenuService _sqlMenu;

    public MenuService(SqlMenuService sqlMenu)
    {
        _sqlMenu = sqlMenu;
    }

    public async Task<List<MenuItemDto>> GetMenuAsync(string? userId = null)
    {
        if (userId is not null)
            return await _sqlMenu.GetItemsAsync(userId);
        return [];
    }

    public async Task<MenuItemDto?> GetMenuItemAsync(string id, string? userId = null)
    {
        if (userId is not null)
            return await _sqlMenu.GetItemAsync(id, userId);
        return null;
    }

    public async Task<string> BuildMenuContextForClaudeAsync(string? userId = null)
    {
        var menu = await GetMenuAsync(userId);
        if (menu.Count == 0)
            return "=== RESTAURANT MENU ===\n(No menu items available)";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== RESTAURANT MENU ===");

        foreach (var category in menu.GroupBy(m => m.Category))
        {
            sb.AppendLine($"\n## {category.Key}");
            foreach (var item in category)
            {
                sb.AppendLine($"- [{item.Id}] {item.Name} — ${item.Price:F2}");
                sb.AppendLine($"  {item.Description}");
                if (item.Allergens.Any())
                    sb.AppendLine($"  ⚠ Allergens: {string.Join(", ", item.Allergens)}");
                if (item.Ingredients.Any())
                    sb.AppendLine($"  Ingredients: {string.Join(", ", item.Ingredients)}");
                if (item.ModifierGroups.Any())
                {
                    foreach (var group in item.ModifierGroups)
                    {
                        sb.AppendLine($"  Options ({group.Name}{(group.Required ? ", required" : "")}): " +
                            string.Join(", ", group.Options.Select(o =>
                                o.PriceModifier > 0 ? $"{o.Name} (+${o.PriceModifier:F2})" : o.Name)));
                    }
                }
            }
        }

        return sb.ToString();
    }
}
