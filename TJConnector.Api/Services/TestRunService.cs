using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Api.TestRun;
using TJConnector.Postgres;
using TJConnector.Postgres.Entities;
using TJConnector.SharedLibrary.DTOs.Forms;

namespace TJConnector.Api.Services;

public interface ITestRunService
{
    Task<Postgres.Entities.TestRun?> CreateAsync(TestRunCreateForm form);
    Task<Postgres.Entities.TestRun?> GetByIdAsync(int id);
    Task<List<Postgres.Entities.TestRun>> ListAsync(int take = 50);
    Task<Postgres.Entities.TestRun?> ReprocessAsync(int parentId, int fromStage);
    Task<Postgres.Entities.TestRun?> CancelAsync(int id);
}

public class TestRunService : ITestRunService
{
    private readonly ApplicationDbContext _context;
    private readonly IPublishEndpoint _publish;
    private readonly ILogger<TestRunService> _logger;

    public TestRunService(ApplicationDbContext context, IPublishEndpoint publish, ILogger<TestRunService> logger)
    {
        _context = context;
        _publish = publish;
        _logger = logger;
    }

    public async Task<Postgres.Entities.TestRun?> CreateAsync(TestRunCreateForm form)
    {
        if (form == null) return null;
        if (form.PacksPerBundle < 1 || form.BundlesPerContainer < 1) return null;

        var pack = await _context.Products.FindAsync(form.PackProductId);
        var bundle = await _context.Products.FindAsync(form.BundleProductId);
        if (pack == null || bundle == null) return null;

        var run = new Postgres.Entities.TestRun
        {
            User = form.User,
            PackProductId = form.PackProductId,
            BundleProductId = form.BundleProductId,
            PacksPerBundle = form.PacksPerBundle,
            BundlesPerContainer = form.BundlesPerContainer,
            FactoryUuid = form.FactoryUuid,
            MarkingLineUuid = form.MarkingLineUuid,
            LocationUuid = form.LocationUuid,
            Stage = 0
        };
        _context.TestRuns.Add(run);
        await _context.SaveChangesAsync();

        await _publish.Publish(new TestRunStart { TestRunId = run.Id });
        _logger.LogInformation("TestRun {Id}: created and dispatched", run.Id);
        return run;
    }

    public async Task<Postgres.Entities.TestRun?> GetByIdAsync(int id)
        => await _context.TestRuns.FindAsync(id);

    public async Task<List<Postgres.Entities.TestRun>> ListAsync(int take = 50)
        => await _context.TestRuns
            .OrderByDescending(r => r.Id)
            .Take(take)
            .ToListAsync();

    public async Task<Postgres.Entities.TestRun?> ReprocessAsync(int parentId, int fromStage)
    {
        if (fromStage < 1 || fromStage > 5) return null;
        var parent = await _context.TestRuns.FindAsync(parentId);
        if (parent == null) return null;

        var historyCopy = parent.PhaseHistory.ToArray();
        var separator = new TestRunPhaseLog
        {
            Stage = 0,
            PhaseName = $"— cloned from run #{parent.Id} at stage {fromStage} —",
            StartedAt = DateTimeOffset.UtcNow,
            FinishedAt = DateTimeOffset.UtcNow,
            Outcome = "OK"
        };

        var clone = new Postgres.Entities.TestRun
        {
            User = parent.User,
            PackProductId = parent.PackProductId,
            BundleProductId = parent.BundleProductId,
            PacksPerBundle = parent.PacksPerBundle,
            BundlesPerContainer = parent.BundlesPerContainer,
            FactoryUuid = parent.FactoryUuid,
            MarkingLineUuid = parent.MarkingLineUuid,
            LocationUuid = parent.LocationUuid,
            Stage = fromStage - 1,
            ClonedFromTestRunId = parent.Id,
            ClonedFromStage = fromStage,
            PhaseHistory = historyCopy.Append(separator).ToArray()
        };

        if (fromStage >= 2) { clone.PackEmissionGuid = parent.PackEmissionGuid; clone.PackCodes = parent.PackCodes; }
        if (fromStage >= 3) { clone.BundleEmissionGuid = parent.BundleEmissionGuid; clone.BundleCodes = parent.BundleCodes; }
        if (fromStage >= 4) { clone.MastercaseEmissionGuid = parent.MastercaseEmissionGuid; clone.MastercaseSscc = parent.MastercaseSscc; }
        if (fromStage >= 5) { clone.ApplicationGuid = parent.ApplicationGuid; }

        _context.TestRuns.Add(clone);
        await _context.SaveChangesAsync();

        object message = fromStage switch
        {
            1 => new TestRunStart { TestRunId = clone.Id },
            2 => new TestRunStage2Bundles { TestRunId = clone.Id },
            3 => new TestRunStage3Mastercase { TestRunId = clone.Id },
            4 => new TestRunStage4Application { TestRunId = clone.Id },
            5 => new TestRunStage5Aggregation { TestRunId = clone.Id },
            _ => throw new InvalidOperationException()
        };
        await _publish.Publish(message);
        _logger.LogInformation("TestRun {Id}: reprocess clone from #{Parent} at stage {Stage}", clone.Id, parent.Id, fromStage);
        return clone;
    }

    public async Task<Postgres.Entities.TestRun?> CancelAsync(int id)
    {
        var run = await _context.TestRuns.FindAsync(id);
        if (run == null) return null;
        run.Stage = -99;
        run.StatusMessage = "Cancelled by user";
        await _context.SaveChangesAsync();
        return run;
    }
}
