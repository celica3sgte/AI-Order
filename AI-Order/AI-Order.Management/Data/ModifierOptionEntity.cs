namespace AI_Order.Management.Data;

public class ModifierOptionEntity
{
    public int Id { get; set; }
    public int ModifierGroupId { get; set; }
    public ModifierGroupEntity Group { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? NameAlt { get; set; }
    public decimal PriceModifier { get; set; }
    public int SortOrder { get; set; }
}
