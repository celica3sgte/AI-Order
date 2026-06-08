using Microsoft.AspNetCore.SignalR;

namespace AI_Order.Api.Hubs;

public class OrderHub : Hub
{
    public async Task AddToGroup(string groupName)
        => await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

    public async Task RemoveFromGroup(string groupName)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
}
