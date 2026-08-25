using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// 起動引数解析の単体テスト。UI なし・WPF 不要。
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

    // ── --nestsuite フラグ未指定ケース（IsNestSuiteMode は --nestsuite 検出専用。
    //    既定の NestSuite 起動判定とは別） ─────────────────────────────────

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

    // ── 同時指定（NestSuite モードでファイルを開く）──────────────────────

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

    // ── .chatnest 起動引数 ───────────────────────────────────────

    [Fact]
    public void GetFilePath_WithNestSuitePlusChatNestFilePath_ReturnsPath()
    {
        // --nestsuite sample.chatnest 起動時にファイルパスが取得できることを確認
        Assert.Equal("sample.chatnest",
            StartupArgParser.GetFilePath(["--nestsuite", "sample.chatnest"]));
    }

    [Fact]
    public void IsNestSuiteMode_WithNestSuitePlusChatNestFilePath_ReturnsTrue()
    {
        // --nestsuite sample.chatnest でも NestSuite モードと判定される
        Assert.True(StartupArgParser.IsNestSuiteMode(["--nestsuite", "sample.chatnest"]));
    }

    // ── .ideanest 起動引数回帰確認 ───────────────────────────────

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
        // --nestsuite sample.ideanest でも NestSuite モードと判定される
        Assert.True(StartupArgParser.IsNestSuiteMode(["--nestsuite", "sample.ideanest"]));
    }

    // ── 既定 NestSuite 起動パターンの確認 ──────────────────────

    [Fact]
    public void GetFilePath_WithNoArgsDefaultNestSuite_ReturnsNull()
    {
        // 引数なし → GetFilePath = null → NestSuite が無題タブを作成
        Assert.Null(StartupArgParser.GetFilePath([]));
    }

    [Fact]
    public void GetFilePath_WithNotenestOnly_ReturnsPath()
    {
        // NoteNest.exe sample.notenest → GetFilePath = "sample.notenest" → NestSuite で開く
        Assert.Equal("sample.notenest", StartupArgParser.GetFilePath(["sample.notenest"]));
    }

    [Fact]
    public void GetFilePath_WithChatnestOnly_ReturnsPath()
    {
        // NoteNest.exe sample.chatnest → GetFilePath = "sample.chatnest" → NestSuite で開く
        Assert.Equal("sample.chatnest", StartupArgParser.GetFilePath(["sample.chatnest"]));
    }

    [Fact]
    public void GetFilePath_WithIdeanestOnly_ReturnsPath()
    {
        // NoteNest.exe sample.ideanest → GetFilePath = "sample.ideanest" → NestSuite で開く
        Assert.Equal("sample.ideanest", StartupArgParser.GetFilePath(["sample.ideanest"]));
    }

    [Fact]
    public void GetFilePath_WithUnknownFlagAndFilePath_ReturnsFilePath()
    {
        // 未知のフラグ（例: --classic-notenest）は '-' 始まりのためスキップされ、パスが返る
        Assert.Equal("sample.notenest",
            StartupArgParser.GetFilePath(["--classic-notenest", "sample.notenest"]));
    }

    [Fact]
    public void GetFilePath_WithUnknownFlagOnly_ReturnsNull()
    {
        // 未知のフラグのみ → null → NestSuite が無題タブを作成
        Assert.Null(StartupArgParser.GetFilePath(["--classic-notenest"]));
    }
}
