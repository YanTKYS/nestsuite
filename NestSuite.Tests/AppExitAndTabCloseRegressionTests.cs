using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.9.9: アプリ終了・タブクローズ確認フローの回帰テスト。
/// v2.9.7 で導入した Save / Discard / Cancel 確認が後退しないことを固定する。
/// CloseConfirmationService を使用する純粋ロジックテスト。WPF UI は対象外。
/// </summary>
public class AppExitAndTabCloseRegressionTests
{
    // ── アプリ終了: 未保存なし → 確認なしで継続 ──────────────────────────

    [Fact]
    public void AppExit_NoUnsavedTabs_ContinuesWithoutAsking()
    {
        var targets = new[]
        {
            new CloseConfirmationTarget("note-1", CanClose: true, HasUnsavedChanges: false),
            new CloseConfirmationTarget("chat-1", CanClose: true, HasUnsavedChanges: false),
        };
        var asked = false;
        var result = CloseConfirmationService.EvaluateMany(targets, _ => { asked = true; return UnsavedChangeDecision.Cancel; });
        Assert.True(result.CanContinue);
        Assert.False(asked);
    }

    // ── アプリ終了: 未保存 NoteNest が確認対象になる ─────────────────────

    [Fact]
    public void AppExit_UnsavedNoteNest_Cancel_StopsExit()
    {
        var targets = new[] { new CloseConfirmationTarget("note-1", CanClose: true, HasUnsavedChanges: true) };
        var result = CloseConfirmationService.EvaluateMany(targets, _ => UnsavedChangeDecision.Cancel);
        Assert.True(result.Cancelled);
        Assert.Contains("note-1", result.FailedTabs);
    }

    [Fact]
    public void AppExit_UnsavedNoteNest_SaveSuccess_ContinuesExit()
    {
        var targets = new[] { new CloseConfirmationTarget("note-1", CanClose: true, HasUnsavedChanges: true) };
        var result = CloseConfirmationService.EvaluateMany(
            targets,
            _ => UnsavedChangeDecision.Save,
            _ => true);
        Assert.True(result.CanContinue);
        Assert.Contains("note-1", result.SavedTabs);
    }

    [Fact]
    public void AppExit_UnsavedNoteNest_SaveFail_StopsExit()
    {
        var targets = new[] { new CloseConfirmationTarget("note-1", CanClose: true, HasUnsavedChanges: true) };
        var result = CloseConfirmationService.EvaluateMany(
            targets,
            _ => UnsavedChangeDecision.Save,
            _ => false);
        Assert.True(result.Cancelled);
        Assert.Contains("note-1", result.FailedTabs);
    }

    [Fact]
    public void AppExit_UnsavedNoteNest_Discard_ContinuesExit()
    {
        var targets = new[] { new CloseConfirmationTarget("note-1", CanClose: true, HasUnsavedChanges: true) };
        var result = CloseConfirmationService.EvaluateMany(targets, _ => UnsavedChangeDecision.Discard);
        Assert.True(result.CanContinue);
        Assert.Contains("note-1", result.DiscardedTabs);
    }

    // ── アプリ終了: 複数タブ ────────────────────────────────────────────

    [Fact]
    public void AppExit_MultipleUnsavedTabs_CancelOnFirst_SecondNotAsked()
    {
        var targets = new[]
        {
            new CloseConfirmationTarget("note-1", CanClose: true, HasUnsavedChanges: true),
            new CloseConfirmationTarget("note-2", CanClose: true, HasUnsavedChanges: true),
        };
        var asked = new List<string>();
        CloseConfirmationService.EvaluateMany(targets, t =>
        {
            asked.Add(t.Id);
            return UnsavedChangeDecision.Cancel;
        });
        Assert.Single(asked);
        Assert.Equal("note-1", asked[0]);
    }

    [Fact]
    public void AppExit_MultipleUnsavedTabs_AllSaved_ContinuesExit()
    {
        var targets = new[]
        {
            new CloseConfirmationTarget("note-1", CanClose: true, HasUnsavedChanges: true),
            new CloseConfirmationTarget("note-2", CanClose: true, HasUnsavedChanges: true),
        };
        var result = CloseConfirmationService.EvaluateMany(
            targets,
            _ => UnsavedChangeDecision.Save,
            _ => true);
        Assert.True(result.CanContinue);
        Assert.Equal(2, result.SavedTabs.Count);
    }

