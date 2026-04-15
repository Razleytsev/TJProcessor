namespace TJConnector.StateSystem.Helpers;

public static class GS1CodeHelper
{
    private const char GS = '\u001d';

    /// <summary>
    /// Removes every GS (0x1D) character from the code. Safe no-op on null or empty.
    /// </summary>
    public static string StripGroupSeparators(string code)
        => string.IsNullOrEmpty(code) ? code : code.Replace("\u001d", string.Empty);
}
