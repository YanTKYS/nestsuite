namespace NestSuite.NoteNest.Editor;

public enum LineHighlightKind { Todo, Fixme, Note, NoteLink }

public sealed record LineHighlightInfo(int LogicalIndex, LineHighlightKind Kind);

public static class MarkerLineDetector
{
    // Returns one LineHighlightInfo per logical line that contains a recognised marker.
    // Priority when multiple markers appear on the same line: FIXME > TODO > NOTE > NoteLink.
    public static IReadOnlyList<LineHighlightInfo> Detect(string text)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<LineHighlightInfo>();

        var result = new List<LineHighlightInfo>();
        int lineIndex = 0;
        int start = 0;
        while (start <= text.Length)
        {
            int end = text.IndexOf('\n', start);
            int lineEnd = end < 0 ? text.Length : end;
            var kind = ClassifyLine(text, start, lineEnd - start);
            if (kind.HasValue)
                result.Add(new LineHighlightInfo(lineIndex, kind.Value));
            if (end < 0) break;
            start = end + 1;
            lineIndex++;
        }
        return result;
    }

    private static LineHighlightKind? ClassifyLine(string text, int offset, int length)
    {
        ReadOnlySpan<char> span = text.AsSpan(offset, length);
        if (span.IndexOf("FIXME", StringComparison.OrdinalIgnoreCase) >= 0) return LineHighlightKind.Fixme;
        if (span.IndexOf("TODO",  StringComparison.OrdinalIgnoreCase) >= 0) return LineHighlightKind.Todo;
        if (ContainsNoteOutsideBrackets(span))                               return LineHighlightKind.Note;
        if (span.IndexOf("[[",    StringComparison.Ordinal)            >= 0) return LineHighlightKind.NoteLink;
        return null;
    }

    // Returns true if "NOTE" (case-insensitive) appears outside any [[...]] span.
    // Content inside [[ ... ]] is skipped so that note titles like [[My Note]] do
    // not falsely trigger the NOTE marker. Unclosed [[ consumes the rest of the line.
    private static bool ContainsNoteOutsideBrackets(ReadOnlySpan<char> span)
    {
        int i = 0;
        while (i < span.Length)
        {
            if (i + 1 < span.Length && span[i] == '[' && span[i + 1] == '[')
            {
                int closeIdx = -1;
                for (int j = i + 2; j + 1 < span.Length; j++)
                {
                    if (span[j] == ']' && span[j + 1] == ']') { closeIdx = j + 2; break; }
                }
                if (closeIdx < 0) return false; // unclosed [[, nothing more to check
                i = closeIdx;
                continue;
            }
            if (i + 4 <= span.Length &&
                span.Slice(i, 4).Equals("NOTE".AsSpan(), StringComparison.OrdinalIgnoreCase))
                return true;
            i++;
        }
        return false;
    }
}
