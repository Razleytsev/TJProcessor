using Microsoft.AspNetCore.SignalR;
using System.ComponentModel;
using TJConnector.Postgres.Entities;

namespace TJConnector.Api.Hubs;

public class ProcessContainerStatus
{
    public List<Package> Containers { get; set; }
}

public class ProcessExternalDbStatus
{
    public Package Container { get; set; }
}

public class ProcessExternalDbContent
{
    public Package Container { get; set; }
}

public class ProcessEmissionService
{
    public Package Container { get; set; }
}

public class ProcessApplicationStatus
{
    public Package Container { get; set; }
}

public class ProcessApplicationRequest
{
    public Package Container { get; set; }
}

public class ProcessContainerAggregation
{
    public Package Container { get; set; }
}
