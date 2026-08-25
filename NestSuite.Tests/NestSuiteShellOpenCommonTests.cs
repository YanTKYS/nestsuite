using System.Reflection;
using NestSuite;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// TD-63: NestSuiteShellWorkspaceLaunchTests から、3 形式共通「開く」導線の統合
/// と複数ファイル一括オープンに関するテストを扱う。
/// WPF ウィンドウは起動しない。
/// </summary>
public class NestSuiteShellOpenCommonTests
{
    // ── NoteNest Save As の重複パス検出 ──────────────────────────────────

    [Fact]
    public void MainViewModel_HasSaveToPathMethod_ReturnsBool()
    {
        // Shell は重複パス検出後にパス指定で保存するため MainViewModel.SaveToPath を使う
        var method = typeof(NestSuite.ViewModels.MainViewModel)
            .GetMethod("SaveToPath",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
                null,
                [typeof(string)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    // ── NestSuite 共通「開く」導線の統合 ──────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasOpenNestSuiteFileMethod()
    {
        // OpenNestSuiteFile が 3 形式共通「開く」の中心メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("OpenNestSuiteFile",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void NestSuiteShellWindow_HasNoMenuNewClickHandler()
    {
        // MenuNew_Click（ツール種別ディスパッチ）は 3 つのツール別ハンドラに置き換えられた
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("MenuNew_Click",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.Null(method);
    }

    [Fact]
    public void NestSuiteTabFactory_TryGetKind_UnsupportedExtension_ReturnsFalse()
    {
        // 未対応拡張子は TryGetKind が false を返すことを確認（OpenNestSuiteFile のエラー分岐の前提）
        // SH-43: .txt は PlainText として対応済みになったため、この一覧からは外した
        // （.txt が対応済みであることは PlainTextTabIntegrationTests で別途確認する）。
        Assert.False(NestSuiteTabFactory.TryGetKind("document.docx", out _));
        Assert.False(NestSuiteTabFactory.TryGetKind("noextension", out _));
    }

    // ── NestSuite 複数ファイル一括オープン ─────────────────────────

    [Fact]
    public void DialogService_HasSelectNestSuiteOpenPathsMethod()
    {
        // SelectNestSuiteOpenPaths が IReadOnlyList<string> を返すメソッドとして存在することを確認
        var method = typeof(NestSuite.Services.DialogService)
            .GetMethod("SelectNestSuiteOpenPaths",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.True(typeof(IReadOnlyList<string>).IsAssignableFrom(method!.ReturnType));
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void DialogService_DoesNotHaveSingleSelectNestSuiteOpenPathMethod()
    {
        // 旧 SelectNestSuiteOpenPath（単一選択）が削除されていることを確認
        var method = typeof(NestSuite.Services.DialogService)
            .GetMethod("SelectNestSuiteOpenPath",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.Null(method);
    }
}
