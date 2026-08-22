using NestSuite.ChatNest;
using NestSuite.Dialogs;
using NestSuite.IdeaNest.ViewModels;

namespace NestSuite;

/// <summary>
/// ChatNest 発言 → IdeaNest カード化。
/// 設計正本: docs/planning/workspace-manual-transfer-helper-design.md。
///
/// <para>ChatNest 側は IdeaNest の型・内部構造を一切参照せず、発言本文（string）だけを
/// <see cref="ChatNestWorkspaceViewModel.TransferMessageToIdeaNestRequested"/> 経由で渡す
/// （TempNest の <c>PromoteRequested</c> と同じ責務分離）。この partial が転送先解決・UX・通知・
/// エラー処理をすべて担う。<see cref="NestSuiteShellWindow.WorkspaceTransfer.cs"/> の共通ヘルパーは
/// タブ解決・受入呼び出し・結果返却のみを行い、UI 文言・ダイアログ表示は持たない。</para>
/// </summary>
public partial class NestSuiteShellWindow
{
    /// <summary>
    /// ChatNestWorkspaceViewModel 生成時に、発言の IdeaNest 転送要求をこの Shell の処理へ配線する。
    /// </summary>
    private void WireChatNestToIdeaNestTransfer(ChatNestWorkspaceViewModel vm) =>
        vm.TransferMessageToIdeaNestRequested = TransferChatNestMessageToIdeaNest;

    /// <summary>
    /// ChatNest の 1 発言本文を、既存の IdeaNest タブへカードとして転送する。
    /// IdeaNest タブが 0 件なら転送せず案内のみ（新規 IdeaNest タブは自動生成しない）。
    /// 1 件ならそのタブへ直接追加する。複数件なら選択ダイアログで明示選択させる
    /// （キャンセル時は何もしない）。転送元 ChatNest はいずれの場合も変更しない。
    /// </summary>
    private void TransferChatNestMessageToIdeaNest(string body)
    {
        var candidates = EnumerateTransferTargets(NestSuiteWorkspaceKind.IdeaNest);

        if (candidates.Count == 0)
        {
            ShowStatusNotification("IdeaNest タブがありません。IdeaNest を開いてから実行してください");
            return;
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
            if (dialog.ShowDialog() != true || dialog.SelectedTarget is not { } selected) return;
            target = selected;
        }

        var content = new WorkspaceTransferContent { Title = null, Body = body };
        var result = TransferToWorkspaceTab<IdeaNestWorkspaceViewModel>(
            target,
            content,
            static (vm, c) => vm.AddCardFromTransfer(c.Title, c.Body));

        switch (result)
        {
            case WorkspaceTransferResult.Success:
                ShowStatusNotification($"IdeaNest「{target.DisplayName}」にカードを追加しました");
                break;
            case WorkspaceTransferResult.NoTarget:
                ShowStatusNotification("転送先の IdeaNest タブが見つかりませんでした");
                break;
            case WorkspaceTransferResult.InvalidContent:
                ShowStatusNotification("本文が空のため追加しませんでした");
                break;
            case WorkspaceTransferResult.TargetRejected:
                ShowStatusNotification("カードを追加できませんでした");
                break;
            case WorkspaceTransferResult.Failed:
                _dialogs.ShowError("IdeaNestカードの追加に失敗しました。", "IdeaNestへ転送");
                break;
        }
    }
}
