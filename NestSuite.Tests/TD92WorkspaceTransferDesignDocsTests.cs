using System;
using System.IO;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.20.1 TD-92: Workspace 間手動転送の共通ヘルパー設計（設計 version）の docs 契約テスト。
///
/// <para>本 version は production code を変更しない設計 version のため、確認対象は
/// 設計文書・backlog・release notes の状態に限る。本文の完全一致や行番号には依存させず、
/// 「設計として確定していなければならない事項が文書に存在するか」だけを固定する。</para>
/// </summary>
public class TD92WorkspaceTransferDesignDocsTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string DesignDocPath =>
        Path.Combine(RepoRoot, "docs", "planning", "workspace-manual-transfer-helper-design.md");

    private static string ReadDesignDoc()
    {
        Assert.True(File.Exists(DesignDocPath), $"設計文書が見つからない: {DesignDocPath}");
        return File.ReadAllText(DesignDocPath);
    }

    // ── 1. 設計文書の存在と version / 対応 ID ────────────────────────────

    [Fact]
    public void DesignDoc_Exists_AndRecordsVersionAndId()
    {
        var doc = ReadDesignDoc();

        Assert.Contains("TD-92", doc);
        Assert.Contains("v2.20.1", doc);
    }

    // ── 2. LK-4 / LK-2 / LK-3 の記載 ─────────────────────────────────────

    [Theory]
    [InlineData("LK-4")]
    [InlineData("LK-2")]
    [InlineData("LK-3")]
    public void DesignDoc_MentionsLinkageBacklogItem(string id)
    {
        Assert.Contains(id, ReadDesignDoc());
    }

    [Fact]
    public void DesignDoc_FixesLk4TitleAndBodyMapping()
    {
        var doc = ReadDesignDoc();

        // LK-4 は ChatNest 発言 → IdeaNest カード。Title / Body のマッピングが確定していること。
        Assert.Contains("ChatNest", doc);
        Assert.Contains("IdeaNest", doc);
        Assert.Contains("Title", doc);
        Assert.Contains("Body", doc);
    }

    [Fact]
    public void DesignDoc_FixesIdeaNestTabCountBehaviour()
    {
        var doc = ReadDesignDoc();

        // IdeaNest タブ 0 件 / 1 件 / 複数件それぞれの挙動が確定していること。
        Assert.Contains("0 件", doc);
        Assert.Contains("1 件", doc);
        Assert.Contains("複数件", doc);
    }

    // ── 3. dirty / save / error の責務 ───────────────────────────────────

    [Theory]
    [InlineData("dirty")]
    [InlineData("save")]
    [InlineData("ErrorLog")]
    public void DesignDoc_DescribesResponsibility(string keyword)
    {
        Assert.Contains(keyword, ReadDesignDoc(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DesignDoc_FixesTransferContractTerms()
    {
        var doc = ReadDesignDoc();

        // 転送データ契約・転送先識別・受入契約・結果契約が具体名で確定していること。
        Assert.Contains("WorkspaceTransferContent", doc);
        Assert.Contains("WorkspaceTransferResult", doc);
        Assert.Contains("NoTarget", doc);
        Assert.Contains("InvalidContent", doc);
        Assert.Contains("TargetRejected", doc);
        Assert.Contains("Success", doc);
        Assert.Contains("Failed", doc);
    }

    [Fact]
    public void DesignDoc_RecordsRelationToTn3()
    {
        Assert.Contains("TN-3", ReadDesignDoc());
    }

    // ── 4. 非対象の明記 ──────────────────────────────────────────────────

    [Fact]
    public void DesignDoc_DeclaresOutOfScope()
    {
        var doc = ReadDesignDoc();

        Assert.Contains("非対象", doc);
        Assert.Contains("自動", doc);      // 自動連携・自動保存・自動生成を非対象として扱う
        Assert.Contains("履歴", doc);      // 転送履歴の永続化は非対象
        Assert.Contains("EventBus", doc);  // Workspace 間 EventBus は非対象
    }

    // ── 5. release notes / backlog の状態 ────────────────────────────────

    [Fact]
    public void ReleaseNotes_RecordsV2201Td92()
    {
        var notes = TestPaths.ReadReleaseNotes();

        Assert.Contains("v2.20.1", notes);
        Assert.Contains("TD-92", notes);
    }

    [Fact]
    public void Backlog_DoesNotContainTd92AsOpenItem()
    {
        var backlog = TestPaths.ReadBacklog();

        Assert.False(backlog.Contains("| TD-92 |", StringComparison.Ordinal),
            "backlog.md に TD-92 が open item の表行として残っている");
    }

    [Fact]
    public void Backlog_DoesNotContainLk4AsOpenItem()
    {
        // v2.20.1 時点では LK-4 は未実装で backlog に open item として残っていたが、
        // v2.21.0 で実装済みとなり欠番化した（NestSuiteDocsContractTests の
        // BacklogCompletedOpenItemAbsenceRecords 側で完了確認を集約する）。
        var backlog = TestPaths.ReadBacklog();

        Assert.False(backlog.Contains("| LK-4 |", StringComparison.Ordinal),
            "LK-4 は v2.21.0 で実装済みのため、backlog に open item として残っていてはならない");
    }

    [Fact]
    public void Backlog_RecordsTransferHelperDesignReviewCompletion()
    {
        var backlog = TestPaths.ReadBacklog();

        // 共通着手トリガー「転送共通ヘルパーの設計レビューが完了した」の成立が記録されていること。
        Assert.Contains("転送共通ヘルパー", backlog);
        Assert.Contains("TD-92", backlog);
        Assert.Contains("v2.20.1", backlog);
        Assert.Contains("workspace-manual-transfer-helper-design.md", backlog);
    }

    [Fact]
    public void Backlog_Lk2_NoLongerOpenItem_ImplementedInLaterVersion()
    {
        // v2.20.1 時点では LK-2 / LK-3 とも未着手で backlog に残っていたが、
        // LK-3 は v2.22.0、LK-2 は v2.23.0 でそれぞれ実装済みとなり欠番化した
        // （NestSuiteDocsContractTests の BacklogCompletedOpenItemAbsenceRecords 側で完了確認を集約する）。
        var backlog = TestPaths.ReadBacklog();

        Assert.False(backlog.Contains("| LK-2 |", StringComparison.Ordinal));
    }
}
