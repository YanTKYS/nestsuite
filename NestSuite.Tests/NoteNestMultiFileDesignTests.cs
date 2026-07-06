using NestSuite.Models;
using NestSuite;
using NestSuite.ChatNest;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v1.9.4: NoteNest 複数ファイルタブ対応の設計固定テスト。
///
/// <para>本格実装は v1.9.5 で行う。本テストは v1.9.4 で整理した設計の前提確認と
/// 既存 ChatNest 複数タブ対応の回帰防止が目的。</para>
///
/// <para>WPF を起動せずに <see cref="NestSuiteWorkspaceSession"/> ・
/// <see cref="NestSuiteWorkspaceSessionManager"/> ・
/// <see cref="NestSuiteTabFactory"/> ・
/// <see cref="NestSuiteOpenFilePolicy"/> のみを使って確認する。</para>
/// </summary>
public class NoteNestMultiFileDesignTests
{
    // ── NoteNest Session の WorkspaceKind 設計固定 ──────────────────────────

    [Fact]
    public void NoteNestSession_WorkspaceKind_IsNoteNest()
    {
        var session = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"),
            NestSuiteWorkspaceKind.NoteNest,
            new object());
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, session.WorkspaceKind);
    }

    [Fact]
    public void NoteNestSession_Untitled_FilePathIsNull()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);
        var session = new NestSuiteWorkspaceSession(
            tab.Id, tab.WorkspaceKind, new object(), tab.FilePath, tab.IsModified);
        Assert.Null(session.FilePath);
        Assert.False(session.IsModified);
    }

    // ── FilePath / IsModified の独立更新設計固定 ───────────────────────────

    [Fact]
    public void NoteNestSession_FilePath_CanBeUpdatedAfterSave()
    {
        var session = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, new object());
        session.FilePath = @"C:\work\project.notenest";
        Assert.Equal(@"C:\work\project.notenest", session.FilePath);
    }

    [Fact]
    public void NoteNestSession_IsModified_CanBeUpdated()
    {
        var session = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, new object(), null, false);
        session.IsModified = true;
        Assert.True(session.IsModified);
    }

    // ── 複数 NoteNest Session の独立性（案C の前提確認） ───────────────────
    //
    // v1.9.5: NestSuiteWorkspaceSession が NoteNest ViewModel を独立して保持できることが
    // 案C（MainViewModel をタブごとに生成する方針）の基盤となる。

    [Fact]
    public void TwoNoteNestSessions_HoldDistinctFilePaths()
    {
        var sessionA = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, new object(), @"C:\a.notenest", false);
        var sessionB = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, new object(), @"C:\b.notenest", false);

        Assert.NotEqual(sessionA.FilePath, sessionB.FilePath);
        Assert.NotEqual(sessionA.TabId, sessionB.TabId);
    }

    [Fact]
    public void TwoNoteNestSessions_CanHoldDistinctViewModelInstances()
    {
        var vmA = new object();
        var vmB = new object();
        var sessionA = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, vmA);
        var sessionB = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, vmB);

        Assert.NotSame(sessionA.WorkspaceViewModel, sessionB.WorkspaceViewModel);
    }

    [Fact]
    public void TwoNoteNestSessions_UpdatingFilePathA_DoesNotAffectB()
    {
        var sessionA = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, new object());
        var sessionB = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, new object());

        sessionA.FilePath = @"C:\a.notenest";

        Assert.Equal(@"C:\a.notenest", sessionA.FilePath);
        Assert.Null(sessionB.FilePath);
    }

    [Fact]
    public void TwoNoteNestSessions_UpdatingIsModifiedA_DoesNotAffectB()
    {
        var sessionA = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, new object(), null, false);
        var sessionB = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, new object(), null, false);

        sessionA.IsModified = true;

        Assert.True(sessionA.IsModified);
        Assert.False(sessionB.IsModified);
    }

    // ── SessionManager で NoteNest Session を管理できる ─────────────────────

    [Fact]
    public void NoteNestSessions_CanBeAddedToSessionManager()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, new object()));
        mgr.Add(new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, new object()));

        Assert.Equal(2, mgr.Count);
        Assert.Equal(2, mgr.Sessions.Count(s => s.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest));
    }

    [Fact]
    public void SessionManager_FilterByNoteNestKind_ExcludesChatNestAndIdeaNest()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("note", NestSuiteWorkspaceKind.NoteNest, new object()));
        mgr.Add(new NestSuiteWorkspaceSession("chat", NestSuiteWorkspaceKind.ChatNest, new object()));
        mgr.Add(new NestSuiteWorkspaceSession("idea", NestSuiteWorkspaceKind.IdeaNest, new object()));

        var noteSessions = mgr.Sessions
            .Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest)
            .ToList();

        Assert.Single(noteSessions);
        Assert.Equal("note", noteSessions[0].TabId);
    }

    // ── NoteNest 保存スキーマ設計固定 ───────────────────────────────────────

    [Fact]
    public void NoteNest_SaveSchema_UsesCurrentSchemaVersion()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".notenest");
        try
        {
            var project = new Project { ProjectName = "MultiFileSchemaGuard", Version = Project.CurrentSchemaVersion };
            new ProjectFileService().Save(path, project);
            var loaded = new ProjectFileService().Load(path);
            Assert.Equal(Project.CurrentSchemaVersion, loaded.Version);
        }
        finally { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
    }

    // ── NestSuiteTabFactory の .notenest 認識確認 ───────────────────────────

    [Fact]
    public void TabFactory_NoteNestExtension_IsRecognizedAsNoteNest()
    {
        var result = NestSuiteTabFactory.TryGetKind("project.notenest", out var kind);
        Assert.True(result);
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, kind);
    }

    [Fact]
    public void TabFactory_NoteNestExtension_IsNotChatNest()
    {
        NestSuiteTabFactory.TryGetKind("project.notenest", out var kind);
        Assert.NotEqual(NestSuiteWorkspaceKind.ChatNest, kind);
    }

    [Fact]
    public void TabFactory_NoteNestExtension_IsNotIdeaNest()
    {
        NestSuiteTabFactory.TryGetKind("project.notenest", out var kind);
        Assert.NotEqual(NestSuiteWorkspaceKind.IdeaNest, kind);
    }

    // ── 二重オープン検出（NoteNest 用途の確認） ──────────────────────────────

    [Fact]
    public void OpenFilePolicy_SameNoteNestPath_IsDuplicate()
    {
        Assert.True(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\projects\work.notenest",
            @"C:\projects\work.notenest"));
    }

    [Fact]
    public void OpenFilePolicy_DifferentNoteNestPaths_AreNotDuplicate()
    {
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\projects\a.notenest",
            @"C:\projects\b.notenest"));
    }

    [Fact]
    public void OpenFilePolicy_CaseInsensitive_NoteNestIsDuplicate()
    {
        Assert.True(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\PROJECTS\Work.notenest",
            @"C:\projects\work.notenest"));
    }

    [Fact]
    public void OpenFilePolicy_AfterNormalization_NoteNestDetectedAsDuplicate()
    {
        var rel = "work.notenest";
        var abs = System.IO.Path.GetFullPath(rel);
        Assert.True(NestSuiteOpenFilePolicy.IsSameFile(
            System.IO.Path.GetFullPath(rel), abs));
    }

    // ── ChatNest 複数タブ対応の回帰確認 ─────────────────────────────────────

    [Fact]
    public void ChatNest_MultiTab_TwoViewModels_AreIndependent()
    {
        var vmA = new ChatNestWorkspaceViewModel();
        var vmB = new ChatNestWorkspaceViewModel();
        vmA.InputText = "A の入力";

        Assert.Equal("A の入力", vmA.InputText);
        Assert.Equal(string.Empty, vmB.InputText);
    }

    [Fact]
    public void ChatNest_MultiTab_SessionManager_StillWorks()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        var vmA = new ChatNestWorkspaceViewModel();
        var vmB = new ChatNestWorkspaceViewModel();
        mgr.Add(new NestSuiteWorkspaceSession("tab-a", NestSuiteWorkspaceKind.ChatNest, vmA));
        mgr.Add(new NestSuiteWorkspaceSession("tab-b", NestSuiteWorkspaceKind.ChatNest, vmB));

        Assert.Equal(2, mgr.Count);
        Assert.True(mgr.TryGet("tab-a", out var sa));
        Assert.True(mgr.TryGet("tab-b", out var sb));
        Assert.NotSame(sa!.WorkspaceViewModel, sb!.WorkspaceViewModel);
    }
}
