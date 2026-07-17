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

        // SH-41 (AT-2 フェーズ1): 「最近のファイルも検索」ON時の未オープンファイル読込は
        // Task.Run（バックグラウンド）＋ Dispatcher（UIスレッドへの反映）で行う。
        // recent files・開いているファイルパスは既存キャッシュ・_tabs をそのまま渡し、
        // fileExists/readAllText は既定（実File.Exists/File.ReadAllText）のまま注入しない。
        _crossSearchViewModel ??= new ShellSearchPanelViewModel(
            CollectSearchTabEntries,
            getRecentFilePaths: () => _recentFilesCache,
            getOpenFilePaths: () => _tabs.Select(t => t.FilePath).ToList(),
            runInBackground: action => Task.Run(action),
            postToUiThread: action => Dispatcher.Invoke(action));
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

    /// <summary>
    /// 結果クリック時の遷移。開いているタブの結果はタブ選択のみ（Workspace 内ジャンプは今回スコープ外）。
    /// SH-41: 未オープンrecent filesの結果は、最近使ったファイルメニューと共有する
    /// <see cref="OpenRecentFile"/> 経由で既存open経路から開く（パネル専用のオープン処理を複製しない）。
    /// </summary>
    private void CrossSearchResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox || listBox.SelectedItem is not ShellSearchResult result) return;
        if (result.IsUnopened)
        {
            if (result.FilePath != null) OpenRecentFile(result.FilePath);
            return;
        }
        var tab = _tabs.FirstOrDefault(t => t.Id == result.TabId);
        if (tab != null) ActivateTab(tab);
    }
}
