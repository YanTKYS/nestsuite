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

    // TD-75a-2 (v2.16.27): TD-25 の backlog 完了確認・v2.10.12 存在確認は
    // NestSuiteDocsContractTests.ReleaseNoteVersionAndIdRecords へ移設した
    // （(v2.10.12, TD-25) のデータ行）。移設元の ReleaseNotes_Contains_V2_10_12 は
    // 実際には "v2.10.13"（TD-26 のバージョン）を確認しており、TD-25 の実際の見出しである
    // "v2.10.12" とは一致していなかった（コピー&ペースト由来と見られる誤り）。
    // 移設にあわせて正しいバージョンで確認するよう修正した。

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

    // ── v2.16.38 TD-59b-4: TryPrepareOpen 化に伴う OpenContext / 読込回数の確認 ──────

    [Theory]
    [InlineData("NoteNest", NestSuiteWorkspaceKind.NoteNest)]
    [InlineData("IdeaNest", NestSuiteWorkspaceKind.IdeaNest)]
    [InlineData("ChatNest", NestSuiteWorkspaceKind.ChatNest)]
    public void TryCreateRestoreTarget_NestSuitePath_OpenContextHoldsPreloadedEnvelope_WithExactlyOneReadCall(
        string kindName, NestSuiteWorkspaceKind expectedKind)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        var wrapped = NestSuiteWorkspaceEnvelope.Wrap(kindName, "0.1.0", "{}");
        var readCalls = 0;

        var ok = SessionTabMapper.TryCreateRestoreTarget(
            path, out var target, out _,
            fileExists: _ => true,
            readAllText: _ => { readCalls++; return wrapped; });

        Assert.True(ok);
        Assert.NotNull(target.OpenContext);
        Assert.Equal(path, target.FilePath);
        Assert.Equal(path, target.OpenContext.FilePath);
        Assert.Equal(expectedKind, target.WorkspaceKind);
        Assert.Equal(expectedKind, target.OpenContext.WorkspaceKind);
        Assert.NotNull(target.OpenContext.Preloaded);
        Assert.Equal(path, target.OpenContext.Preloaded!.SourcePath);
        Assert.Equal(1, readCalls);
    }

    [Theory]
    [InlineData(".notenest", NestSuiteWorkspaceKind.NoteNest)]
    [InlineData(".ideanest", NestSuiteWorkspaceKind.IdeaNest)]
    [InlineData(".chatnest", NestSuiteWorkspaceKind.ChatNest)]
    public void TryCreateRestoreTarget_LegacyPath_OpenContextHasNoPreloadedEnvelope_WithZeroReadCalls(
        string extension, NestSuiteWorkspaceKind expectedKind)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + extension);
        var readCalls = 0;

        var ok = SessionTabMapper.TryCreateRestoreTarget(
            path, out var target,
            fileExists: _ => true,
            readAllText: _ => { readCalls++; return "unused"; });

        Assert.True(ok);
        Assert.NotNull(target.OpenContext);
        Assert.Equal(expectedKind, target.WorkspaceKind);
        Assert.Null(target.OpenContext.Preloaded);
        Assert.Equal(0, readCalls);
    }

    [Fact]
    public void TryCreateRestoreTarget_MissingNestSuiteFile_ReportsFileNotFound_WithZeroReadCalls()
    {
        var readCalls = 0;

        var ok = SessionTabMapper.TryCreateRestoreTarget(
            @"C:\missing\ghost.nestsuite", out var target, out var failure,
            fileExists: _ => false,
            readAllText: _ => { readCalls++; return "unused"; });

        Assert.False(ok);
        Assert.Equal(WorkspaceKindDetectionFailure.FileNotFound, failure);
        Assert.Equal(0, readCalls);
    }

    [Fact]
    public void TryCreateRestoreTarget_MissingLegacyFile_ReportsFileNotFound_WithZeroReadCalls()
    {
        var readCalls = 0;

        var ok = SessionTabMapper.TryCreateRestoreTarget(
            @"C:\missing\ghost.notenest", out var target, out var failure,
            fileExists: _ => false,
            readAllText: _ => { readCalls++; return "unused"; });

        Assert.False(ok);
        Assert.Equal(WorkspaceKindDetectionFailure.FileNotFound, failure);
        Assert.Equal(0, readCalls);
    }

    [Fact]
    public void TryCreateRestoreTarget_InvalidNestSuiteJson_ReportsInvalidFormat_WithOneReadCall()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        var readCalls = 0;

        var ok = SessionTabMapper.TryCreateRestoreTarget(
            path, out var target, out var failure,
            fileExists: _ => true,
            readAllText: _ => { readCalls++; return "not valid json"; });

        Assert.False(ok);
        Assert.Equal(WorkspaceKindDetectionFailure.InvalidFormat, failure);
        Assert.Equal(1, readCalls);
    }

    [Fact]
    public void TryCreateRestoreTarget_UnknownWorkspaceKindInWrapper_ReportsUnknownWorkspaceKind_WithOneReadCall()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        var wrapped = NestSuiteWorkspaceEnvelope.Wrap("MysteryNest", "0.1.0", "{}");
        var readCalls = 0;

        var ok = SessionTabMapper.TryCreateRestoreTarget(
            path, out var target, out var failure,
            fileExists: _ => true,
            readAllText: _ => { readCalls++; return wrapped; });

        Assert.False(ok);
        Assert.Equal(WorkspaceKindDetectionFailure.UnknownWorkspaceKind, failure);
        Assert.Equal(1, readCalls);
    }

    [Fact]
    public void TryCreateRestoreTarget_SchemaVersionTooNewInWrapper_ReportsSchemaVersionTooNew_WithOneReadCall()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        var wrapped = NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "99.0.0", "{}");
        var readCalls = 0;

        var ok = SessionTabMapper.TryCreateRestoreTarget(
            path, out var target, out var failure,
            fileExists: _ => true,
            readAllText: _ => { readCalls++; return wrapped; });

        Assert.False(ok);
        Assert.Equal(WorkspaceKindDetectionFailure.SchemaVersionTooNew, failure);
        Assert.Equal(1, readCalls);
    }

    [Fact]
    public void TryCreateRestoreTarget_UnauthorizedAccessOnRead_ReportsAccessDenied_WithOneReadCall()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        var readCalls = 0;

        var ok = SessionTabMapper.TryCreateRestoreTarget(
            path, out var target, out var failure,
            fileExists: _ => true,
            readAllText: _ => { readCalls++; throw new UnauthorizedAccessException(); });

        Assert.False(ok);
        Assert.Equal(WorkspaceKindDetectionFailure.AccessDenied, failure);
        Assert.Equal(1, readCalls);
    }

    [Fact]
    public void TryCreateRestoreTarget_IOExceptionOnRead_ReportsIoError_WithOneReadCall()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        var readCalls = 0;

        var ok = SessionTabMapper.TryCreateRestoreTarget(
            path, out var target, out var failure,
            fileExists: _ => true,
            readAllText: _ => { readCalls++; throw new IOException(); });

        Assert.False(ok);
        Assert.Equal(WorkspaceKindDetectionFailure.IoError, failure);
        Assert.Equal(1, readCalls);
    }

    [Fact]
    public void TryCreateRestoreTarget_UnexpectedExceptionOnRead_ReportsUnknown_WithOneReadCall()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        var readCalls = 0;

        var ok = SessionTabMapper.TryCreateRestoreTarget(
            path, out var target, out var failure,
            fileExists: _ => true,
            readAllText: _ => { readCalls++; throw new InvalidOperationException(); });

        Assert.False(ok);
        Assert.Equal(WorkspaceKindDetectionFailure.Unknown, failure);
        Assert.Equal(1, readCalls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryCreateRestoreTarget_EmptyOrWhitespacePath_SkipsSilently_NoFailureRecorded(string filePath)
    {
        var ok = SessionTabMapper.TryCreateRestoreTarget(filePath, out var target, out var failure);

        Assert.False(ok);
        Assert.Equal(WorkspaceKindDetectionFailure.None, failure);
    }

    [Fact]
    public void TryCreateRestoreTarget_UnsupportedExtension_SkipsSilently_NoFailureRecorded()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".unknownext");

        var ok = SessionTabMapper.TryCreateRestoreTarget(
            path, out var target, out var failure, fileExists: _ => true);

        Assert.False(ok);
        Assert.Equal(WorkspaceKindDetectionFailure.None, failure);
    }

    [Fact]
    public void CreateRestoreTargets_TabsShape_NestSuitePinnedEntry_PropagatesIsPinned_AndOpenContext()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        var wrapped = NestSuiteWorkspaceEnvelope.Wrap("IdeaNest", "0.1.0", "{}");
        var readCalls = 0;
        var state = new NestSuiteSessionState
        {
            Tabs = new List<NestSuiteSessionTabState>
            {
                new() { FilePath = path, WorkspaceKind = "NoteNest", IsPinned = true },
            },
        };

        var targets = SessionTabMapper.CreateRestoreTargets(
            state, fileExists: _ => true, out var failures,
            readAllText: _ => { readCalls++; return wrapped; });

        Assert.Empty(failures);
        var target = Assert.Single(targets);
        Assert.True(target.IsPinned);
        Assert.Equal(NestSuiteWorkspaceKind.IdeaNest, target.WorkspaceKind);
        Assert.NotNull(target.OpenContext);
        Assert.Equal(1, readCalls);
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

    // ── v2.16.18 TD-70 (review2-fable5.md 新リスク①): pending entry の再試行解除 ─────────

    [Fact]
    public void RemoveFileNotFoundEntries_RemovesOnlyFileNotFound_KeepsOthersAndOrder()
    {
        var entries = new[]
        {
            new SessionRestoreFailure(@"C:\work\a.notenest", WorkspaceKindDetectionFailure.FileNotFound),
            new SessionRestoreFailure(@"C:\work\b.nestsuite", WorkspaceKindDetectionFailure.InvalidFormat),
            new SessionRestoreFailure(@"C:\work\c.notenest", WorkspaceKindDetectionFailure.FileNotFound),
            new SessionRestoreFailure(@"C:\work\d.nestsuite", WorkspaceKindDetectionFailure.SchemaVersionTooNew),
            new SessionRestoreFailure(@"C:\work\e.nestsuite", WorkspaceKindDetectionFailure.AccessDenied),
        };

        var result = SessionTabMapper.RemoveFileNotFoundEntries(entries);

        Assert.Equal(
            new[] { @"C:\work\b.nestsuite", @"C:\work\d.nestsuite", @"C:\work\e.nestsuite" },
            result.Select(f => f.FilePath));
        Assert.DoesNotContain(result, f => f.Failure == WorkspaceKindDetectionFailure.FileNotFound);
    }

    [Fact]
    public void RemoveFileNotFoundEntries_NoFileNotFoundEntries_ReturnsAllUnchanged()
    {
        var entries = new[]
        {
            new SessionRestoreFailure(@"C:\work\b.nestsuite", WorkspaceKindDetectionFailure.InvalidFormat),
            new SessionRestoreFailure(@"C:\work\e.nestsuite", WorkspaceKindDetectionFailure.AccessDenied),
        };

        var result = SessionTabMapper.RemoveFileNotFoundEntries(entries);

        Assert.Equal(entries, result);
    }

    [Fact]
    public void RemoveFileNotFoundEntries_AllFileNotFound_ReturnsEmpty()
    {
        var entries = new[]
        {
            new SessionRestoreFailure(@"C:\work\a.notenest", WorkspaceKindDetectionFailure.FileNotFound),
            new SessionRestoreFailure(@"C:\work\c.notenest", WorkspaceKindDetectionFailure.FileNotFound),
        };

        var result = SessionTabMapper.RemoveFileNotFoundEntries(entries);

        Assert.Empty(result);
    }

    [Fact]
    public void RemoveFileNotFoundEntries_EmptyInput_ReturnsEmpty()
    {
        var result = SessionTabMapper.RemoveFileNotFoundEntries([]);

        Assert.Empty(result);
    }

    [Fact]
    public void CreateSessionState_AfterRemovingFileNotFoundEntries_ExcludesThemFromTabsAndFilePaths()
    {
        // TD-70 解除後の CreateSessionState 出力から、解除済み FileNotFound entry が
        // Tabs[] / FilePaths[] のどちらにも残らないこと、かつ TD-69 の
        // 「FilePaths[] は Tabs[].FilePath から導出」不変条件が引き続き成立することを確認する。
        var pending = new[]
        {
            new SessionRestoreFailure(@"C:\work\missing.notenest", WorkspaceKindDetectionFailure.FileNotFound),
            new SessionRestoreFailure(@"C:\work\broken.nestsuite", WorkspaceKindDetectionFailure.InvalidFormat),
        };

        var afterForget = SessionTabMapper.RemoveFileNotFoundEntries(pending);
        var state = SessionTabMapper.CreateSessionState([], null, afterForget);

        Assert.DoesNotContain(@"C:\work\missing.notenest", state.FilePaths);
        Assert.DoesNotContain(state.Tabs, t => t.FilePath == @"C:\work\missing.notenest");

        var remaining = Assert.Single(state.Tabs);
        Assert.Equal(@"C:\work\broken.nestsuite", remaining.FilePath);
        Assert.Null(remaining.WorkspaceKind);
        Assert.Equal(state.Tabs.Select(t => t.FilePath), state.FilePaths);
    }

    [Fact]
    public void CreateSessionState_WithoutForgetting_StillCarriesFileNotFoundEntryForward()
    {
        // 「いいえ」を選んだ場合（＝ RemoveFileNotFoundEntries を呼ばない場合）は、
        // TD-65 どおり FileNotFound entry も次回へ持ち越される。
        var pending = new[]
        {
            new SessionRestoreFailure(@"C:\work\missing.notenest", WorkspaceKindDetectionFailure.FileNotFound),
        };

        var state = SessionTabMapper.CreateSessionState([], null, pending);

        Assert.Contains(@"C:\work\missing.notenest", state.FilePaths);
        Assert.Contains(state.Tabs, t => t.FilePath == @"C:\work\missing.notenest");
    }

    [Fact]
    public void OfferToForgetFileNotFoundRestoreFailures_MethodIsRemoved()
    {
        // v2.16.21 SH-34 (review4-fable5.md LT-9 フェーズ1): 復元失敗通知と再試行解除確認を
        // 1 ダイアログへ統合したため、別ダイアログを出していた旧 helper は不要になった。
        var src = ReadSessionSource();
        Assert.DoesNotContain("private void OfferToForgetFileNotFoundRestoreFailures", src);
    }

    [Fact]
    public void NotifyRestoreFailures_ConfirmBranch_OnlyReachableWhenFileNotFoundPresent_AndReturnsBeforeShowError()
    {
        var src = ReadSessionSource();
        var methodStart = src.IndexOf("private void NotifyRestoreFailures(IReadOnlyList<SessionRestoreFailure> failures)", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var methodEnd = src.IndexOf("private void ForgetFileNotFoundRestoreFailures", methodStart, StringComparison.Ordinal);
        Assert.True(methodEnd > methodStart);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        var guardIdx = body.IndexOf("f.Failure == WorkspaceKindDetectionFailure.FileNotFound", StringComparison.Ordinal);
        var confirmIdx = body.IndexOf("_dialogs.Confirm(", StringComparison.Ordinal);
        var returnIdx = body.IndexOf("return;", confirmIdx, StringComparison.Ordinal);
        var showErrorIdx = body.IndexOf("_dialogs.ShowError(message, \"セッション復元\");", StringComparison.Ordinal);

        Assert.True(guardIdx >= 0 && confirmIdx > guardIdx, "Confirm は FileNotFound 判定より後で呼ぶ必要がある");
        Assert.True(returnIdx > confirmIdx, "Confirm 分岐は return で終える必要がある（ShowError と重ねて呼ばない）");
        Assert.True(showErrorIdx > returnIdx, "ShowError は Confirm 分岐の return より後（FileNotFound を含まない場合の経路）にある必要がある");

        // Confirm / ShowError とも呼び出しは 1 箇所のみ（同じ起動で 2 枚出ることはない）。
        var secondConfirmIdx = body.IndexOf("_dialogs.Confirm(", confirmIdx + 1, StringComparison.Ordinal);
        var secondShowErrorIdx = body.IndexOf("_dialogs.ShowError(", showErrorIdx + 1, StringComparison.Ordinal);
        Assert.Equal(-1, secondConfirmIdx);
        Assert.Equal(-1, secondShowErrorIdx);
    }

    [Fact]
    public void NotifyRestoreFailures_ConfirmedTrue_ForgetsFileNotFoundEntries_AndSetsStartupFlag()
    {
        var src = ReadSessionSource();
        var methodStart = src.IndexOf("private void NotifyRestoreFailures(IReadOnlyList<SessionRestoreFailure> failures)", StringComparison.Ordinal);
        var methodEnd = src.IndexOf("private void ForgetFileNotFoundRestoreFailures", methodStart, StringComparison.Ordinal);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        var confirmedIfIdx = body.IndexOf("if (confirmed)", StringComparison.Ordinal);
        var forgetCallIdx = body.IndexOf("ForgetFileNotFoundRestoreFailures();", StringComparison.Ordinal);
        var flagSetIdx = body.IndexOf("_forgotFileNotFoundRestoreFailuresDuringStartup = true;", StringComparison.Ordinal);

        Assert.True(confirmedIfIdx >= 0 && forgetCallIdx > confirmedIfIdx, "ForgetFileNotFoundRestoreFailures は if (confirmed) の中で呼ぶ必要がある");
        Assert.True(flagSetIdx > forgetCallIdx);
    }

    [Fact]
    public void NotifyRestoreFailures_UsesMessageBuilder_ForBothConfirmAndShowErrorBranches()
    {
        // ShowError / Confirm 両方の呼び出しが、UI 非依存の SessionRestoreFailuresMessageBuilder の
        // 結果（本文・再試行解除確認文とも）を使っていることを確認する（本文の二重管理を避ける）。
        var src = ReadSessionSource();
        var methodStart = src.IndexOf("private void NotifyRestoreFailures(IReadOnlyList<SessionRestoreFailure> failures)", StringComparison.Ordinal);
        var methodEnd = src.IndexOf("private void ForgetFileNotFoundRestoreFailures", methodStart, StringComparison.Ordinal);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        Assert.Contains("SessionRestoreFailuresMessageBuilder.BuildFailuresMessage(failures)", body);
        Assert.Contains("SessionRestoreFailuresMessageBuilder.ForgetFileNotFoundQuestion", body);
    }

    [Fact]
    public void ForgetFileNotFoundRestoreFailures_DelegatesToSessionTabMapperHelper()
    {
        var src = ReadSessionSource();
        var methodStart = src.IndexOf("private void ForgetFileNotFoundRestoreFailures()", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var body = src.Substring(methodStart, Math.Min(300, src.Length - methodStart));

        Assert.Contains("SessionTabMapper.RemoveFileNotFoundEntries(_pendingSessionRestoreEntries)", body);
    }

    [Fact]
    public void NotifyRestoreFailures_DoesNotCallSaveSessionDirectly()
    {
        // TD-66 の _isRestoringSession ガード下で、復元中に中途半端な session 保存をしないことを
        // 静的に裏付ける。保存はコンストラクターが復元完了後にまとめて行う。
        var src = ReadSessionSource();
        var methodStart = src.IndexOf("private void NotifyRestoreFailures(IReadOnlyList<SessionRestoreFailure> failures)", StringComparison.Ordinal);
        var methodEnd = src.IndexOf("private void ForgetFileNotFoundRestoreFailures", methodStart, StringComparison.Ordinal);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        Assert.DoesNotContain("SaveSession();", body);
        Assert.DoesNotContain("SaveSessionAfterTabChange();", body);
    }

    // v2.16.28 TD-75b: コンストラクター内の SaveSession 呼び出し条件を、ソース文字列の
    // 文順固定確認（saveIdx > ifIdx && nextIfIdx > saveIdx）から、UI 非依存の policy helper
    // StartupRestoreSessionPolicy.ShouldSaveSessionAfterStartupRestore の単体テストへ置き換えた。
    // 「復元成功時は保存する / 復元失敗でも FileNotFound 再試行解除があれば保存する /
    // 復元失敗かつ解除なしなら保存しない」という判断そのものを、実装の書き換え（条件式の
    // 順序や変数名の変更）に強い形で確認する。配線（コンストラクターがこの helper を実際に
    // 使っていること）は NestSuiteShellSessionPersistenceTests 側で軽く確認する。
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public void ShouldSaveSessionAfterStartupRestore_ReturnsExpected(
        bool restoredSession,
        bool forgotFileNotFound,
        bool expected)
    {
        Assert.Equal(
            expected,
            StartupRestoreSessionPolicy.ShouldSaveSessionAfterStartupRestore(restoredSession, forgotFileNotFound));
    }

    [Fact]
    public void ForgetFileNotFoundQuestion_MentionsStopRetryingIntent()
    {
        Assert.Contains("次回から復元対象から外しますか", SessionRestoreFailuresMessageBuilder.ForgetFileNotFoundQuestion);
    }

    [Fact]
    public void ForgetFileNotFoundQuestion_MentionsNetworkOrExternalDriveCaution()
    {
        Assert.Contains("ネットワークドライブ", SessionRestoreFailuresMessageBuilder.ForgetFileNotFoundQuestion);
        Assert.Contains("外部ドライブ", SessionRestoreFailuresMessageBuilder.ForgetFileNotFoundQuestion);
    }

    [Fact]
    public void ForgetFileNotFoundQuestion_ClarifiesOnlyFileNotFoundIsAffected()
    {
        // Yes を選んでも InvalidFormat / AccessDenied / SchemaVersionTooNew は解除対象に含まれないことを、
        // 確認文自体が明示していることを確認する。
        Assert.Contains("見つからないファイル以外は引き続き再試行されます", SessionRestoreFailuresMessageBuilder.ForgetFileNotFoundQuestion);
    }

    // ── v2.16.19 TD-71 (review2-fable5.md 新リスク②): 復元失敗通知の .bak 詳細案内 ─────────
    // v2.16.21 SH-34 で本文組み立てが SessionRestoreFailuresMessageBuilder へ移ったため、
    // ソーステキスト静的確認ではなく builder の実際の挙動でテストする。

    [Fact]
    public void BuildFailuresMessage_AppendsBakDetailHint_OnlyWhenInvalidFormatPresent()
    {
        var withInvalidFormat = new[]
        {
            new SessionRestoreFailure(@"C:\work\broken.nestsuite", WorkspaceKindDetectionFailure.InvalidFormat),
        };
        var withoutInvalidFormat = new[]
        {
            new SessionRestoreFailure(@"C:\work\missing.notenest", WorkspaceKindDetectionFailure.FileNotFound),
        };

        Assert.Contains(
            FileErrorMessages.MultipleFailuresBakDetailHint,
            SessionRestoreFailuresMessageBuilder.BuildFailuresMessage(withInvalidFormat));
        Assert.DoesNotContain(
            FileErrorMessages.MultipleFailuresBakDetailHint,
            SessionRestoreFailuresMessageBuilder.BuildFailuresMessage(withoutInvalidFormat));
    }

    [Fact]
    public void BuildFailuresMessage_MixedFileNotFoundAndInvalidFormat_IncludesBakHint_ButNotForgetQuestion()
    {
        // .bak 誘導はメッセージ本文（builder）側の責務、FileNotFound 再試行解除確認は
        // 呼び出し側（Shell）が別途連結する責務であり、混同していないことを確認する。
        var mixed = new[]
        {
            new SessionRestoreFailure(@"C:\work\missing.notenest", WorkspaceKindDetectionFailure.FileNotFound),
            new SessionRestoreFailure(@"C:\work\broken.nestsuite", WorkspaceKindDetectionFailure.InvalidFormat),
        };

        var message = SessionRestoreFailuresMessageBuilder.BuildFailuresMessage(mixed);

        Assert.Contains(FileErrorMessages.MultipleFailuresBakDetailHint, message);
        Assert.DoesNotContain(SessionRestoreFailuresMessageBuilder.ForgetFileNotFoundQuestion, message);
    }

    [Fact]
    public void MergedConfirmMessage_MixedFileNotFoundAndInvalidFormat_ContainsBothBakHintAndForgetQuestion()
    {
        // NestSuiteShellWindow.NotifyRestoreFailures が実際に組み立てる文字列
        // （message + "\n\n" + ForgetFileNotFoundQuestion）を模して、混在時に両方の要素が
        // 1 つの Confirm メッセージに含まれることを確認する。
        var mixed = new[]
        {
            new SessionRestoreFailure(@"C:\work\missing.notenest", WorkspaceKindDetectionFailure.FileNotFound),
            new SessionRestoreFailure(@"C:\work\broken.nestsuite", WorkspaceKindDetectionFailure.InvalidFormat),
        };

        var merged = SessionRestoreFailuresMessageBuilder.BuildFailuresMessage(mixed) +
            "\n\n" + SessionRestoreFailuresMessageBuilder.ForgetFileNotFoundQuestion;

        Assert.Contains(FileErrorMessages.MultipleFailuresBakDetailHint, merged);
        Assert.Contains("次回から復元対象から外しますか", merged);
    }

    [Fact]
    public void FileErrorMessages_MultipleFailuresBakDetailHint_MentionsBakAndReopeningIndividually()
    {
        Assert.Contains(".bak", FileErrorMessages.MultipleFailuresBakDetailHint);
        Assert.Contains("単体で開き直す", FileErrorMessages.MultipleFailuresBakDetailHint);
    }

    [Fact]
    public void ActivateTab_StillDoesNotCallSaveSessionAfterTabChange()
    {
        // TD-71 は ActiveFilePath の保存タイミングを変更しない。アクティブタブ切替の中心である
        // ActivateTab（NestSuiteShellWindow.TabSelection.cs）が、TD-66 の随時保存 helper を
        // 新たに呼ぶようになっていないことを確認する。
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.TabSelection.cs");
        var src = File.ReadAllText(path);
        var methodStart = src.IndexOf("private void ActivateTab(NestSuiteDocumentTab tab)", StringComparison.Ordinal);
        Assert.True(methodStart >= 0, "ActivateTab が見つからない");
        var methodEnd = src.IndexOf("protected override void OnPreviewKeyDown", methodStart, StringComparison.Ordinal);
        Assert.True(methodEnd > methodStart);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        Assert.DoesNotContain("SaveSessionAfterTabChange();", body);
        Assert.DoesNotContain("SaveSession();", body);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private static string ReadSessionSource() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.Session.cs"));
}
