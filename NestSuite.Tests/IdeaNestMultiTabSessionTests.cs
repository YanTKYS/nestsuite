using NoteNest.NestSuite;
using NoteNest.NestSuite.IdeaNest.ViewModels;
using NoteNest.NestSuite.IdeaNest.Models;
using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.9.7: IdeaNest 複数ファイルタブ対応の Session 独立性テスト。
///
/// <para>WPF を起動せずに <see cref="IdeaNestWorkspaceViewModel"/> /
/// <see cref="NestSuiteWorkspaceSession"/> / <see cref="NestSuiteWorkspaceSessionManager"/> を使って確認する。</para>
/// </summary>
public class IdeaNestMultiTabSessionTests
{
    // ── ViewModel 独立性 ────────────────────────────────────────────────────

    [Fact]
    public void TwoViewModels_HasChanges_AreIndependent()
    {
        var vmA = new IdeaNestWorkspaceViewModel();
        var vmB = new IdeaNestWorkspaceViewModel();

        vmA.MarkDirty();

        Assert.True(vmA.HasChanges);
        Assert.False(vmB.HasChanges);
    }

    [Fact]
    public void TwoViewModels_MarkSaved_IsIndependent()
    {
        var vmA = new IdeaNestWorkspaceViewModel();
        var vmB = new IdeaNestWorkspaceViewModel();

        vmA.MarkDirty();
        vmB.MarkDirty();

        vmA.MarkSaved();

        Assert.False(vmA.HasChanges);
        Assert.True(vmB.HasChanges);
    }

    [Fact]
    public void TwoViewModels_LoadFromWorkspace_DoNotCrossContaminate()
    {
        var vmA = new IdeaNestWorkspaceViewModel();
        var vmB = new IdeaNestWorkspaceViewModel();

        var workspaceA = new Workspace { WorkspaceName = "Workspace A" };
        var workspaceB = new Workspace { WorkspaceName = "Workspace B" };

        vmA.LoadFromWorkspace(workspaceA);
        vmB.LoadFromWorkspace(workspaceB);

        Assert.Equal("Workspace A", vmA.DisplayName);
        Assert.Equal("Workspace B", vmB.DisplayName);
    }

    [Fact]
    public void TwoViewModels_LoadFromWorkspace_SetsHasChangesToFalse()
    {
        var vmA = new IdeaNestWorkspaceViewModel();
        var vmB = new IdeaNestWorkspaceViewModel();

        vmA.MarkDirty();
        vmB.MarkDirty();

        vmA.LoadFromWorkspace(new Workspace());

        Assert.False(vmA.HasChanges);
        Assert.True(vmB.HasChanges);
    }

    [Fact]
    public void ViewModelA_PropertyChanged_DoesNotFireOnViewModelB()
    {
        var vmA = new IdeaNestWorkspaceViewModel();
        var vmB = new IdeaNestWorkspaceViewModel();

        bool bFired = false;
        vmB.PropertyChanged += (_, _) => bFired = true;

        vmA.MarkDirty();

        Assert.False(bFired);
    }

    // ── Session Manager での独立 ViewModel 管理 ────────────────────────────

    [Fact]
    public void IdeaNest_TwoSessions_HoldDistinctViewModelInstances()
    {
        var vmA = new IdeaNestWorkspaceViewModel();
        var vmB = new IdeaNestWorkspaceViewModel();

        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession(Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.IdeaNest, vmA));
        mgr.Add(new NestSuiteWorkspaceSession(Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.IdeaNest, vmB));

