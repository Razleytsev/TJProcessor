using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Api.Hubs;
using TJConnector.Postgres;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.Transit;

public class ProcessAggregationConsumer : IConsumer<ProcessAggregationDocument9>
{
    private readonly IExternalContainer _containerService;
    private readonly ApplicationDbContext _context;

    public ProcessAggregationConsumer(IExternalContainer emissionService, ApplicationDbContext externalDb)
    {
        _containerService = emissionService;
        _context = externalDb;
    }

    public async Task Consume(ConsumeContext<ProcessAggregationDocument9> container)
    {
        var package = container.Message.Container;

        var response = await _containerService.ContainerOperationProcess
            (new ProcessDocument { uuids = new Guid[] { package.AggregationGuid.Value } });

        //if (response.Content?.ProcessResult == null || package.AggregationGuid == null)
        //{
        //    package.Status = -7;
        //    package.AddStatus(-7);
        //    package.Comment = "Failed to process application request";
        //    _context.Entry(package).State = EntityState.Modified;
        //    await _context.SaveChangesAsync();
        //    return;
        //}
        var errorMessage = response.Content?.ProcessResult?[package.AggregationGuid.Value];

        if (errorMessage?.message != null || !response.Success)
        {
            package.Status = -7;
            package.AddStatus(-7);
            package.Comment = "Failed to process application request";
            _context.Entry(package).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return;
        }

        package.Status = -7;
        package.AddStatus(-7);
        _context.Entry(package).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await Task.Delay(1000);
        await container.Publish(new ProcessAggregationDocumentStatus8 { Container = package });
    }
}