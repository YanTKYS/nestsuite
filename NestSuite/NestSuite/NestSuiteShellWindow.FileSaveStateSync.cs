using NestSuite.ChatNest;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // 保存成功後のタブ・Session パス更新（UpdateXxxTabPath → ApplySavedWorkspaceState）を扱う partial。

    /// <summary>v2.7.15: 保存成功後の IdeaNest タブ・Session 更新を共通経路へ委譲する。</summary>
    private void UpdateIdeaNestTabPath(NestSuiteWorkspaceSession session, string path) =>
        UpdateIdeaNestTabPath(session, path, showNotification: true);

    /// <summary>
    /// v2.13.6 TD-45: IdeaNest の保存後状態更新の唯一の定義点。
    /// isModifiedAfterSave は常に false（<c>MarkSaved()</c> で完全にクリアされる）。
    /// 通常保存は通知あり、SaveAll 経路は showNotification: false で呼ぶ。
    /// </summary>
    private void UpdateIdeaNestTabPath(NestSuiteWorkspaceSession session, string path, bool showNotification) =>
        ApplySavedWorkspaceState(session, path, isModifiedAfterSave: false, showNotification);

    /// <summary>
    /// v1.9.2: 指定 Session に対応する ChatNest タブのファイルパスを更新し、タブモデルを最新化する。
    /// 保存成功時に <see cref="ChatNestWorkspaceViewModel.MarkSaved"/> の後で呼ぶ。
    /// </summary>
    private void UpdateChatNestTabPath(NestSuiteWorkspaceSession session, string path) =>
        UpdateChatNestTabPath(session, path, showNotification: true);

    /// <summary>
    /// v2.13.6 TD-45: ChatNest の保存後状態更新の唯一の定義点。
    ///
    /// <para>案A: IsModified は MarkSaved() 後の HasUnsavedChanges を引き継ぐ。
    /// IsDirty は解消されるが InputText が残っている場合は HasUnsavedChanges が true のままになるため、
    /// IsModified = false を固定せず vm.HasUnsavedChanges を参照する。
    /// この差異が IdeaNest との保存フロー最大の違いであり、ここ以外に持たせないこと。</para>
    /// </summary>
    private void UpdateChatNestTabPath(NestSuiteWorkspaceSession session, string path, bool showNotification)
    {
        var vm = (ChatNestWorkspaceViewModel)session.WorkspaceViewModel;
        ApplySavedWorkspaceState(session, path, vm.HasUnsavedChanges, showNotification);
    }

    /// <summary>v2.7.15: 保存成功後の NoteNest タブ・Session 更新を共通経路へ委譲する。</summary>
    private void UpdateNoteNestTabPath(NestSuiteWorkspaceSession session, string path) =>
        ApplySavedWorkspaceState(session, path, isModifiedAfterSave: false);
}
