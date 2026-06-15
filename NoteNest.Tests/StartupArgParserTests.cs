using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.6.3: 起動引数解析の単体テスト。UI なし・WPF 不要。
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

    // ── 同時指定（v1.6.3 以降: NestSuite モードでファイルを開く）─────────

    [Fact]
    public void IsNestSuiteMode_WithNestSuitePlusFilePath_ReturnsTrue()
    {
        Assert.True(StartupArgParser.IsNestSuiteMode(["--nestsuite", "project.notenest"]));
    }

    // ── GetFilePath ──────────────────────────────────────────────────────

    [Fact]
    public void GetFilePath_WithFilePath_ReturnsPath()
    {
        Assert.Equal("project.notenest", StartupArgParser.GetFilePath(["project.notenest"]));
    }

    [Fact]
    public void GetFilePath_WithNestSuitePlusFilePath_ReturnsPath()
    {
        Assert.Equal("project.notenest",
            StartupArgParser.GetFilePath(["--nestsuite", "project.notenest"]));
    }

    [Fact]
    public void GetFilePath_WithFilePathBeforeFlag_ReturnsPath()
    {
        Assert.Equal("project.notenest",
            StartupArgParser.GetFilePath(["project.notenest", "--nestsuite"]));
    }

    [Fact]
    public void GetFilePath_WithOnlyFlag_ReturnsNull()
    {
        Assert.Null(StartupArgParser.GetFilePath(["--nestsuite"]));
    }

    [Fact]
    public void GetFilePath_WithNoArgs_ReturnsNull()
    {
        Assert.Null(StartupArgParser.GetFilePath([]));
    }

    [Fact]
    public void GetFilePath_WithUnsupportedExtension_ReturnsPath()
    {
        // 未対応拡張子もファイルパス候補として返す。拡張子検証は LoadInitialFile() が担当する。
        Assert.Equal("project.json", StartupArgParser.GetFilePath(["--nestsuite", "project.json"]));
    }

    // ── v1.7.7: .chatnest 起動引数 ───────────────────────────────────────

    [Fact]
    public void GetFilePath_WithNestSuitePlusChatNestFilePath_ReturnsPath()
    {
        // v1.7.7: --nestsuite sample.chatnest 起動時にファイルパスが取得できることを確認
        Assert.Equal("sample.chatnest",
            StartupArgParser.GetFilePath(["--nestsuite", "sample.chatnest"]));
    }

    [Fact]
    public void IsNestSuiteMode_WithNestSuitePlusChatNestFilePath_ReturnsTrue()
    {
        // v1.7.7: --nestsuite sample.chatnest でも NestSuite モードと判定される
        Assert.True(StartupArgParser.IsNestSuiteMode(["--nestsuite", "sample.chatnest"]));
    }

    // ── v1.8.4: .ideanest 起動引数回帰確認 ───────────────────────────────

    [Fact]
    public void GetFilePath_WithNestSuitePlusIdeaNestFilePath_ReturnsPath()
    {
        // --nestsuite sample.ideanest のパスを LoadInitialFile へ渡せることを確認
        Assert.Equal("sample.ideanest",
            StartupArgParser.GetFilePath(["--nestsuite", "sample.ideanest"]));
    }

    [Fact]
    public void IsNestSuiteMode_WithNestSuitePlusIdeaNestFilePath_ReturnsTrue()
    {
        // v1.8.1: --nestsuite sample.ideanest でも NestSuite モードと判定される
        Assert.True(StartupArgParser.IsNestSuiteMode(["--nestsuite", "sample.ideanest"]));
    }
}
