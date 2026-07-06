using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.15.1 SH: Shift+←→ によるタブ移動ショートカットの無効化を固定する構造テスト。
/// NestSuiteShellWindow は WPF Window であり STA/App リソース依存のためテスト内で直接
/// インスタンス化できない（本リポジトリの既存方針）。そのため OnPreviewKeyDown の実装
/// ソースを静的に検査し、Key.Left / Key.Right をタブ移動として扱っていないことを確認する。
/// </summary>
public class ShellTabNavigationShortcutTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    [Fact]
    public void TabSelectionSource_OnPreviewKeyDown_DoesNotHandleShiftLeftRightAsTabNavigation()
    {
        var src = ReadTabSelectionSource();

        // 旧実装: `if (shift && !ctrl && (e.Key == Key.Left || e.Key == Key.Right))` で
        // NavigateTab を呼んでいた。この組み合わせが完全に削除されていることを確認する。
        Assert.DoesNotContain("e.Key == Key.Left || e.Key == Key.Right", src);
    }

    [Fact]
    public void TabSelectionSource_StillHandlesCtrlTabAndCtrlNumberShortcuts()
    {
        // Shift+←→ 廃止の対象外（維持する）既存ショートカットが残っていることを確認する。
        var src = ReadTabSelectionSource();
        Assert.Contains("ctrl && e.Key == Key.Tab", src);
        Assert.Contains("e.Key >= Key.D1 && e.Key <= Key.D9", src);
    }

    private string ReadTabSelectionSource()
    {
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.TabSelection.cs");
        Assert.True(File.Exists(path), $"NestSuiteShellWindow.TabSelection.cs not found: {path}");
        return File.ReadAllText(path);
    }
}
