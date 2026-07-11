using System.Reflection;
using NestSuite;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.13 TD-63: NestSuiteShellWorkspaceLaunchTests から、NoteNest の Workspace 起動導線
/// （開く・保存・起動時読込・タブ閉じ確認・PropertyChanged ハンドラ・種別判定）に関する
/// リフレクションベースの静的存在確認テストを分割した。WPF ウィンドウは起動しない。
/// </summary>
public class NestSuiteShellNoteNestLaunchTests
{
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
        // v2.16.37 TD-59b-3: LoadInitialFile が probe 済みの WorkspaceFileOpenContext を渡すため、
        // string path 版から context 版へシグネチャが変わった。
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadInitialNoteNestFile",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(WorkspaceFileOpenContext)],
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

    // ── v1.10.1: NestSuite 共通「開く」導線の統合 ──────────────────────────

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
    public void NestSuiteTabFactory_TryGetKind_NoteNestExtension_ReturnsNoteNestKind()
    {
        // v1.10.1: OpenNestSuiteFile が .notenest を NoteNest として識別できることの確認
        Assert.True(NestSuiteTabFactory.TryGetKind("sample.notenest", out var kind));
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, kind);
    }
}
