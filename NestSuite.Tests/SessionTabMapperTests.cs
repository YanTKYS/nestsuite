using System.Text.Json;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.7.14 TD-6: Tab と SessionState の変換境界の回帰確認テスト。
/// </summary>
public class SessionTabMapperTests
{
    [Fact]
    public void TryCreateSessionEntry_NoteNestTab_UsesWorkspaceFilePath()
    {
        var tab = NestSuiteTabFactory.FromFilePath(@"C:\work\note.notenest");

        var ok = SessionTabMapper.TryCreateSessionEntry(tab, out var filePath);

        Assert.True(ok);
        Assert.Equal(@"C:\work\note.notenest", filePath);
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, tab.WorkspaceKind);
    }

    [Fact]
    public void TryCreateSessionEntry_IdeaNestTab_UsesWorkspaceFilePath()
    {
        var tab = NestSuiteTabFactory.FromFilePath(@"C:\work\idea.ideanest");

        var ok = SessionTabMapper.TryCreateSessionEntry(tab, out var filePath);

        Assert.True(ok);
        Assert.Equal(@"C:\work\idea.ideanest", filePath);
        Assert.Equal(NestSuiteWorkspaceKind.IdeaNest, tab.WorkspaceKind);
    }

    [Fact]
    public void TryCreateSessionEntry_ChatNestTab_UsesWorkspaceFilePath()
    {
        var tab = NestSuiteTabFactory.FromFilePath(@"C:\work\chat.chatnest");

        var ok = SessionTabMapper.TryCreateSessionEntry(tab, out var filePath);

        Assert.True(ok);
        Assert.Equal(@"C:\work\chat.chatnest", filePath);
        Assert.Equal(NestSuiteWorkspaceKind.ChatNest, tab.WorkspaceKind);
    }

    [Fact]
    public void TryCreateSessionEntry_TempTab_IsExcluded()
    {
        var tempTab = NestSuiteTabFactory.CreateTempTab();

        var ok = SessionTabMapper.TryCreateSessionEntry(tempTab, out var filePath);

        Assert.False(ok);
        Assert.Equal(string.Empty, filePath);
    }

    [Fact]
    public void CreateSessionState_ExcludesTempAndUntitledTabs_AndKeepsActiveSavedTab()
    {
        var note = NestSuiteTabFactory.FromFilePath(@"C:\work\note.notenest");
        var chat = NestSuiteTabFactory.FromFilePath(@"C:\work\chat.chatnest");
        var temp = NestSuiteTabFactory.CreateTempTab();
        var untitledIdea = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.IdeaNest);

        var state = SessionTabMapper.CreateSessionState([temp, note, untitledIdea, chat], chat);

        Assert.Equal(new[] { @"C:\work\note.notenest", @"C:\work\chat.chatnest" }, state.FilePaths);
        Assert.Equal(@"C:\work\chat.chatnest", state.ActiveFilePath);
    }

    [Fact]
    public void CreateSessionState_WhenActiveTabIsTemp_SetsActiveFilePathNull()
    {
        var note = NestSuiteTabFactory.FromFilePath(@"C:\work\note.notenest");
        var temp = NestSuiteTabFactory.CreateTempTab();

        var state = SessionTabMapper.CreateSessionState([temp, note], temp);

        Assert.Equal(new[] { @"C:\work\note.notenest" }, state.FilePaths);
        Assert.Null(state.ActiveFilePath);
    }

    [Fact]
    public void TryCreateRestoreTarget_SupportedExtension_ReturnsWorkspaceKind()
    {
        var ok = SessionTabMapper.TryCreateRestoreTarget(
            @"C:\work\idea.ideanest",
            out var target,
            _ => true);

        Assert.True(ok);
        Assert.Equal(@"C:\work\idea.ideanest", target.FilePath);
        Assert.Equal(NestSuiteWorkspaceKind.IdeaNest, target.WorkspaceKind);
    }

    [Fact]
    public void TryCreateRestoreTarget_UnknownExtension_IsSafeFalse()
    {
        var ok = SessionTabMapper.TryCreateRestoreTarget(
            @"C:\work\unknown.txt",
            out _,
            _ => true);

        Assert.False(ok);
    }

    [Fact]
    public void TryCreateRestoreTarget_MissingFile_IsSkippedLikeExistingRestoreFlow()
    {
        var ok = SessionTabMapper.TryCreateRestoreTarget(
            @"C:\work\missing.notenest",
            out _,
            _ => false);

        Assert.False(ok);
    }

    [Fact]
    public void CreateRestoreTargets_FiltersInvalidEntriesWithoutChangingOrder()
    {
        var state = new NestSuiteSessionState
        {
            FilePaths = [@"C:\work\a.notenest", @"C:\work\skip.txt", @"C:\work\b.chatnest"]
        };

        var targets = SessionTabMapper.CreateRestoreTargets(state, path => !path.EndsWith("missing.notenest", StringComparison.Ordinal));

        Assert.Equal(new[] { NestSuiteWorkspaceKind.NoteNest, NestSuiteWorkspaceKind.ChatNest }, targets.Select(t => t.WorkspaceKind));
        Assert.Equal(new[] { @"C:\work\a.notenest", @"C:\work\b.chatnest" }, targets.Select(t => t.FilePath));
    }

    [Fact]
    public void CreateSessionState_SessionJsonShape_RemainsFilePathsAndActiveFilePathOnly()
    {
        var note = NestSuiteTabFactory.FromFilePath(@"C:\work\note.notenest");
        var state = SessionTabMapper.CreateSessionState([note], note);

        var json = JsonSerializer.Serialize(state);

        Assert.Contains("\"FilePaths\"", json);
        Assert.Contains("\"ActiveFilePath\"", json);
        Assert.DoesNotContain("WorkspaceKind", json);
        Assert.DoesNotContain("IsModified", json);
    }

    [Fact]
    public void CloseConfirmationService_SaveFailureStillCancelsCloseFlow()
    {
        var canClose = CloseConfirmationService.CanCloseSingle(
            true,
            () => UnsavedChangeDecision.Save,
            () => false);

        Assert.False(canClose);
    }
}
