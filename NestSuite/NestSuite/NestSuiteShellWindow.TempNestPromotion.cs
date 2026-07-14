using NestSuite.Services;
using NestSuite.TempNest;
using NestSuite.ViewModels;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // TN-3: TempNest スロット本文の NoteNest 新規ノートへの昇格。
    //
    // 責務分離（docs/design/nestsuite-attractiveness-direction.md 4.2 節）:
    //   TempNest 側 - 昇格要求を発行し、成功後に元スロットを消去するかどうかを実行する
    //   Shell 側   - 新規 NoteNest タブの作成・転送の調整・タブ選択・通知・確認を担当する
    //   NoteNest 側 - 新規ノートを作成し、タイトルと本文を設定する（MainViewModel.CreateNoteFromTransfer）
    //
    // TempNestSlotViewModel は NoteNest の内部構造を直接操作しない。Shell がここで仲介する。

    /// <summary>
    /// TempNestWorkspaceViewModel 生成時に、各スロットの昇格要求をこの Shell の処理へ配線する。
    /// </summary>
    private void WireTempNestPromotion(TempNestWorkspaceViewModel vm)
    {
        foreach (var slot in new[] { vm.Slot1, vm.Slot2, vm.Slot3, vm.Slot4 })
            slot.PromoteRequested = PromoteTempNestSlotToNoteNest;
    }

    /// <summary>
    /// 対象スロットの本文を新規 NoteNest タブの新規ノートへ転送する。
    /// 戻り値: <c>null</c>=失敗（元スロットは変更しない）、<c>true</c>=成功し利用者が消去を選択、
    /// <c>false</c>=成功し「残す」を選択。実際の消去は呼び出し元（TempNestSlotViewModel）が行う。
    /// </summary>
    private bool? PromoteTempNestSlotToNoteNest(TempNestSlotViewModel slot)
    {
        var body = slot.Body;
        if (string.IsNullOrWhiteSpace(body)) return null;

        NestSuiteDocumentTab tab;
        NestSuiteWorkspaceSession session;
        MainViewModel vm;
        try
        {
            tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);
            session = CreateSessionForTab(tab);
            vm = (MainViewModel)session.WorkspaceViewModel;
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("TempNestPromoteToNoteNest", ex);
            _dialogs.ShowError("NoteNestへの昇格に失敗しました。", "NoteNestへ昇格");
            return null;
        }

        // タブ・セッションを先に登録してから本文を転送する。本文転送で IsModified が変化した際、
        // OnNoteNestSessionPropertyChanged 経由のタブ同期（SyncNoteNestTabForViewModel）が
        // 登録済みセッションを前提とするため。
        _tabs.Add(tab);
        _sessionManager.Add(session);

        NoteViewModel? note;
        try
        {
            note = vm.CreateNoteFromTransfer(body);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("TempNestPromoteToNoteNest", ex);
            note = null;
        }

        if (note == null)
        {
            RollbackFailedNoteNestPromotion(tab, vm);
            _dialogs.ShowError("NoteNestへの昇格に失敗しました。", "NoteNestへ昇格");
            return null;
        }

        ActivateTab(tab);
        SaveSessionAfterTabChange();

        return _dialogs.ConfirmWithSafeDefault(
            "NoteNestへの昇格が完了しました。\nTempNestの元の内容を消去しますか？",
            "TempNestの内容");
    }

    /// <summary>
    /// 昇格失敗時に、登録済みの新規 NoteNest タブ・セッション・ViewModel を可能な範囲で取り消す。
    /// </summary>
    private void RollbackFailedNoteNestPromotion(NestSuiteDocumentTab tab, MainViewModel vm)
    {
        vm.PropertyChanged -= OnNoteNestSessionPropertyChanged;
        vm.Dispose();
        _sessionManager.Remove(tab.Id);
        var currentTab = _tabs.FirstOrDefault(t => t.Id == tab.Id);
        if (currentTab != null) _tabs.Remove(currentTab);
    }
}
