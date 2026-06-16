namespace AI_Order.Management.Data;

public class MenuItemModifierGroupEntity
{
    public string MenuItemId { get; set; } = string.Empty;
    public int ModifierGroupId { get; set; }
    public int SortOrder { get; set; }
    public MenuItemEntity MenuItem { get; set; } = null!;
    public ModifierGroupEntity ModifierGroup { get; set; } = null!;
}
