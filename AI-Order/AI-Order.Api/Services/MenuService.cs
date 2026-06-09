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
    private readonly ISquareService _squareService;
    private readonly SqlMenuService _sqlMenu;
    private readonly ILogger<MenuService> _logger;
    private List<MenuItemDto>? _cachedMenu;
    private DateTime _cacheExpiry;

    public MenuService(ISquareService squareService, SqlMenuService sqlMenu, ILogger<MenuService> logger)
    {
        _squareService = squareService;
        _sqlMenu = sqlMenu;
        _logger = logger;
    }

    public async Task<List<MenuItemDto>> GetMenuAsync(string? userId = null)
    {
        if (userId is not null)
        {
            var custom = await _sqlMenu.GetItemsAsync(userId);
            if (custom.Count > 0) return custom;
        }

        if (_cachedMenu != null && DateTime.UtcNow < _cacheExpiry)
            return _cachedMenu;

        try
        {
            _cachedMenu = await _squareService.GetCatalogItemsAsync();
            if (_cachedMenu.Count == 0)
                throw new Exception("Square catalog returned no items");
            _cacheExpiry = DateTime.UtcNow.AddMinutes(15);
            _logger.LogInformation("Menu refreshed from Square: {Count} items", _cachedMenu.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load menu from Square, using fallback demo menu");
            _cachedMenu = GetFallbackMenu();
            _cacheExpiry = DateTime.UtcNow.AddMinutes(5);
        }

        return _cachedMenu;
    }

    public async Task<MenuItemDto?> GetMenuItemAsync(string id, string? userId = null)
    {
        if (userId is not null)
        {
            var item = await _sqlMenu.GetItemAsync(id, userId);
            if (item is not null) return item;
        }
        var menu = await GetMenuAsync(userId);
        return menu.FirstOrDefault(m => m.Id == id);
    }

    public async Task<string> BuildMenuContextForClaudeAsync(string? userId = null)
    {
        var menu = await GetMenuAsync(userId);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== RESTAURANT MENU ===");

        var byCategory = menu.GroupBy(m => m.Category);
        foreach (var category in byCategory)
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

    private static List<MenuItemDto> GetFallbackMenu() =>
    [
        new()
        {
            Id = "burger-classic",
            Name = "Classic Burger",
            Description = "8oz beef patty, lettuce, tomato, onion, pickles, house sauce",
            Price = 14.99m,
            Category = "Burgers",
            Ingredients = ["beef patty", "brioche bun", "lettuce", "tomato", "onion", "pickles", "house sauce"],
            Allergens = ["gluten", "dairy"],
            ModifierGroups =
            [
                new()
                {
                    Name = "Doneness",
                    Required = true,
                    Options =
                    [
                        new() { Id = "med-rare", Name = "Medium Rare" },
                        new() { Id = "medium", Name = "Medium" },
                        new() { Id = "well", Name = "Well Done" }
                    ]
                },
                new()
                {
                    Name = "Add-ons",
                    Required = false,
                    Options =
                    [
                        new() { Id = "add-cheese", Name = "Cheese", PriceModifier = 1.00m },
                        new() { Id = "add-bacon", Name = "Bacon", PriceModifier = 2.00m },
                        new() { Id = "add-avocado", Name = "Avocado", PriceModifier = 1.50m }
                    ]
                }
            ]
        },
        new()
        {
            Id = "burger-veggie",
            Name = "Veggie Burger",
            Description = "Black bean patty, avocado, roasted peppers, arugula, chipotle mayo",
            Price = 13.99m,
            Category = "Burgers",
            Ingredients = ["black bean patty", "brioche bun", "avocado", "roasted peppers", "arugula", "chipotle mayo"],
            Allergens = ["gluten", "eggs"]
        },
        new()
        {
            Id = "salad-caesar",
            Name = "Caesar Salad",
            Description = "Romaine, parmesan, croutons, house Caesar dressing",
            Price = 11.99m,
            Category = "Salads",
            Ingredients = ["romaine lettuce", "parmesan cheese", "croutons", "Caesar dressing", "lemon"],
            Allergens = ["gluten", "dairy", "eggs", "fish"],
            ModifierGroups =
            [
                new()
                {
                    Name = "Protein",
                    Required = false,
                    Options =
                    [
                        new() { Id = "add-chicken", Name = "Grilled Chicken", PriceModifier = 4.00m },
                        new() { Id = "add-shrimp", Name = "Shrimp", PriceModifier = 5.00m }
                    ]
                }
            ]
        },
        new()
        {
            Id = "pizza-margherita",
            Name = "Margherita Pizza",
            Description = "San Marzano tomato, fresh mozzarella, basil, olive oil",
            Price = 16.99m,
            Category = "Pizza",
            Ingredients = ["pizza dough", "San Marzano tomatoes", "fresh mozzarella", "basil", "olive oil"],
            Allergens = ["gluten", "dairy"]
        },
        new()
        {
            Id = "pasta-carbonara",
            Name = "Spaghetti Carbonara",
            Description = "Spaghetti, pancetta, egg yolk, pecorino romano, black pepper",
            Price = 17.99m,
            Category = "Pasta",
            Ingredients = ["spaghetti", "pancetta", "egg yolk", "pecorino romano", "black pepper"],
            Allergens = ["gluten", "dairy", "eggs", "pork"]
        },
        new()
        {
            Id = "fries-regular",
            Name = "House Fries",
            Description = "Crispy seasoned fries with house dipping sauce",
            Price = 5.99m,
            Category = "Sides",
            Ingredients = ["potatoes", "seasoning blend", "vegetable oil"],
            Allergens = []
        },
        new()
        {
            Id = "drink-soda",
            Name = "Fountain Soda",
            Description = "Coke, Diet Coke, Sprite, Lemonade",
            Price = 2.99m,
            Category = "Drinks",
            Ingredients = [],
            Allergens = []
        },
        new()
        {
            Id = "drink-shake",
            Name = "Milkshake",
            Description = "Vanilla, chocolate, or strawberry",
            Price = 6.99m,
            Category = "Drinks",
            Ingredients = ["ice cream", "milk", "flavoring"],
            Allergens = ["dairy"]
        }
    ];
}
