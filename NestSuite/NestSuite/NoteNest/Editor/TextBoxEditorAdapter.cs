using System.Windows;
using System.Windows.Controls;

namespace NestSuite.NoteNest.Editor;

public sealed class TextBoxEditorAdapter : ITextEditorAdapter, IDisposable
{
    private readonly TextBox _textBox;

    public TextBoxEditorAdapter(TextBox textBox)
    {
        _textBox = textBox;
        _textBox.TextChanged      += OnTextBoxTextChanged;
        _textBox.SelectionChanged += OnTextBoxSelectionChanged;
    }

    public string Text
    {
        get => _textBox.Text;
        set => _textBox.Text = value;
    }

    public int TextLength => _textBox.Text.Length;

    public int CaretIndex
    {
        get => _textBox.CaretIndex;
        set => _textBox.CaretIndex = Clamp(value, 0, _textBox.Text.Length);
    }

    public int SelectionStart  => _textBox.SelectionStart;
    public int SelectionLength => _textBox.SelectionLength;
    public string SelectedText => _textBox.SelectedText;
    public int LineCount       => _textBox.LineCount;

    public void Select(int start, int length)
    {
        var textLen = _textBox.Text.Length;
        start  = Clamp(start,  0, textLen);
        length = Clamp(length, 0, textLen - start);
        _textBox.Select(start, length);
    }

    public void ReplaceSelection(string text) => _textBox.SelectedText = text;

    public void InsertTextAtCaret(string text)
    {
        var caret = _textBox.CaretIndex;
        _textBox.Select(caret, 0);
        _textBox.SelectedText = text;
        _textBox.CaretIndex   = caret + text.Length;
    }

    public int GetLineIndexFromCharacterIndex(int characterIndex) =>
        _textBox.GetLineIndexFromCharacterIndex(characterIndex);

    public int GetCharacterIndexFromLineIndex(int lineIndex) =>
        _textBox.GetCharacterIndexFromLineIndex(lineIndex);

    public void ScrollToLine(int lineIndex) => _textBox.ScrollToLine(lineIndex);
    public void Focus() => _textBox.Focus();

    public event EventHandler? TextChanged;
    public event EventHandler? SelectionChanged;

    private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e) =>
        TextChanged?.Invoke(this, EventArgs.Empty);

    private void OnTextBoxSelectionChanged(object sender, RoutedEventArgs e) =>
        SelectionChanged?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        _textBox.TextChanged      -= OnTextBoxTextChanged;
        _textBox.SelectionChanged -= OnTextBoxSelectionChanged;
    }

    private static int Clamp(int value, int min, int max) =>
        value < min ? min : value > max ? max : value;
}
