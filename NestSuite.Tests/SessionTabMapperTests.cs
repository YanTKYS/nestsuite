using System.Text.Json;
using NestSuite.Services;
using Xunit;
using NestSuite.ViewModels;
using NestSuite.Models;
using System.IO;

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
    public void CreateSessionState_SessionJsonShape_IncludesTabsForPinnedState()
    {
        var note = NestSuiteTabFactory.FromFilePath(@"C:\work\note.notenest");
        var state = SessionTabMapper.CreateSessionState([note], note);

        var json = JsonSerializer.Serialize(state);

        Assert.Contains("\"FilePaths\"", json);
        Assert.Contains("\"ActiveFilePath\"", json);
        Assert.Contains("\"Tabs\"", json);
        Assert.Contains("\"WorkspaceKind\"", json);
        Assert.Contains("\"IsPinned\"", json);
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

    private static readonly string RepoRoot = TestPaths.RepoRoot;

    // ── バージョン ────────────────────────────────────────────────────────

    // ── session.json 形式不変 ─────────────────────────────────────────────

    [Fact]
    public void SessionJson_AddsTabsButDoesNotPersistDetachedOrModifiedState()
    {
        var note = NestSuiteTabFactory.FromFilePath(@"C:\work\note.notenest");
        var state = SessionTabMapper.CreateSessionState([note], note);

        var json = JsonSerializer.Serialize(state);

        Assert.Contains("\"FilePaths\"", json);
        Assert.Contains("\"ActiveFilePath\"", json);
        Assert.Contains("\"Tabs\"", json);
        Assert.Contains("\"WorkspaceKind\"", json);
        Assert.Contains("\"IsPinned\"", json);
        Assert.DoesNotContain("IsModified", json);
        Assert.DoesNotContain("IsDetached", json);
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
        Assert.Empty(restored.Tabs);
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


    // ── SH-15: タブピン留め session ─────────────────────────────────────

    [Fact]
    public void CreateSessionState_PersistsPinnedStateInTabs()
    {
        var note = NestSuiteTabFactory.FromFilePath(@"C:\work\note.notenest") with { IsPinned = true };
        var chat = NestSuiteTabFactory.FromFilePath(@"C:\work\chat.chatnest");

        var state = SessionTabMapper.CreateSessionState([note, chat], note);

        Assert.Equal(2, state.Tabs.Count);
        Assert.True(state.Tabs[0].IsPinned);
        Assert.False(state.Tabs[1].IsPinned);
        Assert.Equal("NoteNest", state.Tabs[0].WorkspaceKind);
    }

    [Fact]
    public void CreateRestoreTargets_OldSessionWithoutTabs_TreatsPinnedAsFalse()
    {
        var state = new NestSuiteSessionState
        {
            FilePaths = [@"C:\work\note.notenest"]
        };

        var target = Assert.Single(SessionTabMapper.CreateRestoreTargets(state, _ => true));

        Assert.False(target.IsPinned);
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, target.WorkspaceKind);
    }


    [Fact]
    public void SessionState_OldJsonWithoutTabs_DeserializesWithEmptyTabs()
    {
        const string json = """{"FilePaths":["C:\\work\\note.notenest"],"ActiveFilePath":"C:\\work\\note.notenest"}""";

        var restored = JsonSerializer.Deserialize<NestSuiteSessionState>(json)!;
        var target = Assert.Single(SessionTabMapper.CreateRestoreTargets(restored, _ => true));

        Assert.Empty(restored.Tabs);
        Assert.False(target.IsPinned);
    }

    [Fact]
    public void CreateRestoreTargets_NewTabsShape_RestoresPinnedState()
    {
        var state = new NestSuiteSessionState
        {
            Tabs =
            [
                new NestSuiteSessionTabState
                {
                    FilePath = @"C:\work\note.notenest",
                    WorkspaceKind = "NoteNest",
                    IsPinned = true
                }
            ]
        };

        var target = Assert.Single(SessionTabMapper.CreateRestoreTargets(state, _ => true));

        Assert.True(target.IsPinned);
        Assert.Equal(@"C:\work\note.notenest", target.FilePath);
    }

    // ── backlog / release-notes ───────────────────────────────────────────

    // TD-33: 完了済み項目は release-notes.md で管理
    [Fact]
    public void Backlog_TD25_IsMarkedComplete()
    {
        var path = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(path), $"release-notes.md not found: {path}");
        Assert.Contains("TD-25", File.ReadAllText(path));
    }

    [Fact]
    public void ReleaseNotes_Contains_V2_10_12()
    {
        var path = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(path));
        Assert.Contains("v2.10.13", File.ReadAllText(path));
    }

    // ── v2.14.1 FM-1: .nestsuite セッション復元の種別判定 ──────────────

    [Fact]
    public void TryCreateRestoreTarget_NestSuitePath_ResolvesKindFromEnvelope()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        try
        {
            File.WriteAllText(path, NestSuiteWorkspaceEnvelope.Wrap("IdeaNest", "1.1.4", "{}"));

            var ok = SessionTabMapper.TryCreateRestoreTarget(path, out var target, File.Exists);

            Assert.True(ok);
            Assert.Equal(NestSuiteWorkspaceKind.IdeaNest, target.WorkspaceKind);
        }
        finally { File.Delete(path); }
    }

    // ── v2.14.7 SH-31: 読めない .nestsuite の復元通知 ──────────────────

    [Fact]
    public void CreateRestoreTargets_BrokenNestsuite_ReportsFailure_AndRestoresOthers()
    {
        var brokenPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        File.WriteAllText(brokenPath, "not json");
        try
        {
            var state = new NestSuiteSessionState
            {
                FilePaths = [@"C:\work\note.notenest", brokenPath]
            };

            var targets = SessionTabMapper.CreateRestoreTargets(state, _ => true, out var failures);

            Assert.Single(targets);
            Assert.Equal(@"C:\work\note.notenest", targets[0].FilePath);
            Assert.Equal(NestSuiteWorkspaceKind.NoteNest, targets[0].WorkspaceKind);

            var failure = Assert.Single(failures);
            Assert.Equal(brokenPath, failure.FilePath);
            Assert.Equal(WorkspaceKindDetectionFailure.InvalidFormat, failure.Failure);
        }
        finally { File.Delete(brokenPath); }
    }

    // v2.16.7 TD-65 (review1-fable5.md R-3): 存在しないファイルは無言スキップではなく、
    // 通知・持ち越し対象の失敗として報告するようになった（旧: SkipsSilently_NoFailureEntry）。
    [Fact]
    public void CreateRestoreTargets_MissingFile_ReportsFileNotFoundFailure()
    {
        var state = new NestSuiteSessionState
        {
            FilePaths = [@"C:\work\missing.notenest"]
        };

        var targets = SessionTabMapper.CreateRestoreTargets(state, _ => false, out var failures);

        Assert.Empty(targets);
        var failure = Assert.Single(failures);
        Assert.Equal(@"C:\work\missing.notenest", failure.FilePath);
        Assert.Equal(WorkspaceKindDetectionFailure.FileNotFound, failure.Failure);
        Assert.False(failure.IsPinned);
    }

    [Fact]
    public void CreateRestoreTargets_UnsupportedExtension_SkipsSilently()
    {
        var state = new NestSuiteSessionState
        {
            FilePaths = [@"C:\work\notes.txt"]
        };

        var targets = SessionTabMapper.CreateRestoreTargets(state, _ => true, out var failures);

        Assert.Empty(targets);
        Assert.Empty(failures);
    }

    [Fact]
    public void TryCreateRestoreTarget_TooNewNestsuite_ReportsSchemaVersionTooNew()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        try
        {
            File.WriteAllText(path, NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "9.9.9", "{}"));

            var ok = SessionTabMapper.TryCreateRestoreTarget(path, out _, out var failure, File.Exists);

            Assert.False(ok);
            Assert.Equal(WorkspaceKindDetectionFailure.SchemaVersionTooNew, failure);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void CreateRestoreTargets_SchemaVersionTooNew_IsReportedAsFailure()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        File.WriteAllText(path, NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "9.9.9", "{}"));
        try
        {
            var state = new NestSuiteSessionState { FilePaths = [path] };

            var targets = SessionTabMapper.CreateRestoreTargets(state, File.Exists, out var failures);

            Assert.Empty(targets);
            var failure = Assert.Single(failures);
            Assert.Equal(WorkspaceKindDetectionFailure.SchemaVersionTooNew, failure.Failure);
        }
        finally { File.Delete(path); }
    }

    // ── v2.16.7 TD-65: Tabs[] 由来の復元失敗は IsPinned を保持する ─────────

    [Fact]
    public void CreateRestoreTargets_TabsShape_MissingFile_ReportsFailureWithPinnedState()
    {
        var state = new NestSuiteSessionState
        {
            Tabs =
            [
                new NestSuiteSessionTabState
                {
                    FilePath = @"C:\work\missing.notenest",
                    WorkspaceKind = "NoteNest",
                    IsPinned = true,
                }
            ]
        };

        var targets = SessionTabMapper.CreateRestoreTargets(state, _ => false, out var failures);

        Assert.Empty(targets);
        var failure = Assert.Single(failures);
        Assert.Equal(@"C:\work\missing.notenest", failure.FilePath);
        Assert.Equal(WorkspaceKindDetectionFailure.FileNotFound, failure.Failure);
        Assert.True(failure.IsPinned);
    }

    // ── v2.16.7 TD-65: 復元失敗 entry の持ち越し（CreateSessionState） ─────

    [Fact]
    public void CreateSessionState_PendingRestoreEntries_AreAddedToFilePathsAndTabs()
    {
        var pending = new[]
        {
            new SessionRestoreFailure(@"C:\work\missing.notenest", WorkspaceKindDetectionFailure.FileNotFound)
        };

        var state = SessionTabMapper.CreateSessionState([], null, pending);

        Assert.Contains(@"C:\work\missing.notenest", state.FilePaths);
        var tabState = Assert.Single(state.Tabs);
        Assert.Equal(@"C:\work\missing.notenest", tabState.FilePath);
        Assert.Null(tabState.WorkspaceKind);
        Assert.False(tabState.IsPinned);
    }

    [Fact]
    public void CreateSessionState_PendingRestoreEntries_PreservesIsPinned()
    {
        var pending = new[]
        {
            new SessionRestoreFailure(
                @"C:\work\missing.notenest", WorkspaceKindDetectionFailure.FileNotFound, IsPinned: true)
        };

        var state = SessionTabMapper.CreateSessionState([], null, pending);

        Assert.True(Assert.Single(state.Tabs).IsPinned);
    }

    [Fact]
    public void CreateSessionState_PendingRestoreEntries_ExcludesEntryMatchingOpenTab()
    {
        var openTab = NestSuiteTabFactory.FromFilePath(@"C:\work\note.notenest");
        var pending = new[]
        {
            // 前回は復元に失敗したが、今回のセッションでは同じファイルが正常に開かれている。
            new SessionRestoreFailure(@"C:\work\note.notenest", WorkspaceKindDetectionFailure.FileNotFound)
        };

        var state = SessionTabMapper.CreateSessionState([openTab], openTab, pending);

        Assert.Single(state.FilePaths);
        Assert.Single(state.Tabs);
        Assert.Equal(@"C:\work\note.notenest", state.FilePaths[0]);
    }

    [Fact]
    public void CreateSessionState_PendingRestoreEntries_ExcludesEntryMatchingOpenTab_CaseInsensitive()
    {
        var openTab = NestSuiteTabFactory.FromFilePath(@"C:\work\NOTE.notenest");
        var pending = new[]
        {
            new SessionRestoreFailure(@"C:\work\note.notenest", WorkspaceKindDetectionFailure.FileNotFound)
        };

        var state = SessionTabMapper.CreateSessionState([openTab], openTab, pending);

        Assert.Single(state.FilePaths);
        Assert.Single(state.Tabs);
    }

    [Fact]
    public void CreateSessionState_PendingRestoreEntries_DeduplicatesRepeatedFilePath()
    {
        var pending = new[]
        {
            new SessionRestoreFailure(@"C:\work\missing.notenest", WorkspaceKindDetectionFailure.FileNotFound),
            new SessionRestoreFailure(@"C:\work\missing.notenest", WorkspaceKindDetectionFailure.FileNotFound),
        };

        var state = SessionTabMapper.CreateSessionState([], null, pending);

        Assert.Single(state.FilePaths);
        Assert.Single(state.Tabs);
    }

    [Fact]
    public void CreateSessionState_PendingRestoreEntries_Null_BehavesLikeNoPendingEntries()
    {
        var note = NestSuiteTabFactory.FromFilePath(@"C:\work\note.notenest");

        var withNull = SessionTabMapper.CreateSessionState([note], note, null);
        var withoutArg = SessionTabMapper.CreateSessionState([note], note);

        Assert.Equal(withoutArg.FilePaths, withNull.FilePaths);
        Assert.Equal(withoutArg.Tabs.Count, withNull.Tabs.Count);
    }

    [Fact]
    public void CreateSessionState_PendingRestoreEntries_CombinesWithOpenTabsAndKeepsOrder()
    {
        var note = NestSuiteTabFactory.FromFilePath(@"C:\work\note.notenest");
        var pending = new[]
        {
            new SessionRestoreFailure(@"C:\work\missing.chatnest", WorkspaceKindDetectionFailure.FileNotFound)
        };

        var state = SessionTabMapper.CreateSessionState([note], note, pending);

        Assert.Equal(
            new[] { @"C:\work\note.notenest", @"C:\work\missing.chatnest" },
            state.FilePaths);
        Assert.Equal(2, state.Tabs.Count);
        Assert.Equal("NoteNest", state.Tabs[0].WorkspaceKind);
        Assert.Null(state.Tabs[1].WorkspaceKind);
    }

    // ── v2.16.17 TD-69 (review2-fable5.md R-14): FilePaths[] は Tabs[] から導出 ──────────

    [Fact]
    public void CreateSessionState_FilePaths_IsDerivedFromTabsFilePath_ForOpenTabsAndPendingEntries()
    {
        // FilePaths[] は Tabs[].FilePath と完全に一致する（別ロジックでの二重導出をやめた核心テスト）。
        var note = NestSuiteTabFactory.FromFilePath(@"C:\work\note.notenest") with { IsPinned = true };
        var chat = NestSuiteTabFactory.FromFilePath(@"C:\work\chat.chatnest");
        var pending = new[]
        {
            new SessionRestoreFailure(@"C:\work\missing.ideanest", WorkspaceKindDetectionFailure.FileNotFound)
        };

        var state = SessionTabMapper.CreateSessionState([note, chat], note, pending);

        Assert.Equal(state.Tabs.Select(t => t.FilePath), state.FilePaths);
    }

    [Fact]
    public void CreateSessionState_OpenPersistableTab_AppearsInBothTabsAndFilePaths()
    {
        var note = NestSuiteTabFactory.FromFilePath(@"C:\work\note.notenest");

        var state = SessionTabMapper.CreateSessionState([note], note);

        Assert.Single(state.Tabs);
        Assert.Single(state.FilePaths);
        Assert.Equal(@"C:\work\note.notenest", state.Tabs[0].FilePath);
        Assert.Equal(@"C:\work\note.notenest", state.FilePaths[0]);
    }

    [Fact]
    public void CreateSessionState_TempTab_ExcludedFromBothTabsAndFilePaths()
    {
        var tempTab = NestSuiteTabFactory.CreateTempTab();

        var state = SessionTabMapper.CreateSessionState([tempTab], tempTab);

        Assert.Empty(state.Tabs);
        Assert.Empty(state.FilePaths);
    }

    [Fact]
    public void CreateRestoreTargets_OldFilePathsOnlySession_StillRestoresAfterTD69Refactor()
    {
        // TD-69 は CreateSessionState 側の導出のみを変更した。CreateRestoreTargets の
        // 旧 FilePaths[] のみ session に対する復元互換は影響を受けていないことを確認する。
        var state = new NestSuiteSessionState { FilePaths = [@"C:\work\note.notenest"] };

        var target = Assert.Single(SessionTabMapper.CreateRestoreTargets(state, _ => true));

        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, target.WorkspaceKind);
    }

    [Fact]
    public void SessionTabMapper_Source_DerivesFilePathsFromTabsWithoutSeparateAppend()
    {
        // R-14 の核心（二重導出の解消）をソーステキストで固定する。
        // 文言完全一致ではなく、重要語句の存在確認に留める（脆くなりすぎないように）。
        var path = Path.Combine(RepoRoot, "NestSuite", "Services", "SessionTabMapper.cs");
        var src = File.ReadAllText(path);
        var methodStart = src.IndexOf("public static NestSuiteSessionState CreateSessionState(", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var methodEnd = src.IndexOf(
            "private static IEnumerable<SessionRestoreFailure> DeduplicatePendingEntries", methodStart, StringComparison.Ordinal);
        Assert.True(methodEnd > methodStart);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        Assert.Contains("tabStates.Select(state => state.FilePath)", body);
        Assert.DoesNotContain("filePaths.Add(", body);
    }

    // ── v2.16.16 TD-68 (review1-fable5.md R-8): Tabs[].WorkspaceKind は UI 表示ヒント ─────
    // 復元時の最終判定の信頼ソースではないことをテストで固定する。

    [Fact]
    public void CreateSessionState_WritesWorkspaceKindToTabs_AsUiHint()
    {
        // Tabs[].WorkspaceKind は書き込まれる（将来の選択的復元 UI 用のヒント）。
        var note = NestSuiteTabFactory.FromFilePath(@"C:\work\note.notenest");

        var state = SessionTabMapper.CreateSessionState([note], note);

        Assert.Equal("NoteNest", state.Tabs[0].WorkspaceKind);
    }

    [Fact]
    public void NestSuiteSessionTabState_WorkspaceKind_RoundTripsThroughJson()
    {
        // session 形式（Tabs[].WorkspaceKind フィールド自体）は変更していない。
        var state = new NestSuiteSessionState
        {
            Tabs = [new NestSuiteSessionTabState { FilePath = @"C:\work\note.notenest", WorkspaceKind = "NoteNest", IsPinned = false }]
        };

        var json = JsonSerializer.Serialize(state);
        var restored = JsonSerializer.Deserialize<NestSuiteSessionState>(json)!;

        Assert.Equal("NoteNest", restored.Tabs[0].WorkspaceKind);
    }

    [Fact]
    public void CreateRestoreTargets_IgnoresMismatchedTabsWorkspaceKind_UsesFileExtensionInstead()
    {
        // TD-68 の核心: session に書かれた WorkspaceKind（ここでは意図的に実ファイルと矛盾させる）は
        // 復元時の信頼ソースとして使わない。実際の種別は拡張子・ファイル内容から再判定する。
        var state = new NestSuiteSessionState
        {
            Tabs =
            [
                new NestSuiteSessionTabState
                {
                    FilePath = @"C:\work\note.notenest",
                    WorkspaceKind = "ChatNest",
                    IsPinned = false
                }
            ]
        };

        var target = Assert.Single(SessionTabMapper.CreateRestoreTargets(state, _ => true));

        // 復元対象の種別は拡張子（.notenest → NoteNest）から再判定され、
        // session に書かれた誤った "ChatNest" 文字列には影響されない。
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, target.WorkspaceKind);
    }

    [Fact]
    public void CreateRestoreTargets_NullTabsWorkspaceKind_StillRestoresFromFileExtension()
    {
        // WorkspaceKind が null（pending entry 由来等）でも、拡張子から復元できる。
        var state = new NestSuiteSessionState
        {
            Tabs =
            [
                new NestSuiteSessionTabState { FilePath = @"C:\work\chat.chatnest", WorkspaceKind = null, IsPinned = false }
            ]
        };

        var target = Assert.Single(SessionTabMapper.CreateRestoreTargets(state, _ => true));

        Assert.Equal(NestSuiteWorkspaceKind.ChatNest, target.WorkspaceKind);
    }

    [Fact]
    public void CreateRestoreTargets_OldFilePathsOnlyFormat_StillRestoresWithoutWorkspaceKindField()
    {
        // 旧 FilePaths[] 形式（Tabs[] も WorkspaceKind も存在しない）の互換は壊れていない。
        var state = new NestSuiteSessionState { FilePaths = [@"C:\work\idea.ideanest"] };

        var target = Assert.Single(SessionTabMapper.CreateRestoreTargets(state, _ => true));

        Assert.Equal(NestSuiteWorkspaceKind.IdeaNest, target.WorkspaceKind);
        Assert.Empty(state.Tabs);
    }

    [Fact]
    public void SessionTabMapper_Source_DocumentsWorkspaceKindAsUiHintNotTrustSource()
    {
        // R-8: コメントで「UI 表示ヒント」「信頼ソースではない」の趣旨が固定されていることを確認する。
        // 文言完全一致ではなく重要語句の存在確認に留める（静的テストが脆くなりすぎないように）。
        var path = Path.Combine(RepoRoot, "NestSuite", "Services", "SessionTabMapper.cs");
        var src = File.ReadAllText(path);

        Assert.Contains("UI 表示ヒント", src);
        Assert.Contains("信頼ソース", src);
    }

    [Fact]
    public void TryRestoreSession_StillUsesExistingSafeFileOpenPath()
    {
        // WorkspaceKind を使って FileService へ直行するような危険な変更になっていないことを、
        // 既存の ShellFileOpenPlanner.Plan / LoadWorkspaceFileAt 経路が維持されていることで確認する。
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.Session.cs");
        var src = File.ReadAllText(path);
        var methodStart = src.IndexOf("private bool TryRestoreSession()", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var methodEnd = src.IndexOf("private void NotifyRestoreFailures", methodStart, StringComparison.Ordinal);
        Assert.True(methodEnd > methodStart);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        Assert.Contains("SessionTabMapper.CreateRestoreTargets(", body);
        Assert.Contains("ShellFileOpenPlanner.Plan(", body);
        Assert.Contains("LoadWorkspaceFileAt(", body);
    }
}
