//using MassTransit;
//using TJConnector.Api.Hubs;
//using TJConnector.Postgres;
//using TJConnector.Postgres.Entities;
//using TJConnector.StateSystem.Services.Contracts;

//namespace TJConnector.Api.Transit;

//public class ApplicationStatusConsumer : IConsumer<ProcessApplicationStatus>
//{
//    private readonly IExternalEmission _emissionService;
//    private readonly ApplicationDbContext _externalDb;

//    public ApplicationStatusConsumer(IExternalEmission emissionService, ApplicationDbContext externalDb)
//    {
//        _emissionService = emissionService;
//        _externalDb = externalDb;
//    }

//    public async Task Consume(ConsumeContext<ProcessApplicationStatus> context)
//    {
//        var package = context.Message.Container;

//        var statusList = await _emissionService.CheckApplicationStatus(new List<Guid> { package.ContentApplicationGuid });

//        var status = statusList.FirstOrDefault();

//        if (status == null)
//        {
//            package.Status = -5;
//            package.Comment = "Не найден статус документа";
//            //await _externalDb.UpdateContainer(package);
//            return;
//        }

//        if (status.Status == 0)
//        {
//            package.Status = -5;
//            package.Comment = "Сохранено в государственной системе с ошибкой";
//            //await _externalDb.UpdateContainer(package);
//            return;
//        }

//        package.Status = 5;
//        //await _externalDb.UpdateContainer(package);

//        await Task.Delay(500);

//        await context.Publish(new ProcessApplicationRequest { Container = package });
//    }
//}