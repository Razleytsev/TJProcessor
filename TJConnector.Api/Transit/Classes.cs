using Microsoft.AspNetCore.SignalR;
using System.ComponentModel;
using TJConnector.Postgres.Entities;

namespace TJConnector.Api.Hubs;

public class ProcessContainerStatus1
{
    public List<Package> Containers { get; set; }
}

public class ProcessExternalDbStatus2
{
    public Package Container { get; set; }
}

public class ProcessExternalDbContent3
{
    public Package Container { get; set; }
}

public class ProcessEmissionService4
{
    public Package Container { get; set; }
}

public class ProcessAggregationStatus5
{
    public Package Container { get; set; }
}

public class ProcessApplicationRequest6
{
    public Package Container { get; set; }
}

public class ProcessContainerAggregation7
{
    public Package Container { get; set; }
}
public class ProcessAggregationDocumentStatus8
{
    public Package Container { get; set; }
}
public class ProcessAggregationDocument9
{
    public Package Container { get; set; }
}

public class ReprocessContainer0
{
    public Package? Container { get; set; }
}