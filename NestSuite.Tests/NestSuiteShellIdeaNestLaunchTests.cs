using System.Reflection;
using NestSuite;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.13 TD-63: NestSuiteShellWorkspaceLaunchTests から、IdeaNest の Workspace 起動導線
/// （開く・保存・起動時読込・タブ閉じ確認・PropertyChanged ハンドラ・種別判定）に関する
/// リフレクションベースの静的存在確認テストを分割した。WPF ウィンドウは起動しない。
/// </summary>
public class NestSuiteShellIdeaNestLaunchTests
{
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

    // ── v1.10.1: NestSuite 共通「開く」導線の統合 ──────────────────────────

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
}
