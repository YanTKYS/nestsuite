using System.IO;
using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.10.4: SH-20 すべて保存コマンドの回帰テスト。
/// </summary>
public class SaveAllCommandTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    // TD-75a-2 (v2.16.27): SH-20 の backlog 完了確認・v2.10.4 存在確認は
    // NestSuiteDocsContractTests.ReleaseNoteVersionAndIdRecords へ移設した
    // （(v2.10.4, SH-20) のデータ行）。検証内容は変えていない。
    // 下の ReleaseNotes_V2104_MentionsSH20（"すべて保存" の本文確認を含む
    // 個別意図の強いテスト）は維持する。

    [Fact]
    public void ReleaseNotes_V2104_MentionsSH20()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("SH-20", text);
        Assert.Contains("すべて保存", text);
    }
}
