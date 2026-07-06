using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NestSuite.Services;
using NestSuite.ViewModels;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // v2.15.0 SH: Shell横断検索（開いているタブのみを対象とする最小実装）。
    // 新規 SearchNest Workspace ではなく、Shell 側の補助機能として実装する。
    // パネルの表示状態・検索語・検索結果はセッション内のみで保持し、ui-settings.json 等へは保存しない。

    public static readonly RoutedCommand CrossSearchCommand = new RoutedCommand(
        "CrossSearch", typeof(NestSuiteShellWindow),
        new InputGestureCollection { new KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Shift) });

    private ShellSearchPanelViewModel? _crossSearchViewModel;

    private void CommandCrossSearch_Executed(object sender, ExecutedRoutedEventArgs e) => ToggleCrossSearchPanel();

    private void ToggleCrossSearchPanel()
    {
        if (CrossSearchPanel.Visibility == Visibility.Visible)
        {
            CloseCrossSearchPanel();
            return;
        }

        _crossSearchViewModel ??= new ShellSearchPanelViewModel(CollectSearchTabEntries);
        CrossSearchPanel.DataContext = _crossSearchViewModel;
        CrossSearchPanel.Visibility = Visibility.Visible;
        CrossSearchBox.Focus();
    }

    private void CloseCrossSearchPanel()
    {
        CrossSearchPanel.Visibility = Visibility.Collapsed;
        _crossSearchViewModel?.Reset();
    }

    private void CrossSearchCloseButton_Click(object sender, RoutedEventArgs e) => CloseCrossSearchPanel();

    /// <summary>
    /// 現在開いている全タブを <see cref="ShellSearchTabEntry"/> へ変換する。
    /// TempNest 固定タブも含め、_sessionManager に登録済みの全 Session を対象にする。
    /// </summary>
    private IReadOnlyList<ShellSearchTabEntry> CollectSearchTabEntries()
    {
        var entries = new List<ShellSearchTabEntry>();
        foreach (var tab in _tabs)
        {
            if (!_sessionManager.TryGet(tab.Id, out var session) || session == null) continue;
            entries.Add(new ShellSearchTabEntry(tab.Id, tab.ShortDisplayName, tab.WorkspaceKind, session.WorkspaceViewModel));
        }
        return entries;
    }

    /// <summary>結果クリック時にジャンプ先タブへ遷移する（タブ選択のみ。Workspace 内ジャンプは今回スコープ外）。</summary>
    private void CrossSearchResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox || listBox.SelectedItem is not ShellSearchResult result) return;
        var tab = _tabs.FirstOrDefault(t => t.Id == result.TabId);
        if (tab != null) ActivateTab(tab);
    }
}
