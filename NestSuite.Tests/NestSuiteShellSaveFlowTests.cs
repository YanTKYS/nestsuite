using System.Reflection;
using NestSuite;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.13 TD-63: NestSuiteShellWorkspaceLaunchTests から、Ctrl+S 保存ショートカット
/// （v1.16.1）と IdeaNest / ChatNest 保存フロー共通化（v2.13.6 TD-45）に関するテストを
/// 分割した。WPF ウィンドウは起動しない。
/// </summary>
public class NestSuiteShellSaveFlowTests
{
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
        // v2.16.6 TD-64: 自動保存用の createBackup パラメータが追加され 5 引数になった。
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("TrySaveIdeaNestToPath",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteWorkspaceSession), typeof(string), typeof(bool), typeof(bool), typeof(bool)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_TrySaveChatNestToPath_HasShowNotificationOverload()
    {
        // v2.13.6 TD-45: SaveAll から showNotification: false で委譲するためのオーバーロード。
        // v2.14.12 SH-33: 自動保存用の notifyOnError パラメータが追加され 4 引数になった。
        // v2.16.6 TD-64: 自動保存用の createBackup パラメータが追加され 5 引数になった。
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("TrySaveChatNestToPath",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteWorkspaceSession), typeof(string), typeof(bool), typeof(bool), typeof(bool)],
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
