namespace NestSuite.NoteNest.Editor;

public interface ITextEditorAdapter
{
    string Text { get; set; }
    int TextLength { get; }
    int CaretIndex { get; set; }
    int SelectionStart { get; }
    int SelectionLength { get; }
    string SelectedText { get; }
    int LineCount { get; }
    void Select(int start, int length);
    void ReplaceSelection(string text);
    void InsertTextAtCaret(string text);
    int GetLineIndexFromCharacterIndex(int characterIndex);
    int GetCharacterIndexFromLineIndex(int lineIndex);
    void ScrollToLine(int lineIndex);
    void Focus();
    event EventHandler? TextChanged;
    event EventHandler? SelectionChanged;
}
