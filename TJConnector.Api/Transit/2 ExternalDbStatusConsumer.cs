//using MassTransit;
//using TJConnector.Api.Hubs;
//using TJConnector.Postgres;
//using TJConnector.StateSystem.Services.Contracts;

//namespace TJConnector.Api.Transit;

//public class ExternalDbStatusConsumer : IConsumer<ProcessExternalDbStatus>
//{
//    private readonly ApplicationDbContext _externalDb;
//    private readonly IExternalEmission _emissionService;

//    public ExternalDbStatusConsumer(ApplicationDbContext externalDb, IExternalEmission emissionService)
//    {
//        _externalDb = externalDb;
//        _emissionService = emissionService;
//    }

//    public async Task Consume(ConsumeContext<ProcessExternalDbStatus> context)
//    {
//        var package = context.Message.Container;

//        var dbInfoList = await _externalDb.GetDbInfo(new List<string> { package.Code });

//        var dbInfo = dbInfoList.FirstOrDefault();

//        if (dbInfo == null)
//        {
//            package.Status = -2;
//            package.Comment = "Не найден во внешней базе данных";
//            //await _externalDb.UpdateContainer(package);
//            return;
//        }

//        if (dbInfo.ExternalDbStatus != 1)
//        {
//            package.Status = -2;
//            package.Comment = dbInfo.ExternalDbMessage;
//            //await _externalDb.UpdateContainer(package);
//            return;
//        }

//        package.Status = 2;
//        //await _externalDb.UpdateContainer(package);

//        await Task.Delay(500);

//        await context.Publish(new ProcessExternalDbContent { Container = package });
//    }
//}
