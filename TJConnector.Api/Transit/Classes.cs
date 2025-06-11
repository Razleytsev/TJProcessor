using Microsoft.AspNetCore.SignalR;
using System.ComponentModel;
using TJConnector.Postgres.Entities;

namespace TJConnector.Api.Hubs;

public class StateCheckSSCCBody1
{
    public List<Package> Containers { get; set; }
}

public class ExternalDbBody2
{
    public Package Container { get; set; }
}

public class ExternalDbContentBody3
{
    public Package Container { get; set; }
}

public class StateCreateApplicationBody4
{
    public Package Container { get; set; }
}

public class StateApplicationStatusBody5
{
    public Package Container { get; set; }
    public int RetryCount { get; set; } = 0;
}

public class StateApplicationProcessBody6
{
    public Package Container { get; set; }
}

public class StateCreateAggregationBody7
{
    public Package Container { get; set; }
}
public class StateAggregationStatusBody8
{
    public Package Container { get; set; }
    public int RetryCount { get; set; } = 0;
}
public class StateProcessAggregationBody9
{
    public Package Container { get; set; }
}

public class ReprocessContainer0
{
    public Package? Container { get; set; }
}