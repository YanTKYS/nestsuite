using NestSuite;
using NestSuite.TempNest;
using Xunit;
using NestSuite.ViewModels;
using NestSuite.Models;
using System.IO;

namespace NestSuite.Tests;

/// <summary>
/// v2.6.2: TempNest タブ定義・スロットモデル・スロット ViewModel の動作確認テスト。
/// TempNestStoreService はファイルパスが固定のため Load() の戻り件数のみを検証する。
/// </summary>
public class TempNestTests
{
    // ── CreateTempTab ─────────────────────────────────────────────────────

    [Fact]
    public void CreateTempTab_HasFixedId()
    {
        var tab = NestSuiteTabFactory.CreateTempTab();
        Assert.Equal("tempnest-fixed", tab.Id);
    }

    [Fact]
    public void CreateTempTab_WorkspaceKind_IsTemp()
    {
        var tab = NestSuiteTabFactory.CreateTempTab();
        Assert.Equal(NestSuiteWorkspaceKind.Temp, tab.WorkspaceKind);
    }

    [Fact]
    public void CreateTempTab_CanClose_IsFalse()
    {
        var tab = NestSuiteTabFactory.CreateTempTab();
        Assert.False(tab.CanClose);
    }

    [Fact]
    public void CreateTempTab_FilePath_IsNull()
    {
        // FilePath=null により NestSuiteShellWindow.SaveSession() の
        // .Where(t => t.FilePath != null) フィルタでセッションから除外される。
        var tab = NestSuiteTabFactory.CreateTempTab();
        Assert.Null(tab.FilePath);
    }

    [Fact]
    public void CreateTempTab_IsModified_IsFalse()
    {
        var tab = NestSuiteTabFactory.CreateTempTab();
        Assert.False(tab.IsModified);
    }

    [Fact]
    public void CreateTempTab_DisplayName_IsTemp()
    {
        var tab = NestSuiteTabFactory.CreateTempTab();
        Assert.Equal("Temp", tab.DisplayName);
    }

    [Fact]
    public void GetExtension_Temp_ThrowsArgumentOutOfRangeException()
    {
        // Temp は固定タブであり、ファイル拡張子に対応しない。
        Assert.Throws<ArgumentOutOfRangeException>(
            () => NestSuiteTabFactory.GetExtension(NestSuiteWorkspaceKind.Temp));
    }

    [Fact]
    public void TryGetKind_TempExtension_DoesNotExist()
    {
        // ".tempnest" のような拡張子は定義されていない。
        Assert.False(NestSuiteTabFactory.TryGetKind("file.tempnest", out _));
    }

    // ── TempNestSlot モデル ───────────────────────────────────────────────

    [Fact]
    public void TempNestSlot_DefaultTitle_IsEmpty()
    {
        var slot = new TempNestSlot();
        Assert.Equal("", slot.Title);
    }

    [Fact]
    public void TempNestSlot_DefaultBody_IsEmpty()
    {
        var slot = new TempNestSlot();
        Assert.Equal("", slot.Body);
    }

    // ── TempNestStoreService ─────────────────────────────────────────────

    [Fact]
    public void Load_AlwaysReturnsFourSlots()
    {
        var slots = TempNestStoreService.Load();
        Assert.Equal(4, slots.Length);
    }

    [Fact]
    public void Load_AllSlotsAreNonNull()
    {
        var slots = TempNestStoreService.Load();
        Assert.All(slots, slot => Assert.NotNull(slot));
    }

    // ── TempNestSlotViewModel ClearCommand CanExecute ────────────────────

    [Fact]
    public void ClearCommand_BothTitleAndBodyEmpty_IsDisabled()
    {
        var vm = new TempNestSlotViewModel();
        Assert.False(vm.ClearCommand.CanExecute(null));
    }

