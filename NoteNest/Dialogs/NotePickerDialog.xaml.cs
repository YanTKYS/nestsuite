using System.Windows;
using System.Windows.Input;
using NoteNest.ViewModels;

namespace NoteNest.Dialogs;

public record NotePickerItem(string NotebookTitle, NoteViewModel Note)
{
    public string DisplayText => $"{NotebookTitle} / {Note.Title}";
}

public partial class NotePickerDialog : Window
{
    public NoteViewModel? SelectedNote { get; private set; }

    public NotePickerDialog(IEnumerable<NotePickerItem> items)
    {
        InitializeComponent();
        NoteList.ItemsSource = items.ToList();
        Loaded += (_, _) =>
        {
            NoteList.Focus();
            if (NoteList.Items.Count > 0)
                NoteList.SelectedIndex = 0;
        };
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        if (NoteList.SelectedItem is not NotePickerItem item) return;

        // Warn when an existing project file contains duplicate note titles.
        // New projects can't have duplicates (ViewModel enforces uniqueness on add/rename),
        // but old .notenest files may. The inserted link [[title]] resolves to the first match.
        var items = (List<NotePickerItem>)NoteList.ItemsSource;
        bool hasDuplicate = items.Count(i =>
            string.Equals(i.Note.Title, item.Note.Title, StringComparison.OrdinalIgnoreCase)) > 1;
        if (hasDuplicate)
        {
            var result = MessageBox.Show(
                $"「{item.Note.Title}」という名前のノートが複数あります。\n" +
                $"[[{item.Note.Title}]] リンクは最初に見つかったノートへ解決される場合があります。\n\n" +
                "このノートへのリンクを挿入しますか？",
                "同名ノートの警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
        }

        SelectedNote = item.Note;
        DialogResult = true;
    }

    private void NoteList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (NoteList.SelectedItem != null)
            OK_Click(sender, e);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;
}
