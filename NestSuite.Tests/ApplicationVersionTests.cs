using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// アプリバージョン・保存 schema version の production contract を固定する。
///
/// <para>TD-94 (v2.24.1): 「他のテストクラスが特定文字列を含まないこと」を走査していた
/// 2 件（<c>ApplicationVersion_IsNotTested_InOtherTestClasses</c> /
/// <c>CurrentSchemaVersionLiteral_IsNotHardcoded_InOtherTestClasses</c>）は削除した。
/// テストコードそのものを検査する test-of-test であり、失敗しても利用者影響・データ破損・
/// 互換性破壊のいずれも起きないため（方針: <c>docs/development/test-suite-policy.md</c>）。
/// バージョン確認をここへ集約する運用方針自体は開発ルール側で維持する。</para>
/// </summary>
public class ApplicationVersionTests
{
    [Fact]
    public void ApplicationVersion_UsesAssemblyInformationalVersion()
    {
        Assert.Equal("2.25.4", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void WindowTitle_UsesApplicationVersion()
    {
        var viewModel = new MainViewModel();

        Assert.EndsWith(" - ver2.25.4", viewModel.WindowTitle);
    }

    [Fact]
    public void ApplicationAndSchemaVersionsAreManagedBySeparateSources()
    {
        Assert.Equal("2.25.4", MainViewModel.ApplicationVersion);
        Assert.Equal("1.4.2", Project.CurrentSchemaVersion);
    }

    // 次回 schema bump ではこのリテラルのみを更新する
    [Fact]
    public void NoteNestSchemaVersion_IsPinned()
    {
        Assert.Equal("1.4.2", Project.CurrentSchemaVersion);
    }
}
