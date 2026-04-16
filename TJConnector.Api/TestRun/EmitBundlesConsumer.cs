using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Postgres;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Model.ExternalRequests.MarkingCode;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.TestRun;

public class EmitBundlesConsumer : IConsumer<TestRunStage2Bundles>
{
    private readonly ApplicationDbContext _context;
    private readonly IExternalEmission _emission;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmitBundlesConsumer> _logger;

    public EmitBundlesConsumer(
        ApplicationDbContext context,
        IExternalEmission emission,
        IConfiguration configuration,
        ILogger<EmitBundlesConsumer> logger)
    {
        _context = context;
        _emission = emission;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TestRunStage2Bundles> context)
    {
        var run = await _context.TestRuns.FindAsync(context.Message.TestRunId);
        if (run == null || run.Stage is < 0 or >= 100) return;

        run.Stage = 2;
        _logger.LogInformation("TestRun {Id}: stage 2 bundle emission starting", run.Id);

        var product = await _context.Products.FindAsync(run.BundleProductId);
        if (product == null)
        {
            await FailStage(run, 2, "Bundle product not found");
            return;
        }

        var bundleFormat = _configuration.GetValue<int?>("TJConnection:EmissionCodeFormat:Bundle");
        var createBody = TestRunBodyBuilder.BuildBundleEmissionRequest(run, product.ExternalUid, bundleFormat);

        var createPhase = TestRunPhaseLogger.Begin(2, "Create bundle emission");
        var createResp = await _emission.CreateCodeEmission(createBody);
        TestRunPhaseLogger.Complete(createPhase, createResp.Success ? "OK" : "FAIL", createBody, createResp);
        run.AppendPhaseLog(createPhase);
        if (!createResp.Success || createResp.Content?.uuid == null)
        {
            await FailStage(run, 2, createResp.Message ?? "Create emission failed");
            return;
        }
        run.BundleEmissionGuid = createResp.Content.uuid;
        await _context.SaveChangesAsync();

        var processPhase = TestRunPhaseLogger.Begin(2, "Process bundle emission");
        var processResp = await _emission.ProcessCodeEmission(new ProcessDocument { uuids = new[] { run.BundleEmissionGuid.Value } });
        TestRunPhaseLogger.Complete(processPhase, processResp.Success ? "OK" : "FAIL", null, processResp);
        run.AppendPhaseLog(processPhase);
        if (!processResp.Success)
        {
            await FailStage(run, 2, processResp.Message ?? "Process emission failed");
            return;
        }

        for (int attempt = 1; attempt <= TestRunStatusContract.EmissionPollMaxAttempts; attempt++)
        {
            var pollPhase = TestRunPhaseLogger.Begin(2, $"Poll emission (attempt {attempt})");
            var info = await _emission.GetEmissionInfo(run.BundleEmissionGuid.Value);
            TestRunPhaseLogger.Complete(pollPhase, info.Success ? "OK" : "FAIL", null, info);
            run.AppendPhaseLog(pollPhase);
            if (!info.Success)
            {
                await FailStage(run, 2, info.Message ?? "Poll emission failed");
                return;
            }
            if (info.Content?.status == TestRunStatusContract.EmissionReady) break;
            if (attempt == TestRunStatusContract.EmissionPollMaxAttempts)
            {
                await FailStage(run, 2, $"Emission did not reach status {TestRunStatusContract.EmissionReady} after {attempt} attempts (last status={info.Content?.status})");
                return;
            }
            await Task.Delay(TestRunStatusContract.EmissionPollDelayMs);
        }

        var downloadPhase = TestRunPhaseLogger.Begin(2, "Download bundle codes");
        var codesResp = await _emission.GetCodesFromEmission(new DownloadCodesRequest { type = 1, uuid = run.BundleEmissionGuid.Value });
        TestRunPhaseLogger.Complete(downloadPhase, codesResp.Success ? "OK" : "FAIL", null, codesResp);
        run.AppendPhaseLog(downloadPhase);
        if (!codesResp.Success || codesResp.Content?.codes == null)
        {
            await FailStage(run, 2, codesResp.Message ?? "Download codes failed");
            return;
        }
        run.BundleCodes = codesResp.Content.codes;
        await _context.SaveChangesAsync();

        await context.Publish(new TestRunStage3Mastercase { TestRunId = run.Id });
    }

    private async Task FailStage(Postgres.Entities.TestRun run, int stage, string message)
    {
        run.Stage = -stage;
        run.StatusMessage = message;
        _logger.LogError("TestRun {Id}: stage {Stage} failed — {Message}", run.Id, stage, message);
        await _context.SaveChangesAsync();
    }
}
