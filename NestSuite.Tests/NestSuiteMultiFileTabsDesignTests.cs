using NestSuite;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v1.9.0: 同一ツール複数ファイル対応の設計整理に伴う設計固定テスト。
///
/// <para>v1.9.0 は設計整理版であり、WorkspaceSession の本実装は行わない。
/// ここでは将来実装が前提とする不変条件（タブ ID の一意性・二重オープン判定の比較方針・
/// 拡張子判定の既存挙動）だけを固定し、後続バージョンでの回帰を検出できるようにする。</para>
/// </summary>
public class NestSuiteMultiFileTabsDesignTests
{
    // ── タブ ID の一意性（複数ファイル対応の前提） ──────────────────────

    [Fact]
    public void TabFactory_CreateUntitled_GeneratesUniqueIds()
    {
        // 同一ツール複数ファイル対応では、TabId が WorkspaceSession のキーになる。
        // 連続生成しても ID が衝突しないことを確認する。
        var ids = Enumerable.Range(0, 100)
            .Select(_ => NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest).Id)
            .ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void TabFactory_FromFilePath_SamePath_StillGeneratesDistinctIds()
    {
        // 同じファイルを 2 度生成しても別タブとして区別できる（ID は別）。
        // 二重オープンの抑止は ID ではなく FilePath 比較（NestSuiteOpenFilePolicy）で行う方針。
        var a = NestSuiteTabFactory.FromFilePath(@"C:\work\A.notenest");
        var b = NestSuiteTabFactory.FromFilePath(@"C:\work\A.notenest");

        Assert.NotEqual(a.Id, b.Id);
        Assert.Equal(a.FilePath, b.FilePath);
    }

    // ── 二重オープン判定の比較方針（NestSuiteOpenFilePolicy） ────────────

    [Fact]
    public void OpenFilePolicy_SamePath_IsSameFile()
    {
        Assert.True(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\work\A.notenest", @"C:\work\A.notenest"));
    }

    [Fact]
    public void OpenFilePolicy_DifferentCase_IsSameFile()
    {
        // Windows のファイルシステムは大文字小文字を区別しない
        Assert.True(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\work\A.notenest", @"c:\work\a.NOTENEST"));
    }

    [Fact]
    public void OpenFilePolicy_DifferentPath_IsNotSameFile()
    {
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\work\A.notenest", @"C:\work\B.notenest"));
    }

    [Fact]
    public void OpenFilePolicy_NullPath_IsNotSameFile()
    {
        // 無題タブ（FilePath = null）は二重オープン判定の対象外
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(null, @"C:\work\A.notenest"));
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(@"C:\work\A.notenest", null));
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(null, null));
    }

    // ── 既存挙動が壊れていないことの確認 ────────────────────────────────

    [Fact]
    public void MultipleTabs_SameWorkspaceKind_IsStillExpressible()
    {
        // 設計の根幹：1 ツールから複数タブを表現できること（v1.7.2 から維持）
        var a = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.ChatNest);
        var b = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.ChatNest);

        Assert.Equal(a.WorkspaceKind, b.WorkspaceKind);
        Assert.NotEqual(a.Id, b.Id);
    }

    [Theory]
    [InlineData(".notenest", NestSuiteWorkspaceKind.NoteNest)]
    [InlineData(".chatnest", NestSuiteWorkspaceKind.ChatNest)]
    [InlineData(".ideanest", NestSuiteWorkspaceKind.IdeaNest)]
    [InlineData(".txt", NestSuiteWorkspaceKind.PlainText)]
    public void ExtensionResolution_IsUnchanged(string ext, NestSuiteWorkspaceKind expected)
    {
        // 拡張子判定の既存挙動が v1.9.0 設計整理で変わっていないことを固定
        Assert.True(NestSuiteTabFactory.TryGetKind($"file{ext}", out var kind));
        Assert.Equal(expected, kind);
    }
}
