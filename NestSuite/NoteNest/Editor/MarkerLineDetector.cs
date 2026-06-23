namespace NestSuite.NoteNest.Editor;

public static class MarkerLineDetector
{
    // Returns 0-based logical line indices whose text contains TODO, FIXME, or HACK (case-insensitive).
    public static IReadOnlyList<int> Detect(string text)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<int>();

        var result = new List<int>();
        int lineIndex = 0;
        int start = 0;
        while (start <= text.Length)
        {
            int end = text.IndexOf('\n', start);
            int lineEnd = end < 0 ? text.Length : end;
            if (ContainsMarker(text, start, lineEnd - start))
                result.Add(lineIndex);
            if (end < 0) break;
            start = end + 1;
            lineIndex++;
        }
        return result;
    }

    private static bool ContainsMarker(string text, int offset, int length)
    {
        // Scan the span once, looking for TODO, FIXME, HACK case-insensitively.
        // We avoid allocating a substring by walking the span manually.
        ReadOnlySpan<char> span = text.AsSpan(offset, length);
        return span.IndexOf("TODO",  StringComparison.OrdinalIgnoreCase) >= 0
            || span.IndexOf("FIXME", StringComparison.OrdinalIgnoreCase) >= 0
            || span.IndexOf("HACK",  StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
