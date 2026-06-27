using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.9.1 SH-21: NoteNest / IdeaNest / ChatNest 別ウィンドウ表示 — タブ状態・フラグ・スキーマ整合のテスト。
/// Shell UI（WPF）を必要とするウィンドウ操作は対象外。
/// </summary>
public class DetachedWorkspaceTests
{
    // ── IsDetachable ─────────────────────────────────────────────────────

    [Fact]
    public void NoteNestTab_IsDetachable_WhenNotDetached()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);
        Assert.True(tab.IsDetachable);
    }

    [Fact]
    public void IdeaNestTab_IsDetachable_WhenNotDetached()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.IdeaNest);
        Assert.True(tab.IsDetachable);
    }

    [Fact]
    public void ChatNestTab_IsDetachable_WhenNotDetached()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.ChatNest);
        Assert.True(tab.IsDetachable);
    }

    [Fact]
    public void TempTab_IsNotDetachable()
    {
        var tab = new NestSuiteDocumentTab { Id = "t", WorkspaceKind = NestSuiteWorkspaceKind.Temp, DisplayName = "Temp", CanClose = false };
        Assert.False(tab.IsDetachable);
    }

    [Fact]
    public void NoteNestTab_IsNotDetachable_WhenAlreadyDetached()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest) with { IsDetached = true };
        Assert.False(tab.IsDetachable);
    }

    [Fact]
    public void IdeaNestTab_IsNotDetachable_WhenAlreadyDetached()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.IdeaNest) with { IsDetached = true };
        Assert.False(tab.IsDetachable);
    }

    [Fact]
    public void ChatNestTab_IsNotDetachable_WhenAlreadyDetached()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.ChatNest) with { IsDetached = true };
        Assert.False(tab.IsDetachable);
    }

    // ── IsDetached フラグ ────────────────────────────────────────────────

    [Fact]
    public void NewNoteNestTab_IsNotDetached_ByDefault()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);
        Assert.False(tab.IsDetached);
    }

    [Fact]
    public void Tab_WithIsDetachedTrue_ReflectsDetachedState()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest) with { IsDetached = true };
        Assert.True(tab.IsDetached);
        Assert.True(tab.IsNoteNest);
        Assert.False(tab.IsDetachable);
    }

    [Fact]
    public void Tab_WithIsDetachedTrue_CanBeResetToFalse()
    {
        var detached = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest) with { IsDetached = true };
        var reattached = detached with { IsDetached = false };

        Assert.False(reattached.IsDetached);
        Assert.True(reattached.IsDetachable);
    }

    [Fact]
    public void IsDetached_DoesNotAffectIsModified_OrFilePath()
    {
        var tab = NestSuiteTabFactory.FromFilePath(@"C:\work\notes.notenest") with
        {
            IsDetached = true,
            IsModified = true
        };

        Assert.True(tab.IsDetached);
        Assert.True(tab.IsModified);
        Assert.Equal(@"C:\work\notes.notenest", tab.FilePath);
    }

    // ── SavedWorkspaceStateUpdater — IsDetached を引き継ぐ ───────────────

    [Fact]
    public void SavedWorkspaceStateUpdater_PreservesIsDetached_WhenTabIsDetached()
    {
        var currentTab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest) with { IsDetached = true };
        var path = @"C:\work\notes.notenest";

        var ok = NestSuite.Services.SavedWorkspaceStateUpdater.TryCreate(currentTab, path, false, out var state);

        Assert.True(ok);
        Assert.True(state.UpdatedTab.IsDetached);
    }

    [Fact]
    public void SavedWorkspaceStateUpdater_PreservesIsDetachedFalse_WhenTabIsNotDetached()
    {
        var currentTab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);
        var path = @"C:\work\notes.notenest";

        var ok = NestSuite.Services.SavedWorkspaceStateUpdater.TryCreate(currentTab, path, false, out var state);

        Assert.True(ok);
        Assert.False(state.UpdatedTab.IsDetached);
    }

    // ── セッション保存への非漏洩 ─────────────────────────────────────────

    [Fact]
    public void SessionTabMapper_DetachedTab_ExcludesIsDetachedFromSession()
    {
        // IsDetached = true のタブでも FilePath のみがセッションに記録され、分離状態は含まれない
        var tab = NestSuiteTabFactory.FromFilePath(@"C:\work\notes.notenest") with { IsDetached = true };

        var ok = NestSuite.Services.SessionTabMapper.TryCreateSessionEntry(tab, out var filePath);

        Assert.True(ok);
        Assert.Equal(@"C:\work\notes.notenest", filePath);
    }

    [Fact]
    public void SessionTabMapper_CreateSessionState_DetachedTabPreservesFilePath()
    {
        // CreateSessionState は IsDetached に関係なく FilePath だけを記録する
        var tabs = new[]
        {
            NestSuiteTabFactory.FromFilePath(@"C:\work\A.notenest") with { IsDetached = true },
            NestSuiteTabFactory.FromFilePath(@"C:\work\B.notenest"),
        };
        var selected = tabs[1];

        var state = NestSuite.Services.SessionTabMapper.CreateSessionState(tabs, selected);

        Assert.Equal(2, state.FilePaths.Count);
        Assert.Contains(@"C:\work\A.notenest", state.FilePaths);
        Assert.Contains(@"C:\work\B.notenest", state.FilePaths);
    }

    [Fact]
    public void SessionRestoreTarget_FromDetachedTabPath_RestoredAsNormal()
    {
        // セッション復元はファイルパスのみ参照するので、次回起動時は通常タブとして復元される
        var ok = NestSuite.Services.SessionTabMapper.TryCreateRestoreTarget(
            @"C:\work\notes.notenest", out var target);

        Assert.True(ok);
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, target.WorkspaceKind);
        Assert.Equal(@"C:\work\notes.notenest", target.FilePath);
    }

    // ── スキーマバージョン ───────────────────────────────────────────────

    [Fact]
    public void NoteNestSchemaVersion_Remains_1_4_1()
    {
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }

    // ── アプリバージョン ─────────────────────────────────────────────────

    [Fact]
    public void ApplicationVersion_Is_2_9_7()
    {
        Assert.Equal("2.10.8", MainViewModel.ApplicationVersion);
    }
}
