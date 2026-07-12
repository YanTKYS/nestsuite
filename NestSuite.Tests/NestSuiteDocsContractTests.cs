using System.Text.RegularExpressions;
using NestSuite.Models;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// docs/backlog.md と docs/release-notes.md の内容に関する契約テスト。
/// 完了済み項目が release-notes に記録されていることを静的に確認する。
/// </summary>
public class NestSuiteDocsContractTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    // ═══════════════════════════════════════════════════════════════════
    // データ駆動: release notes / backlog 存在確認 (TD-75a, v2.16.26)
    //
    // v2.16.25 TD-74 の棚卸しレビュー（docs/planning/static-test-inventory-review.md）
    // で、バージョンごとに同形の [Fact] が増え続ける構造が最大の課題と整理された。
    // 以下の 3 種類は「機械的な存在確認だけ」のテストであり、[Theory] + MemberData に
    // 集約した。検証内容（どの version / id を確認していたか）は元の個別 Fact から
    // 一切削除せず、データ行として移設している。
    // release notes 本文の設計判断確認・user guide / design-decisions の重要語句確認など、
    // 個別に意図の強いテストはこの節に含めず、下の「個別内容確認」節に維持する。
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// release notes に (version, id) の組が含まれることを確認するレコード。
    /// id が他 ID の部分文字列と衝突する場合（例: "SH-1" は "SH-15" にも一致する）は
    /// idSearchText に見出し専用の厳密な文字列を指定する（衝突しない場合は id と同じ）。
    /// </summary>
    public static IEnumerable<object[]> ReleaseNoteVersionAndIdRecords()
    {
        yield return new object[] { "v2.10.10", "TD-23", "TD-23" };
        yield return new object[] { "v2.10.21", "SH-25", "SH-25" };
        yield return new object[] { "v2.10.22", "ID-14", "ID-14" };
        yield return new object[] { "v2.16.5", "SH-28", "SH-28" };
        yield return new object[] { "v2.16.6", "TD-64", "TD-64" };
        yield return new object[] { "v2.16.7", "TD-65", "TD-65" };
        yield return new object[] { "v2.16.8", "L20", "L20" };
        yield return new object[] { "v2.16.8", "L8", "L8" };
        yield return new object[] { "v2.16.9", "SH-29", "SH-29" };
        // SH-30 は v2.13.3 のエントリでも使われているため、見出し専用の文字列で確認する。
        yield return new object[] { "v2.16.10", "SH-30", "v2.16.10 — SH-30" };
        // "SH-1" は "SH-15" 等の部分文字列にも一致するため、見出し専用の文字列で確認する。
        yield return new object[] { "v2.16.11", "SH-1", "v2.16.11 — SH-1:" };
        yield return new object[] { "v2.16.12", "RJ-10", "RJ-10" };
        yield return new object[] { "v2.16.13", "TD-63", "TD-63" };
        yield return new object[] { "v2.16.14", "TD-66", "TD-66" };
        yield return new object[] { "v2.16.15", "TD-67", "TD-67" };
        yield return new object[] { "v2.16.16", "TD-68", "TD-68" };
        yield return new object[] { "v2.16.17", "TD-69", "TD-69" };
        yield return new object[] { "v2.16.18", "TD-70", "TD-70" };
        yield return new object[] { "v2.16.19", "TD-71", "TD-71" };
        yield return new object[] { "v2.16.20", "TD-72", "TD-72" };
        yield return new object[] { "v2.16.21", "SH-34", "SH-34" };
        yield return new object[] { "v2.16.22", "SH-35", "SH-35" };
        yield return new object[] { "v2.16.23", "TD-73", "TD-73" };
        yield return new object[] { "v2.16.25", "TD-74", "TD-74" };
        yield return new object[] { "v2.16.26", "TD-75a", "TD-75a" };
        yield return new object[] { "v2.16.43", "SH-36a", "SH-36a" };
        yield return new object[] { "v2.16.44", "SH-36a-1", "SH-36a-1" };
        yield return new object[] { "v2.16.45", "SH-36a-2", "SH-36a-2" };
        yield return new object[] { "v2.16.46", "SH-36b", "SH-36b" };
        // 注意: v2.16.24 (LT-9 フェーズ2) は "LT-9" と "フェーズ2" という
        // 2 つのキーワードを 1 テストで確認する形（ID 単体ではない）だったため、
        // この一覧には含めず ReleaseNotes_Contains_V21624 / _LT9Phase2 として個別に維持する。

        // ── TD-75a-2 (v2.16.27): 他ファイルに散在していた同形テストの移設分 ──
        // 移設元は各テストファイルの docs-contract 節（削除済み）。
        yield return new object[] { "v2.10.2", "FM-1", "FM-1" };
        yield return new object[] { "v2.10.3", "TN-2", "TN-2" };
        yield return new object[] { "v2.10.3", "L14", "L14" };
        yield return new object[] { "v2.10.3", "L15", "L15" };
        yield return new object[] { "v2.10.4", "SH-20", "SH-20" };
        yield return new object[] { "v2.10.5", "M10", "M10" };
        yield return new object[] { "v2.10.6", "CH-8", "CH-8" };
        yield return new object[] { "v2.10.6", "CH-14", "CH-14" };
        yield return new object[] { "v2.10.7", "CH-9", "CH-9" };
        yield return new object[] { "v2.10.9", "CH-13", "CH-13" };
        yield return new object[] { "v2.10.13", "TD-26", "TD-26" };
        yield return new object[] { "v2.10.20", "CH-15", "CH-15" };
        // 移設元テストは "SH-19" が backlog に残っていることを確認していたが、
        // SH-19 は v2.16.4 で完了済み（backlog.md には完了済み欠番の要約行にのみ残る）。
        // TD-33 の運用方針（完了済み項目は release-notes.md 側で管理）に合わせて
        // release notes 側の (version, id) 確認へ修正のうえ移設した。
        yield return new object[] { "v2.16.4", "SH-19", "SH-19" };
        // 移設元テストは "TD-24" / "TD-25" の release notes バージョンとして
        // "v2.10.13"（TD-26 のバージョン）を誤って確認していた（コピー&ペースト由来と見られる）。
        // 実際の見出しは「v2.10.11 — TD-24」「v2.10.12 — TD-25」のため、移設にあわせて修正した。
        yield return new object[] { "v2.10.11", "TD-24", "TD-24" };
        yield return new object[] { "v2.10.12", "TD-25", "TD-25" };
    }

    [Theory]
    [MemberData(nameof(ReleaseNoteVersionAndIdRecords))]
    public void ReleaseNotes_RecordsVersionAndId(string version, string id, string idSearchText)
    {
        var text = TestPaths.ReadReleaseNotes();
        Assert.True(text.Contains(version, StringComparison.Ordinal),
            $"release notes に version '{version}' が見つからない（対象 id: {id}）");
        Assert.True(text.Contains(idSearchText, StringComparison.Ordinal),
            $"release notes に id '{id}' が見つからない（検索文字列: '{idSearchText}'）");
    }

    /// <summary>
    /// 完了済み id が backlog.md に open item の表行（"| id |"）として残っていないことを
    /// 確認するレコード一覧。
    /// </summary>
    public static IEnumerable<object[]> BacklogCompletedOpenItemAbsenceRecords()
    {
        yield return new object[] { "SH-28" };
        yield return new object[] { "TD-64" };
        yield return new object[] { "TD-65" };
        yield return new object[] { "L8" };
        yield return new object[] { "L20" };
        yield return new object[] { "SH-29" };
        yield return new object[] { "SH-30" };
        yield return new object[] { "SH-1" };
        yield return new object[] { "M2" };
        yield return new object[] { "TD-63" };
        yield return new object[] { "TD-66" };
        yield return new object[] { "TD-67" };
        yield return new object[] { "TD-68" };
        yield return new object[] { "TD-69" };
        yield return new object[] { "TD-70" };
        yield return new object[] { "TD-71" };
        yield return new object[] { "TD-72" };
        yield return new object[] { "SH-34" };
        yield return new object[] { "TD-73" };
    }

    [Theory]
    [MemberData(nameof(BacklogCompletedOpenItemAbsenceRecords))]
    public void Backlog_DoesNotContainCompletedIdAsOpenItem(string id)
    {
        var backlog = TestPaths.ReadBacklog();
        Assert.False(backlog.Contains($"| {id} |", StringComparison.Ordinal),
            $"backlog.md に完了済み id '{id}' が open item の表行として残っている");
    }

    /// <summary>
    /// TD-A〜TD-B の完了済み範囲（backlog.md 冒頭の「は完了済み（欠番）」行）に
    /// 対象 TD 番号が含まれることを確認するレコード一覧。
    /// </summary>
    public static IEnumerable<object[]> BacklogCompletedTDRangeRecords()
    {
        yield return new object[] { 66 };
        yield return new object[] { 67 };
        yield return new object[] { 68 };
        yield return new object[] { 69 };
        yield return new object[] { 70 };
        yield return new object[] { 71 };
        yield return new object[] { 72 };
        yield return new object[] { 73 };
        yield return new object[] { 74 };
    }

    [Theory]
    [MemberData(nameof(BacklogCompletedTDRangeRecords))]
    public void Backlog_TDCompletedRangeCoversId(int id)
    {
        var backlog = TestPaths.ReadBacklog();
        Assert.True(BacklogCompletedRangeCoversTD(backlog, id),
            $"TD-{id} が backlog の完了済み範囲（TD-A〜TD-B）に含まれていない");
    }

    [Fact]
    public void Backlog_TD59_IsNowCompleted()
    {
        // TD-59a〜TD-59b-5（v2.16.32〜v2.16.39）ですべて完了したため、TD-59b-5 (v2.16.39) で
        // TD-59 は completed range へ移動し open item 表からは外れた。
        // 旧 Backlog_TD59_RemainsOpenItem（元は TD-66〜TD-73 の完了済み範囲チェックそれぞれに
        // 同一の確認が重複していたのを TD-75a で集約したもの）をこのテストへ置き換えた。
        var backlog = TestPaths.ReadBacklog();
        Assert.DoesNotContain("| TD-59 |", backlog);
        Assert.True(BacklogCompletedRangeCoversTD(backlog, 59),
            "TD-59 が backlog の完了済み範囲（TD-A〜TD-B）に含まれていない");
    }

    [Fact]
    public void Backlog_TD75_IsNowCompleted()
    {
        // TD-75a〜TD-75e（v2.16.26〜v2.16.31）ですべて完了したため、TD-75e (v2.16.31) で
        // TD-75 は completed range へ移動し open item 表からは外れた。
        // 旧 Backlog_TD75_RemainsOpenItem（TD-75a で追加）をこのテストへ置き換えた。
        var backlog = TestPaths.ReadBacklog();
        Assert.DoesNotContain("| TD-75 |", backlog);
        Assert.True(BacklogCompletedRangeCoversTD(backlog, 75),
            "TD-75 が backlog の完了済み範囲（TD-A〜TD-B）に含まれていない");
    }

    // ═══════════════════════════════════════════════════════════════════
    // TD-75a-2 (v2.16.27): 移設したが (version, id) の組にならない単独確認
    // 対象の version に backlog ID が付いていない、または backlog 側で
    // 現在も open item のままの確認など、既存の 3 データ群に自然に収まらないもの。
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void ReleaseNotes_Contains_V2101()
    {
        // 移設元: ExpertProposalPlanningTests。v2.10.1 は backlog ID を持たないエントリ。
        Assert.Contains("v2.10.1", TestPaths.ReadReleaseNotes());
    }

    [Fact]
    public void ReleaseNotes_Contains_V21019()
    {
        // 移設元: ExpertProposalPlanningTests。見出しは TD-33 だが、元テストは version のみ確認していた。
        Assert.Contains("v2.10.19", TestPaths.ReadReleaseNotes());
    }

    [Fact]
    public void ReleaseNotes_Contains_V2108()
    {
        // 移設元: PromptStandardContractTests。v2.10.8 は backlog ID を持たないエントリ。
        Assert.Contains("v2.10.8", TestPaths.ReadReleaseNotes());
    }

    [Fact]
    public void Backlog_M15_RemainsOpenItem()
    {
        // 移設元: ExpertProposalPlanningTests。M15 は完了済みではなく現在も open item。
        Assert.Contains("| M15 |", TestPaths.ReadBacklog());
    }

    [Fact]
    public void Backlog_TN7_RemainsOpenItem()
    {
        // 移設元: ExpertProposalPlanningTests。TN-7 は完了済みではなく現在も open item。
        Assert.Contains("| TN-7 |", TestPaths.ReadBacklog());
    }

    [Fact]
    public void Backlog_LK5_RemainsOpenItem()
    {
        // 移設元: ExpertProposalPlanningTests。LK-5 は完了済みではなく現在も open item。
        Assert.Contains("| LK-5 |", TestPaths.ReadBacklog());
    }

    // ═══════════════════════════════════════════════════════════════════
    // 個別内容確認 (維持) — release notes 本文の設計判断・docs の重要語句など、
    // 意図が個別に強いテスト。ガイドライン (static-test-guidelines.md) に基づき、
    // 無理に統合しない。
    // ═══════════════════════════════════════════════════════════════════


    [Fact]
    public void Docs_Record_SH36aDraftWriteSideAndHashMismatchPolicy()
    {
        var releaseNotes = TestPaths.ReadReleaseNotes();
        var backlog = TestPaths.ReadBacklog();
        var planning = File.ReadAllText(Path.Combine(RepoRoot, "docs", "planning", "review6-fable5-3.md"));

        foreach (var term in new[] { "v2.16.43", "SH-36a", "下書き", "ChatNest", "sidecar", "HashMismatch", "隔離", "SH-36b" })
            Assert.Contains(term, releaseNotes + backlog + planning);
        foreach (var term in new[] { "v2.16.44", "SH-36a-1", "TempNest", "Speaker", "sidecar", "回帰" })
            Assert.Contains(term, releaseNotes + backlog + planning);
        foreach (var term in new[] { "v2.16.45", "SH-36a-2", "Speaker", "数値", "自分", "sidecar" })
            Assert.Contains(term, releaseNotes + backlog + planning);
        foreach (var term in new[] { "v2.16.46", "SH-36b", "起動時", "下書き", "復元", "Yes", "No", "Cancel", "ChatNest", "sidecar", "隔離", "SH-36 はこれで完了", "SH-36完了" })
            Assert.Contains(term, releaseNotes + backlog + planning);
        Assert.Contains($"NoteNest schema は `{Project.CurrentSchemaVersion}`", releaseNotes);
    }

    // ── RJ-10: M2 見送り・タスク縮退方針整理 ────────────────────────────────

    [Fact]
    public void Backlog_Contains_RJ10()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.Contains("RJ-10", backlog);
    }

    [Fact]
    public void Backlog_RJ10_MentionsMarkerTaskAndDecline()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        var rj10Line = backlog.Split('\n').FirstOrDefault(l => l.Contains("RJ-10"));
        Assert.NotNull(rj10Line);
        Assert.Contains("マーカー", rj10Line);
        Assert.Contains("タスク", rj10Line);
        Assert.Contains("見送り", rj10Line);
    }

    [Fact]
    public void Backlog_RJ10_MentionsExistingTaskFeatureNotRemoved()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        var rj10Line = backlog.Split('\n').FirstOrDefault(l => l.Contains("RJ-10"));
        Assert.NotNull(rj10Line);
        Assert.Contains("既存タスク機能を今回削除するわけではない", rj10Line);
    }

    [Fact]
    public void Backlog_RJ10_MentionsFutureReconsiderationUsesNewBacklogId()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        var rj10Line = backlog.Split('\n').FirstOrDefault(l => l.Contains("RJ-10"));
        Assert.NotNull(rj10Line);
        Assert.Contains("M2 を復活させず", rj10Line);
        Assert.Contains("新しい backlog ID", rj10Line);
    }

    [Fact]
    public void ReleaseNotes_MentionsM2AsDeclined()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("M2", text);
        // 完了ではなく見送り（取り下げ）であることが分かる文言を確認する
        var m2Mentions = text.Split('\n').Where(l => l.Contains("M2")).ToList();
        Assert.Contains(m2Mentions, l => l.Contains("取り下げ") || l.Contains("見送り"));
    }

    // ── TD-66: タブ変更時の session 随時保存 ────────────────────────────────

    [Fact]
    public void ReleaseNotes_TD66_MentionsR6AndSessionFormatUnchanged()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var td66Section = ExtractReleaseNotesSection(text, "TD-66");

        Assert.Contains("R-6", td66Section);
        Assert.Contains("session 形式", td66Section);
    }

    // ── TD-67: 複数ファイルオープン失敗通知の改善 ──────────────────────────

    [Fact]
    public void ReleaseNotes_TD67_MentionsR7AndNoFormatChange()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var td67Section = ExtractReleaseNotesSection(text, "TD-67");

        Assert.Contains("R-7", td67Section);
        Assert.Contains("保存形式", td67Section);
    }

    // ── TD-68: Tabs[].WorkspaceKind の用途明文化・テスト固定 ─────────────────

    [Fact]
    public void ReleaseNotes_TD68_MentionsR8AndUiHintNotTrustSource()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var td68Section = ExtractReleaseNotesSection(text, "TD-68");

        Assert.Contains("R-8", td68Section);
        Assert.Contains("ヒント", td68Section);
        Assert.Contains("信頼ソース", td68Section);
    }

    // ── TD-69: FilePaths を Tabs から導出する統一 ───────────────────────────

    [Fact]
    public void ReleaseNotes_TD69_MentionsR14AndDerivedFilePaths()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var td69Section = ExtractReleaseNotesSection(text, "TD-69");

        Assert.Contains("R-14", td69Section);
        Assert.Contains("導出", td69Section);
    }

    // ── TD-70: pending entry の再試行解除手段 ───────────────────────────────

    [Fact]
    public void ReleaseNotes_TD70_MentionsFileNotFoundAndUserConfirmation()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var td70Section = ExtractReleaseNotesSection(text, "TD-70");

        Assert.Contains("FileNotFound", td70Section);
        Assert.Contains("利用者", td70Section);
    }

    // ── TD-71: review2 小リスク②③の案内・設計記録 ───────────────────────────

    [Fact]
    public void ReleaseNotes_TD71_MentionsBakHintAndActiveFilePathAsymmetry()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var td71Section = ExtractReleaseNotesSection(text, "TD-71");

        Assert.Contains(".bak", td71Section);
        Assert.Contains("ActiveFilePath", td71Section);
    }

    [Fact]
    public void DesignDecisions_RecordsActiveFilePathFreshnessAsymmetry()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "design", "design-decisions.md"));
        Assert.Contains("ActiveFilePath", text);
        Assert.Contains("鮮度非対称", text);
        Assert.Contains("TD-71", text);
    }

    // ── TD-72: review3 後の docs 整理 ───────────────────────────────────────

    [Fact]
    public void ReleaseNotes_TD72_MentionsLT4AndUserGuideAndBacklogRecording()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var td72Section = ExtractReleaseNotesSection(text, "TD-72");

        Assert.Contains("LT-4", td72Section);
        Assert.Contains("ユーザーガイド", td72Section);
    }

    [Fact]
    public void DesignDecisions_RecordsLT4WindowsSectionAndDetachedGeometryPolicy()
    {
        // review3 の通常エンジニア向け作業: LT-4 方針メモ。文言完全一致ではなく重要語句の存在確認に留める。
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "design", "design-decisions.md"));
        Assert.Contains("LT-4", text);
        Assert.Contains("Windows[]", text);
        Assert.Contains("Tabs[]", text);
        Assert.Contains("session", text);
    }

    [Fact]
    public void UserGuide_MentionsStoppingRetryForMissingFiles()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "guide", "nestsuite-user-guide.md"));
        Assert.Contains("再試行", text);
        Assert.Contains("止め", text);
    }

    [Fact]
    public void Backlog_RecordsReview3FutureCandidate_SH35_AsOpenItem()
    {
        // review3 新リスク②を、実装せず将来候補として backlog に記録したことを確認する。
        // 採番は SH-32/SH-33（既に v2.14.11/v2.14.12 で使用済み）と衝突したため SH-34/SH-35 を使った。
        // SH-34（新リスク①）は v2.16.21 SH-34 として実装済みのため、この時点では SH-35 のみ open item。
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.Contains("| SH-35 |", backlog);
    }

    // ── SH-34: 復元失敗通知と FileNotFound 再試行解除確認の1ダイアログ統合 ───────────

    [Fact]
    public void ReleaseNotes_SH34_MentionsOneDialogAndFileNotFound()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var sh34Section = ExtractReleaseNotesSection(text, "SH-34");

        Assert.Contains("1つの", sh34Section);
        Assert.Contains("FileNotFound", sh34Section);
    }

    [Fact]
    public void Backlog_MentionsSH34InCompletedRange()
    {
        // SH の完了済み欠番は TD のような数値範囲表記ではなく個別文で記録されているため、
        // BacklogCompletedRangeCoversTD の対象には含めず、この専用チェックで維持する。
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.Contains("SH-34", backlog);
        Assert.Contains("SH-34 は v2.16.21", backlog);
    }

    [Fact]
    public void DesignDecisions_RecordsSessionSnapshotAndPreScanPolicy()
    {
        // review4 の LT-9 設計判断: session snapshot を持たない・失敗理由は保存しない・
        // 事前スキャンしない方針。文言完全一致ではなく重要語句の存在確認に留める。
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "design", "design-decisions.md"));
        Assert.Contains("LT-9", text);
        Assert.Contains("snapshot", text);
        Assert.Contains("事前スキャン", text);
    }

    [Fact]
    public void UserGuide_DoesNotClaimSeparateDialogAfterNotification()
    {
        // 旧文言「復元失敗通知のあとに」（別ダイアログを示唆）が残っていないことを確認する。
        // 1 ダイアログ統合後は「復元失敗通知の画面で」に更新済み。
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "guide", "nestsuite-user-guide.md"));
        Assert.DoesNotContain("復元失敗通知のあとに", text);
        Assert.Contains("再試行", text);
    }

    // ── SH-35-docs: 復元失敗が続く場合の案内追記 ─────────────────────────────

    [Fact]
    public void UserGuide_ExplainsNonFileNotFoundRestoreFailures()
    {
        // FileNotFound 以外（形式判定不能・アクセス不可等）の復元失敗は現時点で解除対象外であることの説明。
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "guide", "nestsuite-user-guide.md"));
        Assert.Contains("ファイル形式を判定できない", text);
        Assert.Contains("アクセス", text);
    }

    [Fact]
    public void UserGuide_MentionsBakRestoreGuidanceForCorruptedFiles()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "guide", "nestsuite-user-guide.md"));
        Assert.Contains(".bak", text);
        Assert.Contains("破損が疑われる", text);
    }

    [Fact]
    public void UserGuide_MentionsMovingOrDeletingToBecomeFileNotFound()
    {
        // 不要なファイルを移動・削除すれば次回 FileNotFound として扱われ、
        // SH-34 の「次回から再試行しない」で解除できるという間接経路の記載。
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "guide", "nestsuite-user-guide.md"));
        Assert.Contains("移動または削除", text);
        Assert.Contains("ファイルが見つからない", text);
    }

    [Fact]
    public void Backlog_SH35_RemainsOpenItem_AndIsNotMarkedComplete()
    {
        // SH-35 は今回 docs 対応のみで、解除対象拡張・個別解除の実装は行っていない。
        // open item のまま残し、完了済み欠番リストへは追加しない。
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.Contains("| SH-35 |", backlog);
        Assert.DoesNotContain("SH-35 は v2.16.22", backlog);
    }

    // ── TD-73: 静的テスト持続可能性ガイドライン ─────────────────────────────

    [Fact]
    public void StaticTestGuidelines_FileExists()
    {
        var path = Path.Combine(RepoRoot, "docs", "development", "static-test-guidelines.md");
        Assert.True(File.Exists(path), $"static-test-guidelines.md not found: {path}");
    }

    [Fact]
    public void StaticTestGuidelines_MentionsKeyConcepts()
    {
        // 文言完全一致ではなく、重要語句の存在確認に留める（ガイドライン自体が推奨する方針を、
        // このテスト自身にも適用する）。
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "development", "static-test-guidelines.md"));
        Assert.Contains("docs-contract", text);
        Assert.Contains("見出しアンカー", text);
        Assert.Contains("CRLF", text);
        Assert.Contains("bare", text);
        Assert.Contains("helper", text);
    }

    // ── LT-9: フェーズ2設計判断の backlog 反映 ────────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21624()
    {
        Assert.Contains("v2.16.24", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_LT9Phase2()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("LT-9", text);
        Assert.Contains("フェーズ2", text);
    }

    [Fact]
    public void ReleaseNotesSection_LT9_MentionsDesignHeldNotImplemented()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var section = ExtractReleaseNotesSection(text, "LT-9");
        Assert.Contains("フェーズ2", section);
        Assert.Contains("着手トリガー", section);
        Assert.Contains("実装しない", section);
    }

    [Fact]
    public void Backlog_LT9_MentionsPhase2LaunchTriggers()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.Contains("着手トリガー", backlog);
        Assert.Contains("all-or-nothing", backlog);
    }

    [Fact]
    public void Backlog_LT9_MentionsTwoOrMoreFailuresCondition()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.Contains("2 件以上の場合のみ", backlog);
    }

    [Fact]
    public void Backlog_LT9_MentionsSingleFailureKeepsSH34Behavior()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.Contains("1 件の場合は現行 SH-34 の挙動を維持", backlog);
    }

    [Fact]
    public void Backlog_LT9_ConfirmsSessionJsonFormatChangeNotNeeded()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.Contains("session.json", backlog);
        Assert.Contains("形式変更は不要", backlog);
    }

    [Fact]
    public void Backlog_SH35_StillRemainsOpenItem()
    {
        // LT-9 フェーズ2に解除対象拡張・個別解除を吸収する方針を記録したが、
        // SH-35 自体は完了扱いにせず open item として残す。
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.Contains("| SH-35 |", backlog);
    }

    // ── TD-74: 既存静的テスト棚卸しレビュー ─────────────────────────────────

    [Fact]
    public void StaticTestInventoryReview_ExistsAndMentionsClassifications()
    {
        // 文言完全一致ではなく、5 分類の重要語句の存在確認に留める。
        var path = Path.Combine(RepoRoot, "docs", "planning", "static-test-inventory-review.md");
        Assert.True(File.Exists(path), $"static-test-inventory-review.md not found: {path}");
        var text = File.ReadAllText(path);
        Assert.Contains("維持", text);
        Assert.Contains("挙動テスト化", text);
        Assert.Contains("削除候補", text);
    }

    // ── TD-75a-2: 散在 docs-contract test の集約 ─────────────────────────────

    // TD-75e (v2.16.31) fix: TD-75 完了に伴い backlog.md の TD-75 行自体を削除したため
    // （TD-33 の運用方針: 完了済み項目は backlog.md に残さない）、この節にあった
    // Backlog_TD75_MentionsTD75xCompletion（backlog 側で各サブタスクの完了記録と
    // "| TD-75 |" open item 行の存在を確認していた）4 件は、以後恒久的に成立しない
    // 前提を検証する形になっていた（CI で実際に FAIL した）。各サブタスクの完了記録は
    // 下の ReleaseNotes_Contains_V216XX_AndTD75X が release notes 側で引き続き保証しており、
    // "| TD-75 |" が open item として残っていないことは Backlog_TD75_IsNowCompleted が
    // 保証するため、削除しても検証内容は失われていない。

    [Fact]
    public void ReleaseNotes_Contains_V21627_AndTD75a2()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("v2.16.27", text);
        Assert.Contains("TD-75a-2", text);
    }

    // ── TD-75b: 静的確認 2 件の挙動テスト化 ─────────────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21628_AndTD75b()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("v2.16.28", text);
        Assert.Contains("TD-75b", text);
    }

    // ── TD-75c: test-classification-analysis.md の位置づけ整理 ──────────────

    [Fact]
    public void ReleaseNotes_Contains_V21629_AndTD75c()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("v2.16.29", text);
        Assert.Contains("TD-75c", text);
    }

    // ── TD-75d: 削除候補テストの削除判断レビュー ─────────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21630_AndTD75d()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("v2.16.30", text);
        Assert.Contains("TD-75d", text);
    }

    [Fact]
    public void StaticTestDeletionCandidateReview_ExistsAndMentionsJudgments()
    {
        // 文言完全一致ではなく、4 分類と「削除・skip を行っていない」旨の重要語句確認に留める。
        var path = Path.Combine(RepoRoot, "docs", "planning", "static-test-deletion-candidate-review.md");
        Assert.True(File.Exists(path), $"static-test-deletion-candidate-review.md not found: {path}");
        var text = File.ReadAllText(path);
        Assert.Contains("Delete candidate", text);
        Assert.Contains("Keep", text);
        Assert.Contains("Replace then delete", text);
        Assert.Contains("Defer", text);
        Assert.Contains("削除・skip は 1 件も行っていない", text);
    }

    // ── TD-75e: Delete candidate 10 件の実削除 ─────────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21631_AndTD75e()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("v2.16.31", text);
        Assert.Contains("TD-75e", text);
    }

    [Fact]
    public void Backlog_TD75_MentionsTD75eCompletion()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.Contains("TD-64〜TD-75", backlog);
    }

    [Fact]
    public void StaticTestDeletionCandidateReview_MentionsTD75eImplementationResult()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "planning", "static-test-deletion-candidate-review.md"));
        Assert.Contains("実施結果", text);
        Assert.Contains("TD-75e", text);
    }

    // ── TD-59a: .nestsuite 二重読込解消の設計レビュー ─────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21632_AndTD59a()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("v2.16.32", text);
        Assert.Contains("TD-59a", text);
    }

    // v2.16.39 TD-59b-5 fix: TD-59 完了に伴い backlog.md の TD-59 行自体を削除したため
    // （TD-33 の運用方針: 完了済み項目は backlog.md に残さない。TD-75a-2 で TD-75 系に適用したのと
    // 同じ整理）、この節にあった Backlog_TD59_MentionsTD59aCompletion_AndRemainsOpenItem /
    // Backlog_TD59_MentionsTD59a2_AndRemainsOpenItem / Backlog_TD59_MentionsTD59b1_AndRemainsOpenItem /
    // Backlog_TD59_MentionsTD59b2_AndRemainsOpenItem / Backlog_TD59_MentionsTD59b22_AndRemainsOpenItem /
    // Backlog_TD59_MentionsTD59b3_AndRemainsOpenItem / Backlog_TD59_MentionsTD59b4_AndRemainsOpenItem /
    // Backlog_TD59_MentionsAllUserFacingPathsNowReadOnce（8 件）は、以後恒久的に成立しない前提
    // （"| TD-59 |" が open item として残っている・特定の暫定文言が backlog に残っている）を検証する
    // 形になっていた。各サブタスクの完了記録は下の ReleaseNotes_Contains_V216XX_AndTD59XX が
    // release notes 側（不変の履歴）で引き続き保証しており、"| TD-59 |" が open item として
    // 残っていないことは Backlog_TD59_IsNowCompleted が保証するため、削除しても検証内容は
    // 失われていない。

    [Fact]
    public void DoubleReadDesignReview_ExistsAndMentionsKeyDecisions()
    {
        // 文言完全一致ではなく、読込経路・採用案・TOCTOU・kind 検証・後続実装・
        // production code 未変更の重要語句の存在確認に留める。
        var path = Path.Combine(RepoRoot, "docs", "planning", "nestsuite-double-read-design-review.md");
        Assert.True(File.Exists(path), $"nestsuite-double-read-design-review.md not found: {path}");
        var text = File.ReadAllText(path);
        Assert.Contains("読込経路一覧", text);
        Assert.Contains("採用案", text);
        Assert.Contains("TOCTOU", text);
        Assert.Contains("EnsureKind", text);
        Assert.Contains("TD-59b", text);
        Assert.Contains("production code の変更は行っていない", text);
    }

    // ── TD-59a-2: .nestsuite 二重読込解消設計の安全性補足 ──────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21633_AndTD59a2()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("v2.16.33", text);
        Assert.Contains("TD-59a-2", text);
    }

    [Fact]
    public void DoubleReadDesignReview_MentionsSafetySupplementKeyDecisions()
    {
        // TD-59a-2 の安全性補足: path と解析済み内容の対応保証・同種ファイル間の取り違え・
        // IsSameFile・test seam（読込 delegate）の重要語句確認に留める。
        var text = File.ReadAllText(Path.Combine(
            RepoRoot, "docs", "planning", "nestsuite-double-read-design-review.md"));
        Assert.Contains("path と解析済み内容の対応保証", text);
        Assert.Contains("同種ファイル間の取り違え", text);
        Assert.Contains("IsSameFile", text);
        Assert.Contains("読込回数テスト用 seam", text);
        Assert.Contains("LoadPrepared", text);
        Assert.Contains("PreloadedWorkspaceEnvelope", text);
    }

    // ── TD-59b-1: .nestsuite 基盤API・context・test seam実装 ──────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21634_AndTD59b1()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("v2.16.34", text);
        Assert.Contains("TD-59b-1", text);
    }

    [Fact]
    public void ReleaseNotes_TD59b1_MentionsFileServiceAndShellNotYetSwitched()
    {
        // 実装範囲の境界（FileService / Shell / session 未切替、実利用読込回数は未削減）が
        // 記録されていることを確認する。文言完全一致ではなくキーワードのみ確認する。
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("LoadPrepared", text);
        Assert.Contains("読込回数はまだ減っていない", text);
    }

    [Fact]
    public void DoubleReadDesignReview_MentionsTD59b1ImplementationResult()
    {
        // §19 実施結果: 基盤 API 実装済み・FileService/Shell/session 未切替の重要語句確認に留める。
        var text = File.ReadAllText(Path.Combine(
            RepoRoot, "docs", "planning", "nestsuite-double-read-design-review.md"));
        Assert.Contains("実施結果", text);
        Assert.Contains("ReadFromFile", text);
        Assert.Contains("TryPrepareOpen", text);
        Assert.Contains("FromResolvedKind", text);
        Assert.Contains("WorkspaceFileOpenContext", text);
    }

    // ── TD-59b-2: FileService LoadPrepared・安全性テスト実装 ──────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21635_AndTD59b2()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("v2.16.35", text);
        Assert.Contains("TD-59b-2", text);
    }

    [Fact]
    public void ReleaseNotes_TD59b2_MentionsSafetyGuardsAndShellNotYetSwitched()
    {
        // path 一致再検証・同種ファイル取り違え・kind 誤配線・Shell 未切替・実利用読込回数未削減が
        // 記録されていることを確認する。文言完全一致ではなくキーワードのみ確認する。
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("IsSameFile", text);
        Assert.Contains("同種ファイル", text);
        Assert.Contains("誤配線", text);
        Assert.Contains("OpenPrepared", text);
        Assert.Contains("読込回数はまだ減っていない", text);
    }

    [Fact]
    public void DoubleReadDesignReview_MentionsTD59b2ImplementationResult()
    {
        // §20 実施結果: LoadPrepared 実装済み・安全性テスト実施済み・Shell/session 未切替の
        // 重要語句確認に留める。
        var text = File.ReadAllText(Path.Combine(
            RepoRoot, "docs", "planning", "nestsuite-double-read-design-review.md"));
        Assert.Contains("実施結果（TD-59b-2", text);
        Assert.Contains("LoadPrepared", text);
        Assert.Contains("OpenPrepared", text);
        Assert.Contains("TD-59b-3", text);
    }

    // ── TD-59b-2-2: レガシー prepared 拡張子ガード補完 ─────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21636_AndTD59b22()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("v2.16.36", text);
        Assert.Contains("TD-59b-2-2", text);
    }

    [Fact]
    public void ReleaseNotes_TD59b22_MentionsExtensionGuardAndFileIOBeforeThrow()
    {
        // レガシー拡張子ガード・ArgumentException・ファイル I/O 前の失敗が記録されていることを
        // 確認する。文言完全一致ではなくキーワードのみ確認する。
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("レガシー", text);
        Assert.Contains("拡張子", text);
        Assert.Contains("ArgumentException", text);
        Assert.Contains("ファイル I/O 前", text);
    }

    [Fact]
    public void DoubleReadDesignReview_MentionsTD59b22ImplementationResult()
    {
        // §21 実施結果: レガシー拡張子ガード補完済み・Shell/session 未切替の重要語句確認に留める。
        var text = File.ReadAllText(Path.Combine(
            RepoRoot, "docs", "planning", "nestsuite-double-read-design-review.md"));
        Assert.Contains("実施結果（TD-59b-2-2", text);
        Assert.Contains("レガシー", text);
        Assert.Contains("拡張子", text);
        Assert.Contains("ArgumentException", text);
    }

    // ── TD-59b-3: Shell読込経路のprepared context切替 ──────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21637_AndTD59b3()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("v2.16.37", text);
        Assert.Contains("TD-59b-3", text);
    }

    [Fact]
    public void ReleaseNotes_TD59b3_MentionsOpenContextLoadPreparedFromResolvedKindAndReadCountReduction()
    {
        // OpenContext・LoadPrepared・FromResolvedKind・読込1回化・session 復元未切替が
        // 記録されていることを確認する。文言完全一致ではなくキーワードのみ確認する。
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("OpenContext", text);
        Assert.Contains("LoadPrepared", text);
        Assert.Contains("FromResolvedKind", text);
        Assert.Contains("読込回数が", text);
        Assert.Contains("session 復元は今回変更していない", text);
    }

    [Fact]
    public void DoubleReadDesignReview_MentionsTD59b3ImplementationResult()
    {
        // §22 実施結果: OpenContext・TryPrepareOpen・LoadPrepared・FromResolvedKind・
        // TD-59b-4 への言及の重要語句確認に留める。
        var text = File.ReadAllText(Path.Combine(
            RepoRoot, "docs", "planning", "nestsuite-double-read-design-review.md"));
        Assert.Contains("実施結果（TD-59b-3", text);
        Assert.Contains("OpenContext", text);
        Assert.Contains("TryPrepareOpen", text);
        Assert.Contains("LoadPrepared", text);
        Assert.Contains("FromResolvedKind", text);
        Assert.Contains("TD-59b-4", text);
    }

    // ── TD-59b-4: session復元経路のprepared context切替 ──────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21638_AndTD59b4()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("v2.16.38", text);
        Assert.Contains("TD-59b-4", text);
    }

    [Fact]
    public void ReleaseNotes_TD59b4_MentionsSessionRestoreTargetOpenContextAndTryPrepareOpen()
    {
        // SessionRestoreTarget・OpenContext・TryPrepareOpen への切替が記録されていることを
        // 確認する。文言完全一致ではなくキーワードのみ確認する。
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("SessionRestoreTarget", text);
        Assert.Contains("OpenContext", text);
        Assert.Contains("TryPrepareOpen", text);
    }

    [Fact]
    public void ReleaseNotes_TD59b4_MentionsReadCountReductionAndDetectKindSeamRemoval()
    {
        // session 復元の読込 1 回化・detectKind seam 撤去・旧 path loader 撤去・
        // session.json 不変が記録されていることを確認する。
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var section = ExtractReleaseNotesSection(text, "TD-59b-4");
        Assert.Contains("読込回数", section);
        Assert.Contains("detectKind", section);
        Assert.Contains("撤去", section);
        Assert.Contains("session.json 形式", section);
        Assert.Contains("TD-59b-5", section);
    }

    [Fact]
    public void DoubleReadDesignReview_MentionsTD59b4ImplementationResult()
    {
        // §23 実施結果: SessionRestoreTarget・OpenContext・TryPrepareOpen・detectKind 撤去・
        // 旧 path loader 撤去・TD-59 未完了の重要語句確認に留める。
        var text = File.ReadAllText(Path.Combine(
            RepoRoot, "docs", "planning", "nestsuite-double-read-design-review.md"));
        Assert.Contains("実施結果（TD-59b-4", text);
        Assert.Contains("SessionRestoreTarget", text);
        Assert.Contains("OpenContext", text);
        Assert.Contains("TryPrepareOpen", text);
        Assert.Contains("detectKind", text);
        Assert.Contains("撤去", text);
        Assert.Contains("TD-59 は引き続き未完了", text);
    }

    // ── TD-59b-5: 保存後内部同期の非読込化・全経路最終回帰（TD-59 完了） ─────────

    [Fact]
    public void ReleaseNotes_Contains_V21639_AndTD59b5()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("v2.16.39", text);
        Assert.Contains("TD-59b-5", text);
    }

    [Fact]
    public void ReleaseNotes_TD59b5_MentionsNonReadingSavePathSyncAndFromResolvedKind()
    {
        // 保存後内部同期・NoteNest VM 同期の非読込化・FromResolvedKind への切替が
        // 記録されていることを確認する。文言完全一致ではなくキーワードのみ確認する。
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var section = ExtractReleaseNotesSection(text, "TD-59b-5");
        Assert.Contains("保存後", section);
        Assert.Contains("非読込化", section);
        Assert.Contains("FromResolvedKind", section);
        Assert.Contains("IsPathCompatibleWithResolvedKind", section);
    }

    [Fact]
    public void ReleaseNotes_TD59b5_MentionsAllOpenPathsReadOnceAndTD59Completed()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var section = ExtractReleaseNotesSection(text, "TD-59b-5");
        Assert.Contains("Open 経路", section);
        Assert.Contains("読込は 1 回", section);
        Assert.Contains("TD-59 を完了した", section);
        Assert.Contains($"NoteNest schema（`{Project.CurrentSchemaVersion}`）", section);
    }

    [Fact]
    public void Backlog_TD59_CompletedRangeCoversId_AndHasNoOpenItemRow()
    {
        var backlog = TestPaths.ReadBacklog();
        Assert.True(BacklogCompletedRangeCoversTD(backlog, 59),
            "TD-59 が backlog の完了済み範囲（TD-A〜TD-B）に含まれていない");
        Assert.DoesNotContain("| TD-59 |", backlog);
    }

    [Fact]
    public void DoubleReadDesignReview_MentionsTD59b5ImplementationResult_AndTD59Completion()
    {
        // §24 実施結果: IsPathCompatibleWithResolvedKind・SavedWorkspaceStateUpdater/
        // SyncNoteNestTabForViewModel の非読込化・TD-59 完了の重要語句確認に留める。
        var text = File.ReadAllText(Path.Combine(
            RepoRoot, "docs", "planning", "nestsuite-double-read-design-review.md"));
        Assert.Contains("実施結果（TD-59b-5", text);
        Assert.Contains("IsPathCompatibleWithResolvedKind", text);
        Assert.Contains("SavedWorkspaceStateUpdater", text);
        Assert.Contains("SyncNoteNestTabForViewModel", text);
        Assert.Contains("TD-59 完了", text);
    }

    // ── review6-fable5: TD-59完了後の高リスク・高効果設計課題再評価 ─────────────

    [Fact]
    public void ReleaseNotes_Contains_V21640_AndReview6()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("v2.16.40", text);
        Assert.Contains("review6-fable5", text);
    }

    [Fact]
    public void ReleaseNotes_Review6_MentionsNoProductionCodeChangeAndCandidateSelection()
    {
        // production code 変更なし・通常実装候補の絞り込み・最優先候補の確定・
        // LT-9 継続保留・schema 維持が記録されていることを確認する。
        // 候補名の詳細や文章全文には固定しない（TD-73 ガイドライン）。
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var section = ExtractReleaseNotesSection(text, "review6-fable5");
        Assert.Contains("production code の変更はなし", section);
        Assert.Contains("通常実装候補", section);
        Assert.Contains("最優先候補", section);
        Assert.Contains("LT-9", section);
        Assert.Contains($"`{Project.CurrentSchemaVersion}`", section);
    }

    [Fact]
    public void Review6Doc_ExistsAndMentionsKeySections()
    {
        // 文言完全一致ではなく、章立ての重要語句と結論系キーワードの存在確認に留める。
        var path = Path.Combine(RepoRoot, "docs", "planning", "review6-fable5.md");
        Assert.True(File.Exists(path), $"review6-fable5.md not found: {path}");
        var text = File.ReadAllText(path);
        Assert.Contains("総評", text);
        Assert.Contains("backlog再評価", text);
        Assert.Contains("最優先候補", text);
        Assert.Contains("LT-9", text);
        Assert.Contains("結論", text);
        Assert.Contains("production code の変更は行っていない", text);
    }

    [Fact]
    public void Backlog_SH36_IsRemovedAfterCompletion()
    {
        var backlog = TestPaths.ReadBacklog();
        Assert.DoesNotContain("| SH-36 |", backlog);
        var releaseNotes = TestPaths.ReadReleaseNotes();
        Assert.Contains("SH-36b", releaseNotes);
        Assert.Contains("SH-36 はこれで完了", releaseNotes);
    }

    // ── review6-fable5-2: SH-36下書き保護の設計補完 ─────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21641_AndReview6_2()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("v2.16.41", text);
        Assert.Contains("review6-fable5-2", text);
    }

    [Fact]
    public void ReleaseNotes_Review6_2_MentionsChatNestTransientInputAndQuarantine()
    {
        // ChatNest の未送信入力の保護・下書き・隔離・production code 変更なし・schema 維持が
        // 記録されていることを確認する。ファイル名・API 名・文章全文には固定しない。
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var section = ExtractReleaseNotesSection(text, "review6-fable5-2");
        Assert.Contains("production code の変更はなし", section);
        Assert.Contains("ChatNest", section);
        Assert.Contains("未送信", section);
        Assert.Contains("下書き", section);
        Assert.Contains("隔離", section);
        Assert.Contains("SH-36", section);
        Assert.Contains($"`{Project.CurrentSchemaVersion}`", section);
    }

    [Fact]
    public void Review6_2Doc_ExistsAndMentionsKeySections()
    {
        // 文言完全一致ではなく、章立ての重要語句の存在確認に留める。
        var path = Path.Combine(RepoRoot, "docs", "planning", "review6-fable5-2.md");
        Assert.True(File.Exists(path), $"review6-fable5-2.md not found: {path}");
        var text = File.ReadAllText(path);
        Assert.Contains("総評", text);
        Assert.Contains("InputText", text);
        Assert.Contains("隔離", text);
        Assert.Contains("結論", text);
        Assert.Contains("production code の変更は行っていない", text);
    }

    [Fact]
    public void Review6Doc_PointsToReview6_2_AsAuthoritativeForSH36()
    {
        // 初期設計（review6）と補完後設計（review6-fable5-2）の関係が review6 側に記録されている。
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "planning", "review6-fable5.md"));
        Assert.Contains("review6-fable5-2", text);
        Assert.Contains("正とする", text);
    }

    // ── review6-fable5-3: SH-36復元後ライフサイクル設計補完 ─────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21642_AndReview6_3()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("v2.16.42", text);
        Assert.Contains("review6-fable5-3", text);
    }

    [Fact]
    public void ReleaseNotes_Review6_3_MentionsPostRestoreLifecycleAndSidecarClassification()
    {
        // 復元後ライフサイクル・dirty・下書き・sidecar 分類・production code 変更なし・
        // schema 維持が記録されていることを確認する。API 名・文言全文・後続バージョン一覧には固定しない。
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var section = ExtractReleaseNotesSection(text, "review6-fable5-3");
        Assert.Contains("production code の変更はなし", section);
        Assert.Contains("SH-36", section);
        Assert.Contains("復元", section);
        Assert.Contains("dirty", section);
        Assert.Contains("下書き", section);
        Assert.Contains("sidecar", section);
        Assert.Contains($"`{Project.CurrentSchemaVersion}`", section);
    }

    [Fact]
    public void Review6_3Doc_ExistsAndMentionsKeySections()
    {
        // 文言完全一致ではなく、章立ての重要語句の存在確認に留める。
        var path = Path.Combine(RepoRoot, "docs", "planning", "review6-fable5-3.md");
        Assert.True(File.Exists(path), $"review6-fable5-3.md not found: {path}");
        var text = File.ReadAllText(path);
        Assert.Contains("総評", text);
        Assert.Contains("保護継続", text);
        Assert.Contains("dirty", text);
        Assert.Contains("sidecar", text);
        Assert.Contains("結論", text);
        Assert.Contains("production code の変更は行っていない", text);
    }

    [Fact]
    public void Review6_2Doc_PointsToReview6_3_AsAuthoritativeForSH36()
    {
        // review6-fable5-2 の冒頭注記が最新正本（review6-fable5-3）を参照している
        // （本文の初期判断は履歴として保持したまま）。
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "planning", "review6-fable5-2.md"));
        Assert.Contains("review6-fable5-3", text);
        Assert.Contains("正とする", text);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// v2.16.15 TD-67 fix: 「TD-1〜TD-58、TD-60〜TD-63、TD-64〜TD-67 は完了済み（欠番）。」
    /// のような行から "TD-A" / "TD-A〜TD-B" の範囲をすべて抽出し、id がいずれかの範囲に
    /// 含まれるかを判定する。将来さらに範囲が圧縮されて id の数字がリテラルとして
    /// 残らなくなっても（例: TD-64〜TD-67 → TD-64〜TD-68）判定が壊れないようにするための helper。
    /// </summary>
    private static bool BacklogCompletedRangeCoversTD(string backlog, int id)
    {
        var line = backlog
            .Split('\n')
            .FirstOrDefault(l => l.Contains("は完了済み（欠番）") && l.Contains("TD-"));
        if (line == null) return false;

        foreach (Match m in Regex.Matches(line, @"TD-(\d+)(?:〜TD-(\d+))?"))
        {
            var start = int.Parse(m.Groups[1].Value);
            var end = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : start;
            if (id >= start && id <= end) return true;
        }

        return false;
    }

    /// <summary>
    /// v2.16.19 TD-71 fix: 個々の release notes セクションを、見出し内の bare "TD-NN" 文字列
    /// ではなく「— TD-NN:」という見出し専用の並びで探す。後発バージョンの箇条書きが
    /// 「TD-66 以降、...」のように以前の ID を本文中で言及すると、新しいセクション（ファイル上で
    /// 手前）の本文が先に IndexOf("TD-66") にヒットしてしまい、対象セクションを誤検出する
    /// （TD-71 のリリースノート本文が "TD-66" を含んだことで実際に発生した）。
    /// 見出し行は "## vX.Y.Z — TD-NN: タイトル" の形式に統一されているため、
    /// "— TD-NN:" は本文中の言及とほぼ衝突しない。
    /// </summary>
    private static string ExtractReleaseNotesSection(string releaseNotesText, string id)
    {
        var headingAnchor = $"— {id}:";
        var idx = releaseNotesText.IndexOf(headingAnchor, StringComparison.Ordinal);
        Assert.True(idx >= 0, $"release notes に見出し '{headingAnchor}' が見つからない");

        var section = releaseNotesText.Substring(idx);
        var nextSectionIdx = section.IndexOf("\n## ", 1, StringComparison.Ordinal);
        return nextSectionIdx > 0 ? section.Substring(0, nextSectionIdx) : section;
    }
}
