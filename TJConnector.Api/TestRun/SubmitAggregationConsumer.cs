using MassTransit;
using Microsoft.EntityFrameworkCore;
using TJConnector.Postgres;
using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.Api.TestRun;

public class SubmitAggregationConsumer : IConsumer<TestRunStage5Aggregation>
{
    private readonly ApplicationDbContext _context;
    private readonly IExternalContainer _container;
    private readonly ILogger<SubmitAggregationConsumer> _logger;

    public SubmitAggregationConsumer(
        ApplicationDbContext context,
        IExternalContainer container,
        ILogger<SubmitAggregationConsumer> logger)
    {
        _context = context;
        _container = container;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TestRunStage5Aggregation> context)
    {
        var run = await _context.TestRuns.FindAsync(context.Message.TestRunId);
        if (run == null || run.Stage is < 0 or >= 100) return;

        run.Stage = 5;
        _logger.LogInformation("TestRun {Id}: stage 5 aggregation starting", run.Id);

        TJConnector.StateSystem.Model.ExternalRequests.Container.ContainerOperationCreateRequest createBody;
        try
        {
            createBody = TestRunBodyBuilder.BuildAggregationRequest(run);
        }
        catch (Exception ex)
        {
            await FailStage(run, 5, $"Failed to build aggregation body: {ex.Message}");
            return;
        }

        var createPhase = TestRunPhaseLogger.Begin(5, "Create aggregation");
        var createResp = await _container.ContainerOperation(createBody);
        TestRunPhaseLogger.Complete(createPhase, createResp.Success ? "OK" : "FAIL", createBody, createResp);
        run.AppendPhaseLog(createPhase);
        if (!createResp.Success || createResp.Content?.uuid == null)
        {
            await FailStage(run, 5, createResp.Message ?? "Create aggregation failed");
            return;
        }
        run.AggregationGuid = createResp.Content.uuid;
        await _context.SaveChangesAsync();

        bool processCalled = false;
        for (int attempt = 1; attempt <= TestRunStatusContract.AppAggPollMaxAttempts; attempt++)
        {
            var pollPhase = TestRunPhaseLogger.Begin(5, $"Poll aggregation (attempt {attempt})");
            var info = await _container.ContainerOperationCheck(run.AggregationGuid.Value);
            TestRunPhaseLogger.Complete(pollPhase, info.Success ? "OK" : "FAIL", null, info);
            run.AppendPhaseLog(pollPhase);
            if (!info.Success)
            {
                await FailStage(run, 5, info.Message ?? "Poll aggregation failed");
                return;
            }

            var status = info.Content?.status;
            if (status == TestRunStatusContract.AggregationTerminal) break;

            if (status == TestRunStatusContract.AggregationApproved && !processCalled)
            {
                var processPhase = TestRunPhaseLogger.Begin(5, "Process aggregation");
                var processResp = await _container.ContainerOperationProcess(new ProcessDocument { uuids = new[] { run.AggregationGuid.Value } });
                TestRunPhaseLogger.Complete(processPhase, processResp.Success ? "OK" : "FAIL", null, processResp);
                run.AppendPhaseLog(processPhase);
                if (!processResp.Success)
                {
                    await FailStage(run, 5, processResp.Message ?? "Process aggregation failed");
                    return;
                }
                processCalled = true;
            }
            else if (status.HasValue && TestRunStatusContract.AggregationFail.Contains(status.Value))
            {
                await FailStage(run, 5, $"Aggregation reached fail status {status}");
                return;
            }

            if (attempt == TestRunStatusContract.AppAggPollMaxAttempts)
            {
                await FailStage(run, 5, $"Aggregation did not reach status {TestRunStatusContract.AggregationTerminal} after {attempt} attempts (last status={status})");
                return;
            }
            await Task.Delay(TestRunStatusContract.AppAggDelay(attempt));
        }

        run.Stage = 100;
        run.StatusMessage = "Done";
        await _context.SaveChangesAsync();
        _logger.LogInformation("TestRun {Id}: done", run.Id);
    }

    private async Task FailStage(Postgres.Entities.TestRun run, int stage, string message)
    {
        run.Stage = -stage;
        run.StatusMessage = message;
        _logger.LogError("TestRun {Id}: stage {Stage} failed — {Message}", run.Id, stage, message);
        await _context.SaveChangesAsync();
    }
}
