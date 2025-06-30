using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Api.Hubs;
using TJConnector.Postgres;
using TJConnector.Postgres.Entities;
using TJConnector.StateSystem.Services.Contracts;
using TJConnector.StateSystem.Services.Implementation;

namespace TJConnector.Api.Transit;

public class ExternalDbContent : IConsumer<ExternalDbContentBody3>
{
    private readonly ApplicationDbContext _context;
    private readonly IExternalDBData _externalDBData;
    private readonly ILogger<ExternalDbContent> _logger;

    public ExternalDbContent(ApplicationDbContext context, IExternalDBData externalDBData , ILogger<ExternalDbContent> logger)
    {
        _externalDBData = externalDBData;
        _context = context;
        _logger = logger;   
    }

    public async Task Consume(ConsumeContext<ExternalDbContentBody3> container)
    {
        var package = container.Message.Container;

        package.Status = -3;
        _logger.LogInformation($"Retreiving content from external database: {package.SSCCCode}");

        var dbContent = await _externalDBData.GetContainerContent(package.Code);

        if (dbContent == null)
        {
            package.Comment = "Content of the package not found in external database";
            package.AddStatus(-3);
            _context.Entry(package).State = EntityState.Modified;
            _logger.LogWarning($"Content of the package not found in external database: {package.SSCCCode}");
            await _context.SaveChangesAsync();
            return;
        }

        package.Status = 3;
        package.Content = dbContent.Content;

        _context.Entry(package).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await container.Publish(new StateCreateApplicationBody4 { Container = package });
    }
}