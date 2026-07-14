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

    // ── TN-3 (v2.18.0): PromoteToNoteCommand CanExecute ──────────────────

    [Fact]
    public void PromoteToNoteCommand_EmptyBody_IsDisabled()
    {
        var slot = new TempNestSlotViewModel();
        Assert.False(slot.PromoteToNoteCommand.CanExecute(null));
    }

    [Fact]
    public void PromoteToNoteCommand_WhitespaceOnlyBody_IsDisabled()
    {
        var slot = new TempNestSlotViewModel { Body = "   \r\n\t " };
        Assert.False(slot.PromoteToNoteCommand.CanExecute(null));
    }

    [Fact]
    public void PromoteToNoteCommand_NonEmptyBody_IsEnabled()
    {
        var slot = new TempNestSlotViewModel { Body = "断片" };
        Assert.True(slot.PromoteToNoteCommand.CanExecute(null));
    }

    [Fact]
    public void PromoteToNoteCommand_DisabledWhilePromoting()
    {
        var slot = new TempNestSlotViewModel { Body = "断片" };
        var reentrantCanExecute = true;
        slot.PromoteRequested = s =>
        {
            reentrantCanExecute = slot.PromoteToNoteCommand.CanExecute(null);
            return false;
        };

        slot.PromoteToNoteCommand.Execute(null);

        Assert.False(reentrantCanExecute);
        Assert.True(slot.PromoteToNoteCommand.CanExecute(null));
    }

    // ── TN-3: PromoteRequested 呼び出し・元スロットの保持/消去 ─────────────

    [Fact]
    public void PromoteRequested_DefaultsToNull()
    {
        var slot = new TempNestSlotViewModel();
        Assert.Null(slot.PromoteRequested);
    }

    [Fact]
    public void PromoteToNoteCommand_Execute_InvokesPromoteRequestedWithSelf()
    {
        var slot = new TempNestSlotViewModel { Body = "断片" };
        TempNestSlotViewModel? received = null;
        slot.PromoteRequested = s => { received = s; return false; };

        slot.PromoteToNoteCommand.Execute(null);

        Assert.Same(slot, received);
    }

    [Fact]
    public void PromoteToNoteCommand_Execute_ResultTrue_ClearsSlot()
    {
        var slot = new TempNestSlotViewModel { Title = "タイトル", Body = "断片" };
        slot.PromoteRequested = _ => true;

        slot.PromoteToNoteCommand.Execute(null);

        Assert.Equal("", slot.Title);
        Assert.Equal("", slot.Body);
    }

    [Fact]
    public void PromoteToNoteCommand_Execute_ResultFalse_KeepsSlotContent()
    {
        var slot = new TempNestSlotViewModel { Title = "タイトル", Body = "断片" };
        slot.PromoteRequested = _ => false;

        slot.PromoteToNoteCommand.Execute(null);

        Assert.Equal("タイトル", slot.Title);
        Assert.Equal("断片", slot.Body);
    }

    [Fact]
    public void PromoteToNoteCommand_Execute_ResultNull_KeepsSlotContent_AndNoFeedback()
    {
        var slot = new TempNestSlotViewModel { Title = "タイトル", Body = "断片" };
        slot.PromoteRequested = _ => null;

        slot.PromoteToNoteCommand.Execute(null);

        Assert.Equal("タイトル", slot.Title);
        Assert.Equal("断片", slot.Body);
        Assert.False(slot.HasFeedback);
    }

    [Fact]
    public void PromoteToNoteCommand_Execute_ResultNonNull_ShowsPromotedFeedback()
    {
        var slot = new TempNestSlotViewModel { Body = "断片" };
        slot.PromoteRequested = _ => false;

        slot.PromoteToNoteCommand.Execute(null);

        Assert.Equal("NoteNestへ昇格しました", slot.FeedbackMessage);
        Assert.True(slot.HasFeedback);
    }

    [Fact]
    public void PromoteToNoteCommand_Execute_WhenPromoteRequestedIsNull_DoesNothing()
    {
        var slot = new TempNestSlotViewModel { Title = "タイトル", Body = "断片" };

        slot.PromoteToNoteCommand.Execute(null);

        Assert.Equal("タイトル", slot.Title);
        Assert.Equal("断片", slot.Body);
        Assert.False(slot.HasFeedback);
    }

    // ── TN-3: PromotedNoteTitleGenerator ──────────────────────────────────

    [Fact]
    public void PromotedNoteTitleGenerator_UsesFirstNonEmptyLine()
    {
        var title = NestSuite.Services.PromotedNoteTitleGenerator.Generate("最初の行\n2行目");
        Assert.Equal("最初の行", title);
    }

    [Fact]
    public void PromotedNoteTitleGenerator_SkipsLeadingBlankLines()
    {
        var title = NestSuite.Services.PromotedNoteTitleGenerator.Generate("\n\n  \n本題の行\n続き");
        Assert.Equal("本題の行", title);
    }

    [Fact]
    public void PromotedNoteTitleGenerator_TrimsWhitespaceAroundLine()
    {
        var title = NestSuite.Services.PromotedNoteTitleGenerator.Generate("   前後に空白がある行   \n続き");
        Assert.Equal("前後に空白がある行", title);
    }

    [Fact]
    public void PromotedNoteTitleGenerator_EmptyContent_ReturnsFallback()
    {
        var title = NestSuite.Services.PromotedNoteTitleGenerator.Generate("");
        Assert.Equal("TempNestから昇格", title);
    }

    [Fact]
    public void PromotedNoteTitleGenerator_WhitespaceOnlyContent_ReturnsFallback()
    {
        var title = NestSuite.Services.PromotedNoteTitleGenerator.Generate("   \n\t\n   ");
        Assert.Equal("TempNestから昇格", title);
    }

    [Fact]
    public void PromotedNoteTitleGenerator_LongLine_TruncatesWithEllipsis()
    {
        var longLine = new string('あ', 60);
        var title = NestSuite.Services.PromotedNoteTitleGenerator.Generate(longLine);
        Assert.Equal(new string('あ', 40) + "…", title);
    }

    [Fact]
    public void PromotedNoteTitleGenerator_LineAtMaxLength_DoesNotTruncate()
    {
        var line = new string('あ', 40);
        var title = NestSuite.Services.PromotedNoteTitleGenerator.Generate(line);
        Assert.Equal(line, title);
    }

    // ── TN-3: TempNestWorkspaceView.xaml 静的確認 ─────────────────────────
    // 各スロットから昇格操作へ到達できること、既存のコピー・クリア操作を失っていないことを
    // XAML文字列の存在確認だけで最小限に検証する（表現の細部までは固定しない）。

    [Fact]
    public void TempNestWorkspaceView_Xaml_HasPromoteButtonForEachSlot()
    {
        var xaml = ReadTempNestWorkspaceViewXaml();
        for (var i = 1; i <= 4; i++)
        {
            Assert.Contains($"TempNest.Slot{i}.PromoteButton", xaml);
            Assert.Contains($"Slot{i}.PromoteToNoteCommand", xaml);
        }
    }

    [Fact]
    public void TempNestWorkspaceView_Xaml_StillHasCopyAndClearButtonsForEachSlot()
    {
        var xaml = ReadTempNestWorkspaceViewXaml();
        for (var i = 1; i <= 4; i++)
        {
            Assert.Contains($"TempNest.Slot{i}.CopyButton", xaml);
            Assert.Contains($"TempNest.Slot{i}.ClearButton", xaml);
            Assert.Contains($"Slot{i}.CopyBodyCommand", xaml);
            Assert.Contains($"Slot{i}.ClearCommand", xaml);
        }
    }

    private static string ReadTempNestWorkspaceViewXaml()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "NestSuite.Tests.csproj")))
            dir = dir.Parent;

        Assert.NotNull(dir);
        var path = Path.Combine(dir!.Parent!.FullName, "NestSuite", "NestSuite", "TempNest", "TempNestWorkspaceView.xaml");
        Assert.True(File.Exists(path), $"TempNestWorkspaceView.xaml not found: {path}");
        return File.ReadAllText(path);
    }
}
