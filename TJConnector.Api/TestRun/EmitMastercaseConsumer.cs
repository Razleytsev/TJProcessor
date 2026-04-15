using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Postgres;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Model.ExternalRequests.MarkingCode;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.TestRun;

public class EmitMastercaseConsumer : IConsumer<TestRunStage3Mastercase>
{
    private readonly ApplicationDbContext _context;
    private readonly IExternalEmission _emission;
    private readonly ILogger<EmitMastercaseConsumer> _logger;

    public EmitMastercaseConsumer(
        ApplicationDbContext context,
        IExternalEmission emission,
        ILogger<EmitMastercaseConsumer> logger)
    {
        _context = context;
        _emission = emission;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TestRunStage3Mastercase> context)
    {
        var run = await _context.TestRuns.FindAsync(context.Message.TestRunId);
        if (run == null || run.Stage is < 0 or >= 100) return;

        run.Stage = 3;
        _logger.LogInformation("TestRun {Id}: stage 3 mastercase emission starting", run.Id);

        var createBody = TestRunBodyBuilder.BuildMastercaseEmissionRequest();

        var createPhase = TestRunPhaseLogger.Begin(3, "Create mastercase emission (minimal)");
        var createResp = await _emission.CreateContainerEmissionMinimal(createBody);
        TestRunPhaseLogger.Complete(createPhase, createResp.Success ? "OK" : "FAIL", createBody, createResp);
        run.AppendPhaseLog(createPhase);
        if (!createResp.Success || createResp.Content?.uuid == null)
        {
            await FailStage(run, 3, createResp.Message ?? "Create container emission failed");
            return;
        }
        run.MastercaseEmissionGuid = createResp.Content.uuid;
        await _context.SaveChangesAsync();

        var processPhase = TestRunPhaseLogger.Begin(3, "Process mastercase emission");
        var processResp = await _emission.ProcessContainerEmission(new ProcessDocument { uuids = new[] { run.MastercaseEmissionGuid.Value } });
        TestRunPhaseLogger.Complete(processPhase, processResp.Success ? "OK" : "FAIL", null, processResp);
        run.AppendPhaseLog(processPhase);
        if (!processResp.Success)
        {
            await FailStage(run, 3, processResp.Message ?? "Process container emission failed");
            return;
        }

        for (int attempt = 1; attempt <= TestRunStatusContract.EmissionPollMaxAttempts; attempt++)
        {
            var pollPhase = TestRunPhaseLogger.Begin(3, $"Poll mastercase emission (attempt {attempt})");
            var info = await _emission.GetContainerEmissionInfo(run.MastercaseEmissionGuid.Value);
            TestRunPhaseLogger.Complete(pollPhase, info.Success ? "OK" : "FAIL", null, info);
            run.AppendPhaseLog(pollPhase);
            if (!info.Success)
            {
                await FailStage(run, 3, info.Message ?? "Poll container emission failed");
                return;
            }
            if (info.Content?.status == TestRunStatusContract.EmissionReady) break;
            if (attempt == TestRunStatusContract.EmissionPollMaxAttempts)
            {
                await FailStage(run, 3, $"Container emission did not reach status {TestRunStatusContract.EmissionReady} after {attempt} attempts (last status={info.Content?.status})");
                return;
            }
            await Task.Delay(TestRunStatusContract.EmissionPollDelayMs);
        }

        var downloadPhase = TestRunPhaseLogger.Begin(3, "Download mastercase SSCC");
        var codesResp = await _emission.GetCodesFromContainerEmission(new DownloadCodesRequest { type = 0, uuid = run.MastercaseEmissionGuid.Value });
        TestRunPhaseLogger.Complete(downloadPhase, codesResp.Success ? "OK" : "FAIL", null, codesResp);
        run.AppendPhaseLog(downloadPhase);
        if (!codesResp.Success || codesResp.Content?.codes == null || codesResp.Content.codes.Length == 0)
        {
            await FailStage(run, 3, codesResp.Message ?? "Download SSCC failed or returned empty");
            return;
        }
        run.MastercaseSscc = codesResp.Content.codes[0];
        await _context.SaveChangesAsync();

        await context.Publish(new TestRunStage4Application { TestRunId = run.Id });
    }

    private async Task FailStage(Postgres.Entities.TestRun run, int stage, string message)
    {
        run.Stage = -stage;
        run.StatusMessage = message;
        _logger.LogError("TestRun {Id}: stage {Stage} failed — {Message}", run.Id, stage, message);
        await _context.SaveChangesAsync();
    }
}
