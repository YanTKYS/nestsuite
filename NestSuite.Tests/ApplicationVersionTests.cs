using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// アプリバージョン・保存 schema version の production contract を固定する。
///
/// <para>バージョンリテラルの確認はこのクラスへ集約する（開発ルール §6）。他のテストクラスが
/// リテラルを持たないことを走査するテストは置かない。テストコード自体を検査しても
/// 利用者影響・データ破損・互換性破壊は検出できないため
/// （方針: <c>docs/development/test-suite-policy.md</c>）。</para>
/// </summary>
public class ApplicationVersionTests
{
    [Fact]
    public void ApplicationVersion_UsesAssemblyInformationalVersion()
    {
        Assert.Equal("2.26.1", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void WindowTitle_UsesApplicationVersion()
    {
        var viewModel = new MainViewModel();

        Assert.EndsWith(" - ver2.26.1", viewModel.WindowTitle);
    }

    [Fact]
    public void ApplicationAndSchemaVersionsAreManagedBySeparateSources()
    {
        Assert.Equal("2.26.1", MainViewModel.ApplicationVersion);
        Assert.Equal("1.4.2", Project.CurrentSchemaVersion);
    }

    // 次回 schema bump ではこのリテラルのみを更新する
    [Fact]
    public void NoteNestSchemaVersion_IsPinned()
    {
        Assert.Equal("1.4.2", Project.CurrentSchemaVersion);
    }
}
