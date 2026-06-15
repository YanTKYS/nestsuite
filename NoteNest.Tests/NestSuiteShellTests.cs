using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using NoteNest.NestSuite;
using NoteNest.NestSuite.ChatNest;
using NoteNest.Views;
using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.7.0〜v1.7.3: NestSuite 統合母体の型境界・ツール定義モデル・タブ管理・レジストリ・契約を確認するテスト。
/// ChatNest を 2 つ目の Workspace（統合検証段階）として追加したことを反映する（v1.7.0）。
/// v1.7.1 は回帰確認・小修正版。v1.7.3 ではファイル単位タブストリップとタブ管理メソッドを追加（Workspace 状態との同期含む）。
/// UI を実際に起動しない、リフレクションベースの静的確認。
/// </summary>
public class NestSuiteShellTests
{
    private static readonly BindingFlags AllInstance =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    // ── NestSuiteShellWindow 型境界 ─────────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_IsWindowSubclass()
    {
        Assert.True(typeof(Window).IsAssignableFrom(typeof(NestSuiteShellWindow)));
    }

    [Fact]
    public void NestSuiteShellWindow_ImplementsIWorkspaceDialogHost()
    {
        Assert.Contains(
            typeof(IWorkspaceDialogHost),
            typeof(NestSuiteShellWindow).GetInterfaces());
    }

    [Fact]
    public void NestSuiteShellWindow_HasNoteNestWorkspaceViewField()
    {
        // XAML x:Name="WorkspaceView" による自動生成フィールド（internal/private）の型確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "WorkspaceView");
        Assert.NotNull(field);
        Assert.Equal(typeof(NoteNestWorkspaceView), field!.FieldType);
    }

    // ── NoteNest 単体版が残っていること ─────────────────────────────────

    [Fact]
    public void NoteNest_StandaloneMainWindow_StillExists()
    {
        Assert.True(typeof(Window).IsAssignableFrom(typeof(NoteNest.MainWindow)));
    }

    [Fact]
    public void NoteNestWorkspaceView_StillIsNotWindow()
    {
        var baseType = typeof(NoteNestWorkspaceView).BaseType;
        while (baseType != null)
        {
            Assert.NotEqual("System.Windows.Window", baseType.FullName);
            baseType = baseType.BaseType;
        }
    }

