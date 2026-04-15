using TJConnector.StateSystem.Helpers;
using Xunit;

namespace TJConnector.StateSystem.Tests;

public class GS1CodeHelperTests
{
    // ── StripGroupSeparators ────────────────────────────────────────────

    [Fact]
    public void Strip_NullInput_ReturnsNull()
    {
        Assert.Null(GS1CodeHelper.StripGroupSeparators(null!));
    }

    [Fact]
    public void Strip_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, GS1CodeHelper.StripGroupSeparators(string.Empty));
    }

    [Fact]
    public void Strip_NoGS_ReturnsInputUnchanged()
    {
        const string input = "0104600200000000214567890193abcd";
        Assert.Equal(input, GS1CodeHelper.StripGroupSeparators(input));
    }

    [Fact]
    public void Strip_SingleGS_RemovesIt()
    {
        const string input  = "010460020000000021+UyLGXyZ\u001d93444b155b";
        const string expect = "010460020000000021+UyLGXyZ93444b155b";
        Assert.Equal(expect, GS1CodeHelper.StripGroupSeparators(input));
    }

    [Fact]
    public void Strip_MultipleGS_RemovesAll()
    {
        const string input  = "abc\u001ddef\u001dghi";
        const string expect = "abcdefghi";
        Assert.Equal(expect, GS1CodeHelper.StripGroupSeparators(input));
    }
}
