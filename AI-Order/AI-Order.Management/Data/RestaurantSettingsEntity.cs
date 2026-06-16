namespace AI_Order.Management.Data;

public class RestaurantSettingsEntity
{
    public string AspNetUserId { get; set; } = string.Empty;
    public string PrimaryLanguageCode { get; set; } = "en";
    public string? SecondaryLanguageCode { get; set; }
}
