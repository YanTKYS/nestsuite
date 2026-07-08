using System.Reflection;
using NestSuite;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.13 TD-63: NestSuiteShellWorkspaceLaunchTests から、3 形式共通「開く」導線の統合
/// （v1.10.1）と複数ファイル一括オープン（v1.16.0）に関するテストを分割した。
/// WPF ウィンドウは起動しない。
/// </summary>
public class NestSuiteShellOpenCommonTests
{
    // ── v1.9.8 fix: NoteNest Save As の重複パス検出 ───────────────────────

    [Fact]
    public void MainViewModel_HasSaveToPathMethod_ReturnsBool()
    {
        // v1.9.8 fix: Shell が重複パス検出後にパス指定で保存するため MainViewModel.SaveToPath を追加
        var method = typeof(NestSuite.ViewModels.MainViewModel)
            .GetMethod("SaveToPath",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
                null,
                [typeof(string)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    // ── v1.10.1: NestSuite 共通「開く」導線の統合 ──────────────────────────
    // Note: SelectNestSuiteOpenPath (単一選択) は v1.16.0 で SelectNestSuiteOpenPaths に置き換え済み。

    [Fact]
    public void NestSuiteShellWindow_HasOpenNestSuiteFileMethod()
    {
        // v1.10.1: OpenNestSuiteFile が 3 形式共通「開く」の中心メソッドとして宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("OpenNestSuiteFile",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void NestSuiteShellWindow_MenuNew_Click_IsRemovedInV1101()
    {
        // v1.10.1: MenuNew_Click（ツール種別ディスパッチ）は 3 つのツール別ハンドラに置き換えられた
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("MenuNew_Click",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.Null(method);
    }

    [Fact]
    public void NestSuiteTabFactory_TryGetKind_UnsupportedExtension_ReturnsFalse()
    {
        // v1.10.1: 未対応拡張子は TryGetKind が false を返すことを確認（OpenNestSuiteFile のエラー分岐の前提）
        Assert.False(NestSuiteTabFactory.TryGetKind("document.txt", out _));
        Assert.False(NestSuiteTabFactory.TryGetKind("document.docx", out _));
        Assert.False(NestSuiteTabFactory.TryGetKind("noextension", out _));
    }

    // ── v1.16.0: NestSuite 複数ファイル一括オープン ─────────────────────────

    [Fact]
    public void DialogService_HasSelectNestSuiteOpenPathsMethod()
    {
        // v1.16.0: SelectNestSuiteOpenPaths が IReadOnlyList<string> を返すメソッドとして存在することを確認
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
        // v1.16.0: 旧 SelectNestSuiteOpenPath（単一選択）が削除されていることを確認
        var method = typeof(NestSuite.Services.DialogService)
            .GetMethod("SelectNestSuiteOpenPath",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.Null(method);
    }
}
