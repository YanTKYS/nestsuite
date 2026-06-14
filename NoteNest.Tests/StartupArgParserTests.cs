using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.6.1: 起動引数解析の単体テスト。UI なし・WPF 不要。
/// </summary>
public class StartupArgParserTests
{
    // ── --nestsuite フラグ検出 ────────────────────────────────────────────

    [Fact]
    public void IsNestSuiteMode_WithNestSuiteFlag_ReturnsTrue()
    {
        Assert.True(StartupArgParser.IsNestSuiteMode(["--nestsuite"]));
    }

    [Fact]
    public void IsNestSuiteMode_WithNestSuiteFlagMixedCase_ReturnsTrue()
    {
        Assert.True(StartupArgParser.IsNestSuiteMode(["--NestSuite"]));
    }

    [Fact]
    public void IsNestSuiteMode_WithNestSuiteFlagUpperCase_ReturnsTrue()
    {
        Assert.True(StartupArgParser.IsNestSuiteMode(["--NESTSUITE"]));
    }

    // ── 通常起動扱いになるケース ─────────────────────────────────────────

    [Fact]
    public void IsNestSuiteMode_WithNoArgs_ReturnsFalse()
    {
        Assert.False(StartupArgParser.IsNestSuiteMode([]));
    }

    [Fact]
    public void IsNestSuiteMode_WithFilePathOnly_ReturnsFalse()
    {
        Assert.False(StartupArgParser.IsNestSuiteMode(["project.notenest"]));
    }

    [Fact]
    public void IsNestSuiteMode_WithOtherFlag_ReturnsFalse()
    {
        Assert.False(StartupArgParser.IsNestSuiteMode(["--help"]));
    }

    // ── 同時指定（v1.6.1 非対応: NestSuite モード優先）─────────────────

    [Fact]
    public void IsNestSuiteMode_WithNestSuitePlusFilePath_ReturnsTrue()
    {
        // --nestsuite + ファイルパス同時指定は v1.6.1 非対応。NestSuite モードになる。
        Assert.True(StartupArgParser.IsNestSuiteMode(["--nestsuite", "project.notenest"]));
    }
}
