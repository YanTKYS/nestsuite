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

    // ── v1.11.0: --classic-notenest フラグ検出 ───────────────────────────

    [Fact]
    public void IsClassicMode_WithClassicFlag_ReturnsTrue()
    {
        // v1.11.0: --classic-notenest → 従来 NoteNest 単体版（互換ルート）
        Assert.True(StartupArgParser.IsClassicMode(["--classic-notenest"]));
    }

    [Fact]
    public void IsClassicMode_WithClassicFlagMixedCase_ReturnsTrue()
    {
        Assert.True(StartupArgParser.IsClassicMode(["--Classic-NoteNest"]));
    }

    [Fact]
    public void IsClassicMode_WithNoArgs_ReturnsFalse()
    {
        // v1.11.0: 引数なし → NestSuite（既定）
        Assert.False(StartupArgParser.IsClassicMode([]));
    }

    [Fact]
    public void IsClassicMode_WithNestSuiteFlag_ReturnsFalse()
    {
        // --nestsuite は互換扱いで NestSuite を起動。IsClassicMode は false
        Assert.False(StartupArgParser.IsClassicMode(["--nestsuite"]));
    }

    [Fact]
    public void IsClassicMode_WithFilePath_ReturnsFalse()
    {
        // v1.11.0: ファイルパスのみ → NestSuite で開く（IsClassicMode = false）
        Assert.False(StartupArgParser.IsClassicMode(["sample.notenest"]));
    }

    [Fact]
    public void IsClassicMode_WithClassicFlagAndFilePath_ReturnsTrue()
    {
        // --classic-notenest sample.notenest → 単体版でファイルを開く
        Assert.True(StartupArgParser.IsClassicMode(["--classic-notenest", "sample.notenest"]));
    }

    // ── v1.11.0: 既定 NestSuite 起動パターンの確認 ──────────────────────

    [Fact]
    public void GetFilePath_WithNoArgsDefaultNestSuite_ReturnsNull()
    {
        // v1.11.0: 引数なし → GetFilePath = null → NestSuite が無題タブを作成
        Assert.Null(StartupArgParser.GetFilePath([]));
    }

    [Fact]
    public void GetFilePath_WithNotenestOnly_ReturnsPath()
    {
        // v1.11.0: NoteNest.exe sample.notenest → GetFilePath = "sample.notenest" → NestSuite で開く
        Assert.Equal("sample.notenest", StartupArgParser.GetFilePath(["sample.notenest"]));
    }

    [Fact]
    public void GetFilePath_WithChatnestOnly_ReturnsPath()
    {
        // v1.11.0: NoteNest.exe sample.chatnest → GetFilePath = "sample.chatnest" → NestSuite で開く
        Assert.Equal("sample.chatnest", StartupArgParser.GetFilePath(["sample.chatnest"]));
    }

    [Fact]
    public void GetFilePath_WithIdeanestOnly_ReturnsPath()
    {
        // v1.11.0: NoteNest.exe sample.ideanest → GetFilePath = "sample.ideanest" → NestSuite で開く
        Assert.Equal("sample.ideanest", StartupArgParser.GetFilePath(["sample.ideanest"]));
    }

    [Fact]
    public void GetFilePath_WithClassicFlagAndFilePath_ReturnsFilePath()
    {
        // --classic-notenest sample.notenest → --classic-notenest はフラグ扱い、パスが返る
        Assert.Equal("sample.notenest",
            StartupArgParser.GetFilePath(["--classic-notenest", "sample.notenest"]));
    }

    [Fact]
    public void GetFilePath_WithClassicFlagOnly_ReturnsNull()
    {
        // --classic-notenest のみ → null → MainWindow が StartupDialog を表示
        Assert.Null(StartupArgParser.GetFilePath(["--classic-notenest"]));
    }
}
