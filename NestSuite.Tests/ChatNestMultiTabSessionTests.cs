using NoteNest.NestSuite;
using NoteNest.NestSuite.ChatNest;
using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.9.2: ChatNest 複数ファイルタブ対応の最小実装を確認するテスト。
/// ChatNestWorkspaceViewModel がタブごとに独立していること、
/// Session 経由でタブ別の状態を管理できることを WPF なしで確認する。
/// </summary>
public class ChatNestMultiTabSessionTests
{
    // ── ViewModel 独立性 ────────────────────────────────────────────────────

    [Fact]
    public void TwoViewModels_Messages_AreIndependent()
    {
        var vmA = new ChatNestWorkspaceViewModel();
        var vmB = new ChatNestWorkspaceViewModel();

        vmA.Messages.Add(new Message { Speaker = Speaker.自分, Text = "A のメッセージ" });

        Assert.Single(vmA.Messages);
        Assert.Empty(vmB.Messages);
    }

    [Fact]
    public void TwoViewModels_InputText_IsIndependent()
    {
        var vmA = new ChatNestWorkspaceViewModel();
        var vmB = new ChatNestWorkspaceViewModel();

        vmA.InputText = "A の入力中テキスト";

        Assert.Equal("A の入力中テキスト", vmA.InputText);
        Assert.Equal(string.Empty, vmB.InputText);
    }

    [Fact]
    public void TwoViewModels_HasUnsavedChanges_IsIndependent()
    {
        var vmA = new ChatNestWorkspaceViewModel();
        var vmB = new ChatNestWorkspaceViewModel();

        vmA.InputText = "入力中";

        Assert.True(vmA.HasUnsavedChanges);
        Assert.False(vmB.HasUnsavedChanges);
    }

    [Fact]
    public void TwoViewModels_LoadMessages_DoNotCrossContaminate()
    {
        var vmA = new ChatNestWorkspaceViewModel();
        var vmB = new ChatNestWorkspaceViewModel();

        vmA.LoadMessages([new Message { Speaker = Speaker.自分, Text = "A" }]);
        vmB.LoadMessages([new Message { Speaker = Speaker.反論, Text = "B" }]);

        Assert.Single(vmA.Messages);
        Assert.Equal("A", vmA.Messages[0].Text);
        Assert.Single(vmB.Messages);
        Assert.Equal("B", vmB.Messages[0].Text);
    }

    [Fact]
    public void TwoViewModels_MarkSaved_IsIndependent()
    {
        var vmA = new ChatNestWorkspaceViewModel();
        var vmB = new ChatNestWorkspaceViewModel();

        vmA.LoadMessages([new Message { Speaker = Speaker.自分, Text = "A" }]);
        vmA.InputText = "入力中";

        vmB.LoadMessages([new Message { Speaker = Speaker.反論, Text = "B" }]);
        vmB.MarkSaved();

        Assert.True(vmA.HasUnsavedChanges);   // vmA は未保存のまま
        Assert.False(vmB.HasUnsavedChanges);  // vmB のみ保存済み（InputText も空）
    }

    // ── Session Manager での独立 ViewModel 管理 ────────────────────────────

    [Fact]
    public void TwoSessions_HoldDistinctViewModelInstances()
    {
        var vmA = new ChatNestWorkspaceViewModel();
        var vmB = new ChatNestWorkspaceViewModel();

        var tabIdA = Guid.NewGuid().ToString("N");
        var tabIdB = Guid.NewGuid().ToString("N");

        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession(tabIdA, NestSuiteWorkspaceKind.ChatNest, vmA));
        mgr.Add(new NestSuiteWorkspaceSession(tabIdB, NestSuiteWorkspaceKind.ChatNest, vmB));

