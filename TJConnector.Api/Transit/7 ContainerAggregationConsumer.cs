//using MassTransit;
//using TJConnector.Api.Hubs;
//using TJConnector.Postgres;
//using TJConnector.StateSystem.Services.Contracts;

//namespace TJConnector.Api.Transit;

//public class ContainerAggregationConsumer : IConsumer<ProcessContainerAggregation>
//{
//    private readonly IExternalContainer _containerService;
//    private readonly ApplicationDbContext _externalDb;

//    public ContainerAggregationConsumer(IExternalContainer containerService, ApplicationDbContext externalDb)
//    {
//        _containerService = containerService;
//        _externalDb = externalDb;
//    }

//    public async Task Consume(ConsumeContext<ProcessContainerAggregation> context)
//    {
//        var package = context.Message.Container;

//        var response = await _containerService.CreateContainerAggregation(package.Code);

//        if (!response.Success)
//        {
//            package.Status = -5;
//            package.Comment = response.Message;
//            //await _externalDb.UpdateContainer(package);
//            return;
//        }

//        package.Status = 5;
//        package.AggregationGuid = response.Uuid;
//        //await _externalDb.UpdateContainer(package);
//    }
//}