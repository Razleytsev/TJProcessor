namespace TJConnector.Postgres.Entities
{
    public class TestRun
    {
        public int Id { get; set; }
        public DateTimeOffset RecordDate { get; set; }
        public string? User { get; set; }

        public int PackProductId { get; set; }
        public int BundleProductId { get; set; }
        public int PacksPerBundle { get; set; }
        public int BundlesPerContainer { get; set; }
        public Guid FactoryUuid { get; set; }
        public Guid MarkingLineUuid { get; set; }
        public Guid LocationUuid { get; set; }

        public int Stage { get; set; }
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

        public void AppendPhaseLog(TestRunPhaseLog entry)
            => PhaseHistory = PhaseHistory.Append(entry).ToArray();
    }

    public class TestRunPhaseLog
    {
        public int Stage { get; set; }
        public string PhaseName { get; set; } = string.Empty;
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? FinishedAt { get; set; }
        public string Outcome { get; set; } = "IN_PROGRESS";
        public string? ExternalRequestJson { get; set; }
        public string? ExternalResponseJson { get; set; }
        public string? Notes { get; set; }
    }
}
