using TJConnector.Postgres.Entities;
using TJConnector.StateSystem.Helpers;
using TJConnector.StateSystem.Model.ExternalRequests.Container;
using TJConnector.StateSystem.Model.ExternalRequests.MarkingCode;

namespace TJConnector.Api.TestRun;

public static class TestRunBodyBuilder
{
    public static EmissionCreateRequest BuildPackEmissionRequest(Postgres.Entities.TestRun run, Guid packProductUuid, int? packFormat)
        => new()
        {
            codesCount = run.PacksPerBundle * run.BundlesPerContainer,
            productUuid = packProductUuid,
            factoryUuid = run.FactoryUuid,
            markingLineUuid = run.MarkingLineUuid,
            Type = 0,
            format = packFormat
        };

    public static EmissionCreateRequest BuildBundleEmissionRequest(Postgres.Entities.TestRun run, Guid bundleProductUuid, int? bundleFormat)
        => new()
        {
            codesCount = run.BundlesPerContainer,
            productUuid = bundleProductUuid,
            factoryUuid = run.FactoryUuid,
            markingLineUuid = run.MarkingLineUuid,
            Type = 1,
            format = bundleFormat
        };

    public static ContainerEmissionCreateRequest BuildMastercaseEmissionRequest()
        => new() { codesCount = 1, type = 0 };

    public static ApplicationCreateRequest BuildApplicationRequest(Postgres.Entities.TestRun run)
    {
        if (run.PackCodes == null || run.BundleCodes == null)
            throw new InvalidOperationException("Cannot build application body: pack or bundle codes missing from TestRun.");
        if (run.PackCodes.Length != run.PacksPerBundle * run.BundlesPerContainer)
            throw new InvalidOperationException($"Pack code count mismatch: expected {run.PacksPerBundle * run.BundlesPerContainer}, got {run.PackCodes.Length}.");
        if (run.BundleCodes.Length != run.BundlesPerContainer)
            throw new InvalidOperationException($"Bundle code count mismatch: expected {run.BundlesPerContainer}, got {run.BundleCodes.Length}.");

        var groupCodes = new List<GroupCode>(run.BundlesPerContainer);
        for (int i = 0; i < run.BundlesPerContainer; i++)
        {
            var bundleWithGs = GS1CodeHelper.TryInsertGroupSeparator(run.BundleCodes[i], out var gs) ? gs : run.BundleCodes[i];
            var packsForBundle = new string[run.PacksPerBundle];
            Array.Copy(run.PackCodes, i * run.PacksPerBundle, packsForBundle, 0, run.PacksPerBundle);
            groupCodes.Add(new GroupCode
            {
                groupCode = bundleWithGs,
                codes = packsForBundle
            });
        }

        var now = DateTimeOffset.UtcNow;
        return new ApplicationCreateRequest
        {
            applicationDate = now,
            productionDate = now,
            factoryUuid = run.FactoryUuid,
            markingLineUuid = run.MarkingLineUuid,
            locationUuid = run.LocationUuid,
            groupCodes = groupCodes,
            result = 0,
            type = 2
        };
    }

    public static ContainerOperationCreateRequest BuildAggregationRequest(Postgres.Entities.TestRun run)
    {
        if (run.BundleCodes == null)
            throw new InvalidOperationException("Cannot build aggregation body: bundle codes missing.");
        if (string.IsNullOrEmpty(run.MastercaseSscc))
            throw new InvalidOperationException("Cannot build aggregation body: mastercase SSCC missing.");

        var codes = run.BundleCodes
            .Select(b => GS1CodeHelper.TryInsertGroupSeparator(b, out var gs) ? gs : b)
            .ToArray();

        return new ContainerOperationCreateRequest
        {
            codes = codes,
            containerCode = run.MastercaseSscc,
            locationUuid = run.LocationUuid,
            transferCodes = Array.Empty<string>(),
            type = 0
        };
    }
}
