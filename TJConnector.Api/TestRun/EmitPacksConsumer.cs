using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Postgres;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Model.ExternalRequests.MarkingCode;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.TestRun;

public class EmitPacksConsumer : IConsumer<TestRunStart>
{
    private readonly ApplicationDbContext _context;
    private readonly IExternalEmission _emission;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmitPacksConsumer> _logger;

    public EmitPacksConsumer(
        ApplicationDbContext context,
        IExternalEmission emission,
        IConfiguration configuration,
        ILogger<EmitPacksConsumer> logger)
    {
        _context = context;
        _emission = emission;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TestRunStart> context)
    {
        var run = await _context.TestRuns.FindAsync(context.Message.TestRunId);
        if (run == null || run.Stage is < 0 or >= 100) return;

        run.Stage = 1;
        _logger.LogInformation("TestRun {Id}: stage 1 pack emission starting", run.Id);

        var product = await _context.Products.FindAsync(run.PackProductId);
        if (product == null)
        {
            await FailStage(run, 1, "Pack product not found");
            return;
        }

        var packFormat = _configuration.GetValue<int?>("TJConnection:EmissionCodeFormat:Pack");
        var createBody = TestRunBodyBuilder.BuildPackEmissionRequest(run, product.ExternalUid, packFormat);

        var createPhase = TestRunPhaseLogger.Begin(1, "Create pack emission");
        var createResp = await _emission.CreateCodeEmission(createBody);
        TestRunPhaseLogger.Complete(createPhase, createResp.Success ? "OK" : "FAIL", createBody, createResp);
        run.AppendPhaseLog(createPhase);
        if (!createResp.Success || createResp.Content?.uuid == null)
        {
            await FailStage(run, 1, createResp.Message ?? "Create emission failed");
            return;
        }
        run.PackEmissionGuid = createResp.Content.uuid;
        await _context.SaveChangesAsync();

        var processPhase = TestRunPhaseLogger.Begin(1, "Process pack emission");
        var processResp = await _emission.ProcessCodeEmission(new ProcessDocument { uuids = new[] { run.PackEmissionGuid.Value } });
        TestRunPhaseLogger.Complete(processPhase, processResp.Success ? "OK" : "FAIL", null, processResp);
        run.AppendPhaseLog(processPhase);
        if (!processResp.Success)
        {
            await FailStage(run, 1, processResp.Message ?? "Process emission failed");
            return;
        }

        for (int attempt = 1; attempt <= TestRunStatusContract.EmissionPollMaxAttempts; attempt++)
        {
            var pollPhase = TestRunPhaseLogger.Begin(1, $"Poll emission (attempt {attempt})");
            var info = await _emission.GetEmissionInfo(run.PackEmissionGuid.Value);
            TestRunPhaseLogger.Complete(pollPhase, info.Success ? "OK" : "FAIL", null, info);
            run.AppendPhaseLog(pollPhase);
            if (!info.Success)
            {
                await FailStage(run, 1, info.Message ?? "Poll emission failed");
                return;
            }
            var st = info.Content?.status;
            if (st.HasValue && TestRunStatusContract.EmissionReady.Contains(st.Value)) break;
            if (st.HasValue && TestRunStatusContract.EmissionFail.Contains(st.Value))
            {
                await FailStage(run, 1, $"Emission reached fail status {st}");
                return;
            }
            if (attempt == TestRunStatusContract.EmissionPollMaxAttempts)
            {
                await FailStage(run, 1, $"Emission did not reach ready status after {attempt} attempts (last status={st})");
                return;
            }
            await Task.Delay(TestRunStatusContract.EmissionPollDelayMs);
        }

        var downloadPhase = TestRunPhaseLogger.Begin(1, "Download pack codes");
        var codesResp = await _emission.GetCodesFromEmission(new DownloadCodesRequest { type = 0, uuid = run.PackEmissionGuid.Value });
        TestRunPhaseLogger.Complete(downloadPhase, codesResp.Success ? "OK" : "FAIL", null, codesResp);
        run.AppendPhaseLog(downloadPhase);
        if (!codesResp.Success || codesResp.Content?.codes == null)
        {
            await FailStage(run, 1, codesResp.Message ?? "Download codes failed");
            return;
        }
        run.PackCodes = codesResp.Content.codes;
        await _context.SaveChangesAsync();

        await context.Publish(new TestRunStage2Bundles { TestRunId = run.Id });
    }

    private async Task FailStage(Postgres.Entities.TestRun run, int stage, string message)
    {
        run.Stage = -stage;
        run.StatusMessage = message;
        _logger.LogError("TestRun {Id}: stage {Stage} failed — {Message}", run.Id, stage, message);
        await _context.SaveChangesAsync();
    }
}
