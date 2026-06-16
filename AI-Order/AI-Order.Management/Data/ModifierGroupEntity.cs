namespace AI_Order.Management.Data;

public class ModifierGroupEntity
{
    public int Id { get; set; }
    public string AspNetUserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameAlt { get; set; }
    public string? DisplayName { get; set; }
    public string? DisplayNameAlt { get; set; }
    public bool Required { get; set; }
    public int SortOrder { get; set; }
    public ICollection<ModifierOptionEntity> Options { get; set; } = [];
    public ICollection<MenuItemModifierGroupEntity> MenuItemLinks { get; set; } = [];
}
