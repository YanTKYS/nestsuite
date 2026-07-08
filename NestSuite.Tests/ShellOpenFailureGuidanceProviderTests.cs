using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.11 SH-1: 起動時・外部オープン時のファイルオープン失敗に添える
/// 「NestSuite は起動している」ヒントの、UI 非依存の文言 helper のテスト。
/// </summary>
public class ShellOpenFailureGuidanceProviderTests
{
    [Fact]
    public void AppendStillUsableHint_KeepsOriginalMessage()
    {
        var result = ShellOpenFailureGuidanceProvider.AppendStillUsableHint("ファイルが見つかりません。");
        Assert.StartsWith("ファイルが見つかりません。", result);
    }

    [Fact]
    public void AppendStillUsableHint_MentionsNestSuiteIsRunning()
    {
        var result = ShellOpenFailureGuidanceProvider.AppendStillUsableHint("ファイルが見つかりません。");
        Assert.Contains("NestSuite は起動しています", result);
    }

    [Fact]
    public void AppendStillUsableHint_MentionsOpeningAnotherFile()
    {
        var result = ShellOpenFailureGuidanceProvider.AppendStillUsableHint("ファイルが見つかりません。");
        Assert.Contains("別のファイルを開く", result);
    }

    [Fact]
    public void AppendStillUsableHint_MentionsStartingNewTabWork()
    {
        var result = ShellOpenFailureGuidanceProvider.AppendStillUsableHint("ファイルが見つかりません。");
        Assert.Contains("新しいタブで作業", result);
    }

    [Fact]
    public void AppendStillUsableHint_DoesNotLeakInternalTypeNames()
    {
        var result = ShellOpenFailureGuidanceProvider.AppendStillUsableHint("ファイルが見つかりません。");
        Assert.DoesNotContain("Exception", result);
        Assert.DoesNotContain("ViewModel", result);
        Assert.DoesNotContain("null", result);
    }

    [Fact]
    public void AppendStillUsableHint_IsShort()
    {
        // ダイアログを長くしすぎない方針（SH-1 改善方針 5）: ヒント自体は短い 1 文にとどめる。
        Assert.True(ShellOpenFailureGuidanceProvider.StillUsableHint.Length <= 60);
    }
}
