using TJConnector.Postgres.Entities;

namespace TJConnector.SharedLibrary.Models;

public class TestRunDto
{
    public int Id { get; set; }
    public DateTimeOffset RecordDate { get; set; }
    public string? User { get; set; }

    public int PackProductId { get; set; }
    public string PackGtin { get; set; } = string.Empty;
    public int BundleProductId { get; set; }
    public string BundleGtin { get; set; } = string.Empty;
    public int PacksPerBundle { get; set; }
    public int BundlesPerContainer { get; set; }

    public int Stage { get; set; }
    public string StageLabel { get; set; } = string.Empty;
    public string? StatusMessage { get; set; }

    public TestRunPhaseLog[] PhaseHistory { get; set; } = Array.Empty<TestRunPhaseLog>();

    public Guid? PackEmissionGuid { get; set; }
    public Guid? BundleEmissionGuid { get; set; }
    public Guid? MastercaseEmissionGuid { get; set; }
    public string[]? PackCodes { get; set; }
    public string[]? BundleCodes { get; set; }
    public string? MastercaseSscc { get; set; }
    public Guid? ApplicationGuid { get; set; }
    public Guid? AggregationGuid { get; set; }

    public int? ClonedFromTestRunId { get; set; }
    public int? ClonedFromStage { get; set; }

    public static TestRunDto From(TestRun run, string packGtin, string bundleGtin) => new()
    {
        Id = run.Id,
        RecordDate = run.RecordDate,
        User = run.User,
        PackProductId = run.PackProductId,
        PackGtin = packGtin,
        BundleProductId = run.BundleProductId,
        BundleGtin = bundleGtin,
        PacksPerBundle = run.PacksPerBundle,
        BundlesPerContainer = run.BundlesPerContainer,
        Stage = run.Stage,
        StageLabel = MapStageLabel(run.Stage),
        StatusMessage = run.StatusMessage,
        PhaseHistory = run.PhaseHistory,
        PackEmissionGuid = run.PackEmissionGuid,
        BundleEmissionGuid = run.BundleEmissionGuid,
        MastercaseEmissionGuid = run.MastercaseEmissionGuid,
        PackCodes = run.PackCodes,
        BundleCodes = run.BundleCodes,
        MastercaseSscc = run.MastercaseSscc,
        ApplicationGuid = run.ApplicationGuid,
        AggregationGuid = run.AggregationGuid,
        ClonedFromTestRunId = run.ClonedFromTestRunId,
        ClonedFromStage = run.ClonedFromStage
    };

    private static string MapStageLabel(int stage) => stage switch
    {
        0 => "Created",
        1 => "Pack emission",
        2 => "Bundle emission",
        3 => "Mastercase emission",
        4 => "Application",
        5 => "Aggregation",
        100 => "Done",
        -1 => "Failed at pack emission",
        -2 => "Failed at bundle emission",
        -3 => "Failed at mastercase emission",
        -4 => "Failed at application",
        -5 => "Failed at aggregation",
        -99 => "Cancelled",
        _ => "Unknown"
    };
}
