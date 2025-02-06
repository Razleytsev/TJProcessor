//using MassTransit;
//using TJConnector.Api.Hubs;
//using TJConnector.Postgres;
//using TJConnector.StateSystem.Services.Contracts;

//namespace TJConnector.Api.Transit;

//public class ProcessApplicationConsumer : IConsumer<ProcessApplicationRequest>
//{
//    private readonly IExternalEmission _emissionService;
//    private readonly ApplicationDbContext _externalDb;

//    public ProcessApplicationConsumer(IExternalEmission emissionService, ApplicationDbContext externalDb)
//    {
//        _emissionService = emissionService;
//        _externalDb = externalDb;
//    }

//    public async Task Consume(ConsumeContext<ProcessApplicationRequest> context)
//    {
//        var package = context.Message.Container;

//        var response = await _emissionService.ProcessApplicationRequest(new List<Guid> { package.ContentApplicationGuid });

//        var errorMessage = response[package.ContentApplicationGuid];

//        if (errorMessage != null)
//        {
//            package.Status = -6;
//            package.Comment = errorMessage;
//            //await _externalDb.UpdateContainer(package);
//            return;
//        }

//        package.Status = 6;
//        //await _externalDb.UpdateContainer(package);

//        await context.Publish(new ProcessContainerAggregation { Container = package });
//    }
//}