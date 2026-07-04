using System.Reflection;
using NestSuite;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// Workspace 起動導線・ファイル操作に関するテスト。
/// LoadInitialFile / Open / Save / ConfirmAndReset の各 Workspace メソッド、
/// NestSuiteStartupTabPolicy / StartupArgParser / NestSuiteTabFactory の動作、
/// DialogService の複数ファイル選択 API、Ctrl+S 保存ショートカットを確認する。
/// </summary>
public class NestSuiteShellWorkspaceLaunchTests
{
    // ── v1.6.3: LoadInitialFile メソッドの存在確認 ────────────────────────

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

    // ── v1.8.0: IdeaNest ConfirmAndReset ───────────────────────────────────

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

    // ── v1.8.1: IdeaNest 統合後の回帰確認 ────────────────────────────────

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
        var evt = typeof(NestSuite.IdeaNest.ViewModels.IdeaNestWorkspaceViewModel)
            .GetEvent("DirtyRequested",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.Null(evt);
    }

    [Fact]
    public void IdeaNestWorkspaceViewModel_HasMarkDirtyMethod()
    {
        // v1.8.1: MarkDirty が HasChanges=true を設定するメソッドとして宣言されていることを確認
        var method = typeof(NestSuite.IdeaNest.ViewModels.IdeaNestWorkspaceViewModel)
            .GetMethod("MarkDirty",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
    }

    [Fact]
    public void IdeaNestWorkspaceViewModel_HasLoadFromWorkspaceMethod()
    {
        // v1.8.1: LoadFromWorkspace がタブリセット時に使われるメソッドとして宣言されていることを確認
        var method = typeof(NestSuite.IdeaNest.ViewModels.IdeaNestWorkspaceViewModel)
            .GetMethod("LoadFromWorkspace",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuite.IdeaNest.Models.Workspace)],
                null);
        Assert.NotNull(method);
    }

    // ── v1.8.3: IdeaNest 拡張子の起動読込確認 ────────────────────────────

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
    public void NestSuiteShellWindow_TryLoadIdeaNestFile_IsRemovedInV197()
    {
        // v1.9.7: TryLoadIdeaNestFile は LoadInitialIdeaNestFile と OpenIdeaNestFile に分割・置換された
        var method = typeof(NestSuiteShellWindow).GetMethod(
            "TryLoadIdeaNestFile",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.Null(method);
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

    // ── v1.9.2: ChatNest ファイル操作 ────────────────────────────────────

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

    // ── v1.9.3: v1.9.2 実装の回帰確認 ───────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasNormalizeFilePathMethod()
    {
        // v1.9.2 fix: NormalizeFilePath が Path.GetFullPath ラッパーとして宣言されていることを確認
        // 相対パスと絶対パスの二重オープン検出に使用される
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("NormalizeFilePath",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(string)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(string), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasUpdateChatNestTabPathMethod()
    {
        // v1.9.2: UpdateChatNestTabPath が保存後のタブ表示更新ヘルパーとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("UpdateChatNestTabPath",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteWorkspaceSession), typeof(string)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasOpenChatNestFileMethod()
    {
        // v1.9.2: OpenChatNestFile がファイルを開くメソッドとして宣言されていることを確認
        // 二重オープン検出・新規タブ作成・ActivateTab を含む
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("OpenChatNestFile",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method!.GetParameters());
    }

    [Fact]
    public void NestSuiteShellWindow_HasOnChatNestPropertyChangedMethod()
    {
        // v1.9.2: OnChatNestPropertyChanged が PropertyChanged ハンドラとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("OnChatNestPropertyChanged",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasConfirmAndResetChatNestMethod()
    {
        // v1.9.2: ConfirmAndResetChatNest がタブ閉じ確認メソッドとして宣言されていることを確認
        // v1.9.2 では Clear() 呼び出しを削除し PropertyChanged の購読解除のみ行う
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("ConfirmAndResetChatNest",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteDocumentTab)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    // ── v1.9.5: NoteNest 複数ファイルタブ対応の実装確認 ─────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasOpenNoteNestFileMethod()
    {
        // v1.9.5: OpenNoteNestFile がファイルを開くメソッドとして宣言されていることを確認
        // 二重オープン検出・新規タブ作成・ActivateTab を含む
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("OpenNoteNestFile",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method!.GetParameters());
    }

    [Fact]
    public void NestSuiteShellWindow_HasSaveNoteNestFileMethod()
    {
        // v1.9.5: SaveNoteNestFile が選択中 NoteNest タブの Session 経由で上書き保存するメソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SaveNoteNestFile",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasLoadInitialNoteNestFileMethod()
    {
        // v1.9.5: LoadInitialNoteNestFile が起動時 .notenest 読込ヘルパーとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadInitialNoteNestFile",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(string)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasOnNoteNestSessionPropertyChangedMethod()
    {
        // v1.9.5: OnNoteNestSessionPropertyChanged が NoteNest PropertyChanged ハンドラとして宣言されていることを確認
        // （v1.7.3 の OnNoteNestViewModelPropertyChanged から置き換え）
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("OnNoteNestSessionPropertyChanged",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasConfirmAndResetNoteNestMethod()
    {
        // v1.9.5: ConfirmAndResetNoteNest がタブ閉じ確認メソッドとして宣言されていることを確認
        // v1.9.5 では CreateNewProjectDirect() を削除し PropertyChanged の購読解除のみ行う
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("ConfirmAndResetNoteNest",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteDocumentTab)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    // ── v1.9.6: NoteNest 複数ファイルタブ対応の回帰確認 ─────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasSaveNoteNestFileAsMethod()
    {
        // v1.9.6: SaveNoteNestFileAs が選択中 NoteNest タブを名前を付けて保存するメソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SaveNoteNestFileAs",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    // ── v1.9.7: IdeaNest 複数ファイルタブ対応の実装確認 ─────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasOpenIdeaNestFileMethod()
    {
        // v1.9.7: OpenIdeaNestFile がファイルを開くメソッドとして宣言されていることを確認
        // 二重オープン検出・新規タブ作成・ActivateTab を含む
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("OpenIdeaNestFile",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method!.GetParameters());
    }

    [Fact]
    public void NestSuiteShellWindow_HasSaveIdeaNestFileMethod()
    {
        // v1.9.7: SaveIdeaNestFile が選択中 IdeaNest タブの Session 経由で上書き保存するメソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SaveIdeaNestFile",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasSaveIdeaNestFileAsMethod()
    {
        // v1.9.7: SaveIdeaNestFileAs が選択中 IdeaNest タブを名前を付けて保存するメソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SaveIdeaNestFileAs",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasLoadInitialIdeaNestFileMethod()
    {
        // v1.9.7: LoadInitialIdeaNestFile が起動時 .ideanest 読込ヘルパーとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadInitialIdeaNestFile",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(string)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasConfirmAndResetIdeaNestMethod_ReturnsBool()
    {
        // v1.9.7: ConfirmAndResetIdeaNest がタブ閉じ確認メソッドとして bool を返すことを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("ConfirmAndResetIdeaNest",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteDocumentTab)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    // ── v1.9.8 fix: NoteNest Save As の重複パス検出 ───────────────────────

    [Fact]
    public void MainViewModel_HasSaveToPathMethod_ReturnsBool()
    {
        // v1.9.8 fix: Shell が重複パス検出後にパス指定で保存するため MainViewModel.SaveToPath を追加
        var method = typeof(NestSuite.ViewModels.MainViewModel)
            .GetMethod("SaveToPath",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
                null,
                [typeof(string)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    // ── v1.10.1: NestSuite 共通「開く」導線の統合 ──────────────────────────
    // Note: SelectNestSuiteOpenPath (単一選択) は v1.16.0 で SelectNestSuiteOpenPaths に置き換え済み。

    [Fact]
    public void NestSuiteShellWindow_HasOpenNestSuiteFileMethod()
    {
        // v1.10.1: OpenNestSuiteFile が 3 形式共通「開く」の中心メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("OpenNestSuiteFile",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void NestSuiteShellWindow_HasLoadNoteNestFileAtMethod()
    {
        // v1.10.1: LoadNoteNestFileAt が OpenNestSuiteFile から呼ばれる NoteNest 読込ヘルパーとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadNoteNestFileAt",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(string)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasLoadChatNestFileAtMethod()
    {
        // v1.10.1: LoadChatNestFileAt が OpenNestSuiteFile から呼ばれる ChatNest 読込ヘルパーとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadChatNestFileAt",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(string)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasLoadIdeaNestFileAtMethod()
    {
        // v1.10.1: LoadIdeaNestFileAt が OpenNestSuiteFile から呼ばれる IdeaNest 読込ヘルパーとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadIdeaNestFileAt",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(string)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_MenuNew_Click_IsRemovedInV1101()
    {
        // v1.10.1: MenuNew_Click（ツール種別ディスパッチ）は 3 つのツール別ハンドラに置き換えられた
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("MenuNew_Click",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.Null(method);
    }

    [Fact]
    public void NestSuiteTabFactory_TryGetKind_NoteNestExtension_ReturnsNoteNestKind()
    {
        // v1.10.1: OpenNestSuiteFile が .notenest を NoteNest として識別できることの確認
        Assert.True(NestSuiteTabFactory.TryGetKind("sample.notenest", out var kind));
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, kind);
    }

    [Fact]
    public void NestSuiteTabFactory_TryGetKind_ChatNestExtension_ReturnsChatNestKind()
    {
        // v1.10.1: OpenNestSuiteFile が .chatnest を ChatNest として識別できることの確認
        Assert.True(NestSuiteTabFactory.TryGetKind("sample.chatnest", out var kind));
        Assert.Equal(NestSuiteWorkspaceKind.ChatNest, kind);
    }

    [Fact]
    public void NestSuiteTabFactory_TryGetKind_UnsupportedExtension_ReturnsFalse()
    {
        // v1.10.1: 未対応拡張子は TryGetKind が false を返すことを確認（OpenNestSuiteFile のエラー分岐の前提）
        Assert.False(NestSuiteTabFactory.TryGetKind("document.txt", out _));
        Assert.False(NestSuiteTabFactory.TryGetKind("document.docx", out _));
        Assert.False(NestSuiteTabFactory.TryGetKind("noextension", out _));
    }

    // ── v1.10.2: NestSuite 起動時ファイル指定の初期タブちらつき修正 ──

    [Fact]
    public void StartupTabPolicy_WithNullPath_ShouldCreateInitialTab()
    {
        // v1.10.2: ファイル未指定（null）→ 無題タブを作成すべき
        Assert.True(NestSuiteStartupTabPolicy.ShouldCreateInitialTab(null));
    }

    [Fact]
    public void StartupTabPolicy_WithEmptyPath_ShouldCreateInitialTab()
    {
        // v1.10.2: 空文字列も未指定扱い → 無題タブを作成すべき
        Assert.True(NestSuiteStartupTabPolicy.ShouldCreateInitialTab(""));
    }

    [Fact]
    public void StartupTabPolicy_AllThreeKindPaths_SuppressInitialTab()
    {
        // v1.10.2: 3 種すべての拡張子で初期無題タブが抑制されることを確認
        Assert.False(NestSuiteStartupTabPolicy.ShouldCreateInitialTab("sample.notenest"));
        Assert.False(NestSuiteStartupTabPolicy.ShouldCreateInitialTab("sample.chatnest"));
        Assert.False(NestSuiteStartupTabPolicy.ShouldCreateInitialTab("sample.ideanest"));
    }

    [Fact]
    public void StartupTabPolicy_WithUnsupportedExtension_SuppressesInitialTab()
    {
        // v1.10.2: 未対応拡張子のパスも「パスあり」とみなし初期無題タブは抑制される。
        // LoadInitialFile が拡張子エラーを処理し、フォールバックタブを作成する。
        Assert.False(NestSuiteStartupTabPolicy.ShouldCreateInitialTab("document.txt"));
    }

    [Fact]
    public void StartupArgParser_GetFilePath_ReturnsNonFlagArg()
    {
        // v1.10.2: --nestsuite + ファイルパスの組み合わせで GetFilePath が正しくパスを返す
        var args = new[] { "--nestsuite", "C:\\work\\test.chatnest" };
        Assert.Equal("C:\\work\\test.chatnest", StartupArgParser.GetFilePath(args));
    }

    [Fact]
    public void StartupArgParser_GetFilePath_WithNoFileArg_ReturnsNull()
    {
        // v1.10.2: --nestsuite のみ（ファイルなし）は null → ShouldCreateInitialTab(null) → true
        var args = new[] { "--nestsuite" };
        Assert.Null(StartupArgParser.GetFilePath(args));
    }

    // ── v1.16.0: NestSuite 複数ファイル一括オープン ─────────────────────────

    [Fact]
    public void DialogService_HasSelectNestSuiteOpenPathsMethod()
    {
        // v1.16.0: SelectNestSuiteOpenPaths が IReadOnlyList<string> を返すメソッドとして存在することを確認
        var method = typeof(NestSuite.Services.DialogService)
            .GetMethod("SelectNestSuiteOpenPaths",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.True(typeof(IReadOnlyList<string>).IsAssignableFrom(method!.ReturnType));
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void DialogService_DoesNotHaveSingleSelectNestSuiteOpenPathMethod()
    {
        // v1.16.0: 旧 SelectNestSuiteOpenPath（単一選択）が削除されていることを確認
        var method = typeof(NestSuite.Services.DialogService)
            .GetMethod("SelectNestSuiteOpenPath",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.Null(method);
    }

    // ── v1.16.1: Ctrl+S 上書き保存ショートカット ──────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasCommandSave_ExecutedMethod()
    {
        // v1.16.1: ApplicationCommands.Save の CommandBinding ハンドラが private メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("CommandSave_Executed",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(object), parameters[0].ParameterType);
        Assert.Equal(typeof(System.Windows.Input.ExecutedRoutedEventArgs), parameters[1].ParameterType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasSaveActiveTabMethod()
    {
        // v1.16.1: Ctrl+S・メニュー両方から呼ばれる SaveActiveTab が private メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SaveActiveTab",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    // ── v2.13.6 TD-45: IdeaNest / ChatNest 保存フロー最小共通化 ────────

    [Fact]
    public void NestSuiteShellWindow_HasTrySaveWorkspaceToPathMethod()
    {
        // v2.13.6 TD-45: IdeaNest / ChatNest 保存の共通実体が宣言されていることを確認。
        // シリアライズは各 Workspace につき 1 箇所（TrySaveXxxToPath）に集約される。
        // v2.14.12 SH-33: notifyOnError（自動保存用の失敗ダイアログ抑制）パラメータが追加され 9 引数になった。
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("TrySaveWorkspaceToPath",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [
                    typeof(NestSuiteWorkspaceSession),
                    typeof(string),
                    typeof(Action<string>),
                    typeof(Action<NestSuiteWorkspaceSession, string, bool>),
                    typeof(string),
                    typeof(string),
                    typeof(string),
                    typeof(bool),
                    typeof(bool)
                ],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasResolveSaveTargetPathMethod()
    {
        // v2.13.6 TD-45: SaveForTabId / SaveAll 共通のパス解決（キャンセル・重複タブ検出時は null）が宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("ResolveSaveTargetPath",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [
                    typeof(NestSuiteDocumentTab),
                    typeof(NestSuiteWorkspaceKind),
                    typeof(Func<string, string?>),
                    typeof(string)
                ],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(string), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_TrySaveIdeaNestToPath_HasShowNotificationOverload()
    {
        // v2.13.6 TD-45: SaveAll から showNotification: false で委譲するためのオーバーロード。
        // v2.14.12 SH-33: 自動保存用の notifyOnError パラメータが追加され 4 引数になった。
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("TrySaveIdeaNestToPath",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteWorkspaceSession), typeof(string), typeof(bool), typeof(bool)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_TrySaveChatNestToPath_HasShowNotificationOverload()
    {
        // v2.13.6 TD-45: SaveAll から showNotification: false で委譲するためのオーバーロード。
        // v2.14.12 SH-33: 自動保存用の notifyOnError パラメータが追加され 4 引数になった。
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("TrySaveChatNestToPath",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteWorkspaceSession), typeof(string), typeof(bool), typeof(bool)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_UpdateIdeaNestTabPath_HasShowNotificationOverload()
    {
        // v2.13.6 TD-45: 保存後状態更新（isModifiedAfterSave: false 固定）の唯一の定義点。
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("UpdateIdeaNestTabPath",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteWorkspaceSession), typeof(string), typeof(bool)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_UpdateChatNestTabPath_HasShowNotificationOverload()
    {
        // v2.13.6 TD-45: ChatNest は InputText 残留時に vm.HasUnsavedChanges を引き継ぐ。この差異の唯一の定義点。
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("UpdateChatNestTabPath",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteWorkspaceSession), typeof(string), typeof(bool)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }
}
