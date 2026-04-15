namespace TJConnector.Api.TestRun;

public static class TestRunStatusContract
{
    public const int EmissionAfterCreate = 1;
    public const int EmissionReady = 6;
    public const int EmissionPollMaxAttempts = 5;
    public const int EmissionPollDelayMs = 2000;

    public const int ApplicationApproved = 1;
    public const int ApplicationProcessing = 3;
    public const int ApplicationReady = 5;
    public static readonly int[] ApplicationFail = { 0, 2, 4 };

    public const int AggregationApproved = 1;
    public const int AggregationProcessing = 3;
    public const int AggregationTerminal = 5;
    public static readonly int[] AggregationFail = { 0, 2, 4 };

    public const int AppAggPollMaxAttempts = 20;
    public const int AppAggPollBaseDelayMs = 6000;
    public const int AppAggPollMaxDelayMs = 60000;

    public static int AppAggDelay(int attempt)
        => Math.Min(attempt * AppAggPollBaseDelayMs, AppAggPollMaxDelayMs);
}
