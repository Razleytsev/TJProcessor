using Newtonsoft.Json;
using TJConnector.Postgres.Entities;

namespace TJConnector.Api.TestRun;

public static class TestRunPhaseLogger
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.None
    };

    public static TestRunPhaseLog Begin(int stage, string phaseName) => new()
    {
        Stage = stage,
        PhaseName = phaseName,
        StartedAt = DateTimeOffset.UtcNow,
        Outcome = "IN_PROGRESS"
    };

    public static void Complete(TestRunPhaseLog entry, string outcome, object? request = null, object? response = null, string? notes = null)
    {
        entry.FinishedAt = DateTimeOffset.UtcNow;
        entry.Outcome = outcome;
        if (request != null) entry.ExternalRequestJson = JsonConvert.SerializeObject(request, JsonSettings);
        if (response != null) entry.ExternalResponseJson = JsonConvert.SerializeObject(response, JsonSettings);
        if (notes != null) entry.Notes = notes;
    }
}
