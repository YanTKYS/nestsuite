using NestSuite.Dialogs;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.TempNest;

namespace NestSuite;

/// <summary>
/// LK-3 (v2.22.0): TempNest スロット → IdeaNest カード化。
/// 設計正本: docs/planning/workspace-manual-transfer-helper-design.md（TD-92 / v2.20.1、§14）。
///
/// <para>LK-4（<see cref="NestSuiteShellWindow.ChatNestToIdeaNest.cs"/>）と同じ共通ヘルパー
/// （<c>EnumerateTransferTargets</c>・<c>TransferToWorkspaceTab</c>・<c>WorkspaceTransferTargetDialog</c>・
/// <c>IdeaNestWorkspaceViewModel.AddCardFromTransfer</c>）をそのまま再利用する。この partial が持つのは
/// TempNest 固有の入口（Title/Body の取り出し・転送成功後に元スロットを消去するかどうかの確認）のみで、
/// 転送先解決・受入・エラー処理は共通ヘルパーに委ねる。TN-3（TempNest → NoteNest 昇格）とは責務が異なり、
/// TN-3 の実装・挙動には一切触れない。</para>
/// </summary>
public partial class NestSuiteShellWindow
{
    /// <summary>
    /// TempNestWorkspaceViewModel 生成時に、各スロットの IdeaNest 転送要求をこの Shell の処理へ配線する。
    /// </summary>
    private void WireTempNestToIdeaNestTransfer(TempNestWorkspaceViewModel vm)
    {
        foreach (var slot in new[] { vm.Slot1, vm.Slot2, vm.Slot3, vm.Slot4 })
            slot.TransferToIdeaNestRequested = TransferTempNestSlotToIdeaNest;
    }

    /// <summary>
    /// 対象スロットの Title/Body を、既存の IdeaNest タブへカードとして転送する。
    /// IdeaNest タブが 0 件なら転送せず案内のみ（新規 IdeaNest タブは自動生成しない）。
    /// 1 件ならそのタブへ直接追加する。複数件なら既存の <see cref="WorkspaceTransferTargetDialog"/>
    /// で明示選択させる（キャンセル時は何もしない）。転送成功後は自動切替・自動保存を行わず、
    /// 元スロットを消去するかどうかを TN-3 と同じ考え方（既定は「残す」の確認ダイアログ）で確認する。
    /// 戻り値: null=失敗・キャンセル・対象なし（元スロットは変更しない）、
    /// true=成功し利用者が消去を選択、false=成功し残すを選択。実際の消去は呼び出し元（スロット自身）が行う。
    /// </summary>
    private bool? TransferTempNestSlotToIdeaNest(TempNestSlotViewModel slot)
    {
        var content = new WorkspaceTransferContent
        {
            Title = string.IsNullOrWhiteSpace(slot.Title) ? null : slot.Title,
            Body = slot.Body,
        };

        var candidates = EnumerateTransferTargets(NestSuiteWorkspaceKind.IdeaNest);

        if (candidates.Count == 0)
        {
            ShowStatusNotification("IdeaNest タブがありません。IdeaNest を開いてから実行してください");
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
                windowTitle: "IdeaNestへ転送",
                promptText: "転送先の IdeaNest タブを選択してください:",
                listAutomationName: "転送先のIdeaNestタブ一覧")
            {
                Owner = this,
            };
            if (dialog.ShowDialog() != true || dialog.SelectedTarget is not { } selected) return null;
            target = selected;
        }

        var result = TransferToWorkspaceTab<IdeaNestWorkspaceViewModel>(
            target,
            content,
            static (vm, c) => vm.AddCardFromTransfer(c.Title, c.Body));

        switch (result)
        {
            case WorkspaceTransferResult.Success:
                ShowStatusNotification($"IdeaNest「{target.DisplayName}」にカードを追加しました");
                return _dialogs.ConfirmWithSafeDefault(
                    "IdeaNestへのカード追加が完了しました。\nTempNestの元の内容を消去しますか？",
                    "TempNestの内容");
            case WorkspaceTransferResult.NoTarget:
                ShowStatusNotification("転送先の IdeaNest タブが見つかりませんでした");
                return null;
            case WorkspaceTransferResult.InvalidContent:
                ShowStatusNotification("内容が空のため追加しませんでした");
                return null;
            case WorkspaceTransferResult.TargetRejected:
                ShowStatusNotification("カードを追加できませんでした");
                return null;
            case WorkspaceTransferResult.Failed:
                _dialogs.ShowError("IdeaNestカードの追加に失敗しました。", "IdeaNestへ転送");
                return null;
            default:
                return null;
        }
    }
}
