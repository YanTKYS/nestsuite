using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NestSuite.Services;
using NestSuite.ViewModels;

namespace NestSuite.Dialogs;

public partial class BrokenLinksDialog : Window
{
    public NoteViewModel? SelectedNote { get; private set; }
    private readonly IReadOnlyList<BrokenLinkResult> _results;

    public BrokenLinksDialog(IReadOnlyList<BrokenLinkResult> results)
    {
        _results = results;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_results.Count == 0)
        {
            HeaderText.Text = "リンク切れは見つかりませんでした。";
            EmptyMessage.Visibility = Visibility.Visible;
            NavigateButton.IsEnabled = false;
        }
        else
        {
            HeaderText.Text = $"リンク切れが {_results.Count} 件見つかりました:";
            ResultList.Visibility = Visibility.Visible;
            ResultList.ItemsSource = _results;
            ResultList.SelectedIndex = 0;
        }
    }

    private void Navigate_Click(object sender, RoutedEventArgs e)
    {
        if (ResultList.SelectedItem is not BrokenLinkResult item) return;
        SelectedNote = item.SourceNote;
        DialogResult = true;
    }

    private void ResultList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ResultList.SelectedItem != null)
            Navigate_Click(sender, e);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;
}
