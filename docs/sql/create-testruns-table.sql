-- Create the TestRuns table for the TestRun harness feature.
-- Run this manually against your PostgreSQL database before deploying,
-- because EnsureCreated() does NOT add new tables to an existing database.

CREATE TABLE IF NOT EXISTS "TestRuns" (
    "Id"                      SERIAL PRIMARY KEY,
    "RecordDate"              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "User"                    VARCHAR(50),
    "PackProductId"           INTEGER NOT NULL,
    "BundleProductId"         INTEGER NOT NULL,
    "PacksPerBundle"          INTEGER NOT NULL,
    "BundlesPerContainer"     INTEGER NOT NULL,
    "FactoryUuid"             UUID NOT NULL,
    "MarkingLineUuid"         UUID NOT NULL,
    "LocationUuid"            UUID NOT NULL,
    "Stage"                   INTEGER NOT NULL,
    "StatusMessage"           VARCHAR(1000),
    "PhaseHistory"            JSONB,
    "PackEmissionGuid"        UUID,
    "BundleEmissionGuid"      UUID,
    "MastercaseEmissionGuid"  UUID,
    "PackCodes"               JSONB,
    "BundleCodes"             JSONB,
    "MastercaseSscc"          VARCHAR(200),
    "ApplicationGuid"         UUID,
    "AggregationGuid"         UUID,
    "ClonedFromTestRunId"     INTEGER,
    "ClonedFromStage"         INTEGER
);
