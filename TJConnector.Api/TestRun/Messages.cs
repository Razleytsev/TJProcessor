namespace TJConnector.Api.TestRun;

public record TestRunStart { public int TestRunId { get; init; } }
public record TestRunStage2Bundles { public int TestRunId { get; init; } }
public record TestRunStage3Mastercase { public int TestRunId { get; init; } }
public record TestRunStage4Application { public int TestRunId { get; init; } }
public record TestRunStage5Aggregation { public int TestRunId { get; init; } }
