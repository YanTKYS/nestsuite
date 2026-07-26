using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
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

    // SH-44: 横断検索を開く直前にフォーカスされていた要素。開いている間だけ保持し、
    // 閉じたら即クリアする（保存対象・長期保持なし）。
    private IInputElement? _crossSearchPreviousFocus;

    private void CommandCrossSearch_Executed(object sender, ExecutedRoutedEventArgs e) => ToggleCrossSearchPanel();

    private void ToggleCrossSearchPanel()
    {
        if (CrossSearchPanel.Visibility == Visibility.Visible)
        {
            CloseCrossSearchPanel();
            return;
        }

        // SH-44: Closed→Open のときだけ復帰先を保存する。既に開いている状態は上の分岐で
        // 必ずクローズへ回るため、ここに到達するのは初回オープンの1回だけであり、
        // 検索欄への再フォーカス等で復帰先が上書きされることはない。
        _crossSearchPreviousFocus = Keyboard.FocusedElement;

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

    /// <summary>
    /// SH-44: 横断検索パネルを閉じる共通処理。×ボタン・Escapeのどちらから呼ばれても
    /// 同じ経路（検索状態クリア＋フォーカス復帰）を通る。
    /// </summary>
    /// <param name="restorePreviousFocus">
    /// true: 開く前のフォーカス要素へ復帰する（単純クローズ）。
    /// false: 復帰しない（検索結果を実行して既に遷移先へフォーカスが移っている場合用）。
    /// 現状、検索結果選択（<see cref="CrossSearchResultsList_SelectionChanged"/>）はこの共通クローズを
    /// 呼ばずパネルを開いたままにするため、false は現時点で呼び出し箇所がないが、
    /// 将来「結果実行時に自動で閉じる」仕様が入っても分岐できるよう用意しておく。
    /// </param>
    private void CloseCrossSearchPanel(bool restorePreviousFocus = true)
    {
        // SH-44: 二重クローズ防止。既に閉じている場合は何もせず、古い復帰先も破棄する。
        if (CrossSearchPanel.Visibility != Visibility.Visible)
        {
            _crossSearchPreviousFocus = null;
            return;
        }

        CrossSearchPanel.Visibility = Visibility.Collapsed;
        _crossSearchViewModel?.Reset();

        var previousFocus = _crossSearchPreviousFocus;
        _crossSearchPreviousFocus = null;

        if (restorePreviousFocus)
            RestoreFocusAfterCrossSearchClose(previousFocus);
    }

    /// <summary>
    /// SH-44: Escapeが横断検索パネル内で押された場合だけ処理する。パネル外や非表示中のEscapeは
    /// 横取りしない（ダイアログのIsCancel・NoteNest補完ポップアップ・他Workspace検索等に影響しない）。
    /// </summary>
    private bool TryHandleCrossSearchEscape(KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return false;
        if (CrossSearchPanel.Visibility != Visibility.Visible) return false;
        if (!CrossSearchPanel.IsKeyboardFocusWithin) return false;

        CloseCrossSearchPanel();
        e.Handled = true;
        return true;
    }

    /// <summary>
    /// SH-44: 通常クローズ後のフォーカス復帰。Visibility変更と同一スタック内のFocus()は
    /// 失敗しうるため、既存パターン（<c>ChatNestWorkspaceView</c>のCtrl+F開閉と同様）に合わせ
    /// Dispatcher.InvokeAsync(DispatcherPriority.Input) でレイアウト更新後に実行する。
    /// フォールバック順: 1. 保存済み要素 → 2. アクティブWorkspaceの既定フォーカス
    /// （現状IdeaNestのみ既存API有り）→ 3. Shellのタブ一覧 → 4. メインウィンドウ。
    /// </summary>
    private void RestoreFocusAfterCrossSearchClose(IInputElement? previousFocus)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (TryFocus(previousFocus as UIElement)) return;
            if (TryFocusActiveWorkspaceDefault()) return;
            if (TryFocus(TabStrip)) return;
            Focus();
        }, DispatcherPriority.Input);
    }

    /// <summary>
    /// SH-44: 現在アクティブなWorkspaceが提供する既定フォーカス先。既存のフォーカス復帰API
    /// （<see cref="NestSuite.IdeaNest.Views.IdeaNestWorkspaceView.FocusWorkspace"/>）がある
    /// IdeaNestのみ対応する。他Workspaceは同種のAPIが未整備のため、次のフォールバック
    /// （タブ一覧）へ進む。今回のために新しいWorkspace種別switchを大量に追加しない。
    /// </summary>
    private bool TryFocusActiveWorkspaceDefault()
    {
        if (_selectedTab is not { WorkspaceKind: NestSuiteWorkspaceKind.IdeaNest } tab) return false;
        if (_detachedWindows.ContainsKey(tab.Id)) return false;
        if (IdeaNestWorkspaceView.Visibility != Visibility.Visible) return false;

        IdeaNestWorkspaceView.FocusWorkspace();
        return true;
    }

    /// <summary>
    /// SH-44: 破棄済み・非表示・無効・フォーカス不可な要素へ無理にFocus()しない。
    /// 判定自体は <see cref="CrossSearchFocusRestoreLogic.ShouldUseSavedFocus"/>（純粋関数）に委譲し、
    /// 実際のWPF Focus()呼び出しだけをここで行う。
    /// </summary>
    private static bool TryFocus(UIElement? element)
    {
        var state = element == null
            ? FocusRestoreCandidateState.Missing
            : new FocusRestoreCandidateState(true, element.IsVisible, element.IsEnabled, element.Focusable);

        return CrossSearchFocusRestoreLogic.ShouldUseSavedFocus(state) && element!.Focus();
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
