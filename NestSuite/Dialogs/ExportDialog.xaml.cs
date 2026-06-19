using System.Windows;
using NoteNest.Models;

namespace NoteNest.Dialogs;

public partial class ExportDialog : Window
{
    public ExportOptions Options { get; private set; } = new(ExportTarget.Project, ExportFormat.Text, false, false);

    public ExportDialog() => InitializeComponent();

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        var target = NoteTarget.IsChecked == true ? ExportTarget.CurrentNote : NotebookTarget.IsChecked == true ? ExportTarget.CurrentNotebook : ExportTarget.Project;
        var format = FormatBox.SelectedIndex switch { 1 => ExportFormat.Markdown, 2 => ExportFormat.Html, _ => ExportFormat.Text };
        Options = new(target, format, IncludeTasksBox.IsChecked == true, IncludeMarkersBox.IsChecked == true);
        DialogResult = true;
    }
}
