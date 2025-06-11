using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using TJConnector.Api.Hubs;
using TJConnector.Postgres;
using TJConnector.Postgres.Entities;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.Transit;
public class StateCheckSSCC : IConsumer<StateCheckSSCCBody1>
{
    private readonly IExternalContainer _containerService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StateCheckSSCC> _logger;

    public StateCheckSSCC(IExternalContainer containerService, ApplicationDbContext context, ILogger<StateCheckSSCC> logger)
    {
        _containerService = containerService;
        _context = context;
        _logger = logger;   
    }

    public async Task Consume(ConsumeContext<StateCheckSSCCBody1> message)
    {
        var containers = message.Message.Containers;

        containers.ForEach(x => x.Status = -1);

        foreach (var container in containers)
            container.StatusHistory = new[] { new StatusHistory { Status = -1, StatusDate = DateTimeOffset.UtcNow } };

        var batches = containers.Chunk(50);

        foreach (var batch in batches)
        {
            var containerStatusList = await _containerService.ContainerInfoList(new ListRequestRequest
            {
                filters = new Filter { code = batch.Select(x => x.SSCCCode).ToArray() }
            });

            if (containerStatusList.Content?.items == null)
                continue;

            foreach (var package in batch)
            {
                var containerInfo = containerStatusList.Content.items.FirstOrDefault(c => c.code == package.SSCCCode);

                if (containerInfo == null)
                {
                    package.Comment = "Not found in TJ state system";
                    package.AddStatus(-1);
                    _context.Entry(package).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    continue;
                }

                if (containerInfo.status != 0)
                {
                    package.Comment = "Incorrect status in TJ state system";
                    package.AddStatus(-1);
                    _context.Entry(package).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    continue;
                }

                package.Status = 1;
                _context.Entry(package).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                await Task.Delay(500);

                await message.Publish(new ExternalDbBody2 { Container = package });
            }
        }
    }
}
