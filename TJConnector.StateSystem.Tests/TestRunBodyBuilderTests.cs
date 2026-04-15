using TJConnector.Api.TestRun;
using TJConnector.Postgres.Entities;
using Xunit;

namespace TJConnector.StateSystem.Tests;

public class TestRunBodyBuilderTests
{
    private static TestRun MakeRun(int packsPerBundle, int bundlesPerContainer, string[]? packCodes = null, string[]? bundleCodes = null, string? sscc = null)
        => new()
        {
            Id = 1,
            PacksPerBundle = packsPerBundle,
            BundlesPerContainer = bundlesPerContainer,
            FactoryUuid = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MarkingLineUuid = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            LocationUuid = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            PackCodes = packCodes,
            BundleCodes = bundleCodes,
            MastercaseSscc = sscc
        };

    [Fact]
    public void PackEmissionRequest_UsesProductCountMultiplication()
    {
        var run = MakeRun(3, 2);
        var productUuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var body = TestRunBodyBuilder.BuildPackEmissionRequest(run, productUuid, 0);

        Assert.Equal(6, body.codesCount);
        Assert.Equal(productUuid, body.productUuid);
        Assert.Equal(run.FactoryUuid, body.factoryUuid);
        Assert.Equal(run.MarkingLineUuid, body.markingLineUuid);
        Assert.Equal((sbyte)0, body.Type);
        Assert.Equal(0, body.format);
    }

    [Fact]
    public void BundleEmissionRequest_UsesBundleCountOnly()
    {
        var run = MakeRun(3, 2);
        var productUuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var body = TestRunBodyBuilder.BuildBundleEmissionRequest(run, productUuid, 1);

        Assert.Equal(2, body.codesCount);
        Assert.Equal((sbyte)1, body.Type);
        Assert.Equal(1, body.format);
    }

    [Fact]
    public void MastercaseEmissionRequest_IsMinimal()
    {
        var body = TestRunBodyBuilder.BuildMastercaseEmissionRequest();

        Assert.Equal(1, body.codesCount);
        Assert.Equal((sbyte)0, body.type);
    }

    [Fact]
    public void ApplicationRequest_PairsPacksToBundlesPositionally()
    {
        var packCodes = new[]
        {
            "010460020000000021AAAAAAAA93aa11",
            "010460020000000021BBBBBBBB93bb22",
            "010460020000000021CCCCCCCC93cc33",
            "010460020000000021DDDDDDDD93dd44",
            "010460020000000021EEEEEEEE93ee55",
            "010460020000000021FFFFFFFF93ff66"
        };
        var bundleCodes = new[]
        {
            "010460020000000021GGGGGGGG93gg77",
            "010460020000000021HHHHHHHH93hh88"
        };
        var run = MakeRun(3, 2, packCodes, bundleCodes);

        var body = TestRunBodyBuilder.BuildApplicationRequest(run);

        Assert.Equal(2, body.groupCodes.Count);
        Assert.Equal(3, body.groupCodes[0].codes.Length);
        Assert.Equal(3, body.groupCodes[1].codes.Length);
        Assert.Equal(packCodes[0], body.groupCodes[0].codes[0]);
        Assert.Equal(packCodes[2], body.groupCodes[0].codes[2]);
        Assert.Equal(packCodes[3], body.groupCodes[1].codes[0]);
        Assert.Equal(packCodes[5], body.groupCodes[1].codes[2]);
    }

    [Fact]
    public void ApplicationRequest_InsertsGSOnBundleGroupCodes()
    {
        var run = MakeRun(1, 1,
            packCodes: new[] { "010460020000000021PACKPK0093pk0001" },
            bundleCodes: new[] { "010460020000000021BUNDBD0093bd0001" });

        var body = TestRunBodyBuilder.BuildApplicationRequest(run);

        Assert.Contains("\u001d93", body.groupCodes[0].groupCode);
    }

    [Fact]
    public void ApplicationRequest_ThrowsOnMissingCodes()
    {
        var run = MakeRun(1, 1);
        Assert.Throws<InvalidOperationException>(() => TestRunBodyBuilder.BuildApplicationRequest(run));
    }

    [Fact]
    public void ApplicationRequest_ThrowsOnCountMismatch()
    {
        var run = MakeRun(3, 2,
            packCodes: new[] { "code1", "code2" }, // wrong length
            bundleCodes: new[] { "010460020000000021BB1234BB93bb1234", "010460020000000021CC1234CC93cc1234" });
        Assert.Throws<InvalidOperationException>(() => TestRunBodyBuilder.BuildApplicationRequest(run));
    }

    [Fact]
    public void AggregationRequest_UsesBundleCodesAndMastercaseSscc()
    {
        var run = MakeRun(1, 2,
            bundleCodes: new[] { "010460020000000021BUND0001A93b001", "010460020000000021BUND0002B93b002" },
            sscc: "00000046274038123456");

        var body = TestRunBodyBuilder.BuildAggregationRequest(run);

        Assert.Equal(2, body.codes.Length);
        Assert.Equal("00000046274038123456", body.containerCode);
        Assert.Equal(run.LocationUuid, body.locationUuid);
        Assert.Empty(body.transferCodes);
        Assert.Equal((sbyte)0, body.type);
        Assert.All(body.codes, c => Assert.Contains("\u001d93", c));
    }

    [Fact]
    public void AggregationRequest_ThrowsOnMissingSscc()
    {
        var run = MakeRun(1, 1, bundleCodes: new[] { "bundle" });
        Assert.Throws<InvalidOperationException>(() => TestRunBodyBuilder.BuildAggregationRequest(run));
    }
}
