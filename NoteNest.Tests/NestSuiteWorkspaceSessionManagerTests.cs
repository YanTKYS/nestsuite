using NoteNest.NestSuite;
using NoteNest.NestSuite.ChatNest;
using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.9.1: NestSuiteWorkspaceSessionManager の動作テスト。
///
/// <para>WPF ウィンドウを生成せずに Session 管理の正しさを確認する。
/// ViewModel の実インスタンスは不要なため <see cref="object"/> で代替する。</para>
/// </summary>
public class NestSuiteWorkspaceSessionManagerTests
{
    private static NestSuiteWorkspaceSession MakeSession(
        string tabId,
        NestSuiteWorkspaceKind kind = NestSuiteWorkspaceKind.NoteNest,
        string? filePath = null,
        bool isModified = false)
        => new(tabId, kind, new object(), filePath, isModified);

    // ── Session 追加 ─────────────────────────────────────────────────────

    [Fact]
    public void Add_Session_Count_Increases()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(MakeSession("tab-1"));
        Assert.Equal(1, mgr.Count);
    }

    [Fact]
    public void Add_DuplicateTabId_Overwrites()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        var first  = MakeSession("tab-x", filePath: @"C:\a.notenest");
        var second = MakeSession("tab-x", filePath: @"C:\b.notenest");
        mgr.Add(first);
        mgr.Add(second);
        // 上書きのため件数は変わらない
        Assert.Equal(1, mgr.Count);
        Assert.True(mgr.TryGet("tab-x", out var stored));
        Assert.Equal(@"C:\b.notenest", stored!.FilePath);
    }

    // ── Session 取得 ─────────────────────────────────────────────────────

    [Fact]
    public void TryGet_ExistingTabId_ReturnsSession()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        var session = MakeSession("tab-1", NestSuiteWorkspaceKind.ChatNest, @"C:\chat.chatnest");
        mgr.Add(session);

        Assert.True(mgr.TryGet("tab-1", out var result));
        Assert.NotNull(result);
        Assert.Equal(NestSuiteWorkspaceKind.ChatNest, result!.WorkspaceKind);
        Assert.Equal(@"C:\chat.chatnest", result.FilePath);
    }

    [Fact]
    public void TryGet_NonExistentTabId_ReturnsFalse()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        Assert.False(mgr.TryGet("no-such-tab", out var session));
        Assert.Null(session);
    }

    // ── Session 削除 ─────────────────────────────────────────────────────

    [Fact]
    public void Remove_ExistingTabId_ReturnsTrue_AndDecrementsCount()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(MakeSession("tab-1"));
        var removed = mgr.Remove("tab-1");
        Assert.True(removed);
        Assert.Equal(0, mgr.Count);
    }

    [Fact]
    public void Remove_NonExistentTabId_ReturnsFalse()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        Assert.False(mgr.Remove("no-such-tab"));
    }

    [Fact]
    public void After_Remove_TryGet_ReturnsFalse()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(MakeSession("tab-1"));
        mgr.Remove("tab-1");
        Assert.False(mgr.TryGet("tab-1", out _));
    }

    // ── Sessions 一覧 ────────────────────────────────────────────────────

    [Fact]
    public void Sessions_ReturnsAllAddedSessions()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(MakeSession("tab-1", NestSuiteWorkspaceKind.NoteNest));
        mgr.Add(MakeSession("tab-2", NestSuiteWorkspaceKind.ChatNest));
        mgr.Add(MakeSession("tab-3", NestSuiteWorkspaceKind.IdeaNest));

        Assert.Equal(3, mgr.Sessions.Count);
        Assert.Contains(mgr.Sessions, s => s.TabId == "tab-1" && s.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest);
        Assert.Contains(mgr.Sessions, s => s.TabId == "tab-2" && s.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest);
        Assert.Contains(mgr.Sessions, s => s.TabId == "tab-3" && s.WorkspaceKind == NestSuiteWorkspaceKind.IdeaNest);
    }

    [Fact]
    public void Sessions_Empty_ReturnsEmptyCollection()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        Assert.Empty(mgr.Sessions);
    }

    // ── Contains ─────────────────────────────────────────────────────────

    [Fact]
    public void Contains_ExistingTabId_ReturnsTrue()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(MakeSession("tab-1"));
        Assert.True(mgr.Contains("tab-1"));
    }

    [Fact]
    public void Contains_NonExistentTabId_ReturnsFalse()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        Assert.False(mgr.Contains("tab-x"));
    }

    // ── Session モデルの FilePath / IsModified 更新 ───────────────────────

    [Fact]
    public void Session_FilePath_CanBeUpdated()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        var session = MakeSession("tab-1", filePath: null);
        mgr.Add(session);

        // ReplaceTab が Session.FilePath を更新するのと同じ操作
        session.FilePath = @"C:\saved.notenest";
        Assert.True(mgr.TryGet("tab-1", out var stored));
        Assert.Equal(@"C:\saved.notenest", stored!.FilePath);
    }

    [Fact]
    public void Session_IsModified_CanBeUpdated()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        var session = MakeSession("tab-1", isModified: false);
        mgr.Add(session);

        session.IsModified = true;
        Assert.True(mgr.TryGet("tab-1", out var stored));
        Assert.True(stored!.IsModified);
    }

    // ── NestSuiteWorkspaceSession 基本プロパティ ─────────────────────────

    [Fact]
    public void Session_Properties_AreSetFromConstructor()
    {
        var vm = new object();
        var session = new NestSuiteWorkspaceSession(
            "tab-abc",
            NestSuiteWorkspaceKind.IdeaNest,
            vm,
            filePath: @"C:\ideas.ideanest",
            isModified: true);

        Assert.Equal("tab-abc", session.TabId);
        Assert.Equal(NestSuiteWorkspaceKind.IdeaNest, session.WorkspaceKind);
        Assert.Same(vm, session.WorkspaceViewModel);
        Assert.Equal(@"C:\ideas.ideanest", session.FilePath);
        Assert.True(session.IsModified);
    }

    [Fact]
    public void Session_DefaultFilePath_IsNull()
    {
        var session = new NestSuiteWorkspaceSession("tab-1", NestSuiteWorkspaceKind.NoteNest, new object());
        Assert.Null(session.FilePath);
        Assert.False(session.IsModified);
    }

    // ── タブ ID の一意性（複数 Session が同じ VM を参照できることの確認） ──

    [Fact]
    public void MultipleSessionsOfSameKind_CanShareSameViewModelReference()
    {
        // SessionManager 自体は VM の共有／独立を問わない。
        // v1.9.2: ChatNest は Shell がタブごとに独立 VM を生成する。NoteNest/IdeaNest は引き続き単一 VM。
        var sharedVm = new ChatNestWorkspaceViewModel();
        var s1 = new NestSuiteWorkspaceSession("tab-1", NestSuiteWorkspaceKind.ChatNest, sharedVm);
        var s2 = new NestSuiteWorkspaceSession("tab-2", NestSuiteWorkspaceKind.ChatNest, sharedVm);

        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(s1);
        mgr.Add(s2);

        Assert.Equal(2, mgr.Count);
        Assert.NotEqual(s1.TabId, s2.TabId);
        Assert.Same(s1.WorkspaceViewModel, s2.WorkspaceViewModel);
    }
}
