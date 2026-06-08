namespace AI_Order.Management.Services;

public class OrderNotificationService
{
    public event Action<string>? OnOrderUpdated;

    public void NotifyOrderUpdated(string aspNetUserId)
        => OnOrderUpdated?.Invoke(aspNetUserId);
}
