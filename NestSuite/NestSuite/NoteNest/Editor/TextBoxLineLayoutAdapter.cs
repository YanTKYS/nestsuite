using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace NestSuite.NoteNest.Editor;

// Centralises logical↔visual line mapping for a TextBox with TextWrapping=Wrap.
// Static methods have no WPF dependency and are unit-testable.
// Instance methods require a post-layout call site (DispatcherPriority.Render or later).
public sealed class TextBoxLineLayoutAdapter
{
    private readonly TextBox _editor;

    public TextBoxLineLayoutAdapter(TextBox editor) => _editor = editor;

    // Returns the character offset of the start of the given logical line (0-based).
    // Returns -1 when logicalIndex exceeds the number of lines in text.
    public static int LogicalLineStartChar(string text, int logicalIndex)
    {
        if (logicalIndex == 0) return 0;
        int offset = 0;
        for (int i = 0; i < logicalIndex; i++)
        {
            int nl = text.IndexOf('\n', offset);
            if (nl < 0) return -1;
            offset = nl + 1;
        }
        return offset <= text.Length ? offset : -1;
    }

    // Number of extra visual lines produced by word-wrap for the given logical line.
    // Requires valid layout (call at DispatcherPriority.Render or later).
    public int ExtraVisualLines(string text, int logicalIndex)
    {
        int start = LogicalLineStartChar(text, logicalIndex);
        if (start < 0) return 0;
        int nl  = text.IndexOf('\n', start);
        int end = nl < 0 ? text.Length : nl;
        if (end <= start) return 0;
        try
        {
            int firstVis = _editor.GetLineIndexFromCharacterIndex(start);
            int lastVis  = _editor.GetLineIndexFromCharacterIndex(end - 1);
            if (firstVis < 0 || lastVis < 0) return 0;
            return Math.Max(0, lastVis - firstVis);
        }
        catch { return 0; }
    }

    // Builds gutter text: logical line numbers with blank entries for wrapped visual lines.
    // Requires valid layout.
    public string BuildLineNumberText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "1";
        var sb = new StringBuilder();
        int charOffset = 0, logIdx = 0;
        bool first = true;
        while (true)
        {
            int nl      = text.IndexOf('\n', charOffset);
            int lineEnd = nl < 0 ? text.Length : nl;
            if (!first) sb.Append('\n');
            first = false;
            sb.Append(logIdx + 1);
            int extra = ExtraVisualLines(text, logIdx);
            for (int w = 0; w < extra; w++) sb.Append('\n');
            logIdx++;
            charOffset = lineEnd + 1;
            if (nl < 0) break;
        }
        return sb.ToString();
    }

    // Validates that rendered line-number Y positions remain one-to-one and non-overlapping.
    // The production gutter uses TextBox layout rectangles; this helper keeps the monotonicity
    // rule unit-testable without depending on a live WPF render pass.
    public static bool AreLineTopsStrictlyIncreasing(IEnumerable<double> tops)
    {
        var hasPrevious = false;
        var previous = 0.0;

        foreach (var top in tops)
        {
            if (double.IsNaN(top) || double.IsInfinity(top)) return false;
            if (hasPrevious && top <= previous) return false;
            previous = top;
            hasPrevious = true;
        }

        return true;
    }

    // Returns the Y-top and total height spanning all visual lines of a logical line.
    // Requires valid layout.
    public (double Top, double Height) HighlightBounds(string text, int logicalIndex)
    {
        int start = LogicalLineStartChar(text, logicalIndex);
        if (start < 0 || start > text.Length) return default;
        int nl  = text.IndexOf('\n', start);
        int end = nl < 0 ? text.Length : nl;

        Rect startRect;
        try   { startRect = _editor.GetRectFromCharacterIndex(start); }
        catch { return default; }
        if (startRect.IsEmpty || startRect.Height <= 0) return default;

        double top    = startRect.Top;
        double bottom = startRect.Bottom;

        if (end > start)
        {
            try
            {
                var endRect = _editor.GetRectFromCharacterIndex(end - 1);
                if (!endRect.IsEmpty && endRect.Height > 0)
                    bottom = Math.Max(bottom, endRect.Bottom);
            }
            catch { }
        }

        return (top, Math.Max(startRect.Height, bottom - top));
    }
}
