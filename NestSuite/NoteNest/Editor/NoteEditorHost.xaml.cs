using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NestSuite.NoteNest.Editor;

public partial class NoteEditorHost : UserControl
{
    private ScrollViewer? _editorScrollViewer;
    private ScrollViewer? _lineNumberScrollViewer;

    public ITextEditorAdapter Editor { get; private set; } = null!;

    public event RoutedEventHandler? OpenNoteLinkClicked;
    public event RoutedEventHandler? InsertNoteLinkClicked;
    public event EventHandler? EditorReady;

    public NoteEditorHost()
    {
        InitializeComponent();
    }

    private void EditorBox_Loaded(object sender, RoutedEventArgs e)
    {
        _editorScrollViewer     = GetDescendant<ScrollViewer>(EditorBox);
        _lineNumberScrollViewer = GetDescendant<ScrollViewer>(LineNumberBox);
        if (_editorScrollViewer != null)
            _editorScrollViewer.ScrollChanged += EditorScrollViewer_ScrollChanged;
        Editor = new TextBoxEditorAdapter(EditorBox);
        UpdateLineNumbers();
        EditorReady?.Invoke(this, EventArgs.Empty);
    }

    private void EditorBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateLineNumbers();

    private void EditorScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _lineNumberScrollViewer?.ScrollToVerticalOffset(e.VerticalOffset);
    }

    private void UpdateLineNumbers()
    {
        var count = EditorBox.Text.Count(c => c == '\n') + 1;
        LineNumberBox.Text = string.Join("\n", Enumerable.Range(1, count));
    }

    private void OpenNoteLink_ItemClick(object sender, RoutedEventArgs e) =>
        OpenNoteLinkClicked?.Invoke(this, e);

    private void InsertNoteLink_ItemClick(object sender, RoutedEventArgs e) =>
        InsertNoteLinkClicked?.Invoke(this, e);

    private static T? GetDescendant<T>(DependencyObject obj) where T : DependencyObject
    {
        if (obj is T t) return t;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var result = GetDescendant<T>(VisualTreeHelper.GetChild(obj, i));
            if (result != null) return result;
        }
        return null;
    }
}