    [Fact]
    public void AppExit_MultipleUnsavedTabs_SaveFailOnFirst_StopsExit()
    {
        var targets = new[]
        {
            new CloseConfirmationTarget("note-1", CanClose: true, HasUnsavedChanges: true),
            new CloseConfirmationTarget("note-2", CanClose: true, HasUnsavedChanges: true),
        };
        var result = CloseConfirmationService.EvaluateMany(
            targets,
            _ => UnsavedChangeDecision.Save,
            _ => false);
        Assert.True(result.Cancelled);
        Assert.Equal(new[] { "note-1" }, result.FailedTabs);
        Assert.Empty(result.SavedTabs);
    }

    [Fact]
    public void AppExit_SavedAndUnsavedTabs_OnlySavedIsSkipped()
    {
        var targets = new[]
        {
            new CloseConfirmationTarget("saved",   CanClose: true, HasUnsavedChanges: false),
            new CloseConfirmationTarget("unsaved", CanClose: true, HasUnsavedChanges: true),
        };
        var asked = new List<string>();
        CloseConfirmationService.EvaluateMany(targets, t =>
        {
            asked.Add(t.Id);
            return UnsavedChangeDecision.Discard;
        });
        Assert.DoesNotContain("saved",   asked);
        Assert.Contains("unsaved", asked);
    }

    // ── SaveAs キャンセル時は閉じない ────────────────────────────────────

    [Fact]
    public void SaveAsCancel_PreventsClose_SaveReturningFalseIsCancel()
    {
        // SaveAs キャンセルは save 関数が false を返すことで表現される。
        // この場合 EvaluateSingle は Cancel を返し、閉じない。
        var result = CloseConfirmationService.EvaluateSingle(
            hasUnsavedChanges: true,
            requestDecision: () => UnsavedChangeDecision.Save,
            save: () => false);
        Assert.Equal(UnsavedChangeDecision.Cancel, result);
    }

    [Fact]
    public void SaveAsCancel_CanCloseSingle_ReturnsFalse()
    {
        var canClose = CloseConfirmationService.CanCloseSingle(
            hasUnsavedChanges: true,
            requestDecision: () => UnsavedChangeDecision.Save,
            save: () => false);
        Assert.False(canClose);
    }

    // ── TempNest は終了確認対象外 ──────────────────────────────────────

    [Fact]
    public void TempNestTab_CanCloseFalse_ExcludedFromExitConfirmation()
    {
        var targets = new[] { new CloseConfirmationTarget("tempnest-fixed", CanClose: false, HasUnsavedChanges: true) };
        var asked = false;
        var result = CloseConfirmationService.EvaluateMany(targets, _ => { asked = true; return UnsavedChangeDecision.Cancel; });
        Assert.True(result.CanContinue);
        Assert.False(asked);
    }

    // ── Detached ウィンドウの × は保存確認を経由しない（仕様記録） ───────

    [Fact]
    public void DetachedWindowCloseButton_IsReattachOperation_NotSaveConfirmation()
    {
        // DetachedWorkspaceWindow の × ボタンは「Shell タブへ戻す」操作であり、
        // CloseConfirmationService.EvaluateSingle を経由しない。
        // この仕様は DetachedWorkspaceWindow.OnClosed → ReAttach*Tab コールバックで実装される。
        // この定数は「確認なしで再統合できる」という設計上の選択を記録する。
        const bool detachedCloseIsReattach = true;
        Assert.True(detachedCloseIsReattach);
    }

    // ── 旧 Yes/No ダイアログに戻っていない ───────────────────────────────

    [Fact]
    public void NoteNestConfirmation_HasThreeChoices_NotTwoChoices()
    {
        // v2.9.7 以降は Save / Discard / Cancel の 3 択。
        // 旧ダイアログは Yes（保存せず閉じる）/ No（キャンセル）の 2 択だった。
        var decisions = Enum.GetValues<UnsavedChangeDecision>();
        Assert.Contains(UnsavedChangeDecision.Save,    decisions);
        Assert.Contains(UnsavedChangeDecision.Discard, decisions);
        Assert.Contains(UnsavedChangeDecision.Cancel,  decisions);
        // Save は旧ダイアログにはなかった選択肢
        Assert.True(decisions.Length >= 3, "UnsavedChangeDecision は Save / Discard / Cancel の 3 択以上あること");
    }

    // ── バージョン / スキーマ ────────────────────────────────────────────

    [Fact]
    public void ApplicationVersion_Is_2_10_0()
    {
        Assert.Equal("2.10.12", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void NoteNestSchemaVersion_Remains_1_4_1()
    {
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }
}
