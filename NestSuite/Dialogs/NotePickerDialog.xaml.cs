using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NestSuite.ViewModels;

namespace NestSuite.Dialogs;

public record NotePickerItem(string NotebookTitle, NoteViewModel Note)
{
    public string DisplayText => $"{NotebookTitle} / {Note.Title}";
}

public partial class NotePickerDialog : Window
{
    public NoteViewModel? SelectedNote { get; private set; }
    private readonly List<NotePickerItem> _allItems;

    public NotePickerDialog(IEnumerable<NotePickerItem> items)
    {
        _allItems = items.ToList();
        InitializeComponent();
        NoteList.ItemsSource = _allItems;
        Loaded += (_, _) =>
        {
            NoteFilterBox.Focus();
            if (NoteList.Items.Count > 0)
                NoteList.SelectedIndex = 0;
        };
    }

    private void NoteFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
        var filterText = NoteFilterBox.Text;
        NoteList.ItemsSource = string.IsNullOrEmpty(filterText)
            ? _allItems
            : _allItems.Where(i => i.Note.Title.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        if (NoteList.Items.Count > 0)
            NoteList.SelectedIndex = 0;
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        if (NoteList.SelectedItem is not NotePickerItem item) return;

        // Warn when an existing project file contains duplicate note titles.
        // New projects can't have duplicates (ViewModel enforces uniqueness on add/rename),
        // but old .notenest files may. The inserted link [[title]] resolves to the first match.
        bool hasDuplicate = _allItems.Count(i =>
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
