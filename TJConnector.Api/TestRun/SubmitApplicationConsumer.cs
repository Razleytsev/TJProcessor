using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Postgres;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.TestRun;

public class SubmitApplicationConsumer : IConsumer<TestRunStage4Application>
{
    private readonly ApplicationDbContext _context;
    private readonly IExternalEmission _emission;
    private readonly ILogger<SubmitApplicationConsumer> _logger;

    public SubmitApplicationConsumer(
        ApplicationDbContext context,
        IExternalEmission emission,
        ILogger<SubmitApplicationConsumer> logger)
    {
        _context = context;
        _emission = emission;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TestRunStage4Application> context)
    {
        var run = await _context.TestRuns.FindAsync(context.Message.TestRunId);
        if (run == null || run.Stage is < 0 or >= 100) return;

        run.Stage = 4;
        _logger.LogInformation("TestRun {Id}: stage 4 application starting", run.Id);

        TJConnector.StateSystem.Model.ExternalRequests.MarkingCode.ApplicationCreateRequest createBody;
        try
        {
            createBody = TestRunBodyBuilder.BuildApplicationRequest(run);
        }
        catch (Exception ex)
        {
            await FailStage(run, 4, $"Failed to build application body: {ex.Message}");
            return;
        }

        var createPhase = TestRunPhaseLogger.Begin(4, "Create application");
        var createResp = await _emission.CreateCodeApplication(createBody);
        TestRunPhaseLogger.Complete(createPhase, createResp.Success ? "OK" : "FAIL", createBody, createResp);
        run.AppendPhaseLog(createPhase);
        if (!createResp.Success || createResp.Content?.uuid == null)
        {
            await FailStage(run, 4, createResp.Message ?? "Create application failed");
            return;
        }
        run.ApplicationGuid = createResp.Content.uuid;
        await _context.SaveChangesAsync();

        bool processCalled = false;
        for (int attempt = 1; attempt <= TestRunStatusContract.AppAggPollMaxAttempts; attempt++)
        {
            var pollPhase = TestRunPhaseLogger.Begin(4, $"Poll application (attempt {attempt})");
            var info = await _emission.GetCodeApplicationInfo(run.ApplicationGuid.Value);
            TestRunPhaseLogger.Complete(pollPhase, info.Success ? "OK" : "FAIL", null, info);
            run.AppendPhaseLog(pollPhase);
            if (!info.Success)
            {
                await FailStage(run, 4, info.Message ?? "Poll application failed");
                return;
            }

            var status = info.Content?.status;
            if (status == TestRunStatusContract.ApplicationTerminal) break;

            if (status == TestRunStatusContract.ApplicationApproved && !processCalled)
            {
                var processPhase = TestRunPhaseLogger.Begin(4, "Process application");
                var processResp = await _emission.ProcessCodeApplication(new ProcessDocument { uuids = new[] { run.ApplicationGuid.Value } });
                TestRunPhaseLogger.Complete(processPhase, processResp.Success ? "OK" : "FAIL", null, processResp);
                run.AppendPhaseLog(processPhase);
                if (!processResp.Success)
                {
                    await FailStage(run, 4, processResp.Message ?? "Process application failed");
                    return;
                }
                processCalled = true;
            }
            else if (status.HasValue && TestRunStatusContract.ApplicationFail.Contains(status.Value))
            {
                await FailStage(run, 4, $"Application reached fail status {status}");
                return;
            }

            if (attempt == TestRunStatusContract.AppAggPollMaxAttempts)
            {
                await FailStage(run, 4, $"Application did not reach ready status after {attempt} attempts (last status={status})");
                return;
            }
            await Task.Delay(TestRunStatusContract.AppAggDelay(attempt));
        }

        await _context.SaveChangesAsync();
        await context.Publish(new TestRunStage5Aggregation { TestRunId = run.Id });
    }

    private async Task FailStage(Postgres.Entities.TestRun run, int stage, string message)
    {
        run.Stage = -stage;
        run.StatusMessage = message;
        _logger.LogError("TestRun {Id}: stage {Stage} failed — {Message}", run.Id, stage, message);
        await _context.SaveChangesAsync();
    }
}
