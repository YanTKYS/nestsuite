using NoteNest.NestSuite;
using NoteNest.NestSuite.ChatNest;
using NoteNest.NestSuite.IdeaNest.ViewModels;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.9.8: 3ツール（NoteNest / ChatNest / IdeaNest）複数ファイルタブ対応後の回帰確認テスト。
/// 3ツールが SessionManager 上で独立して共存できること、相互干渉がないことを確認する。
/// WPF を起動せずに <see cref="NestSuiteWorkspaceSessionManager"/> と各 ViewModel を使って確認する。
/// </summary>
public class ThreeToolsMultiTabRegressionTests
{
    // ── SessionManager 混在共存 ─────────────────────────────────────────────

    [Fact]
    public void SessionManager_CanHoldAllThreeKindsSimultaneously()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("note", NestSuiteWorkspaceKind.NoteNest, new MainViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("chat", NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("idea", NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel()));

        Assert.Equal(3, mgr.Count);
    }

    [Fact]
    public void SessionManager_FilterByKind_EachKindHasExactlyOne()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("note", NestSuiteWorkspaceKind.NoteNest, new MainViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("chat", NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("idea", NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel()));

        Assert.Single(mgr.Sessions.Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest));
        Assert.Single(mgr.Sessions.Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest));
        Assert.Single(mgr.Sessions.Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.IdeaNest));
    }

    // ── Session 削除の独立性（ツール間） ─────────────────────────────────────

    [Fact]
    public void RemoveNoteNestSession_DoesNotAffectChatNestAndIdeaNestSessions()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("note", NestSuiteWorkspaceKind.NoteNest, new MainViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("chat", NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("idea", NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel()));

        mgr.Remove("note");

        Assert.Equal(2, mgr.Count);
        Assert.True(mgr.TryGet("chat", out _));
        Assert.True(mgr.TryGet("idea", out _));
    }

    [Fact]
    public void RemoveChatNestSession_DoesNotAffectNoteNestAndIdeaNestSessions()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("note", NestSuiteWorkspaceKind.NoteNest, new MainViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("chat", NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("idea", NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel()));

        mgr.Remove("chat");

        Assert.Equal(2, mgr.Count);
        Assert.True(mgr.TryGet("note", out _));
        Assert.True(mgr.TryGet("idea", out _));
    }

    [Fact]
    public void RemoveIdeaNestSession_DoesNotAffectNoteNestAndChatNestSessions()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("note", NestSuiteWorkspaceKind.NoteNest, new MainViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("chat", NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("idea", NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel()));

        mgr.Remove("idea");

        Assert.Equal(2, mgr.Count);
        Assert.True(mgr.TryGet("note", out _));
        Assert.True(mgr.TryGet("chat", out _));
    }

    // ── FilePath 独立性（ツール間） ─────────────────────────────────────────

    [Fact]
    public void FilePath_IsIndependent_AcrossAllThreeKinds()
    {
        var sessionNote = new NestSuiteWorkspaceSession("note", NestSuiteWorkspaceKind.NoteNest, new MainViewModel(), @"C:\doc.notenest", false);
        var sessionChat = new NestSuiteWorkspaceSession("chat", NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel(), @"C:\chat.chatnest", false);
        var sessionIdea = new NestSuiteWorkspaceSession("idea", NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel(), @"C:\idea.ideanest", false);

        sessionNote.FilePath = @"C:\updated.notenest";

        Assert.Equal(@"C:\updated.notenest", sessionNote.FilePath);
        Assert.Equal(@"C:\chat.chatnest", sessionChat.FilePath);
        Assert.Equal(@"C:\idea.ideanest", sessionIdea.FilePath);
    }

    // ── IsModified 独立性（ツール間） ───────────────────────────────────────

    [Fact]
    public void IsModified_IsIndependent_AcrossAllThreeKinds()
    {
        var sessionNote = new NestSuiteWorkspaceSession("note", NestSuiteWorkspaceKind.NoteNest, new MainViewModel(), null, false);
        var sessionChat = new NestSuiteWorkspaceSession("chat", NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel(), null, false);
        var sessionIdea = new NestSuiteWorkspaceSession("idea", NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel(), null, false);

        sessionNote.IsModified = true;

        Assert.True(sessionNote.IsModified);
        Assert.False(sessionChat.IsModified);
        Assert.False(sessionIdea.IsModified);
    }

    [Fact]
    public void IsModified_SetOnChatNest_DoesNotAffectNoteNestOrIdeaNest()
    {
        var sessionNote = new NestSuiteWorkspaceSession("note", NestSuiteWorkspaceKind.NoteNest, new MainViewModel(), null, false);
        var sessionChat = new NestSuiteWorkspaceSession("chat", NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel(), null, false);
        var sessionIdea = new NestSuiteWorkspaceSession("idea", NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel(), null, false);

        sessionChat.IsModified = true;

        Assert.False(sessionNote.IsModified);
        Assert.True(sessionChat.IsModified);
        Assert.False(sessionIdea.IsModified);
    }

    // ── ViewModel の型確認（ツールごとに正しい型） ──────────────────────────

    [Fact]
    public void WorkspaceViewModel_CorrectType_PerKind()
    {
        var sessionNote = new NestSuiteWorkspaceSession("note", NestSuiteWorkspaceKind.NoteNest, new MainViewModel());
        var sessionChat = new NestSuiteWorkspaceSession("chat", NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel());
        var sessionIdea = new NestSuiteWorkspaceSession("idea", NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel());

        Assert.IsType<MainViewModel>(sessionNote.WorkspaceViewModel);
        Assert.IsType<ChatNestWorkspaceViewModel>(sessionChat.WorkspaceViewModel);
        Assert.IsType<IdeaNestWorkspaceViewModel>(sessionIdea.WorkspaceViewModel);
    }

    [Fact]
    public void WorkspaceViewModels_AreNotShared_AcrossKinds()
    {
        var sessionNote = new NestSuiteWorkspaceSession("note", NestSuiteWorkspaceKind.NoteNest, new MainViewModel());
        var sessionChat = new NestSuiteWorkspaceSession("chat", NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel());
        var sessionIdea = new NestSuiteWorkspaceSession("idea", NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel());

        Assert.NotSame(sessionNote.WorkspaceViewModel, sessionChat.WorkspaceViewModel);
        Assert.NotSame(sessionChat.WorkspaceViewModel, sessionIdea.WorkspaceViewModel);
        Assert.NotSame(sessionNote.WorkspaceViewModel, sessionIdea.WorkspaceViewModel);
    }

    // ── 複数タブ混在（各ツール 2 タブずつ）────────────────────────────────

    [Fact]
    public void SessionManager_SixTabs_TwoPerTool_CountsCorrectly()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("note-1", NestSuiteWorkspaceKind.NoteNest, new MainViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("note-2", NestSuiteWorkspaceKind.NoteNest, new MainViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("chat-1", NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("chat-2", NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("idea-1", NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("idea-2", NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel()));

        Assert.Equal(6, mgr.Count);
        Assert.Equal(2, mgr.Sessions.Count(s => s.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest));
        Assert.Equal(2, mgr.Sessions.Count(s => s.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest));
        Assert.Equal(2, mgr.Sessions.Count(s => s.WorkspaceKind == NestSuiteWorkspaceKind.IdeaNest));
    }

    [Fact]
    public void SessionManager_SixTabs_RemoveAll_LeavesZero()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        var ids = new[] { "note-1", "note-2", "chat-1", "chat-2", "idea-1", "idea-2" };
        mgr.Add(new NestSuiteWorkspaceSession("note-1", NestSuiteWorkspaceKind.NoteNest, new MainViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("note-2", NestSuiteWorkspaceKind.NoteNest, new MainViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("chat-1", NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("chat-2", NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("idea-1", NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("idea-2", NestSuiteWorkspaceKind.IdeaNest, new IdeaNestWorkspaceViewModel()));

        foreach (var id in ids) mgr.Remove(id);

        Assert.Equal(0, mgr.Count);
    }

    // ── OpenFilePolicy: 重複判定はツール内でのみ行う ────────────────────────

    [Fact]
    public void OpenFilePolicy_SameFile_IsDuplicate_WithinSameKind()
    {
        Assert.True(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\project\notes.notenest",
            @"C:\project\notes.notenest"));
    }

    [Fact]
    public void OpenFilePolicy_NullPath_IsNeverDuplicate_AcrossAllTools()
    {
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(null, @"C:\notes.notenest"));
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(null, @"C:\chat.chatnest"));
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(null, @"C:\idea.ideanest"));
    }

    [Fact]
    public void OpenFilePolicy_CaseInsensitive_Works_AcrossAllExtensions()
    {
        Assert.True(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\NOTES\File.NoteNest",
            @"C:\notes\file.notenest"));
        Assert.True(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\CHAT\Chat.ChatNest",
            @"C:\chat\chat.chatnest"));
        Assert.True(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\IDEAS\Idea.IdeaNest",
            @"C:\ideas\idea.ideanest"));
    }
}
