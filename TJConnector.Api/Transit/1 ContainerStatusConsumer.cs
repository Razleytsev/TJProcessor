//using MassTransit;
//using System.Linq;
//using TJConnector.Api.Hubs;
//using TJConnector.Postgres;
//using TJConnector.StateSystem.Model.ExternalRequests.Generic;
//using TJConnector.StateSystem.Services.Contracts;

//namespace TJConnector.Api.Transit;
//public class ContainerStatusConsumer : IConsumer<ProcessContainerStatus>
//{
//    private readonly IExternalContainer _containerService;
//    private readonly ApplicationDbContext _context;

//    public ContainerStatusConsumer(IExternalContainer containerService, ApplicationDbContext externalDb)
//    {
//        _containerService = containerService;
//        _context = externalDb;
//    }

//    public async Task Consume(ConsumeContext<ProcessContainerStatus> context)
//    {
//        var containers = context.Message.Containers;

//        containers.ForEach(x => x.Status = -1);

//        var batches = containers.Chunk(50);

//        foreach (var batch in batches)
//        {
//            var containerStatusList = await _containerService.ContainerInfoList(new ListRequestRequest
//            {
//                filter = new Filter { code = batch.Select(x => x.Code).ToList() }
//            });

//            if (containerStatusList.Content == null)
//                continue;

//            foreach (var package in batch)
//            {
//                var containerInfo = containerStatusList.Content.FirstOrDefault(c => c.code == package.Code);

//                if (containerInfo == null)
//                {
//                    package.Status = -1;
//                    package.Comment = "Не найден во внешней системе";
//                    await _context.SaveChangesAsync();
//                    continue;
//                }

//                if (containerInfo.Status != 0)
//                {
//                    package.Status = -1;
//                    package.Comment = "Некорректный статус во внешней системе";
//                    //await _context.UpdateContainer(package);
//                    continue;
//                }

//                package.Status = 1;
//                //await _context.UpdateContainer(package);

//                await context.Publish(new ProcessExternalDbStatus { package = package });
//            }
//        }
//    }
//}
