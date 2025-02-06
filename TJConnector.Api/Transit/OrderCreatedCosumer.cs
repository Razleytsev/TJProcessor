using MassTransit;
using TJConnector.Api.Hubs;
using TJConnector.Postgres;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.Transit;

public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    //private readonly IExternalContainer _containerService;
    //private readonly IExternalDBData _externalDb;
    //private readonly IExternalEmission _emissionService;
    //private readonly ApplicationDbContext _context;

    //public OrderCreatedConsumer(IExternalContainer containerService, IExternalDBData externalDb, IExternalEmission emissionService, ApplicationDbContext context)
    //{
    //    _containerService = containerService;
    //    _externalDb = externalDb;
    //    _emissionService = emissionService;
    //    _context = context;
    //}

    //public async Task Consume(ConsumeContext<OrderCreated> context)
    //{
    //    var orderId = context.Message.OrderId;
    //    var containerIds = context.Message.ContainerIds;

    //    foreach (var containerId in containerIds)
    //    {
    //        await ProcessContainer(containerId);
    //    }
    //}

    //private async Task ProcessContainer(int containerId)
    //{
    //    var localPackage = _context.Packages.FirstOrDefault(x => x.Id == containerId);
    //    if (localPackage == null)
    //        return;
    //    var containerStatus = await _containerService.ContainerInfo(localPackage.SSCCCode);
    //    if (containerStatus.Content == null) 
    //        return;
    //    if (containerStatus == null || containerStatus.Content.status != 0)
    //    {
    //        await UpdateContainerStatus(containerId, -1, "Не найден во внешней системе");
    //        return;
    //    }

    //    await Task.Delay(500); 

    //    var dbInfo = await _externalDb.GetContainerInfo(new List<string> { localPackage.Code.ToString() });
    //    if (dbInfo == null || dbInfo.Content.Content.FirstOrDefault().ExternalDbStatus != 1)
    //    {
    //        await UpdateContainerStatus(containerId, -2, "Не найден во внешней базе данных");
    //        return;
    //    }

    //    await Task.Delay(500); 
    //    var dbContent = await _externalDb.GetContainerContent(localPackage.Code);
    //    if (dbContent == null)
    //    {
    //        // Обновление статуса контейнера в базе данных
    //        await UpdateContainerStatus(containerId, -3, "Не найдена вложенность во внешней базе данных");
    //        return;
    //    }

    //    // Обновление контента контейнера в базе данных
    //    await UpdateContainerContent(containerId, dbContent);

    //    // Шаг 4: Отправка контента через EmissionService
    //    var createResponse = await _emissionService.CreateCodeApplication(containerId.ToString());
    //    if (!createResponse.Success)
    //    {
    //        // Обновление статуса контейнера в базе данных
    //        await UpdateContainerStatus(containerId, -4, createResponse.Message);
    //        return;
    //    }

    //    // Шаг 5: Проверка статуса документа через EmissionService
    //    await Task.Delay(500); // Задержка
    //    var applicationStatus = await _emissionService.CheckApplicationStatus(new List<Guid> { createResponse.Uuid });
    //    if (applicationStatus == null || applicationStatus.Status != 1)
    //    {
    //        // Обновление статуса контейнера в базе данных
    //        await UpdateContainerStatus(containerId, -5, "Сохранено в государственной системе с ошибкой");
    //        return;
    //    }

    //    // Шаг 6: Подтверждение обработки документа через EmissionService
    //    await Task.Delay(500); // Задержка
    //    var processResponse = await _emissionService.ProcessApplicationRequest(new List<Guid> { createResponse.Uuid });
    //    if (processResponse.ContainsKey(createResponse.Uuid) && processResponse[createResponse.Uuid] != null)
    //    {
    //        // Обновление статуса контейнера в базе данных
    //        await UpdateContainerStatus(containerId, -6, processResponse[createResponse.Uuid]);
    //        return;
    //    }

    //    // Шаг 7: Отправка агрегации через ContainerService
    //    var aggregationResponse = await _containerService.CreateContainerAggregation(containerId.ToString());
    //    if (!aggregationResponse.Success)
    //    {
    //        // Обновление статуса контейнера в базе данных
    //        await UpdateContainerStatus(containerId, -5, aggregationResponse.Message);
    //        return;
    //    }

    //    // Обновление статуса контейнера в базе данных
    //    await UpdateContainerStatus(containerId, 5, aggregationResponse.Uuid.ToString());
    //}

    //private async Task UpdateContainerStatus(int containerId, int status, string comment)
    //{
    //    // Обновление статуса контейнера в базе данных
    //    // Ваш код для обновления базы данных
    //}

    //private async Task UpdateContainerContent(int containerId, object content)
    //{
    //    // Обновление контента контейнера в базе данных
    //    // Ваш код для обновления базы данных
    //}
    public Task Consume(ConsumeContext<OrderCreated> context)
    {
        throw new NotImplementedException();
    }
}

