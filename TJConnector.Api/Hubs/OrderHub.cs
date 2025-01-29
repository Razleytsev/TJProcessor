using Microsoft.AspNetCore.SignalR;

namespace TJConnector.Api.Hubs;

public class OrderHub : Hub
{
    public async Task SendMessage(Guid uid, int newStatus)
    {
        await Clients.All.SendAsync("OrderStatusUpdate", uid, newStatus);
    }
}