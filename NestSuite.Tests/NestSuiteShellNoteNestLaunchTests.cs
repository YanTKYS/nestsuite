using System.Reflection;
using NestSuite;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// TD-63: NestSuiteShellWorkspaceLaunchTests から、NoteNest の Workspace 起動導線
/// （開く・保存・起動時読込・タブ閉じ確認・PropertyChanged ハンドラ・種別判定）に関する
/// リフレクションベースの静的存在確認テストを分割した。WPF ウィンドウは起動しない。
/// </summary>
public class NestSuiteShellNoteNestLaunchTests
{
    // ── NoteNest 複数ファイルタブ対応の実装確認 ─────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasOpenNoteNestFileMethod()
    {
        // OpenNoteNestFile がファイルを開くメソッドとして宣言されていることを確認
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
        // SaveNoteNestFile が選択中 NoteNest タブの Session 経由で上書き保存するメソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SaveNoteNestFile",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasLoadInitialNoteNestFileMethod()
    {
        // LoadInitialNoteNestFile が起動時 .notenest 読込ヘルパーとして宣言されていることを確認
        // TD-59b-3: LoadInitialFile が probe 済みの WorkspaceFileOpenContext を渡すため、
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
        // OnNoteNestSessionPropertyChanged が NoteNest PropertyChanged ハンドラとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("OnNoteNestSessionPropertyChanged",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasConfirmAndResetNoteNestMethod()
    {
        // ConfirmAndResetNoteNest がタブ閉じ確認メソッドとして宣言されていることを確認
        // タブ破棄では新規プロジェクト生成を行わず、PropertyChanged の購読解除のみ行う
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("ConfirmAndResetNoteNest",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteDocumentTab)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    // ── NoteNest 複数ファイルタブ対応の回帰確認 ─────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasSaveNoteNestFileAsMethod()
    {
        // SaveNoteNestFileAs が選択中 NoteNest タブを名前を付けて保存するメソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SaveNoteNestFileAs",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    // ── NestSuite 共通「開く」導線の統合 ──────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasLoadNoteNestFileAtMethod()
    {
        // LoadNoteNestFileAt が OpenNestSuiteFile から呼ばれる NoteNest 読込ヘルパーとして宣言されていることを確認。
        // TD-59b-4: session 復元専用だった string path overload は撤去し、
        // WorkspaceFileOpenContext overload のみを残した。
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadNoteNestFileAt",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(WorkspaceFileOpenContext)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteTabFactory_TryGetKind_NoteNestExtension_ReturnsNoteNestKind()
    {
        // OpenNestSuiteFile が .notenest を NoteNest として識別できることの確認
        Assert.True(NestSuiteTabFactory.TryGetKind("sample.notenest", out var kind));
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, kind);
    }
}
