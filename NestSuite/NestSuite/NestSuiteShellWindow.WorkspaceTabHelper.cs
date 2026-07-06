using System.Windows;
using NestSuite.Services;
using System.Windows.Input;
using System.Windows.Threading;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // ステータスバー通知・フォーカス復元・タブ閉じる確認・新規タブ作成・未保存状態同期の共通ヘルパーを扱う partial。

    private DispatcherTimer? _notificationTimer;
    private bool _isShowingNotification;

    /// <summary>
    /// ステータスバーに一時通知メッセージを表示し、durationMs 後に自動解除する。
    /// 通知中は RefreshWorkspaceStatus による上書きを抑制する。
    /// </summary>
    private void ShowStatusNotification(string message, int durationMs = 2000)
    {
        StopNotificationTimer();
        WorkspaceStatusText.Text = message;
        _isShowingNotification = true;
        _notificationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
        _notificationTimer.Tick += NotificationTimer_Tick;
        _notificationTimer.Start();
    }

    private void NotificationTimer_Tick(object? sender, EventArgs e)
    {
        StopNotificationTimer();
        _isShowingNotification = false;
        RefreshWorkspaceStatus();
    }

    private void StopNotificationTimer()
    {
        if (_notificationTimer == null) return;
        _notificationTimer.Stop();
        _notificationTimer.Tick -= NotificationTimer_Tick;
        _notificationTimer = null;
    }

    /// <summary>
    /// アクティブな Workspace ビューの最初のフォーカス可能要素にフォーカスを移す。
    /// ダイアログを閉じた後などにフォーカスを Workspace に戻すために使う。
    /// </summary>
    private void RestoreFocusToWorkspace()
    {
        if (_selectedTab == null) return;
        UIElement? target = _selectedTab.WorkspaceKind switch
        {
            NestSuiteWorkspaceKind.NoteNest => WorkspaceView,
            NestSuiteWorkspaceKind.ChatNest => ChatWorkspaceView,
            NestSuiteWorkspaceKind.IdeaNest => IdeaNestWorkspaceView,
            NestSuiteWorkspaceKind.Temp     => TempNestWorkspaceView,
            _                               => null
        };
        if (target == null) return;
        Dispatcher.BeginInvoke(
            () => target.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)),
            DispatcherPriority.Loaded);
    }

    /// <summary>
    /// 未保存確認ダイアログを表示し、ユーザーが承諾した場合に cleanup を実行して true を返す。
    /// キャンセル時は cleanup を呼ばずに false を返す。
    /// </summary>
    private bool ConfirmTabClose(NestSuiteDocumentTab tab, Action cleanup)
    {
        var canClose = CloseConfirmationService.CanCloseSingle(
            tab.IsModified,
            () => _dialogs.Confirm(
                    $"「{tab.DisplayName}」には保存されていない変更があります。\n保存せずに閉じますか？",
                    "タブを閉じる", MessageBoxImage.Warning)
                ? UnsavedChangeDecision.Discard
                : UnsavedChangeDecision.Cancel);
        if (!canClose) return false;

        cleanup();
        return true;
    }

    /// <summary>
    /// 指定 WorkspaceKind の無題タブを作成してセッションを登録し、アクティブ化する。
    /// </summary>
    private void NewWorkspaceSession(NestSuiteWorkspaceKind kind)
    {
        var tab = NestSuiteTabFactory.CreateUntitled(kind);
        _tabs.Add(tab);
        _sessionManager.Add(CreateSessionForTab(tab));
        ActivateTab(tab);
    }

    /// <summary>
    /// ViewModel に対応する Session からタブを逆引きし、IsModified を更新する。
    /// SyncChatNestTabForViewModel / SyncIdeaNestTabForViewModel の共通処理。
    /// </summary>
    private void SyncTabModifiedState(object vm, bool isModified)
    {
        var session = _sessionManager.Sessions.FirstOrDefault(s => ReferenceEquals(s.WorkspaceViewModel, vm));
        if (session == null) return;
        var tab = _tabs.FirstOrDefault(t => t.Id == session.TabId);
        if (tab == null) return;
        ReplaceTab(tab, tab with { IsModified = isModified });
    }
}
