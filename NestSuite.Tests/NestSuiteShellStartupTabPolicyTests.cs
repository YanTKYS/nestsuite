using System.Reflection;
using NestSuite;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.13 TD-63: NestSuiteShellWorkspaceLaunchTests から、起動時の初期タブ生成・
/// フォールバックタブ判断（NestSuiteStartupTabPolicy）と起動引数パース（StartupArgParser）に
/// 関するテストを分割した。ワークスペース種別に依存しない、Shell 起動導線の共通基盤を扱う。
/// WPF ウィンドウを生成せずに初期タブ生成判断の正しさを自動確認する。
/// Shell がポリシーを使うことで、ポリシーへの変更は即座に回帰テストに反映される。
/// </summary>
public class NestSuiteShellStartupTabPolicyTests
{
    // ── v1.6.3: LoadInitialFile メソッドの存在確認 ────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasLoadInitialFileMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadInitialFile",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
                null,
                [typeof(string)],
                null);
        Assert.NotNull(method);
    }

    // ── v1.8.6: 起動時ファイル指定時の無題タブ生成修正 ─────────────────────

    [Fact]
    public void NestSuiteShellWindow_Constructor_AcceptsOptionalStringParameter()
    {
        // v1.8.6: コンストラクタが string? initialFilePath = null を受け取れることを確認
        var ctor = typeof(NestSuiteShellWindow)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(c =>
            {
                var p = c.GetParameters();
                return p.Length == 1 &&
                       p[0].ParameterType == typeof(string) &&
                       p[0].IsOptional;
            });
        Assert.NotNull(ctor);
    }

    [Fact]
    public void NestSuiteShellWindow_HasEnsureDefaultTabMethod()
    {
        // v1.8.6: EnsureDefaultTab がフォールバック NoteNest タブ生成の中心メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("EnsureDefaultTab",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    // ── v1.8.6: NestSuiteStartupTabPolicy 動作テスト ────────────────────────
    // WPF ウィンドウを生成せずに初期タブ生成判断の正しさを自動確認する。
    // Shell がポリシーを使うことで、ポリシーへの変更は即座に回帰テストに反映される。

    [Fact]
    public void StartupTabPolicy_NullFilePath_ShouldCreateInitialTab()
    {
        // ファイル指定なし起動 → 無題NoteNestタブを作成する
        Assert.True(NestSuiteStartupTabPolicy.ShouldCreateInitialTab(null));
    }

    [Fact]
    public void StartupTabPolicy_EmptyFilePath_ShouldCreateInitialTab()
    {
        // 空文字列 → ファイル指定なしと同等に扱い、無題NoteNestタブを作成する
        Assert.True(NestSuiteStartupTabPolicy.ShouldCreateInitialTab(""));
    }

    [Fact]
    public void StartupTabPolicy_WithFilePath_ShouldNotCreateInitialTab()
    {
        // ファイル指定ありの場合は初期タブを作成しない
        Assert.False(NestSuiteStartupTabPolicy.ShouldCreateInitialTab("sample.chatnest"));
        Assert.False(NestSuiteStartupTabPolicy.ShouldCreateInitialTab("sample.ideanest"));
        Assert.False(NestSuiteStartupTabPolicy.ShouldCreateInitialTab("sample.notenest"));
    }

    [Fact]
    public void StartupTabPolicy_ZeroTabs_ShouldEnsureFallbackTab()
    {
        // 読込失敗後タブが0枚 → フォールバック無題NoteNestタブを作成する
        Assert.True(NestSuiteStartupTabPolicy.ShouldEnsureFallbackTab(0));
    }

    [Fact]
    public void StartupTabPolicy_HasTabs_ShouldNotEnsureFallbackTab()
    {
        // タブが1枚以上存在する場合は追加しない
        Assert.False(NestSuiteStartupTabPolicy.ShouldEnsureFallbackTab(1));
        Assert.False(NestSuiteStartupTabPolicy.ShouldEnsureFallbackTab(2));
    }

    // ── v1.10.2: NestSuite 起動時ファイル指定の初期タブちらつき修正 ──

    [Fact]
    public void StartupTabPolicy_WithNullPath_ShouldCreateInitialTab()
    {
        // v1.10.2: ファイル未指定（null）→ 無題タブを作成すべき
        Assert.True(NestSuiteStartupTabPolicy.ShouldCreateInitialTab(null));
    }

    [Fact]
    public void StartupTabPolicy_WithEmptyPath_ShouldCreateInitialTab()
    {
        // v1.10.2: 空文字列も未指定扱い → 無題タブを作成すべき
        Assert.True(NestSuiteStartupTabPolicy.ShouldCreateInitialTab(""));
    }

    [Fact]
    public void StartupTabPolicy_AllThreeKindPaths_SuppressInitialTab()
    {
        // v1.10.2: 3 種すべての拡張子で初期無題タブが抑制されることを確認
        Assert.False(NestSuiteStartupTabPolicy.ShouldCreateInitialTab("sample.notenest"));
        Assert.False(NestSuiteStartupTabPolicy.ShouldCreateInitialTab("sample.chatnest"));
        Assert.False(NestSuiteStartupTabPolicy.ShouldCreateInitialTab("sample.ideanest"));
    }

    [Fact]
    public void StartupTabPolicy_WithUnsupportedExtension_SuppressesInitialTab()
    {
        // v1.10.2: 未対応拡張子のパスも「パスあり」とみなし初期無題タブは抑制される。
        // LoadInitialFile が拡張子エラーを処理し、フォールバックタブを作成する。
        Assert.False(NestSuiteStartupTabPolicy.ShouldCreateInitialTab("document.txt"));
    }

    [Fact]
    public void StartupArgParser_GetFilePath_ReturnsNonFlagArg()
    {
        // v1.10.2: --nestsuite + ファイルパスの組み合わせで GetFilePath が正しくパスを返す
        var args = new[] { "--nestsuite", "C:\\work\\test.chatnest" };
        Assert.Equal("C:\\work\\test.chatnest", StartupArgParser.GetFilePath(args));
    }

    [Fact]
    public void StartupArgParser_GetFilePath_WithNoFileArg_ReturnsNull()
    {
        // v1.10.2: --nestsuite のみ（ファイルなし）は null → ShouldCreateInitialTab(null) → true
        var args = new[] { "--nestsuite" };
        Assert.Null(StartupArgParser.GetFilePath(args));
    }
}
