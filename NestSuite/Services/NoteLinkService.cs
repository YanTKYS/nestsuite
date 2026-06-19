using System.Text.RegularExpressions;

namespace NoteNest.Services;

public static class NoteLinkService
{
    private static readonly Regex LinkPattern = new(@"\[\[([^\[\]]+)\]\]", RegexOptions.Compiled);

    // Returns the note title inside [[...]] if the caret is within a link span, else null.
    public static string? ExtractLinkAtCursor(string text, int caretIndex)
    {
        foreach (Match m in LinkPattern.Matches(text))
        {
            if (caretIndex >= m.Index && caretIndex < m.Index + m.Length)
                return m.Groups[1].Value;
        }
        return null;
    }

    public static IEnumerable<string> ExtractAllLinks(string text)
    {
        foreach (Match m in LinkPattern.Matches(text))
            yield return m.Groups[1].Value;
    }
}