        Assert.Equal(2, mgr.Count);
        Assert.True(mgr.TryGet(tabIdA, out var storedA));
        Assert.True(mgr.TryGet(tabIdB, out var storedB));
        Assert.NotSame(storedA!.WorkspaceViewModel, storedB!.WorkspaceViewModel);
    }

    [Fact]
    public void TwoSessions_ActivatingByTabId_ReturnsCorrectViewModel()
    {
        var vmA = new ChatNestWorkspaceViewModel();
        var vmB = new ChatNestWorkspaceViewModel();

        vmA.LoadMessages([new Message { Speaker = Speaker.自分, Text = "A のメッセージ" }]);
        vmB.LoadMessages([new Message { Speaker = Speaker.反論, Text = "B のメッセージ" }]);

        var tabIdA = Guid.NewGuid().ToString("N");
        var tabIdB = Guid.NewGuid().ToString("N");

        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession(tabIdA, NestSuiteWorkspaceKind.ChatNest, vmA));
        mgr.Add(new NestSuiteWorkspaceSession(tabIdB, NestSuiteWorkspaceKind.ChatNest, vmB));

        Assert.True(mgr.TryGet(tabIdA, out var sessionA));
        var activeVmA = (ChatNestWorkspaceViewModel)sessionA!.WorkspaceViewModel;
        Assert.Equal("A のメッセージ", activeVmA.Messages[0].Text);

        Assert.True(mgr.TryGet(tabIdB, out var sessionB));
        var activeVmB = (ChatNestWorkspaceViewModel)sessionB!.WorkspaceViewModel;
        Assert.Equal("B のメッセージ", activeVmB.Messages[0].Text);
    }

    [Fact]
    public void Session_Remove_RemovesOnlyTargetSession()
    {
        var tabIdA = Guid.NewGuid().ToString("N");
        var tabIdB = Guid.NewGuid().ToString("N");

        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession(tabIdA, NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession(tabIdB, NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel()));

        mgr.Remove(tabIdA);

        Assert.Equal(1, mgr.Count);
        Assert.False(mgr.Contains(tabIdA));
        Assert.True(mgr.Contains(tabIdB));
    }

    // ── 保存・読込でのファイルパス独立性 ───────────────────────────────────

    [Fact]
    public void SaveSessionA_DoesNotChangeSessionB_FilePath()
    {
        var tabIdA = Guid.NewGuid().ToString("N");
        var tabIdB = Guid.NewGuid().ToString("N");

        var sessionA = new NestSuiteWorkspaceSession(tabIdA, NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel(), null, false);
        var sessionB = new NestSuiteWorkspaceSession(tabIdB, NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel(), null, false);

        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(sessionA);
        mgr.Add(sessionB);

        // A を保存（FilePath を更新）
        sessionA.FilePath = @"C:\a.chatnest";

        Assert.Equal(@"C:\a.chatnest", sessionA.FilePath);
        Assert.Null(sessionB.FilePath);
    }

    [Fact]
    public void SaveSessionA_DoesNotChangeSessionB_IsModified()
    {
        var vmA = new ChatNestWorkspaceViewModel();
        var vmB = new ChatNestWorkspaceViewModel();

        vmA.LoadMessages([new Message { Speaker = Speaker.自分, Text = "A" }]);
        vmB.LoadMessages([new Message { Speaker = Speaker.反論, Text = "B" }]);

        var sessionA = new NestSuiteWorkspaceSession(Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.ChatNest, vmA, null, true);
        var sessionB = new NestSuiteWorkspaceSession(Guid.NewGuid().ToString("N"), NestSuiteWorkspaceKind.ChatNest, vmB, null, true);

        // A だけ保存済みに
        vmA.MarkSaved();
        sessionA.IsModified = false;

        Assert.False(sessionA.IsModified);
        Assert.True(sessionB.IsModified);
    }

    // ── 二重オープン検出（ChatNest 用途の確認） ──────────────────────────────

    [Fact]
    public void OpenFilePolicy_SameChatNestPath_IsDuplicate()
    {
        Assert.True(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\projects\meeting.chatnest",
            @"C:\projects\meeting.chatnest"));
    }

    [Fact]
    public void OpenFilePolicy_DifferentChatNestPaths_AreNotDuplicate()
    {
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\projects\a.chatnest",
            @"C:\projects\b.chatnest"));
    }

    [Fact]
    public void OpenFilePolicy_CaseInsensitive_ChatNestIsDuplicate()
    {
        Assert.True(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\PROJECTS\Meeting.chatnest",
            @"C:\projects\meeting.chatnest"));
    }

    // ── パス正規化後の二重オープン検出（v1.9.2 fix） ────────────────────────

    [Fact]
    public void OpenFilePolicy_AfterNormalization_RelativeAndAbsolute_AreSameFile()
    {
        // v1.9.2 fix: Shell は IsSameFile に渡す前に Path.GetFullPath() で正規化する。
        // 相対パス（起動引数）と絶対パス（ファイルダイアログ）が同じファイルを指す場合、
        // 正規化後の比較で同一ファイルと判定できることを確認する。
        var relPath = "sample.chatnest";
        var absPath = Path.GetFullPath(relPath);  // Shell が行う正規化と同等

        Assert.True(NestSuiteOpenFilePolicy.IsSameFile(
            Path.GetFullPath(relPath),
            absPath));
    }

    [Fact]
    public void OpenFilePolicy_BothNormalized_DifferentFiles_AreNotSame()
    {
        var pathA = Path.GetFullPath("a.chatnest");
        var pathB = Path.GetFullPath("b.chatnest");

        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(pathA, pathB));
    }

    // ── PropertyChanged 通知の独立性 ────────────────────────────────────────

    [Fact]
    public void ViewModelA_PropertyChanged_DoesNotFireOnViewModelB()
    {
        var vmA = new ChatNestWorkspaceViewModel();
        var vmB = new ChatNestWorkspaceViewModel();

        bool bFired = false;
        vmB.PropertyChanged += (_, _) => bFired = true;

        // vmA を変更してもvmBのイベントは発火しない
        vmA.InputText = "A の変更";

        Assert.False(bFired);
    }

    [Fact]
    public void TwoViewModels_EachFirePropertyChanged_Independently()
    {
        var vmA = new ChatNestWorkspaceViewModel();
        var vmB = new ChatNestWorkspaceViewModel();

        int aCount = 0;
        int bCount = 0;
        vmA.PropertyChanged += (_, _) => aCount++;
        vmB.PropertyChanged += (_, _) => bCount++;

        vmA.InputText = "A のみ変更";

        Assert.True(aCount > 0);
        Assert.Equal(0, bCount);
    }

    // ── v1.9.3 回帰確認テスト ─────────────────────────────────────────────

    // Session 逆引きパターン（ReferenceEquals）の確認
    [Fact]
    public void SessionReverseLoopup_ByReferenceEquals_FindsCorrectSession()
    {
        var vmA = new ChatNestWorkspaceViewModel();
        var vmB = new ChatNestWorkspaceViewModel();

        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("tab-a", NestSuiteWorkspaceKind.ChatNest, vmA));
        mgr.Add(new NestSuiteWorkspaceSession("tab-b", NestSuiteWorkspaceKind.ChatNest, vmB));

        // SyncChatNestTabForViewModel 内で行う逆引きと同等の操作
        var found = mgr.Sessions.FirstOrDefault(s => ReferenceEquals(s.WorkspaceViewModel, vmB));

        Assert.NotNull(found);
        Assert.Equal("tab-b", found!.TabId);
        Assert.Same(vmB, found.WorkspaceViewModel);
    }

    [Fact]
    public void SessionReverseLoopup_UnknownViewModel_ReturnsNull()
    {
        var vmA = new ChatNestWorkspaceViewModel();
        var vmUnregistered = new ChatNestWorkspaceViewModel();

        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("tab-a", NestSuiteWorkspaceKind.ChatNest, vmA));

        var found = mgr.Sessions.FirstOrDefault(s => ReferenceEquals(s.WorkspaceViewModel, vmUnregistered));

        Assert.Null(found);
    }

    // OnClosing での WorkspaceKind フィルタの確認
    [Fact]
    public void Sessions_FilterByChatNestKind_ExcludesOtherKinds()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("tab-note", NestSuiteWorkspaceKind.NoteNest, new object()));
        mgr.Add(new NestSuiteWorkspaceSession("tab-chat1", NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("tab-chat2", NestSuiteWorkspaceKind.ChatNest, new ChatNestWorkspaceViewModel()));
        mgr.Add(new NestSuiteWorkspaceSession("tab-idea", NestSuiteWorkspaceKind.IdeaNest, new object()));

        // OnClosing 内の foreach と同等のフィルタ
        var chatSessions = mgr.Sessions
            .Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest)
            .ToList();

        Assert.Equal(2, chatSessions.Count);
        Assert.All(chatSessions, s => Assert.Equal(NestSuiteWorkspaceKind.ChatNest, s.WorkspaceKind));
    }

    [Fact]
    public void Sessions_FilterByChatNestKind_WhenNoChatNestSessions_ReturnsEmpty()
    {
        var mgr = new NestSuiteWorkspaceSessionManager();
        mgr.Add(new NestSuiteWorkspaceSession("tab-note", NestSuiteWorkspaceKind.NoteNest, new object()));
        mgr.Add(new NestSuiteWorkspaceSession("tab-idea", NestSuiteWorkspaceKind.IdeaNest, new object()));

        var chatSessions = mgr.Sessions
            .Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest)
            .ToList();

        Assert.Empty(chatSessions);
    }

    // IsDirty と HasUnsavedChanges の独立性
    [Fact]
    public void TwoViewModels_IsDirty_IsIndependent()
    {
        var vmA = new ChatNestWorkspaceViewModel();
        var vmB = new ChatNestWorkspaceViewModel();

        vmA.LoadMessages([new Message { Speaker = Speaker.自分, Text = "A" }]);
        vmA.MarkSaved();

        // vmA は既存メッセージをロードしただけなので IsDirty=false
        Assert.False(vmA.HasUnsavedChanges);
        Assert.False(vmB.HasUnsavedChanges);

        // vmA に InputText を設定 → vmA のみ未保存
        vmA.InputText = "入力中";
        Assert.True(vmA.HasUnsavedChanges);
        Assert.False(vmB.HasUnsavedChanges);
    }

    [Fact]
    public void MarkSaved_ResetsHasUnsavedChanges_OnlyForThatViewModel()
    {
        var vmA = new ChatNestWorkspaceViewModel();
        var vmB = new ChatNestWorkspaceViewModel();

        vmA.LoadMessages([new Message { Speaker = Speaker.自分, Text = "A" }]);
        vmB.LoadMessages([new Message { Speaker = Speaker.反論, Text = "B" }]);

        // 両方ともメッセージを追加して dirty にする
        vmA.InputText = "入力A";
        vmB.InputText = "入力B";

        // vmA のみ MarkSaved（InputText は残るので HasUnsavedChanges=true のまま）
        vmA.MarkSaved();

        // MarkSaved 後も InputText が残れば HasUnsavedChanges=true のまま
        Assert.True(vmA.HasUnsavedChanges);
        Assert.True(vmB.HasUnsavedChanges);

        // vmA の InputText をクリアして完全に保存済み状態にする
        vmA.InputText = string.Empty;
        Assert.False(vmA.HasUnsavedChanges);
        Assert.True(vmB.HasUnsavedChanges);
    }

    // 二重オープン検出：FilePath null の扱い
    [Fact]
    public void OpenFilePolicy_NullPathA_IsNotSameAsAnyPath()
    {
        // null（無題タブ）は既存ファイルタブとは別物
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(null, @"C:\projects\meeting.chatnest"));
    }

    [Fact]
    public void OpenFilePolicy_BothNull_IsNotSameFile()
    {
        // null 同士も「同じファイル」とは判定しない（無題タブの二重作成防止は別のロジック）
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(null, null));
    }
}
