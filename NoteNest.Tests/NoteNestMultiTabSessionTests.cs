using NoteNest.NestSuite;
using NoteNest.NestSuite.ChatNest;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.9.5: NoteNest 複数ファイルタブ対応の Session 独立性テスト。
///
/// <para>WPF を起動せずに <see cref="NestSuiteWorkspaceSession"/> /
/// <see cref="NestSuiteWorkspaceSessionManager"/> / <see cref="MainViewModel"/> のみを使って確認する。</para>
/// </summary>
public class NoteNestMultiTabSessionTests
{
    // ── NoteNest Session での MainViewModel 独立インスタンス確認 ──────────

    [Fact]
    public void NoteNest_TwoSessions_HoldDistinctMainViewModelInstances()
    {
        var vmA = new MainViewModel();
        var vmB = new MainViewModel();
        var sessionA = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, vmA);
        var sessionB = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, vmB);

        Assert.NotSame(sessionA.WorkspaceViewModel, sessionB.WorkspaceViewModel);
    }

    [Fact]
    public void NoteNest_SessionManager_CanHoldMultipleNoteNestSessions()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        var vmA = new MainViewModel();
        var vmB = new MainViewModel();
        mgr.Add(new NestSuiteWorkspaceSession(Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, vmA));
        mgr.Add(new NestSuiteWorkspaceSession(Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, vmB));

        Assert.Equal(2, mgr.Count);
        Assert.Equal(2, mgr.Sessions.Count(s => s.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest));
    }

    [Fact]
    public void NoteNest_SessionManager_ReverseLookupByViewModel_FindsCorrectSession()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        var vmA = new MainViewModel();
        var vmB = new MainViewModel();
        mgr.Add(new NestSuiteWorkspaceSession("tab-a", NestSuiteWorkspaceKind.NoteNest, vmA));
        mgr.Add(new NestSuiteWorkspaceSession("tab-b", NestSuiteWorkspaceKind.NoteNest, vmB));

        var found = mgr.Sessions.FirstOrDefault(s => ReferenceEquals(s.WorkspaceViewModel, vmB));
        Assert.NotNull(found);
        Assert.Equal("tab-b", found!.TabId);
    }

    [Fact]
    public void NoteNest_SessionManager_ReverseLookup_UnregisteredViewModel_ReturnsNull()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("tab-a", NestSuiteWorkspaceKind.NoteNest, new MainViewModel()));

        var unregistered = new MainViewModel();
        var found = mgr.Sessions.FirstOrDefault(s => ReferenceEquals(s.WorkspaceViewModel, unregistered));
        Assert.Null(found);
    }

    // ── NoteNest と ChatNest の混在確認 ──────────────────────────────────

    [Fact]
    public void SessionManager_FilterNoteNestOnly_ExcludesChatNestSessions()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("note", NestSuiteWorkspaceKind.NoteNest, new MainViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("chat", NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel()));

        var noteSessions = mgr.Sessions
            .Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest)
            .ToList();

        Assert.Single(noteSessions);
        Assert.Equal("note", noteSessions[0].TabId);
    }

    [Fact]
    public void SessionManager_FilterNoteNestOnly_WhenNoNoteNest_ReturnsEmpty()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("chat", NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel()));

        var noteSessions = mgr.Sessions
            .Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest)
            .ToList();

        Assert.Empty(noteSessions);
    }

    // ── FilePath 独立性確認 ───────────────────────────────────────────────

    [Fact]
    public void NoteNest_TwoSessions_FilePathsAreIndependent()
    {
        var sessionA = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, new MainViewModel(),
            @"C:\a.notenest", false);
        var sessionB = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, new MainViewModel(),
            @"C:\b.notenest", false);

        Assert.Equal(@"C:\a.notenest", sessionA.FilePath);
        Assert.Equal(@"C:\b.notenest", sessionB.FilePath);
        Assert.NotEqual(sessionA.FilePath, sessionB.FilePath);
    }

    [Fact]
    public void NoteNest_UpdatingSessionAFilePath_DoesNotAffectSessionB()
    {
        var sessionA = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, new MainViewModel());
        var sessionB = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, new MainViewModel());

        sessionA.FilePath = @"C:\updated.notenest";

        Assert.Equal(@"C:\updated.notenest", sessionA.FilePath);
        Assert.Null(sessionB.FilePath);
    }

    // ── IsModified 独立性確認 ─────────────────────────────────────────────

    [Fact]
    public void NoteNest_UpdatingIsModifiedA_DoesNotAffectSessionB()
    {
        var sessionA = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, new MainViewModel(), null, false);
        var sessionB = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, new MainViewModel(), null, false);

        sessionA.IsModified = true;

        Assert.True(sessionA.IsModified);
        Assert.False(sessionB.IsModified);
    }

    // ── 二重オープン検出（NoteNest 複数タブ対応での実用確認） ────────────

    [Fact]
    public void NoteNest_OpenFilePolicy_SamePath_DetectsAsDuplicate()
    {
        Assert.True(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\projects\work.notenest",
            @"C:\projects\work.notenest"));
    }

    [Fact]
    public void NoteNest_OpenFilePolicy_DifferentPaths_NotDuplicate()
    {
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\projects\a.notenest",
            @"C:\projects\b.notenest"));
    }

    [Fact]
    public void NoteNest_OpenFilePolicy_NullFilePath_NeverDuplicate()
    {
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(null, @"C:\projects\work.notenest"));
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(@"C:\projects\work.notenest", null));
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(null, null));
    }

    // ── WorkspaceKind 確認 ────────────────────────────────────────────────

    [Fact]
    public void NoteNest_Session_WorkspaceKind_IsNoteNest()
    {
        var session = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, new MainViewModel());
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, session.WorkspaceKind);
    }

    [Fact]
    public void NoteNest_Session_WorkspaceViewModel_IsMainViewModelType()
    {
        var vm = new MainViewModel();
        var session = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.NoteNest, vm);
        Assert.IsType<MainViewModel>(session.WorkspaceViewModel);
    }
}
