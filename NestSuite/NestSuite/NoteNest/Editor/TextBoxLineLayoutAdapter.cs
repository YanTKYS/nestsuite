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
