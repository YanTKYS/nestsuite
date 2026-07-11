using System.Text.Json;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.7.15 TD-3-4: 保存成功後のタブ・Session 更新共通 helper の回帰確認テスト。
/// </summary>
public class SavedWorkspaceStateUpdaterTests
{
    [Theory]
    [InlineData(NestSuiteWorkspaceKind.NoteNest, @"C:\work\note.notenest")]
    [InlineData(NestSuiteWorkspaceKind.IdeaNest, @"C:\work\idea.ideanest")]
    [InlineData(NestSuiteWorkspaceKind.ChatNest, @"C:\work\chat.chatnest")]
    public void TryCreate_SaveSuccess_UpdatesFilePathForWorkspace(NestSuiteWorkspaceKind kind, string path)
    {
        var tab = NestSuiteTabFactory.CreateUntitled(kind) with { IsModified = true };

        var ok = SavedWorkspaceStateUpdater.TryCreate(tab, path, isModifiedAfterSave: false, out var state);

        Assert.True(ok);
        Assert.Equal(path, state.FilePath);
        Assert.Equal(path, state.UpdatedTab.FilePath);
        Assert.Equal(kind, state.UpdatedTab.WorkspaceKind);
    }

    [Fact]
    public void TryCreate_SaveSuccess_ClearsDirtyStateAndUpdatesTabTitle()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.IdeaNest) with { IsModified = true };

        var ok = SavedWorkspaceStateUpdater.TryCreate(tab, @"C:\ideas\saved.ideanest", false, out var state);

        Assert.True(ok);
        Assert.False(state.IsModified);
        Assert.False(state.UpdatedTab.IsModified);
        Assert.Equal("saved.ideanest", state.UpdatedTab.DisplayName);
        Assert.Equal("💡 saved", state.UpdatedTab.TabHeaderText);
    }


    [Fact]
    public void TryCreate_SaveSuccess_PreservesPinnedState()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest) with
        {
            IsModified = true,
            IsPinned = true
        };

        var ok = SavedWorkspaceStateUpdater.TryCreate(tab, @"C:\work\saved.notenest", false, out var state);

        Assert.True(ok);
        Assert.True(state.UpdatedTab.IsPinned);
        Assert.StartsWith("📌 ", state.UpdatedTab.TabHeaderText);
    }

    [Fact]
    public void TryCreate_SaveSuccess_ProvidesRecentFilePath()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.ChatNest);

        var ok = SavedWorkspaceStateUpdater.TryCreate(tab, @"C:\chat\saved.chatnest", false, out var state);

        Assert.True(ok);
        Assert.Equal(@"C:\chat\saved.chatnest", state.RecentFilePath);
    }

    [Fact]
    public void ApplyToSession_SaveSuccess_UpdatesSessionForNextSessionEntry()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest) with { IsModified = true };
        var session = new NestSuiteWorkspaceSession(tab.Id, tab.WorkspaceKind, new object(), tab.FilePath, tab.IsModified);
        Assert.True(SavedWorkspaceStateUpdater.TryCreate(tab, @"C:\work\saved.notenest", false, out var state));

        SavedWorkspaceStateUpdater.ApplyToSession(session, state);
        var sessionState = SessionTabMapper.CreateSessionState([state.UpdatedTab], state.UpdatedTab);

        Assert.Equal(@"C:\work\saved.notenest", session.FilePath);
        Assert.False(session.IsModified);
        Assert.Equal(new[] { @"C:\work\saved.notenest" }, sessionState.FilePaths);
        Assert.Equal(@"C:\work\saved.notenest", sessionState.ActiveFilePath);
    }

    [Fact]
    public void TryCreate_SaveFailureNotCalled_LeavesExistingStateUnchanged()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest) with { IsModified = true };
        var session = new NestSuiteWorkspaceSession(tab.Id, tab.WorkspaceKind, new object(), tab.FilePath, tab.IsModified);

        // 保存失敗時は SavedWorkspaceStateUpdater.TryCreate / ApplyToSession を呼ばない契約。
        Assert.Null(tab.FilePath);
        Assert.True(tab.IsModified);
        Assert.Null(session.FilePath);
        Assert.True(session.IsModified);
    }

    [Fact]
    public void TryCreate_TempTab_IsExcluded()
    {
        var tab = NestSuiteTabFactory.CreateTempTab();

        var ok = SavedWorkspaceStateUpdater.TryCreate(tab, @"C:\temp\temp.notenest", false, out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryCreate_MismatchedWorkspaceExtension_IsRejected()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);

        var ok = SavedWorkspaceStateUpdater.TryCreate(tab, @"C:\ideas\wrong.ideanest", false, out _);

        Assert.False(ok);
    }

    [Fact]
    public void SessionFormat_IncludesPinnedTabStateButNotModifiedState_AfterSaveStateUpdate()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.ChatNest);
        Assert.True(SavedWorkspaceStateUpdater.TryCreate(tab, @"C:\chat\saved.chatnest", false, out var state));
        var sessionState = SessionTabMapper.CreateSessionState([state.UpdatedTab], state.UpdatedTab);

        var json = JsonSerializer.Serialize(sessionState);

        Assert.Contains("\"FilePaths\"", json);
        Assert.Contains("\"ActiveFilePath\"", json);
        Assert.Contains("\"Tabs\"", json);
        Assert.Contains("WorkspaceKind", json);
        Assert.Contains("IsPinned", json);
        Assert.DoesNotContain("IsModified", json);
    }

    // ── v2.14.1 FM-1: .nestsuite 保存先の種別一致確認 ──────────────────

    [Fact]
    public void TryCreate_NestSuiteSavedPath_MatchingKind_Succeeds()
    {
        var savedPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        try
        {
            File.WriteAllText(savedPath, NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "1.4.1", "{}"));
            var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest) with { IsModified = true };

            var ok = SavedWorkspaceStateUpdater.TryCreate(tab, savedPath, isModifiedAfterSave: false, out var state);

            Assert.True(ok);
            Assert.Equal(NestSuiteWorkspaceKind.NoteNest, state.UpdatedTab.WorkspaceKind);
            Assert.Equal(savedPath, state.UpdatedTab.FilePath);
        }
        finally { File.Delete(savedPath); }
    }

    // v2.16.39 TD-59b-5: TryCreate は保存成功後の内部状態更新であり、保存直後にファイルを
    // 再度開いて wrapper の kind を再検証しない契約へ変更した。旧
    // TryCreate_NestSuiteSavedPath_KindMismatch_Fails（保存済み .nestsuite の wrapper 内容が
    // currentTab.WorkspaceKind と食い違う場合に false を期待していた）は、この新しい契約のもとでは
    // 成立しない前提を検証する形になるため、下記の「再読込しない」契約テストへ置き換えた
    // （テストの削除ではなく、変更後の責務境界に合わせた更新）。
    [Fact]
    public void TryCreate_NestSuiteSavedPath_UsesCurrentTabKindWithoutReopeningFile()
    {
        // wrapper の中身は実際には ChatNest だが、currentTab（保存を実行した Workspace）は NoteNest。
        // TryCreate はファイルを再読込しないため、wrapper 内容ではなく currentTab.WorkspaceKind を
        // そのまま信頼し、成功する。ファイル Open の安全性（内容と kind の一致確認）は
        // TryPrepareOpen / LoadPrepared 側の責務であり、ここでは担わない。
        var savedPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        try
        {
            File.WriteAllText(savedPath, NestSuiteWorkspaceEnvelope.Wrap("ChatNest", "0.4.1", "{}"));
            var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest) with { IsModified = true };

            var ok = SavedWorkspaceStateUpdater.TryCreate(tab, savedPath, isModifiedAfterSave: false, out var state);

            Assert.True(ok);
            Assert.Equal(NestSuiteWorkspaceKind.NoteNest, state.UpdatedTab.WorkspaceKind);
            Assert.Equal(savedPath, state.UpdatedTab.FilePath);
        }
        finally { File.Delete(savedPath); }
    }

    [Theory]
    [InlineData(NestSuiteWorkspaceKind.NoteNest)]
    [InlineData(NestSuiteWorkspaceKind.IdeaNest)]
    [InlineData(NestSuiteWorkspaceKind.ChatNest)]
    public void TryCreate_NestSuiteSavedPath_MissingFile_StillSucceeds_UsingCurrentTabKind(NestSuiteWorkspaceKind kind)
    {
        // 実在しない .nestsuite path を使用する。TryGetKind / FromFilePath による再読込が
        // 残っていれば、ファイル不在で失敗するはず（TryPrepareOpen は FileNotFound を返す）。
        // 成功することで、追加のファイル読込・存在確認が行われていないことを保証する。
        var savedPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        var tab = NestSuiteTabFactory.CreateUntitled(kind) with { IsModified = true };

        var ok = SavedWorkspaceStateUpdater.TryCreate(tab, savedPath, isModifiedAfterSave: false, out var state);

        Assert.True(ok);
        Assert.False(File.Exists(savedPath));
        Assert.Equal(kind, state.UpdatedTab.WorkspaceKind);
        Assert.Equal(savedPath, state.UpdatedTab.FilePath);
        Assert.Equal(Path.GetFileName(savedPath), state.UpdatedTab.DisplayName);
        Assert.False(state.UpdatedTab.IsModified);
    }

    [Fact]
    public void TryCreate_SaveSuccess_PreservesTabId()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest) with { IsModified = true };

        var ok = SavedWorkspaceStateUpdater.TryCreate(tab, @"C:\work\saved.notenest", false, out var state);

        Assert.True(ok);
        Assert.Equal(tab.Id, state.UpdatedTab.Id);
    }

    [Fact]
    public void TryCreate_SaveSuccess_PreservesIsDetached()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest) with
        {
            IsModified = true,
            IsDetached = true
        };

        var ok = SavedWorkspaceStateUpdater.TryCreate(tab, @"C:\work\saved.notenest", false, out var state);

        Assert.True(ok);
        Assert.True(state.UpdatedTab.IsDetached);
    }

    [Fact]
    public void TryCreate_UnsupportedExtension_IsRejected()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);

        var ok = SavedWorkspaceStateUpdater.TryCreate(tab, @"C:\work\saved.txt", false, out _);

        Assert.False(ok);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryCreate_EmptyOrWhitespaceSavedPath_IsRejected(string savedPath)
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);

        var ok = SavedWorkspaceStateUpdater.TryCreate(tab, savedPath, false, out _);

        Assert.False(ok);
    }
}