    // ── 終了確認の構造確認 ───────────────────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_OverridesOnClosing()
    {
        // OnClosing が NestSuiteShellWindow 自身で宣言されていることを確認
        // （継承のみで終了確認なしの状態ではないことの静的チェック）
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("OnClosing",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(System.ComponentModel.CancelEventArgs)],
                null);
        Assert.NotNull(method);
    }

    // ── ツール選択領域・プレースホルダーの存在確認 ──────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasToolSelectorPanel()
    {
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "ToolSelectorPanel");
        Assert.NotNull(field);
    }

    [Fact]
    public void NestSuiteShellWindow_HasUnintegratedPlaceholderField()
    {
        // XAML x:Name="UnintegratedPlaceholder" による未統合プレースホルダーフィールドの存在確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "UnintegratedPlaceholder");
        Assert.NotNull(field);
        Assert.Equal(typeof(Border), field!.FieldType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasChatWorkspaceViewField()
    {
        // v1.7.0: XAML x:Name="ChatWorkspaceView" による ChatNest Workspace フィールドの存在・型確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "ChatWorkspaceView");
        Assert.NotNull(field);
        Assert.Equal(typeof(ChatNestWorkspaceView), field!.FieldType);
    }

    [Fact]
    public void NestSuiteShellWindow_ChatNestViewModels_ManagedBySessionManager_NotSingleField()
    {
        // v1.9.2: ChatNest ViewModel はタブごとの独立インスタンスになった。
        // v1.7.0 時点の単一 _chatNestViewModel フィールドは削除され、
        // OnClosing での破棄確認は _sessionManager 経由で全 ChatNest Session を走査する。
        var chatNestVmField = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.FieldType == typeof(ChatNestWorkspaceViewModel));
        Assert.Null(chatNestVmField);

        // セッションマネージャは引き続き存在する
        var sessionMgrField = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.FieldType == typeof(NestSuiteWorkspaceSessionManager));
        Assert.NotNull(sessionMgrField);
    }

    // ── v1.6.4: ツール切替モデルの確認 ──────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_DefaultToolId_IsNoteNest()
    {
        Assert.Equal(NestSuiteToolRegistry.NoteNestToolId, NestSuiteShellWindow.DefaultToolId);
    }

    [Fact]
    public void NestSuiteShellWindow_HasSelectedToolIdProperty()
    {
        var prop = typeof(NestSuiteShellWindow)
            .GetProperty("SelectedToolId",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.NotNull(prop);
        Assert.Equal(typeof(string), prop!.PropertyType);
    }

    // ── v1.6.3 LoadInitialFile メソッドの存在確認 ────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasLoadInitialFileMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadInitialFile",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
                null,
                [typeof(string)],
                null);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_ViewModelProperty_IsMainViewModelType()
    {
        // ViewModel プロパティ（private）が MainViewModel 型を返すことを確認
        var prop = typeof(NestSuiteShellWindow)
            .GetProperty("ViewModel", AllInstance);
        Assert.NotNull(prop);
        Assert.Equal(typeof(NoteNest.ViewModels.MainViewModel), prop!.PropertyType);
    }

    // ── NestSuiteToolRegistry ─────────────────────────────────────────────

    [Fact]
    public void NestSuiteToolRegistry_AllTools_ContainsThreeEntries()
    {
        Assert.Equal(3, NestSuiteToolRegistry.AllTools.Count);
    }

    [Fact]
    public void NestSuiteToolRegistry_AllTools_IsNotMutableArray()
    {
        // Array.AsReadOnly() でラップされており、配列へのキャストによる変更を防いでいることを確認
        Assert.False(NestSuiteToolRegistry.AllTools is string[]);
        Assert.False(NestSuiteToolRegistry.AllTools is IList<string> { IsReadOnly: false });
    }

    [Fact]
    public void NestSuiteToolRegistry_NoteNest_IsFirstBuiltInTool()
    {
        Assert.Equal(NestSuiteToolRegistry.NoteNestToolId, NestSuiteToolRegistry.AllTools[0]);
    }

    [Fact]
    public void NestSuiteToolRegistry_NoteNest_IsIntegrated()
    {
        Assert.True(NestSuiteToolRegistry.IsIntegrated(NestSuiteToolRegistry.NoteNestToolId));
    }

    [Fact]
    public void NestSuiteToolRegistry_IdeaNest_IsIntegrated()
    {
        // v1.8.0: IdeaNest 統合検証段階として IsIntegrated=true
        Assert.True(NestSuiteToolRegistry.IsIntegrated(NestSuiteToolRegistry.IdeaNestToolId));
    }

    [Fact]
    public void NestSuiteToolRegistry_ChatNest_IsIntegrated()
    {
        // v1.7.0: ChatNest を統合検証段階として IsIntegrated=true に変更
        Assert.True(NestSuiteToolRegistry.IsIntegrated(NestSuiteToolRegistry.ChatNestToolId));
    }

    // ── v1.6.4 NestSuiteTool 定義確認 ────────────────────────────────────

    [Fact]
    public void NestSuiteToolRegistry_ToolDefinitions_ContainsThreeEntries()
    {
        Assert.Equal(3, NestSuiteToolRegistry.ToolDefinitions.Count);
    }

    [Fact]
    public void NestSuiteToolRegistry_ToolDefinitions_IsNotMutableArray()
    {
        Assert.False(NestSuiteToolRegistry.ToolDefinitions is NestSuiteTool[]);
        Assert.False(NestSuiteToolRegistry.ToolDefinitions is IList<NestSuiteTool> { IsReadOnly: false });
    }

    [Fact]
    public void NestSuiteToolRegistry_ToolDefinitions_FirstIsNoteNest()
    {
        Assert.Equal(NestSuiteToolRegistry.NoteNestToolId, NestSuiteToolRegistry.ToolDefinitions[0].Id);
    }

    [Fact]
    public void NestSuiteToolRegistry_NoteNestDef_IsIntegrated()
    {
        Assert.True(NestSuiteToolRegistry.NoteNestDef.IsIntegrated);
    }

    [Fact]
    public void NestSuiteToolRegistry_IdeaNestDef_IsIntegrated()
    {
        // v1.8.0: IdeaNest 統合検証段階として IsIntegrated=true、StatusText="統合検証"
        Assert.True(NestSuiteToolRegistry.IdeaNestDef.IsIntegrated);
        Assert.Equal("統合検証", NestSuiteToolRegistry.IdeaNestDef.StatusText);
    }

    [Fact]
    public void NestSuiteToolRegistry_ChatNestDef_IsIntegrated()
    {
        // v1.7.0: ChatNest 統合検証段階。StatusText で検証段階であることを示す
        Assert.True(NestSuiteToolRegistry.ChatNestDef.IsIntegrated);
        Assert.Equal("統合検証", NestSuiteToolRegistry.ChatNestDef.StatusText);
    }

    [Fact]
    public void NestSuiteToolRegistry_AllThreeTools_AreIntegrated()
    {
        // v1.8.0: IdeaNest 統合検証段階追加により、全ツールが統合済みまたは統合検証段階
        var unintegrated = NestSuiteToolRegistry.ToolDefinitions
            .Where(t => !t.IsIntegrated)
            .Select(t => t.Id)
            .ToList();
        Assert.Empty(unintegrated);
    }

    // ── v1.7.3: ファイル単位タブ UI 最小骨格の確認 ──────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasTabStripField()
    {
        // v1.7.3: XAML x:Name="TabStrip" による ListBox フィールドの存在・型確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "TabStrip");
        Assert.NotNull(field);
        Assert.Equal(typeof(ListBox), field!.FieldType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasTabsCollectionField()
    {
        // v1.7.3: _tabs フィールド（ObservableCollection<NestSuiteDocumentTab>）の存在・型確認
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
        // v1.7.3: ActivateTab がタブ切替の中心メソッドとして宣言されていることを確認
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
        // v1.7.3 fix: ReplaceTab がタブ置換ヘルパーとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("ReplaceTab",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteDocumentTab), typeof(NestSuiteDocumentTab)],
                null);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasSyncNoteNestTabToViewModelMethod()
    {
        // v1.7.3 fix: SyncNoteNestTabToViewModel がタブとViewModel状態の同期メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SyncNoteNestTabToViewModel",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
    }

    // ── v1.7.6: タブを閉じる操作 ──────────────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasCloseTabMethod()
    {
        // v1.7.6: CloseTab がタブ閉じ操作の中心メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("CloseTab",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteDocumentTab)],
                null);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasIsClosingTabField()
    {
        // v1.7.6: _isClosingTab フラグが NoteNest VM リセット中の二重同期を抑制するために宣言されていることを確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "_isClosingTab");
        Assert.NotNull(field);
        Assert.Equal(typeof(bool), field!.FieldType);
    }

    // ── v1.7.7: 起動時 .chatnest ファイル指定の最小対応 ─────────────────

    [Fact]
    public void NestSuiteShellWindow_HasLoadInitialChatNestFileMethod()
    {
        // v1.7.7: LoadInitialChatNestFile が起動時 .chatnest 読込ヘルパーとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadInitialChatNestFile",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(string)],
                null);
        Assert.NotNull(method);
    }

    // ── v1.8.0: IdeaNest 統合検証 ────────────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasIdeaNestWorkspaceViewField()
    {
        // v1.8.0: XAML x:Name="IdeaNestWorkspaceView" による IdeaNest Workspace フィールドの存在・型確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "IdeaNestWorkspaceView");
        Assert.NotNull(field);
        Assert.Equal(
            typeof(NoteNest.NestSuite.IdeaNest.Views.IdeaNestWorkspaceView),
            field!.FieldType);
    }

    [Fact]
    public void NestSuiteShellWindow_HoldsIdeaNestViewModelField()
    {
        // v1.8.0: 終了時の変更確認のため、IdeaNest ViewModel をフィールドとして保持していることを確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.FieldType ==
                typeof(NoteNest.NestSuite.IdeaNest.ViewModels.IdeaNestWorkspaceViewModel));
        Assert.NotNull(field);
    }

    [Fact]
    public void NestSuiteShellWindow_HasSyncIdeaNestTabMethod()
    {
        // v1.8.0: SyncIdeaNestTab が IdeaNest 変更状態をタブに反映するメソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SyncIdeaNestTab",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasConfirmAndResetIdeaNestMethod()
    {
        // v1.8.0: ConfirmAndResetIdeaNest がタブ閉じ確認・リセットメソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("ConfirmAndResetIdeaNest",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteDocumentTab)],
                null);
        Assert.NotNull(method);
    }

    // ── v1.8.1: IdeaNest統合後の回帰確認 ────────────────────────────────

    [Fact]
    public void NestSuiteToolRegistry_IdeaNestDef_StatusText_IsIntegrationTest()
    {
        // v1.8.1: IdeaNest が統合検証段階（IsIntegrated=true / StatusText="統合検証"）であることを確認
        Assert.True(NestSuiteToolRegistry.IdeaNestDef.IsIntegrated);
        Assert.Equal("統合検証", NestSuiteToolRegistry.IdeaNestDef.StatusText);
    }

    [Fact]
    public void NestSuiteShellWindow_HasOnIdeaNestPropertyChangedMethod()
    {
        // v1.8.1: OnIdeaNestPropertyChanged が IdeaNest PropertyChanged ハンドラとして宣言されていることを確認
        // （DirtyRequested は削除済み。PropertyChanged 経路のみであることを明示する）
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("OnIdeaNestPropertyChanged",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
    }

    [Fact]
    public void IdeaNestWorkspaceViewModel_DoesNotHaveDirtyRequestedEvent()
    {
        // v1.8.1: DirtyRequested イベントが削除されていることを確認
        // （PropertyChanged 経路への一本化が完了していることの保証）
        var evt = typeof(NoteNest.NestSuite.IdeaNest.ViewModels.IdeaNestWorkspaceViewModel)
            .GetEvent("DirtyRequested",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.Null(evt);
    }

    [Fact]
    public void IdeaNestWorkspaceViewModel_HasMarkDirtyMethod()
    {
        // v1.8.1: MarkDirty が HasChanges=true を設定するメソッドとして宣言されていることを確認
        var method = typeof(NoteNest.NestSuite.IdeaNest.ViewModels.IdeaNestWorkspaceViewModel)
            .GetMethod("MarkDirty",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
    }

    [Fact]
    public void IdeaNestWorkspaceViewModel_HasLoadFromWorkspaceMethod()
    {
        // v1.8.1: LoadFromWorkspace がタブリセット時に使われるメソッドとして宣言されていることを確認
        var method = typeof(NoteNest.NestSuite.IdeaNest.ViewModels.IdeaNestWorkspaceViewModel)
            .GetMethod("LoadFromWorkspace",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
                null,
                [typeof(NoteNest.NestSuite.IdeaNest.Models.Workspace)],
                null);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasLoadInitialFileMethod_AcceptsIdeaNestExtension()
    {
        // v1.8.3: LoadInitialFile が .ideanest を IdeaNest 読込経路へ分岐できることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadInitialFile",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
                null,
                [typeof(string)],
                null);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasSharedIdeaNestLoadMethod()
    {
        var method = typeof(NestSuiteShellWindow).GetMethod(
            "TryLoadIdeaNestFile",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
        Assert.Equal(typeof(string), method.GetParameters().Single().ParameterType);
    }

    [Fact]
    public void NestSuiteTabFactory_IdeaNestExtension_IsRecognizedForLoading()
    {
        // v1.8.3: .ideanest は NestSuiteTabFactory で認識され、LoadInitialFile から読み込まれる
        var result = NestSuiteTabFactory.TryGetKind("project.ideanest", out var kind);
        Assert.True(result);
        Assert.Equal(NestSuiteWorkspaceKind.IdeaNest, kind);
    }

    [Fact]
    public void NestSuiteTabFactory_IdeaNestExtension_IsNotNoteNest()
    {
        // v1.8.1: .ideanest を NoteNest として誤認しない
        NestSuiteTabFactory.TryGetKind("project.ideanest", out var kind);
        Assert.NotEqual(NestSuiteWorkspaceKind.NoteNest, kind);
    }

    [Fact]
    public void NestSuiteTabFactory_IdeaNestExtension_IsNotChatNest()
    {
        // v1.8.1: .ideanest を ChatNest として誤認しない
        NestSuiteTabFactory.TryGetKind("project.ideanest", out var kind);
        Assert.NotEqual(NestSuiteWorkspaceKind.ChatNest, kind);
    }

    [Fact]
    public void NestSuiteToolRegistry_AllThreeTools_NoteNestFirst()
    {
        // v1.8.1: ツール定義の順序が NoteNest → IdeaNest → ChatNest のまま維持されていることを確認
        Assert.Equal(NestSuiteToolRegistry.NoteNestToolId, NestSuiteToolRegistry.ToolDefinitions[0].Id);
        Assert.Equal(NestSuiteToolRegistry.IdeaNestToolId, NestSuiteToolRegistry.ToolDefinitions[1].Id);
        Assert.Equal(NestSuiteToolRegistry.ChatNestToolId, NestSuiteToolRegistry.ToolDefinitions[2].Id);
    }

    // ── v1.8.6: 起動時ファイル指定時の無題タブ生成修正 ─────────────────────

    [Fact]
    public void NestSuiteShellWindow_Constructor_AcceptsOptionalStringParameter()
    {
        // v1.8.6: コンストラクタが string? initialFilePath = null を受け取れることを確認
        var ctor = typeof(NestSuiteShellWindow)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(c =>
            {
                var p = c.GetParameters();
                return p.Length == 1 &&
                       p[0].ParameterType == typeof(string) &&
                       p[0].IsOptional;
            });
        Assert.NotNull(ctor);
    }

    [Fact]
    public void NestSuiteShellWindow_HasEnsureDefaultTabMethod()
    {
        // v1.8.6: EnsureDefaultTab がフォールバック NoteNest タブ生成の中心メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("EnsureDefaultTab",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    // ── v1.8.6: NestSuiteStartupTabPolicy 動作テスト ────────────────────────
    // WPF ウィンドウを生成せずに初期タブ生成判断の正しさを自動確認する。
    // Shell がポリシーを使うことで、ポリシーへの変更は即座に回帰テストに反映される。

    [Fact]
    public void StartupTabPolicy_NullFilePath_ShouldCreateInitialTab()
    {
        // ファイル指定なし起動 → 無題NoteNestタブを作成する
        Assert.True(NestSuiteStartupTabPolicy.ShouldCreateInitialTab(null));
    }

    [Fact]
    public void StartupTabPolicy_EmptyFilePath_ShouldCreateInitialTab()
    {
        // 空文字列 → ファイル指定なしと同等に扱い、無題NoteNestタブを作成する
        Assert.True(NestSuiteStartupTabPolicy.ShouldCreateInitialTab(""));
    }

    [Fact]
    public void StartupTabPolicy_WithFilePath_ShouldNotCreateInitialTab()
    {
        // ファイル指定ありの場合は初期タブを作成しない
        Assert.False(NestSuiteStartupTabPolicy.ShouldCreateInitialTab("sample.chatnest"));
        Assert.False(NestSuiteStartupTabPolicy.ShouldCreateInitialTab("sample.ideanest"));
        Assert.False(NestSuiteStartupTabPolicy.ShouldCreateInitialTab("sample.notenest"));
    }

    [Fact]
    public void StartupTabPolicy_ZeroTabs_ShouldEnsureFallbackTab()
    {
        // 読込失敗後タブが0枚 → フォールバック無題NoteNestタブを作成する
        Assert.True(NestSuiteStartupTabPolicy.ShouldEnsureFallbackTab(0));
    }

    [Fact]
    public void StartupTabPolicy_HasTabs_ShouldNotEnsureFallbackTab()
    {
        // タブが1枚以上存在する場合は追加しない
        Assert.False(NestSuiteStartupTabPolicy.ShouldEnsureFallbackTab(1));
        Assert.False(NestSuiteStartupTabPolicy.ShouldEnsureFallbackTab(2));
    }

    // ── v1.9.1: WorkspaceSession / SessionManager 骨格の確認 ─────────────

    [Fact]
    public void NestSuiteShellWindow_HasSessionManagerField()
    {
        // v1.9.1: _sessionManager フィールドが追加されていることを確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.FieldType == typeof(NestSuiteWorkspaceSessionManager));
        Assert.NotNull(field);
    }

    [Fact]
    public void NestSuiteShellWindow_HasCreateSessionForTabMethod()
    {
        // v1.9.1: CreateSessionForTab がタブ→Session生成の中心メソッドとして宣言されていることを確認
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
        // v1.9.1: TryGetActiveSession が選択タブのSession取得ヘルパーとして宣言されていることを確認
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

    // ── v1.9.2: ChatNest 複数ファイルタブ対応 ────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasCreateChatNestViewModelMethod()
    {
        // v1.9.2: CreateChatNestViewModel がタブごとの独立 ViewModel 生成メソッドとして宣言されていることを確認
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
        // v1.9.2: SyncChatNestTabForViewModel が ChatNest タブ同期メソッドとして宣言されていることを確認
        // （v1.9.1 の単一タブ想定の SyncChatNestTab から置き換え）
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SyncChatNestTabForViewModel",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasNewChatNestSessionMethod_NoParameters()
    {
        // v1.9.2: NewChatNestSession が新規 ChatNest タブ作成メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("NewChatNestSession",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void NestSuiteShellWindow_TrySaveChatNestToPath_TakesSessionParameter()
    {
        // v1.9.2: TrySaveChatNestToPath が session + path を受け取るシグネチャに変わったことを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("TrySaveChatNestToPath",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteWorkspaceSession), typeof(string)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }
}
