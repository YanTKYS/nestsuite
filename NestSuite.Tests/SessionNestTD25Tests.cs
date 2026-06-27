using System.IO;
using System.Text.Json;
using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.10.13 TD-25: SessionNest 第一段階整理の回帰テスト。
/// session.json 読込 / 保存 / 復元まわりの責務境界確認。
/// TempNest session 対象外・detached 状態非保存をテストで固定する。
/// </summary>
public class SessionNestTD25Tests
{
    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    // ── バージョン ────────────────────────────────────────────────────────

    // ── session.json 形式不変 ─────────────────────────────────────────────

    [Fact]
    public void SessionJson_HasOnlyFilePathsAndActiveFilePath()
    {
        var note = NestSuiteTabFactory.FromFilePath(@"C:\work\note.notenest");
        var state = SessionTabMapper.CreateSessionState([note], note);

        var json = JsonSerializer.Serialize(state);

        Assert.Contains("\"FilePaths\"", json);
        Assert.Contains("\"ActiveFilePath\"", json);
        Assert.DoesNotContain("WorkspaceKind", json);
        Assert.DoesNotContain("IsModified", json);
        Assert.DoesNotContain("IsDetached", json);
        Assert.DoesNotContain("IsPinned", json);
    }

    [Fact]
    public void SessionState_RoundTrip_PreservesFilePathsAndActiveFilePath()
    {
        var state = new NestSuiteSessionState
        {
            FilePaths = [@"C:\work\a.notenest", @"C:\work\b.chatnest"],
            ActiveFilePath = @"C:\work\b.chatnest"
        };

        var json = JsonSerializer.Serialize(state);
        var restored = JsonSerializer.Deserialize<NestSuiteSessionState>(json)!;

        Assert.Equal(state.FilePaths, restored.FilePaths);
        Assert.Equal(state.ActiveFilePath, restored.ActiveFilePath);
    }

    // ── TempNest session 対象外 ───────────────────────────────────────────

    [Fact]
    public void TempNest_IsExcludedFromSession_ByKind()
    {
        var tempTab = NestSuiteTabFactory.CreateTempTab();

        var ok = SessionTabMapper.TryCreateSessionEntry(tempTab, out var filePath);

        Assert.False(ok);
        Assert.Equal(string.Empty, filePath);
    }

    [Fact]
    public void TempNest_IsExcludedFromSession_WhenMixedWithSavedTabs()
    {
        var note = NestSuiteTabFactory.FromFilePath(@"C:\work\note.notenest");
        var chat = NestSuiteTabFactory.FromFilePath(@"C:\work\chat.chatnest");
        var temp = NestSuiteTabFactory.CreateTempTab();

        var state = SessionTabMapper.CreateSessionState([temp, note, chat], note);

        Assert.Equal(2, state.FilePaths.Count);
        Assert.DoesNotContain(state.FilePaths, p => p.Contains("temp", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(@"C:\work\note.notenest", state.FilePaths);
        Assert.Contains(@"C:\work\chat.chatnest", state.FilePaths);
    }

    [Fact]
    public void TempNest_WhenActiveTab_ActiveFilePathIsNull()
    {
        var note = NestSuiteTabFactory.FromFilePath(@"C:\work\note.notenest");
        var temp = NestSuiteTabFactory.CreateTempTab();

        var state = SessionTabMapper.CreateSessionState([note, temp], temp);

        Assert.Equal(new[] { @"C:\work\note.notenest" }, state.FilePaths);
        Assert.Null(state.ActiveFilePath);
    }

    [Fact]
    public void TempNest_IsNotRestorable_ByRestoreTarget()
    {
        // TryCreateRestoreTarget は Temp 拡張子を持つパスを除外する
        var ok = SessionTabMapper.TryCreateRestoreTarget(
            @"C:\AppData\NoteNest\tempnest.json",
            out _,
            _ => true);

        Assert.False(ok);
    }

    // ── detached 状態は session に保存されない ────────────────────────────

    [Fact]
    public void DetachedState_IsNotPresent_InSessionJson()
    {
        var detachedTab = NestSuiteTabFactory.FromFilePath(@"C:\work\notes.notenest") with { IsDetached = true };

        var state = SessionTabMapper.CreateSessionState([detachedTab], detachedTab);
        var json = JsonSerializer.Serialize(state);

        Assert.DoesNotContain("IsDetached", json);
        Assert.DoesNotContain("Detached", json);
    }

    [Fact]
    public void DetachedTab_FilePathIsSaved_ToSession()
    {
        // detached タブはファイルパスとしてセッションに含まれる
        var detachedTab = NestSuiteTabFactory.FromFilePath(@"C:\work\notes.notenest") with { IsDetached = true };

        var ok = SessionTabMapper.TryCreateSessionEntry(detachedTab, out var filePath);

        Assert.True(ok);
        Assert.Equal(@"C:\work\notes.notenest", filePath);
    }

    [Fact]
    public void DetachedTab_RestoresAsNormal_OnNextLaunch()
    {
        // セッション復元はファイルパスのみ参照するので、次回起動時は通常タブとして復元される
        var ok = SessionTabMapper.TryCreateRestoreTarget(
            @"C:\work\notes.notenest", out var target);

        Assert.True(ok);
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, target.WorkspaceKind);
        Assert.Equal(@"C:\work\notes.notenest", target.FilePath);
    }

    [Fact]
    public void MultipleDetachedTabs_AllFilePathsSaved_NoFlagLeak()
    {
        var tabs = new[]
        {
            NestSuiteTabFactory.FromFilePath(@"C:\work\A.notenest") with { IsDetached = true },
            NestSuiteTabFactory.FromFilePath(@"C:\work\B.ideanest") with { IsDetached = false },
            NestSuiteTabFactory.FromFilePath(@"C:\work\C.chatnest") with { IsDetached = true },
        };

        var state = SessionTabMapper.CreateSessionState(tabs, tabs[1]);
        var json = JsonSerializer.Serialize(state);

        Assert.Equal(3, state.FilePaths.Count);
        Assert.DoesNotContain("Detached", json);
        Assert.Equal(@"C:\work\B.ideanest", state.ActiveFilePath);
    }

    // ── SessionTabMapper / 復元フロー ─────────────────────────────────────

    [Fact]
    public void CreateRestoreTargets_FiltersUnknownExtensions_Silently()
    {
        var state = new NestSuiteSessionState
        {
            FilePaths = [@"C:\work\a.notenest", @"C:\work\x.unknown", @"C:\work\b.chatnest"],
            ActiveFilePath = @"C:\work\a.notenest"
        };

        var targets = SessionTabMapper.CreateRestoreTargets(state, _ => true);

        Assert.Equal(2, targets.Count);
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, targets[0].WorkspaceKind);
        Assert.Equal(NestSuiteWorkspaceKind.ChatNest, targets[1].WorkspaceKind);
    }

    [Fact]
    public void UntitledTab_IsExcludedFromSession()
    {
        var untitled = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);

        var ok = SessionTabMapper.TryCreateSessionEntry(untitled, out _);

        Assert.False(ok);
    }

    // ── backlog / release-notes ───────────────────────────────────────────

    [Fact]
    public void Backlog_TD25_IsMarkedComplete()
    {
        var path = Path.Combine(RepoRoot, "docs", "backlog.md");
        Assert.True(File.Exists(path), $"backlog.md not found: {path}");
        Assert.Contains("~~TD-25~~", File.ReadAllText(path));
    }

    [Fact]
    public void ReleaseNotes_Contains_V2_10_12()
    {
        var path = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(path));
        Assert.Contains("v2.10.13", File.ReadAllText(path));
    }
}
