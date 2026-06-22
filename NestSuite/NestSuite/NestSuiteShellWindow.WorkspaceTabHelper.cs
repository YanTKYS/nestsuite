using System.Windows;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    /// <summary>
    /// 未保存確認ダイアログを表示し、ユーザーが承諾した場合に cleanup を実行して true を返す。
    /// キャンセル時は cleanup を呼ばずに false を返す。
    /// </summary>
    private bool ConfirmTabClose(NestSuiteDocumentTab tab, Action cleanup)
    {
        if (tab.IsModified &&
            !_dialogs.Confirm(
                $"「{tab.DisplayName}」には保存されていない変更があります。\n保存せずに閉じますか？",
                "タブを閉じる", MessageBoxImage.Warning))
            return false;
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
