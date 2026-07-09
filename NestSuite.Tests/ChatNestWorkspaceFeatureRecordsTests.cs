using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.13 TD-63: ChatNestWorkspaceViewModelTests から、CH-8/CH-13/CH-15 の完了記録
/// （backlog / release-notes 確認）に関するテストを分割した。
/// TD-33: 完了済み項目は release-notes.md で管理する方針の確認。
/// </summary>
public class ChatNestWorkspaceFeatureRecordsTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    // TD-75a-2 (v2.16.27): CH-8 / CH-13 の release notes 存在確認・v2.10.9 存在確認は
    // NestSuiteDocsContractTests.ReleaseNoteVersionAndIdRecords へ移設した
    // （(v2.10.6, CH-8) / (v2.10.9, CH-13) のデータ行）。検証内容は変えていない。
    // v2.10.20 の存在確認も同様に (v2.10.20, CH-15) として移設し、下の
    // ReleaseNotes_Contains_CH15（"文脈メニュー" の本文確認を含む個別意図の強いテスト）と
    // 統合した。

    // ── CH-15: release-notes 確認 ─────────────────────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_CH15()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("CH-15", text);
        Assert.Contains("文脈メニュー", text);
    }
}
