using System.Reflection;
using System.Windows.Controls;
using NestSuite;
using NestSuite.ChatNest;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// タブ管理・セッション管理に関するテスト。
/// NestSuiteShellWindow のタブストリップ・タブコレクション・セッションバインディングと、
/// NestSuiteWorkspaceSession / NestSuiteWorkspaceSessionManager の API、
/// 最近使ったファイルおよびセッション復元サービスを確認する。
/// </summary>
public class NestSuiteShellTabTests
{
    private static readonly BindingFlags AllInstance =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    // ── ファイル単位タブ UI 最小骨格の確認 ──────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasTabStripField()
    {
        // XAML x:Name="TabStrip" による ListBox フィールドの存在・型確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "TabStrip");
        Assert.NotNull(field);
        Assert.Equal(typeof(ListBox), field!.FieldType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasTabsCollectionField()
    {
        // _tabs フィールド（ObservableCollection<NestSuiteDocumentTab>）の存在・型確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "_tabs");
        Assert.NotNull(field);
        Assert.Equal(
            typeof(System.Collections.ObjectModel.ObservableCollection<NestSuiteDocumentTab>),
            field!.FieldType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasActivateTabMethod()
    {
        // ActivateTab がタブ切替の中心メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("ActivateTab",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteDocumentTab)],
                null);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasReplaceTabMethod()
    {
        // ReplaceTab がタブ置換ヘルパーとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("ReplaceTab",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteDocumentTab), typeof(NestSuiteDocumentTab)],
                null);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasSyncNoteNestTabForViewModelMethod()
    {
        // SyncNoteNestTabForViewModel が NoteNest タブ同期メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SyncNoteNestTabForViewModel",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    // ── タブを閉じる操作 ──────────────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasCloseTabMethod()
    {
        // CloseTab がタブ閉じ操作の中心メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("CloseTab",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteDocumentTab)],
                null);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasNoIsClosingTabField()
    {
        // _isClosingTab は一度も true にならない死コードであったため削除した。
        // ConfirmAndResetNoteNest が PropertyChanged 購読解除後に Dispose() を呼ぶため、ガード不要。
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "_isClosingTab");
        Assert.Null(field);
    }

    // ── IdeaNest タブ同期 ──────────────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasSyncIdeaNestTabForViewModelMethod()
    {
        // SyncIdeaNestTabForViewModel が IdeaNest 変更状態を対応タブへ反映するメソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SyncIdeaNestTabForViewModel",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    // ── WorkspaceSession / SessionManager 骨格の確認 ─────────────

    [Fact]
    public void NestSuiteShellWindow_HasSessionManagerField()
    {
        // _sessionManager フィールドが追加されていることを確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.FieldType == typeof(NestSuiteWorkspaceSessionManager));
        Assert.NotNull(field);
    }

    [Fact]
    public void NestSuiteShellWindow_HasCreateSessionForTabMethod()
    {
        // CreateSessionForTab がタブ→Session生成の中心メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("CreateSessionForTab",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteDocumentTab)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(NestSuiteWorkspaceSession), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasTryGetActiveSessionMethod()
    {
        // TryGetActiveSession が選択タブのSession取得ヘルパーとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("TryGetActiveSession",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteWorkspaceSession_HasTabIdProperty()
    {
        var prop = typeof(NestSuiteWorkspaceSession).GetProperty("TabId");
        Assert.NotNull(prop);
        Assert.Equal(typeof(string), prop!.PropertyType);
    }

    [Fact]
    public void NestSuiteWorkspaceSession_HasWorkspaceKindProperty()
    {
        var prop = typeof(NestSuiteWorkspaceSession).GetProperty("WorkspaceKind");
        Assert.NotNull(prop);
        Assert.Equal(typeof(NestSuiteWorkspaceKind), prop!.PropertyType);
    }

    [Fact]
    public void NestSuiteWorkspaceSession_HasMutableFilePathAndIsModified()
    {
        // FilePath と IsModified は ReplaceTab から更新されるため setter が必要
        var filePath   = typeof(NestSuiteWorkspaceSession).GetProperty("FilePath");
        var isModified = typeof(NestSuiteWorkspaceSession).GetProperty("IsModified");
        Assert.NotNull(filePath);
        Assert.NotNull(isModified);
        Assert.NotNull(filePath!.GetSetMethod());
        Assert.NotNull(isModified!.GetSetMethod());
    }

    [Fact]
    public void NestSuiteWorkspaceSessionManager_HasAddRemoveTryGetMethods()
    {
        var type = typeof(NestSuiteWorkspaceSessionManager);
        Assert.NotNull(type.GetMethod("Add",    BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(type.GetMethod("Remove", BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(type.GetMethod("TryGet", BindingFlags.Public | BindingFlags.Instance));
    }

    // ── ChatNest 複数ファイルタブ対応 ────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasCreateChatNestViewModelMethod()
    {
        // CreateChatNestViewModel がタブごとの独立 ViewModel 生成メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("CreateChatNestViewModel",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(ChatNestWorkspaceViewModel), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void NestSuiteShellWindow_HasSyncChatNestTabForViewModelMethod()
    {
        // SyncChatNestTabForViewModel が ChatNest タブ同期メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SyncChatNestTabForViewModel",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasNewChatNestSessionMethod_NoParameters()
    {
        // NewChatNestSession が新規 ChatNest タブ作成メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("NewChatNestSession",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    // ── NoteNest 複数ファイルタブ対応 ─────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasCreateNoteNestViewModelMethod()
    {
        // CreateNoteNestViewModel がタブごとの独立 MainViewModel 生成メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("CreateNoteNestViewModel",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(NestSuite.ViewModels.MainViewModel), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void NestSuiteShellWindow_HasNewNoteNestSessionMethod()
    {
        // NewNoteNestSession が新規 NoteNest タブ作成メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("NewNoteNestSession",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    // ── IdeaNest 複数ファイルタブ対応 ─────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasCreateIdeaNestViewModelMethod()
    {
        // CreateIdeaNestViewModel がタブごとの独立 IdeaNestWorkspaceViewModel 生成メソッドとして宣言されていることを確認
        // ChatNest の CreateChatNestViewModel / NoteNest の CreateNoteNestViewModel と対称な実装
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("CreateIdeaNestViewModel",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(
            typeof(NestSuite.IdeaNest.ViewModels.IdeaNestWorkspaceViewModel),
            method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void NestSuiteShellWindow_HasNewIdeaNestSessionMethod()
    {
        // NewIdeaNestSession が新規 IdeaNest タブ作成メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("NewIdeaNestSession",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    // ── NestSuite 最近使ったファイル ──────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasRecentFilesMenuField()
    {
        // XAML x:Name="RecentFilesMenu" による最近ファイルメニューフィールドの存在・型確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "RecentFilesMenu");
        Assert.NotNull(field);
        Assert.Equal(typeof(MenuItem), field!.FieldType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasUpdateRecentFilesMenuMethod()
    {
        // UpdateRecentFilesMenu が最近ファイルメニュー更新ヘルパーとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("UpdateRecentFilesMenu",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void NestSuiteShellWindow_HasRecentFilesServiceField()
    {
        // _recentFiles フィールド（NestSuiteRecentFilesService）が追加されていることを確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.FieldType == typeof(NestSuiteRecentFilesService));
        Assert.NotNull(field);
    }

    [Fact]
    public void NestSuiteRecentFilesService_DefaultDataPath_ContainsNestSuiteFileName()
    {
        // デフォルトパスが旧単体版 recent-files.json と別ファイルであることを確認
        // NestSuiteRecentFilesService と RecentFilesService のストレージが分離されている
        var svcField = typeof(NestSuiteRecentFilesService)
            .GetFields(BindingFlags.Static | BindingFlags.NonPublic)
            .FirstOrDefault(f => f.Name == "DefaultDataPath");
        Assert.NotNull(svcField);
        var path = (string?)svcField!.GetValue(null);
        Assert.NotNull(path);
        Assert.Contains("nestsuite-recent-files.json", path, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recent-files.json\"", path, StringComparison.OrdinalIgnoreCase);
    }

    // ── NestSuite タブ復元 ──────────────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasSessionStateServiceField()
    {
        // _sessionState フィールド（NestSuiteSessionStateService）が追加されていることを確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.FieldType == typeof(NestSuiteSessionStateService));
        Assert.NotNull(field);
    }

    [Fact]
    public void NestSuiteShellWindow_HasSaveSessionMethod()
    {
        // SaveSession が void・引数なしで宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SaveSession",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void NestSuiteShellWindow_HasTryRestoreSessionMethod()
    {
        // TryRestoreSession が bool・引数なしで宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("TryRestoreSession",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    // ── TD-66: タブ変更時の session 随時保存 ───────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasSaveSessionAfterTabChangeMethod()
    {
        // TD-66: タブ追加・閉鎖・ピン留め変更・並び替え後に呼ぶ helper が
        // void・引数なしで宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SaveSessionAfterTabChange",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void NestSuiteShellWindow_HasIsRestoringSessionField()
    {
        // TD-66: セッション復元中の随時保存抑止に使う bool フィールドが存在することを確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "_isRestoringSession");
        Assert.NotNull(field);
        Assert.Equal(typeof(bool), field!.FieldType);
    }

    [Fact]
    public void NestSuiteSessionStateService_DefaultDataPath_ContainsSessionFileName()
    {
        // デフォルトパスが nestsuite-session.json を含むことを確認
        // NestSuiteSessionStateService と NestSuiteRecentFilesService のストレージが分離されている
        var field = typeof(NestSuiteSessionStateService)
            .GetFields(BindingFlags.Static | BindingFlags.NonPublic)
            .FirstOrDefault(f => f.Name == "DefaultDataPath");
        Assert.NotNull(field);
        var path = (string?)field!.GetValue(null);
        Assert.NotNull(path);
        Assert.Contains("nestsuite-session.json", path, StringComparison.OrdinalIgnoreCase);
    }

    // ── SH-30: ステータスバーのアクティブタブ基準化 ────────────────

    [Fact]
    public void NestSuiteShellWindow_HasRefreshShellStatusBarMethod()
    {
        // RefreshShellStatusBar が _selectedTab 基準でファイル名・未保存表示を
        // 再計算するメソッドとして宣言されていることを確認。
        // Window.DataContext（NoteNest 固有）への直接 Binding を廃止した代替経路。
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("RefreshShellStatusBar",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void NestSuiteShellWindow_HasShellStatusFileTextField()
    {
        // XAML x:Name="ShellStatusFileText" によるファイル名表示 TextBlock フィールドの存在・型確認
        // AutomationId="Shell.StatusBar" はこの要素に付与されており UI Smoke の検出対象と一致する
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "ShellStatusFileText");
        Assert.NotNull(field);
        Assert.Equal(typeof(TextBlock), field!.FieldType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasShellStatusUnsavedTextField()
    {
        // XAML x:Name="ShellStatusUnsavedText" による未保存表示 TextBlock フィールドの存在・型確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "ShellStatusUnsavedText");
        Assert.NotNull(field);
        Assert.Equal(typeof(TextBlock), field!.FieldType);
    }

    // ── L8: バックアップ復元ガイドのクリックハンドラ ──

    [Fact]
    public void NestSuiteShellWindow_HasMenuBackupRestoreGuideClickHandler()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("MenuBackupRestoreGuide_Click",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(object), parameters[0].ParameterType);
    }

    // ── TD-59b-5: SyncNoteNestTabForViewModel の非読込化契約 ────────────
    // WPF Window を生成せず、メソッド範囲だけを対象にした狭い contract test。
    // production API を public 化しない既存方針に合わせる。

    [Fact]
    public void SyncNoteNestTabForViewModel_UsesPathCompatibilityCheck_NotTryGetKindOrFromFilePath()
    {
        var src = ReadSource("NestSuiteShellWindow.TabLifecycle.cs");
        var methodStart = src.IndexOf(
            "private void SyncNoteNestTabForViewModel(MainViewModel vm)", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var methodEnd = src.IndexOf(
            "private void SyncChatNestTabForViewModel(ChatNestWorkspaceViewModel vm)",
            methodStart, StringComparison.Ordinal);
        Assert.True(methodEnd > methodStart);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        Assert.Contains("IsPathCompatibleWithResolvedKind(", body);
        Assert.Contains("FromResolvedKind(", body);
        Assert.DoesNotContain("TryGetKind(", body);
        Assert.DoesNotContain("FromFilePath(", body);
        Assert.Contains("CreateUntitled(", body);
        Assert.Contains("ReplaceTab(", body);
    }

    private static string ReadSource(string fileName)
    {
        var path = Path.Combine(TestPaths.RepoRoot, "NestSuite", "NestSuite", fileName);
        Assert.True(File.Exists(path), $"{fileName} not found: {path}");
        return File.ReadAllText(path);
    }
}
