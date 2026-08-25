using System.Reflection;
using NestSuite;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// TD-63: NestSuiteShellWorkspaceLaunchTests から、ChatNest の Workspace 起動導線
/// （開く・保存・起動時読込・タブ閉じ確認・PropertyChanged ハンドラ・種別判定）に関する
/// リフレクションベースの静的存在確認テストを分割した。WPF ウィンドウは起動しない。
/// </summary>
public class NestSuiteShellChatNestLaunchTests
{
    // ── 起動時 .chatnest ファイル指定の最小対応 ─────────────────

    [Fact]
    public void NestSuiteShellWindow_HasLoadInitialChatNestFileMethod()
    {
        // LoadInitialChatNestFile が起動時 .chatnest 読込ヘルパーとして宣言されていることを確認
        // TD-59b-3: LoadInitialFile が probe 済みの WorkspaceFileOpenContext を渡すため、
        // string path 版から context 版へシグネチャが変わった。
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadInitialChatNestFile",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(WorkspaceFileOpenContext)],
                null);
        Assert.NotNull(method);
    }

    // ── ChatNest ファイル操作 ────────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_TrySaveChatNestToPath_TakesSessionParameter()
    {
        // TrySaveChatNestToPath が session + path を受け取るシグネチャに変わったことを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("TrySaveChatNestToPath",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteWorkspaceSession), typeof(string)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    // ── 回帰確認 ───────────────────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasUpdateChatNestTabPathMethod()
    {
        // UpdateChatNestTabPath が保存後のタブ表示更新ヘルパーとして宣言されていることを確認
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
        // OpenChatNestFile がファイルを開くメソッドとして宣言されていることを確認
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
        // OnChatNestPropertyChanged が PropertyChanged ハンドラとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("OnChatNestPropertyChanged",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasConfirmAndResetChatNestMethod()
    {
        // ConfirmAndResetChatNest がタブ閉じ確認メソッドとして宣言されていることを確認
        // タブ破棄では Clear() を呼ばず、PropertyChanged の購読解除のみ行う
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("ConfirmAndResetChatNest",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteDocumentTab)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    // ── NestSuite 共通「開く」導線の統合 ──────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasLoadChatNestFileAtMethod()
    {
        // LoadChatNestFileAt が OpenNestSuiteFile から呼ばれる ChatNest 読込ヘルパーとして宣言されていることを確認。
        // TD-59b-4: session 復元専用だった string path overload は撤去し、
        // WorkspaceFileOpenContext overload のみを残した。
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadChatNestFileAt",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(WorkspaceFileOpenContext)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteTabFactory_TryGetKind_ChatNestExtension_ReturnsChatNestKind()
    {
        // OpenNestSuiteFile が .chatnest を ChatNest として識別できることの確認
        Assert.True(NestSuiteTabFactory.TryGetKind("sample.chatnest", out var kind));
        Assert.Equal(NestSuiteWorkspaceKind.ChatNest, kind);
    }
}
