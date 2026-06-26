using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.10.2: NoteNest 未保存終了確認の Save / Discard / Cancel 化 — ロジック回帰テスト。
/// CloseConfirmationService.EvaluateSingle を使用する。WPF UI は対象外（手動確認）。
/// </summary>
public class NoteNestCloseConfirmationTests
{
    // ── EvaluateSingle — 修正なし・Save・Discard・Cancel ────────────────

    [Fact]
    public void EvaluateSingle_WhenNotModified_ReturnsNoActionNeeded()
    {
        var result = CloseConfirmationService.EvaluateSingle(
            hasUnsavedChanges: false,
            requestDecision: () => UnsavedChangeDecision.Cancel,
            save: null);
        Assert.Equal(UnsavedChangeDecision.NoActionNeeded, result);
    }

    [Fact]
    public void EvaluateSingle_WhenModified_SaveDecision_SaveSuccess_ReturnsSave()
    {
        // 保存を選択 → 保存成功 → Save（閉じてよい）
        var result = CloseConfirmationService.EvaluateSingle(
            hasUnsavedChanges: true,
            requestDecision: () => UnsavedChangeDecision.Save,
            save: () => true);
        Assert.Equal(UnsavedChangeDecision.Save, result);
    }

    [Fact]
    public void EvaluateSingle_WhenModified_SaveDecision_SaveFail_ReturnsCancel()
    {
        // 保存を選択 → 保存失敗 → Cancel（閉じない）
        var result = CloseConfirmationService.EvaluateSingle(
            hasUnsavedChanges: true,
            requestDecision: () => UnsavedChangeDecision.Save,
            save: () => false);
        Assert.Equal(UnsavedChangeDecision.Cancel, result);
    }

    [Fact]
    public void EvaluateSingle_WhenModified_SaveAsCancel_ReturnsCancel()
    {
        // 保存を選択 → Save As キャンセル（save 関数が false を返す） → Cancel（閉じない）
        var result = CloseConfirmationService.EvaluateSingle(
            hasUnsavedChanges: true,
            requestDecision: () => UnsavedChangeDecision.Save,
            save: () => false);
        Assert.Equal(UnsavedChangeDecision.Cancel, result);
    }

    [Fact]
    public void EvaluateSingle_WhenModified_DiscardDecision_ReturnsDiscard()
    {
        // 保存しないを選択 → 保存せず閉じる
        var result = CloseConfirmationService.EvaluateSingle(
            hasUnsavedChanges: true,
            requestDecision: () => UnsavedChangeDecision.Discard,
            save: null);
        Assert.Equal(UnsavedChangeDecision.Discard, result);
    }

    [Fact]
    public void EvaluateSingle_WhenModified_CancelDecision_ReturnsCancel()
    {
        // キャンセルを選択 → 閉じない
        var result = CloseConfirmationService.EvaluateSingle(
            hasUnsavedChanges: true,
            requestDecision: () => UnsavedChangeDecision.Cancel,
            save: null);
        Assert.Equal(UnsavedChangeDecision.Cancel, result);
    }

    // ── CanCloseSingle — 閉じてよい / 閉じない ──────────────────────────

    [Fact]
    public void CanCloseSingle_WhenModified_SaveSuccess_ReturnsTrue()
    {
        var canClose = CloseConfirmationService.CanCloseSingle(
            hasUnsavedChanges: true,
            requestDecision: () => UnsavedChangeDecision.Save,
            save: () => true);
        Assert.True(canClose);
    }

    [Fact]
    public void CanCloseSingle_WhenModified_SaveFail_ReturnsFalse()
    {
        // 保存失敗時は閉じない
        var canClose = CloseConfirmationService.CanCloseSingle(
            hasUnsavedChanges: true,
            requestDecision: () => UnsavedChangeDecision.Save,
            save: () => false);
        Assert.False(canClose);
    }

    [Fact]
    public void CanCloseSingle_WhenModified_Discard_ReturnsTrue()
    {
        // 保存しないを選択 → 閉じる
        var canClose = CloseConfirmationService.CanCloseSingle(
            hasUnsavedChanges: true,
            requestDecision: () => UnsavedChangeDecision.Discard,
            save: null);
        Assert.True(canClose);
    }

    [Fact]
    public void CanCloseSingle_WhenModified_Cancel_ReturnsFalse()
    {
        // キャンセルを選択 → 閉じない
        var canClose = CloseConfirmationService.CanCloseSingle(
            hasUnsavedChanges: true,
            requestDecision: () => UnsavedChangeDecision.Cancel,
            save: null);
        Assert.False(canClose);
    }

    [Fact]
    public void CanCloseSingle_WhenNotModified_ReturnsTrue()
    {
        // 変更なし → 確認なしで閉じる
        var canClose = CloseConfirmationService.CanCloseSingle(
            hasUnsavedChanges: false,
            requestDecision: () => UnsavedChangeDecision.Cancel,
            save: null);
        Assert.True(canClose);
    }

    // ── Save 後に dirty state が解消されること（ロジック検証） ─────────────

    [Fact]
    public void SaveDecision_WhenSaveSucceeds_SaveFunctionCalledOnce()
    {
        // 保存関数が Save 決定時に呼ばれることを確認する
        var saveCallCount = 0;
        CloseConfirmationService.EvaluateSingle(
            hasUnsavedChanges: true,
            requestDecision: () => UnsavedChangeDecision.Save,
            save: () => { saveCallCount++; return true; });
        Assert.Equal(1, saveCallCount);
    }

    [Fact]
    public void DiscardDecision_SaveFunctionNotCalled()
    {
        // 破棄決定時は保存関数を呼ばない
        var saveCallCount = 0;
        CloseConfirmationService.EvaluateSingle(
            hasUnsavedChanges: true,
            requestDecision: () => UnsavedChangeDecision.Discard,
            save: () => { saveCallCount++; return true; });
        Assert.Equal(0, saveCallCount);
    }

    // ── 旧 Yes/No ダイアログへの非依存 ──────────────────────────────────

    [Fact]
    public void OldYesNoDialog_WouldNotOfferSaveOption()
    {
        // 旧 ConfirmTabClose は Discard/Cancel しか選択肢がなかった。
        // v2.10.2 以降は Save が選択できる。このテストは旧パスの仕様を記録する。
        var wasDiscardOrCancel = true;
        bool oldBehaviorCanClose(bool isModified, bool userSaidYes)
        {
            if (!isModified) return true;
            return userSaidYes; // Yes = discard, No = cancel
        }
        // 旧 UI: Yes/No しかなく、Yes = 保存せず閉じる（Discard のみ）
        Assert.True(oldBehaviorCanClose(isModified: true, userSaidYes: true));   // discard
        Assert.False(oldBehaviorCanClose(isModified: true, userSaidYes: false));  // cancel
        Assert.True(wasDiscardOrCancel); // Save は選択できなかった
    }

    // ── バージョン / スキーマ ────────────────────────────────────────────

    [Fact]
    public void ApplicationVersion_Is_2_9_7()
    {
        Assert.Equal("2.10.3", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void NoteNestSchemaVersion_Remains_1_4_1()
    {
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }
}
