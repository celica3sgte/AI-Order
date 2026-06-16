namespace AI_Order.Management.Data;

public class MenuItemEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AspNetUserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public string? MainImage { get; set; }
    public string? Image1 { get; set; }
    public string? Image2 { get; set; }
    public string? Image3 { get; set; }
    public string IngredientsJson { get; set; } = "[]";
    public string AllergensJson { get; set; } = "[]";
    public string ModifierGroupsJson { get; set; } = "[]";
    public string? NameAlt { get; set; }
    public string? DescriptionAlt { get; set; }
    public string ModifierGroupsJsonAlt { get; set; } = "[]";
}
