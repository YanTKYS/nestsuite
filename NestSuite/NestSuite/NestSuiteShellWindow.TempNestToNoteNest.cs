using NestSuite.Dialogs;
using NestSuite.TempNest;
using NestSuite.ViewModels;

namespace NestSuite;

/// <summary>
/// TempNest スロット → 既存 NoteNest タブへ新規ノートとして追加。
/// 転送契約は <see cref="NestSuiteShellWindow.WorkspaceTransfer.cs"/> の共通ヘルパーが正本。
///
/// <para>TempNest → IdeaNest（<see cref="NestSuiteShellWindow.TempNestToIdeaNest.cs"/>）・
/// ChatNest → IdeaNest（<see cref="NestSuiteShellWindow.ChatNestToIdeaNest.cs"/>）と同じ共通ヘルパー
/// （<c>EnumerateTransferTargets</c>・<c>TransferToWorkspaceTab</c>・<c>WorkspaceTransferTargetDialog</c>）
/// を再利用する。この partial が持つのは TempNest 固有の入口（Title/Body の取り出し・
/// 転送成功後に元スロットを消去するかどうかの確認）のみで、転送先解決・エラー処理は共通ヘルパーに委ねる。
/// 受入側は <see cref="MainViewModel.CreateNoteFromTransfer(string?, string)"/>。
/// TempNest スロットを新規 NoteNest タブへ昇格させる経路とは別導線であり、そちらの挙動には触れない。</para>
/// </summary>
public partial class NestSuiteShellWindow
{
    /// <summary>
    /// TempNestWorkspaceViewModel 生成時に、各スロットの既存 NoteNest タブへの転送要求をこの Shell の処理へ配線する。
    /// </summary>
    private void WireTempNestToNoteNestTransfer(TempNestWorkspaceViewModel vm)
    {
        foreach (var slot in new[] { vm.Slot1, vm.Slot2, vm.Slot3, vm.Slot4 })
            slot.TransferToNoteNestRequested = TransferTempNestSlotToNoteNest;
    }

    /// <summary>
    /// 対象スロットの Title/Body を、既存の NoteNest タブへ新規ノートとして転送する。
    /// NoteNest タブが 0 件なら転送せず案内のみ（新規 NoteNest タブは自動生成しない＝昇格経路とは別導線）。
    /// 1 件ならそのタブへ直接追加する。複数件なら既存の <see cref="WorkspaceTransferTargetDialog"/>
    /// で明示選択させる（キャンセル時は何もしない）。転送成功後は自動切替・自動保存を行わず、
    /// 元スロットを消去するかどうかを他の転送経路と同じ考え方（既定は「残す」の確認ダイアログ）で確認する。
    /// 戻り値: null=失敗・キャンセル・対象なし（元スロットは変更しない）、
    /// true=成功し利用者が消去を選択、false=成功し残すを選択。実際の消去は呼び出し元（スロット自身）が行う。
    /// </summary>
    private bool? TransferTempNestSlotToNoteNest(TempNestSlotViewModel slot)
    {
        var content = new WorkspaceTransferContent
        {
            Title = string.IsNullOrWhiteSpace(slot.Title) ? null : slot.Title,
            Body = slot.Body,
        };

        var candidates = EnumerateTransferTargets(NestSuiteWorkspaceKind.NoteNest);

        if (candidates.Count == 0)
        {
            ShowStatusNotification("NoteNest タブがありません。NoteNest を開いてから実行してください");
            return null;
        }

        WorkspaceTransferTarget target;
        if (candidates.Count == 1)
        {
            target = candidates[0];
        }
        else
        {
            var dialog = new WorkspaceTransferTargetDialog(
                candidates,
                windowTitle: "NoteNestへ転送",
                promptText: "転送先のNoteNestタブを選択してください:",
                listAutomationName: "転送先のNoteNestタブ一覧")
            {
                Owner = this,
            };
            if (dialog.ShowDialog() != true || dialog.SelectedTarget is not { } selected) return null;
            target = selected;
        }

        var result = TransferToWorkspaceTab<MainViewModel>(
            target,
            content,
            static (vm, c) => vm.CreateNoteFromTransfer(c.Title, c.Body) != null);

        switch (result)
        {
            case WorkspaceTransferResult.Success:
                ShowStatusNotification($"NoteNest「{target.DisplayName}」にノートを追加しました");
                return _dialogs.ConfirmWithSafeDefault(
                    "NoteNestへのノート追加が完了しました。\nTempNestの元の内容を消去しますか？",
                    "TempNestの内容");
            case WorkspaceTransferResult.NoTarget:
                ShowStatusNotification("転送先の NoteNest タブが見つかりませんでした");
                return null;
            case WorkspaceTransferResult.InvalidContent:
                ShowStatusNotification("内容が空のため追加しませんでした");
                return null;
            case WorkspaceTransferResult.TargetRejected:
                ShowStatusNotification("ノートを追加できませんでした");
                return null;
            case WorkspaceTransferResult.Failed:
                _dialogs.ShowError("NoteNestへのノート追加に失敗しました。", "NoteNestへ転送");
                return null;
            default:
                return null;
        }
    }
}
