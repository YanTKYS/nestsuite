using System.IO;
using NestSuite.Models;
using NestSuite.TempNest;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.10.3: backlog 棚卸し + 軽量改善まとめ — TN-2 / L14 / L15 回帰テスト。
/// </summary>
public class LightImprovementsV2103Tests
{
    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    // ── バージョン ────────────────────────────────────────────────────────

    [Fact]
    public void ApplicationVersion_Is_2_10_3()
    {
        Assert.Equal("2.10.12", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void NoteNestSchemaVersion_Remains_1_4_1()
    {
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }

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

    // ── L14 / L15: ステータスバー — docs / backlog 確認 ─────────────────

    [Fact]
    public void Backlog_TN2_IsMarkedComplete()
    {
        var backlog = ReadBacklog();
        Assert.Contains("~~TN-2~~", backlog);
    }

    [Fact]
    public void Backlog_L14_IsMarkedComplete()
    {
        var backlog = ReadBacklog();
        Assert.Contains("~~L14~~", backlog);
    }

    [Fact]
    public void Backlog_L15_IsMarkedComplete()
    {
        var backlog = ReadBacklog();
        Assert.Contains("~~L15~~", backlog);
    }

    [Fact]
    public void Backlog_CH13_InChatNestSection()
    {
        var backlog = ReadBacklog();
        Assert.Contains("CH-13", backlog);
    }

    // ── release-notes.md ─────────────────────────────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V2103()
    {
        var releaseNotes = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(releaseNotes));
        Assert.Contains("v2.10.3", File.ReadAllText(releaseNotes));
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private string ReadBacklog()
    {
        var path = Path.Combine(RepoRoot, "docs", "backlog.md");
        Assert.True(File.Exists(path), $"backlog.md not found: {path}");
        return File.ReadAllText(path);
    }
}
