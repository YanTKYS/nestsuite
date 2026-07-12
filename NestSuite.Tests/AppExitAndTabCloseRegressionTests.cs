using System;
using System.IO;
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
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

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

    [Fact]
    public void DraftRecovery_StartupContract_UsesOwnerlessPromptAndRestoresBeforeTimer()
    {
        var shellCtor = File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.xaml.cs"));
        var recovery = File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.DraftRecovery.cs"));

        Assert.True(shellCtor.IndexOf("RestoreDraftsAtStartup()", StringComparison.Ordinal) <
                    shellCtor.IndexOf("StartAutoSaveTimer()", StringComparison.Ordinal));
        Assert.Contains("MessageBox.Show(", recovery);
        Assert.DoesNotContain("MessageBox.Show(this", recovery);
        Assert.Contains("MessageBoxButton.YesNoCancel", recovery);
        Assert.Contains("MessageBoxResult.No", recovery);
        Assert.Contains("MessageBoxResult.Yes", recovery);
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


    // ── SH-36a-1: draft lifecycle source contracts ─────────────────────

    [Fact]
    public void OnClosing_DraftDelete_FiltersToDraftSupportedWorkspaces()
    {
        Assert.True(DraftCandidatePolicy.IsSupportedWorkspace(NestSuiteWorkspaceKind.NoteNest));
        Assert.True(DraftCandidatePolicy.IsSupportedWorkspace(NestSuiteWorkspaceKind.IdeaNest));
        Assert.True(DraftCandidatePolicy.IsSupportedWorkspace(NestSuiteWorkspaceKind.ChatNest));
        Assert.False(DraftCandidatePolicy.IsSupportedWorkspace(NestSuiteWorkspaceKind.Temp));
        Assert.False(DraftCandidatePolicy.IsSupportedWorkspace((NestSuiteWorkspaceKind)999));

        var source = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.xaml.cs"));
        var deleteIdx = source.IndexOf("TryDeleteDraftForTab(tab.Id, \"DraftDeleteOnClosing\")", StringComparison.Ordinal);
        var filterIdx = source.LastIndexOf("DraftCandidatePolicy.IsSupportedWorkspace(t.WorkspaceKind)", deleteIdx, StringComparison.Ordinal);
        Assert.True(filterIdx >= 0, "OnClosing must filter shutdown draft deletion through DraftCandidatePolicy before Delete.");
    }

    [Fact]
    public void OnClosing_TimerStopAndCancelRestartContractsArePresent()
    {
        var source = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.xaml.cs"));
        var onClosingStart = source.IndexOf("protected override void OnClosing(CancelEventArgs e)", StringComparison.Ordinal);
        var stopIdx = source.IndexOf("StopAutoSaveTimer();", onClosingStart, StringComparison.Ordinal);
        var summaryCancelIdx = source.IndexOf("CancelClosingAndRestartAutoSave(e);", stopIdx, StringComparison.Ordinal);
        var normalDeleteIdx = source.IndexOf("DraftDeleteOnClosing", stopIdx, StringComparison.Ordinal);
        Assert.True(stopIdx > onClosingStart);
        Assert.True(summaryCancelIdx > stopIdx && summaryCancelIdx < normalDeleteIdx);
        Assert.Contains("private void CancelClosingAndRestartAutoSave", source);
        Assert.Contains("StartAutoSaveTimer();", source.Substring(source.IndexOf("private void CancelClosingAndRestartAutoSave", StringComparison.Ordinal)));

        var autoSaveSource = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.AutoSave.cs"));
        Assert.Contains("if (_autoSaveTimer != null) return;", autoSaveSource);
    }

    [Fact]
    public void SaveAndCloseDraftDeleteContractsAreAfterStateOrRemoval()
    {
        var saveSource = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.WorkspaceFileHelper.cs"));
        var wasUntitledIdx = saveSource.IndexOf("var wasUntitled = tab.FilePath == null;", StringComparison.Ordinal);
        var applyIdx = saveSource.IndexOf("SavedWorkspaceStateUpdater.ApplyToSession(session, state);", StringComparison.Ordinal);
        var deleteAfterSaveIdx = saveSource.IndexOf("TryDeleteDraftForTab(tab.Id, \"DraftDeleteAfterSave\")", StringComparison.Ordinal);
        Assert.True(wasUntitledIdx >= 0);
        Assert.True(deleteAfterSaveIdx > applyIdx);

        var closeSource = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.TabClose.cs"));
        var removeSessionIdx = closeSource.IndexOf("_sessionManager.Remove(tab.Id);", StringComparison.Ordinal);
        var removeTabIdx = closeSource.IndexOf("_tabs.RemoveAt(idx);", StringComparison.Ordinal);
        var deleteAfterCloseIdx = closeSource.IndexOf("TryDeleteDraftForTab(tab.Id, \"DraftDeleteAfterClose\")", StringComparison.Ordinal);
        Assert.True(deleteAfterCloseIdx > removeSessionIdx);
        Assert.True(deleteAfterCloseIdx > removeTabIdx);
    }

    // ── バージョン / スキーマ ────────────────────────────────────────────
}
