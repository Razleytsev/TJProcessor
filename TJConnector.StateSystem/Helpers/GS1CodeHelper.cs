namespace TJConnector.StateSystem.Helpers;

public static class GS1CodeHelper
{
    private const char GS = '\u001d';

    /// <summary>
    /// Removes every GS (0x1D) character from the code. Safe no-op on null or empty.
    /// </summary>
    public static string StripGroupSeparators(string code)
        => string.IsNullOrEmpty(code) ? code : code.Replace("\u001d", string.Empty);

    /// <summary>
    /// Inserts a GS (0x1D) before the "93" AI in a GS1 pack code.
    /// Expected input (without GS): 01&lt;14-digit GTIN&gt;21&lt;8-char serial&gt;93&lt;rest&gt;
    /// Fixed prefix length before "93" = 2 + 14 + 2 + 8 = 26.
    /// Returns true on success (or idempotently when the code already contains a GS).
    /// Returns false when the input doesn't match the expected shape — caller must log and abort, not send.
    /// </summary>
    public static bool TryInsertGroupSeparator(string code, out string result)
    {
        result = code;
        if (string.IsNullOrEmpty(code)) return false;
        if (code.Contains(GS)) return true;
        if (code.Length < 28) return false;
        if (code[26] != '9' || code[27] != '3') return false;
        result = code.Insert(26, "\u001d");
        return true;
    }
}
