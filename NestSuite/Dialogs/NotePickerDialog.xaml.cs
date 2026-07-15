using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NestSuite.Services;
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
    private readonly NoteViewModel? _preselect;
    private readonly bool _selectFirstWhenNoMatch;

    /// <summary>
    /// L24: <paramref name="preselect"/> と一致するノートがあれば、開いた時点でそれを選択状態にする
    /// （厳密一致のみ。曖昧な候補は選択しない）。一致しない場合、<paramref name="selectFirstWhenNoMatch"/>
    /// が true なら従来どおり先頭候補を選択する（挿入リンク選択の既存動作）。false なら何も選択しない
    /// （タスクの関連ノート設定で、既存の関連ノートが未設定・削除済みの場合に誤って選択させないため）。
    /// </summary>
    public NotePickerDialog(
        IEnumerable<NotePickerItem> items,
        NoteViewModel? preselect = null,
        bool selectFirstWhenNoMatch = true,
        string? windowTitle = null,
        string? promptText = null)
    {
        _allItems = items.ToList();
        _preselect = preselect;
        _selectFirstWhenNoMatch = selectFirstWhenNoMatch;
        InitializeComponent();
        if (windowTitle != null) Title = windowTitle;
        if (promptText != null) PromptText.Text = promptText;
        NoteList.ItemsSource = _allItems;
        UpdateEmptyState();
        Loaded += (_, _) =>
        {
            NoteFilterBox.Focus();
            var match = _preselect == null ? null : _allItems.FirstOrDefault(i => ReferenceEquals(i.Note, _preselect));
            if (match != null)
            {
                NoteList.SelectedItem = match;
                NoteList.ScrollIntoView(match);
            }
            else if (_selectFirstWhenNoMatch && NoteList.Items.Count > 0)
            {
                NoteList.SelectedIndex = 0;
            }
        };
    }

    private void NoteFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
        var filterText = NoteFilterBox.Text;
        NoteList.ItemsSource = string.IsNullOrEmpty(filterText)
            ? _allItems
            : _allItems.Where(i => NotePickerFilterService.ItemMatchesFilter(i.NotebookTitle, i.Note.Title, filterText)).ToList();
        if (NoteList.Items.Count > 0)
            NoteList.SelectedIndex = 0;
        UpdateEmptyState();
    }

    private void UpdateEmptyState()
        => EmptyStateText.Visibility = NoteList.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        if (NoteList.SelectedItem is not NotePickerItem item) return;

        // Warn when an existing project file contains duplicate note titles.
        // New projects can't have duplicates (ViewModel enforces uniqueness on add/rename),
        // but old .notenest files may. The inserted link [[title]] resolves to the first match.
        bool hasDuplicate = NotePickerFilterService.HasDuplicateTitle(_allItems.Select(i => i.Note), item.Note.Title);
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
