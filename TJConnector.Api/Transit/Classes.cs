using Microsoft.AspNetCore.SignalR;

namespace TJConnector.Api.Hubs;

public class OrderCreated
{
    public int OrderId { get; set; }
    public List<int> ContainerIds { get; set; } = new List<int>();
}

public class ContainerStatusUpdated
{
    public int ContainerId { get; set; }
    public int Status { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public class ContainerProcessed
{
    public int ContainerId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
