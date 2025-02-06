//using MassTransit;
//using TJConnector.Api.Hubs;
//using TJConnector.Postgres;
//using TJConnector.StateSystem.Services.Contracts;

//namespace TJConnector.Api.Transit;

//public class EmissionServiceConsumer : IConsumer<ProcessEmissionService>
//{
//    private readonly IExternalEmission _emissionService;
//    private readonly ApplicationDbContext _externalDb;

//    public EmissionServiceConsumer(IExternalEmission emissionService, ApplicationDbContext externalDb)
//    {
//        _emissionService = emissionService;
//        _externalDb = externalDb;
//    }

//    public async Task Consume(ConsumeContext<ProcessEmissionService> context)
//    {
//        var package = context.Message.Container;

//        var response = await _emissionService.CreateCodeApplication(package.Code);

//        if (!response.Success)
//        {
//            package.Status = -4;
//            package.Comment = response.Message;
//            //await _externalDb.UpdateContainer(package);
//            return;
//        }

//        package.Status = 4;
//        package.ContentApplicationGuid = response.Uuid;
//        //await _externalDb.UpdateContainer(package);

//        await context.Publish(new ProcessApplicationStatus { Container = package });
//    }
//}