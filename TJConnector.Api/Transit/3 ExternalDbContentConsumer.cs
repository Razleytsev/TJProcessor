//using MassTransit;
//using TJConnector.Api.Hubs;
//using TJConnector.Postgres;
//using TJConnector.StateSystem.Services.Contracts;

//namespace TJConnector.Api.Transit;

//public class ExternalDbContentConsumer : IConsumer<ProcessExternalDbContent>
//{
//    private readonly ApplicationDbContext _externalDb;
//    private readonly IExternalEmission _emissionService;

//    public ExternalDbContentConsumer(ApplicationDbContext externalDb, IExternalEmission emissionService)
//    {
//        _externalDb = externalDb;
//        _emissionService = emissionService;
//    }

//    public async Task Consume(ConsumeContext<ProcessExternalDbContent> context)
//    {
//        var package = context.Message.Container;

//        var dbContent = await _externalDb.GetDbContent(package.Code);

//        if (dbContent == null)
//        {
//            package.Status = -3;
//            package.Comment = "Не найдена вложенность во внешней базе данных";
//            //await _externalDb.UpdateContainer(package);
//            return;
//        }

//        package.Status = 3;
//        package.Content = dbContent;
//        //await _externalDb.UpdateContainer(package);

//        await Task.Delay(500);

//        await context.Publish(new ProcessEmissionService { Container = package });
//    }
//}