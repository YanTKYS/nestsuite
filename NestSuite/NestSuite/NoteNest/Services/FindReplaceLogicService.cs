using System.Text.RegularExpressions;

namespace NestSuite.Services;

public sealed record SearchMatchSegments(string Before, string Match, string After)
{
    public bool HasMatch => Match.Length > 0;

    public static SearchMatchSegments Split(
        string? text,
        string? query,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        var source = text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(query))
            return new SearchMatchSegments(source, string.Empty, string.Empty);

        var index = source.IndexOf(query, comparison);
        if (index < 0)
            return new SearchMatchSegments(source, string.Empty, string.Empty);

        return new SearchMatchSegments(
            source[..index],
            source.Substring(index, query.Length),
            source[(index + query.Length)..]);
    }
}

public static class FindReplaceLogicService
{
    // Returns all start indices of keyword in text using the given comparison.
    // Advances by 1 after each match to allow overlapping matches.
    public static IReadOnlyList<int> ComputeMatchPositions(string keyword, string text, StringComparison comparison)
    {
        var positions = new List<int>();
        if (string.IsNullOrEmpty(keyword)) return positions;
        int pos = 0;
        while (pos < text.Length)
        {
            var idx = text.IndexOf(keyword, pos, comparison);
            if (idx < 0) break;
            positions.Add(idx);
            pos = idx + 1;
        }
        return positions;
    }

    // Advances forward from currentIndex in count items, wrapping around at the end.
    public static (int NextIndex, bool Wrapped) AdvanceForward(int currentIndex, int count)
    {
        if (count == 0) return (-1, false);
        if (currentIndex < count - 1) return (currentIndex + 1, false);
        return (0, true);
    }

    // Retreats backward from currentIndex, wrapping around at the start.
    public static (int PrevIndex, bool Wrapped) AdvanceBackward(int currentIndex, int count)
    {
        if (count == 0) return (-1, false);
        if (currentIndex > 0) return (currentIndex - 1, false);
        return (count - 1, true);
    }

    // Replaces all occurrences of keyword in text with replacement.
    public static string ReplaceAll(string text, string keyword, string replacement, StringComparison comparison)
    {
        if (string.IsNullOrEmpty(keyword)) return text;
        var flags = comparison == StringComparison.Ordinal
            ? RegexOptions.None
            : RegexOptions.IgnoreCase;
        return Regex.Replace(text, Regex.Escape(keyword), _ => replacement, flags);
    }

    // Builds a display context string around matchStart in content.
    public static string BuildMatchContext(string content, int matchStart, string keyword)
    {
        const int contextLen = 35;
        var excerptStart = Math.Max(0, matchStart - contextLen);
        var excerptEnd   = Math.Min(content.Length, matchStart + keyword.Length + contextLen);
        var excerpt      = content.Substring(excerptStart, excerptEnd - excerptStart)
                                  .Replace('\n', ' ').Replace('\r', ' ');
        var prefix = excerptStart > 0 ? "…" : "";
        var suffix = excerptEnd < content.Length ? "…" : "";
        return $"{prefix}{excerpt}{suffix}";
    }
}