        Assert.Equal(2, mgr.Count);
        Assert.NotSame(
            mgr.Sessions.ElementAt(0).WorkspaceViewModel,
            mgr.Sessions.ElementAt(1).WorkspaceViewModel);
    }

    [Fact]
    public void IdeaNest_SessionManager_CanHoldMultipleIdeaNestSessions()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession(Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession(Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel()));

        Assert.Equal(2, mgr.Count);
        Assert.Equal(2, mgr.Sessions.Count(s => s.WorkspaceKind == NestSuiteWorkspaceKind.IdeaNest));
    }

    [Fact]
    public void IdeaNest_SessionManager_ReverseLookupByViewModel_FindsCorrectSession()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        var vmA = new IdeaNestWorkspaceViewModel();
        var vmB = new IdeaNestWorkspaceViewModel();
        mgr.Add(new NestSuiteWorkspaceSession("tab-a", NestSuiteWorkspaceKind.IdeaNest, vmA));
        mgr.Add(new NestSuiteWorkspaceSession("tab-b", NestSuiteWorkspaceKind.IdeaNest, vmB));

        var found = mgr.Sessions.FirstOrDefault(s => ReferenceEquals(s.WorkspaceViewModel, vmB));
        Assert.NotNull(found);
        Assert.Equal("tab-b", found!.TabId);
    }

    [Fact]
    public void IdeaNest_SessionManager_ReverseLookup_UnregisteredViewModel_ReturnsNull()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("tab-a", NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel()));

        var unregistered = new IdeaNestWorkspaceViewModel();
        var found = mgr.Sessions.FirstOrDefault(s => ReferenceEquals(s.WorkspaceViewModel, unregistered));
        Assert.Null(found);
    }

    // ── FilePath 独立性確認 ───────────────────────────────────────────────

    [Fact]
    public void IdeaNest_TwoSessions_FilePathsAreIndependent()
    {
        var sessionA = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel(),
            @"C:\a.ideanest", false);
        var sessionB = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel(),
            @"C:\b.ideanest", false);

        Assert.Equal(@"C:\a.ideanest", sessionA.FilePath);
        Assert.Equal(@"C:\b.ideanest", sessionB.FilePath);
        Assert.NotEqual(sessionA.FilePath, sessionB.FilePath);
    }

    [Fact]
    public void IdeaNest_UpdatingSessionAFilePath_DoesNotAffectSessionB()
    {
        var sessionA = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel());
        var sessionB = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel());

        sessionA.FilePath = @"C:\updated.ideanest";

        Assert.Equal(@"C:\updated.ideanest", sessionA.FilePath);
        Assert.Null(sessionB.FilePath);
    }

    // ── IsModified 独立性確認 ─────────────────────────────────────────────

    [Fact]
    public void IdeaNest_UpdatingIsModifiedA_DoesNotAffectSessionB()
    {
        var sessionA = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel(), null, false);
        var sessionB = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel(), null, false);

        sessionA.IsModified = true;

        Assert.True(sessionA.IsModified);
        Assert.False(sessionB.IsModified);
    }

    // ── Session 削除確認 ──────────────────────────────────────────────────

    [Fact]
    public void IdeaNest_SessionManager_RemoveByTabId_DecreasesCount()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("tab-x", NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel()));
        Assert.Equal(1, mgr.Count);

        mgr.Remove("tab-x");

        Assert.Equal(0, mgr.Count);
    }

    [Fact]
    public void IdeaNest_SessionManager_Remove_ThenTryGet_ReturnsFalse()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("tab-x", NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel()));
        mgr.Remove("tab-x");

        Assert.False(mgr.TryGet("tab-x", out _));
    }

    [Fact]
    public void IdeaNest_TwoSessions_InManager_RemoveOne_OtherRemains()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        var vmA = new IdeaNestWorkspaceViewModel();
        var vmB = new IdeaNestWorkspaceViewModel();
        mgr.Add(new NestSuiteWorkspaceSession("tab-a", NestSuiteWorkspaceKind.IdeaNest, vmA));
        mgr.Add(new NestSuiteWorkspaceSession("tab-b", NestSuiteWorkspaceKind.IdeaNest, vmB));

        mgr.Remove("tab-a");

        Assert.Equal(1, mgr.Count);
        Assert.True(mgr.TryGet("tab-b", out var remaining));
        Assert.True(ReferenceEquals(remaining!.WorkspaceViewModel, vmB));
    }

    // ── NoteNest / ChatNest との混在確認 ─────────────────────────────────

    [Fact]
    public void SessionManager_FilterIdeaNestOnly_ExcludesOtherKinds()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("note", NestSuiteWorkspaceKind.NoteNest, new object()));
        mgr.Add(new NestSuiteWorkspaceSession("idea", NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("chat", NestSuiteWorkspaceKind.ChatNest, new object()));

        var ideaSessions = mgr.Sessions
            .Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.IdeaNest)
            .ToList();

        Assert.Single(ideaSessions);
        Assert.Equal("idea", ideaSessions[0].TabId);
    }

    [Fact]
    public void SessionManager_FilterIdeaNestOnly_WhenNoIdeaNest_ReturnsEmpty()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("note", NestSuiteWorkspaceKind.NoteNest, new object()));
        mgr.Add(new NestSuiteWorkspaceSession("chat", NestSuiteWorkspaceKind.ChatNest, new object()));

        var ideaSessions = mgr.Sessions
            .Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.IdeaNest)
            .ToList();

        Assert.Empty(ideaSessions);
    }

    // ── 二重オープン検出（IdeaNest 複数タブ対応での実用確認） ───────────────

    [Fact]
    public void IdeaNest_OpenFilePolicy_SamePath_DetectsAsDuplicate()
    {
        Assert.True(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\ideas\business.ideanest",
            @"C:\ideas\business.ideanest"));
    }

    [Fact]
    public void IdeaNest_OpenFilePolicy_DifferentPaths_NotDuplicate()
    {
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\ideas\a.ideanest",
            @"C:\ideas\b.ideanest"));
    }

    [Fact]
    public void IdeaNest_OpenFilePolicy_CaseInsensitive_IsDuplicate()
    {
        Assert.True(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\IDEAS\Business.ideanest",
            @"C:\ideas\business.ideanest"));
    }

    [Fact]
    public void IdeaNest_OpenFilePolicy_NullFilePath_NeverDuplicate()
    {
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(null, @"C:\ideas\business.ideanest"));
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(@"C:\ideas\business.ideanest", null));
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(null, null));
    }

    // ── Manager 経由の Session 独立性確認 ─────────────────────────────────

    [Fact]
    public void IdeaNest_SessionA_FilePathUpdate_DoesNotAffectSessionB_InManager()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        var vmA = new IdeaNestWorkspaceViewModel();
        var vmB = new IdeaNestWorkspaceViewModel();
        var sessionA = new NestSuiteWorkspaceSession("tab-a", NestSuiteWorkspaceKind.IdeaNest, vmA);
        var sessionB = new NestSuiteWorkspaceSession("tab-b", NestSuiteWorkspaceKind.IdeaNest, vmB);
        mgr.Add(sessionA);
        mgr.Add(sessionB);

        sessionA.FilePath = @"C:\a.ideanest";

        mgr.TryGet("tab-b", out var foundB);
        Assert.Null(foundB!.FilePath);
    }

    [Fact]
    public void IdeaNest_SessionA_IsModifiedUpdate_DoesNotAffectSessionB_InManager()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        var vmA = new IdeaNestWorkspaceViewModel();
        var vmB = new IdeaNestWorkspaceViewModel();
        var sessionA = new NestSuiteWorkspaceSession("tab-a", NestSuiteWorkspaceKind.IdeaNest, vmA, null, false);
        var sessionB = new NestSuiteWorkspaceSession("tab-b", NestSuiteWorkspaceKind.IdeaNest, vmB, null, false);
        mgr.Add(sessionA);
        mgr.Add(sessionB);

        sessionA.IsModified = true;

        mgr.TryGet("tab-b", out var foundB);
        Assert.False(foundB!.IsModified);
    }

    // ── WorkspaceKind 確認 ────────────────────────────────────────────────

    [Fact]
    public void IdeaNest_Session_WorkspaceKind_IsIdeaNest()
    {
        var session = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel());
        Assert.Equal(NestSuiteWorkspaceKind.IdeaNest, session.WorkspaceKind);
    }

    [Fact]
    public void IdeaNest_Session_WorkspaceViewModel_IsIdeaNestViewModelType()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        var session = new NestSuiteWorkspaceSession(
            Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.IdeaNest, vm);
        Assert.IsType<IdeaNestWorkspaceViewModel>(session.WorkspaceViewModel);
    }
}
