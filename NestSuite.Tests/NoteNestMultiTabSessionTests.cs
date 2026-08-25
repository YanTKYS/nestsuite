using System.Reflection;
using System.Windows.Threading;
using NestSuite;
using NestSuite.ChatNest;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// NoteNest 複数ファイルタブの Session 独立性テスト。
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

    // ── MainViewModel タイマー・イベント破棄確認 ─────────────────

    [Fact]
    public void MainViewModel_ImplementsIDisposable()
    {
        // DispatcherTimer リーク防止のため IDisposable を実装していることを確認
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(MainViewModel)));
    }

    [Fact]
    public void MainViewModel_Dispose_DoesNotThrow()
    {
        // Dispose() が例外なく呼び出せることを確認
        var vm = new MainViewModel();
        vm.Dispose();
    }

    [Fact]
    public void MainViewModel_Dispose_CanBeCalledTwice_WithoutError()
    {
        // 二重 Dispose を呼んでも例外が発生しないことを確認
        // （DispatcherTimer.Stop() は複数回呼んでも安全）
        var vm = new MainViewModel();
        vm.Dispose();
        vm.Dispose();
    }

    // ── TD-61: 未保存ステータスタイマー停止の実効確認 ──────────
    // 旧 NoteNest Classic 由来の自動保存タイマー（_autoSaveTimer）は撤去済み。
    // Dispose が停止すべき DispatcherTimer は未保存ステータス更新用の _unsavedTimer。
    // 現行の自動保存は NestSuiteShellWindow.AutoSave.cs（SH-33）が担う。

    private static DispatcherTimer GetUnsavedTimer(MainViewModel vm) =>
        (DispatcherTimer)typeof(MainViewModel)
            .GetField("_unsavedTimer", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(vm)!;

    [Fact]
    public void MainViewModel_UnsavedTimer_IsEnabled_WhenModified()
    {
        // IsModified になると _unsavedTimer が稼働することを確認
        // （Dispose が必要な根拠となるタイマーが実際に稼働し得ることの検証）
        using var vm = new MainViewModel();
        vm.IsModified = true;
        Assert.True(GetUnsavedTimer(vm).IsEnabled);
    }

    [Fact]
    public void MainViewModel_Dispose_StopsUnsavedTimer()
    {
        // Dispose() によって _unsavedTimer が停止されることを確認
        // NoteNest タブを閉じると対応 MainViewModel のタイマーが呼ばれなくなる
        var vm = new MainViewModel();
        vm.IsModified = true;
        Assert.True(GetUnsavedTimer(vm).IsEnabled);  // 破棄前は動作中

        vm.Dispose();

        Assert.False(GetUnsavedTimer(vm).IsEnabled); // 破棄後は停止
    }

    // ── タブ削除時の Session 削除確認 ────────────────────────────

    [Fact]
    public void NoteNest_SessionManager_RemoveByTabId_DecreasesCount()
    {
        // CloseTab が _sessionManager.Remove を呼ぶことで Session が削除されることを確認
        var mgr = new NestSuiteWorkspaceSessionManager();
        using var vm = new MainViewModel();
        mgr.Add(new NestSuiteWorkspaceSession("tab-x", NestSuiteWorkspaceKind.NoteNest, vm));
        Assert.Equal(1, mgr.Count);

        mgr.Remove("tab-x");

        Assert.Equal(0, mgr.Count);
    }

    [Fact]
    public void NoteNest_SessionManager_Remove_ThenTryGet_ReturnsFalse()
    {
        // Session 削除後は TryGet が false を返すことを確認
        // 閉じたタブの Session が残らないことの基盤検証
        var mgr = new NestSuiteWorkspaceSessionManager();
        using var vm = new MainViewModel();
        mgr.Add(new NestSuiteWorkspaceSession("tab-x", NestSuiteWorkspaceKind.NoteNest, vm));
        mgr.Remove("tab-x");

        Assert.False(mgr.TryGet("tab-x", out _));
    }

    [Fact]
    public void NoteNest_TwoSessions_InManager_RemoveOne_OtherRemains()
    {
        // 2 つの NoteNest Session がある場合、片方を削除してももう片方が残ることを確認
        var mgr = new NestSuiteWorkspaceSessionManager();
        using var vmA = new MainViewModel();
        using var vmB = new MainViewModel();
        mgr.Add(new NestSuiteWorkspaceSession("tab-a", NestSuiteWorkspaceKind.NoteNest, vmA));
        mgr.Add(new NestSuiteWorkspaceSession("tab-b", NestSuiteWorkspaceKind.NoteNest, vmB));

        mgr.Remove("tab-a");

        Assert.Equal(1, mgr.Count);
        Assert.True(mgr.TryGet("tab-b", out var remaining));
        Assert.True(ReferenceEquals(remaining!.WorkspaceViewModel, vmB));
    }

    // ── FilePath 保存独立性確認（タブA保存時にタブBが変わらない） ─

    [Fact]
    public void NoteNest_SessionA_FilePathUpdate_DoesNotAffectSessionB_InManager()
    {
        // タブA保存時（FilePath 更新）がタブBの Session に影響しないことを確認
        var mgr = new NestSuiteWorkspaceSessionManager();
        using var vmA = new MainViewModel();
        using var vmB = new MainViewModel();
        var sessionA = new NestSuiteWorkspaceSession("tab-a", NestSuiteWorkspaceKind.NoteNest, vmA);
        var sessionB = new NestSuiteWorkspaceSession("tab-b", NestSuiteWorkspaceKind.NoteNest, vmB);
        mgr.Add(sessionA);
        mgr.Add(sessionB);

        sessionA.FilePath = @"C:\a.notenest";

        mgr.TryGet("tab-b", out var foundB);
        Assert.Null(foundB!.FilePath);
    }

    [Fact]
    public void NoteNest_SessionA_IsModifiedUpdate_DoesNotAffectSessionB_InManager()
    {
        // タブAの IsModified 変更がタブBの Session に影響しないことを確認
        var mgr = new NestSuiteWorkspaceSessionManager();
        using var vmA = new MainViewModel();
        using var vmB = new MainViewModel();
        var sessionA = new NestSuiteWorkspaceSession("tab-a", NestSuiteWorkspaceKind.NoteNest, vmA, null, false);
        var sessionB = new NestSuiteWorkspaceSession("tab-b", NestSuiteWorkspaceKind.NoteNest, vmB, null, false);
        mgr.Add(sessionA);
        mgr.Add(sessionB);

        sessionA.IsModified = true;

        mgr.TryGet("tab-b", out var foundB);
        Assert.False(foundB!.IsModified);
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