    [Fact]
    public void ClearCommand_TitleNonEmpty_IsEnabled()
    {
        var vm = new TempNestSlotViewModel { Title = "メモタイトル" };
        Assert.True(vm.ClearCommand.CanExecute(null));
    }

    [Fact]
    public void ClearCommand_BodyNonEmpty_IsEnabled()
    {
        var vm = new TempNestSlotViewModel { Body = "本文あり" };
        Assert.True(vm.ClearCommand.CanExecute(null));
    }

    [Fact]
    public void ClearCommand_BothTitleAndBodyNonEmpty_IsEnabled()
    {
        var vm = new TempNestSlotViewModel { Title = "タイトル", Body = "本文" };
        Assert.True(vm.ClearCommand.CanExecute(null));
    }

    // ── TempNestSlotViewModel CopyBodyCommand CanExecute ─────────────────

    [Fact]
    public void CopyBodyCommand_EmptyBody_IsDisabled()
    {
        var vm = new TempNestSlotViewModel();
        Assert.False(vm.CopyBodyCommand.CanExecute(null));
    }

    [Fact]
    public void CopyBodyCommand_NonEmptyBody_IsEnabled()
    {
        var vm = new TempNestSlotViewModel { Body = "コピーする内容" };
        Assert.True(vm.CopyBodyCommand.CanExecute(null));
    }

    [Fact]
    public void CopyBodyCommand_WhitespaceOnlyBody_IsEnabled()
    {
        // string.IsNullOrEmpty("  ") == false なので実際にはコピー可能。
        // Copy CanExecute は IsNullOrEmpty で判定するため、空白のみは有効。
        var vm = new TempNestSlotViewModel { Body = "  " };
        Assert.True(vm.CopyBodyCommand.CanExecute(null));
    }

    // ── TempNestSlotViewModel ToSlot / LoadFromSlot ──────────────────────

    [Fact]
    public void ToSlot_PreservesTitle()
    {
        var vm = new TempNestSlotViewModel { Title = "テストタイトル", Body = "本文" };
        Assert.Equal("テストタイトル", vm.ToSlot().Title);
    }

    [Fact]
    public void ToSlot_PreservesBody()
    {
        var vm = new TempNestSlotViewModel { Title = "T", Body = "本文テスト" };
        Assert.Equal("本文テスト", vm.ToSlot().Body);
    }

    [Fact]
    public void LoadFromSlot_RestoresTitleAndBody()
    {
        var vm = new TempNestSlotViewModel();
        vm.LoadFromSlot(new TempNestSlot { Title = "読み込みタイトル", Body = "読み込み本文" });
        Assert.Equal("読み込みタイトル", vm.Title);
        Assert.Equal("読み込み本文", vm.Body);
    }

    [Fact]
    public void ToSlot_LoadFromSlot_RoundTrip_PreservesContent()
    {
        var original = new TempNestSlotViewModel { Title = "往復テスト", Body = "内容確認" };
        var slot = original.ToSlot();
        var restored = new TempNestSlotViewModel();
        restored.LoadFromSlot(slot);
        Assert.Equal(original.Title, restored.Title);
        Assert.Equal(original.Body, restored.Body);
    }

    // ── バージョン ────────────────────────────────────────────────────────

    // ── TN-2: TempNest スロットのクリア確認ダイアログ ────────────────────

    [Fact]
    public void TempNestSlotViewModel_ClearCommand_CanExecute_False_WhenEmpty()
    {
        var slot = new TempNestSlotViewModel();
        Assert.False(slot.ClearCommand.CanExecute(null));
    }

    [Fact]
    public void TempNestSlotViewModel_ClearCommand_CanExecute_True_WhenTitleNonEmpty()
    {
        var slot = new TempNestSlotViewModel();
        slot.Title = "テスト";
        Assert.True(slot.ClearCommand.CanExecute(null));
    }

    [Fact]
    public void TempNestSlotViewModel_ClearCommand_CanExecute_True_WhenBodyNonEmpty()
    {
        var slot = new TempNestSlotViewModel();
        slot.Body = "メモ内容";
        Assert.True(slot.ClearCommand.CanExecute(null));
    }

    [Fact]
    public void TempNestSlotViewModel_ConfirmClear_Property_DefaultsToNull()
    {
        var slot = new TempNestSlotViewModel();
        Assert.Null(slot.ConfirmClear);
    }

    [Fact]
    public void TempNestSlotViewModel_ClearCommand_Execute_ClearsWithoutConfirm_WhenConfirmClearNull()
    {
        var slot = new TempNestSlotViewModel();
        slot.Title = "タイトル";
        slot.Body  = "本文";
        // ConfirmClear = null → 確認なしでクリア
        slot.ClearCommand.Execute(null);
        Assert.Equal("", slot.Title);
        Assert.Equal("", slot.Body);
    }

    [Fact]
    public void TempNestSlotViewModel_ClearCommand_Execute_ClearsWhenConfirmClearReturnsTrue()
    {
        var slot = new TempNestSlotViewModel();
        slot.Title = "タイトル";
        slot.Body  = "本文";
        slot.ConfirmClear = () => true;
        slot.ClearCommand.Execute(null);
        Assert.Equal("", slot.Title);
        Assert.Equal("", slot.Body);
    }

    [Fact]
    public void TempNestSlotViewModel_ClearCommand_Execute_DoesNotClearWhenConfirmClearReturnsFalse()
    {
        var slot = new TempNestSlotViewModel();
        slot.Title = "タイトル";
        slot.Body  = "本文";
        slot.ConfirmClear = () => false;
        slot.ClearCommand.Execute(null);
        Assert.Equal("タイトル", slot.Title);
        Assert.Equal("本文", slot.Body);
    }

    // ── SH-28: TempNest クリア完了の一時フィードバック ────────────────────

    [Fact]
    public void TempNestSlotViewModel_FeedbackMessage_IsEmpty_ByDefault()
    {
        var slot = new TempNestSlotViewModel();
        Assert.Equal("", slot.FeedbackMessage);
        Assert.False(slot.HasFeedback);
    }

    [Fact]
    public void TempNestSlotViewModel_ClearCommand_Execute_ShowsClearedFeedback()
    {
        var slot = new TempNestSlotViewModel();
        slot.Title = "タイトル";
        slot.Body  = "本文";
        slot.ClearCommand.Execute(null);
        Assert.Equal("クリアしました", slot.FeedbackMessage);
        Assert.True(slot.HasFeedback);
    }

    [Fact]
    public void TempNestSlotViewModel_ClearCommand_Execute_DoesNotShowFeedback_WhenConfirmClearReturnsFalse()
    {
        var slot = new TempNestSlotViewModel();
        slot.Title = "タイトル";
        slot.ConfirmClear = () => false;
        slot.ClearCommand.Execute(null);
        Assert.False(slot.HasFeedback);
    }

    [Fact]
    public void TempNestSlotViewModel_StopFeedback_ClearsFeedbackMessage()
    {
        var slot = new TempNestSlotViewModel();
        slot.Title = "タイトル";
        slot.ClearCommand.Execute(null);
        slot.StopFeedback();
        Assert.Equal("", slot.FeedbackMessage);
        Assert.False(slot.HasFeedback);
    }

    // TD-75a-2 (v2.16.27): TN-2 / L14 / L15 / CH-13 の release notes 存在確認・
    // v2.10.3 存在確認は NestSuiteDocsContractTests.ReleaseNoteVersionAndIdRecords へ
    // 移設した（(v2.10.3, TN-2) / (v2.10.3, L14) / (v2.10.3, L15) のデータ行。
    // CH-13 は実際の完了バージョンである (v2.10.9, CH-13) として移設し、
    // ChatNestWorkspaceFeatureRecordsTests 側の同一チェックと重複していたぶんを統合した）。
    // 検証内容は変えていない。
}
