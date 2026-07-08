using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// docs/backlog.md と docs/release-notes.md の内容に関する契約テスト。
/// 完了済み項目が release-notes に記録されていることを静的に確認する。
/// </summary>
public class NestSuiteDocsContractTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    // ── TD-33: 完了済み項目は release-notes.md で管理 ────────────────────

    [Fact]
    public void Backlog_TD23_IsMarkedComplete()
    {
        Assert.Contains("TD-23", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_V2_10_10()
    {
        var path = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(path));
        Assert.Contains("v2.10.10", File.ReadAllText(path));
    }

    // ── SH-25: Shell 上部バー削除・メニュー導線整理 ──────────────────────

    [Fact]
    public void ReleaseNotes_Contains_SH25()
    {
        Assert.Contains("SH-25", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_V21021()
    {
        Assert.Contains("v2.10.21", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    // ── ID-14: IdeaNest 新規カードのサンプル表示削減 ──────────────────────

    [Fact]
    public void ReleaseNotes_Contains_ID14()
    {
        Assert.Contains("ID-14", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_V21022()
    {
        Assert.Contains("v2.10.22", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    // ── SH-28: 直近操作の一時フィードバック統一 ──────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_SH28()
    {
        Assert.Contains("SH-28", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_V2165()
    {
        Assert.Contains("v2.16.5", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void Backlog_DoesNotContain_SH28AsOpenItem()
    {
        // SH-15 / SH-19 と同様、完了済み ID は「実装済み（欠番）」の注記としてのみ残り、
        // No/概要/優先度を伴う表の行としては残らない（完了済み項目は backlog.md に残さない）。
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.DoesNotContain("| SH-28 |", backlog);
    }

    // ── TD-64: 自動保存では .bak を更新しない ─────────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_TD64()
    {
        Assert.Contains("TD-64", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_V2166()
    {
        Assert.Contains("v2.16.6", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void Backlog_DoesNotContain_TD64AsOpenItem()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.DoesNotContain("| TD-64 |", backlog);
    }

    // ── TD-65: session 復元失敗 entry の持ち越し・破損 session 診断 ────────

    [Fact]
    public void ReleaseNotes_Contains_TD65()
    {
        Assert.Contains("TD-65", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_V2167()
    {
        Assert.Contains("v2.16.7", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void Backlog_DoesNotContain_TD65AsOpenItem()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.DoesNotContain("| TD-65 |", backlog);
    }

    // ── L20 + L8: `.bak` 復元導線の追加 ────────────────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_L20()
    {
        Assert.Contains("L20", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_L8()
    {
        Assert.Contains("L8", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_V2168()
    {
        Assert.Contains("v2.16.8", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void Backlog_DoesNotContain_L8AsOpenItem()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.DoesNotContain("| L8 |", backlog);
    }

    [Fact]
    public void Backlog_DoesNotContain_L20AsOpenItem()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.DoesNotContain("| L20 |", backlog);
    }

    // ── SH-29: 未保存タブ終了確認への件数サマリ追加 ────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_SH29()
    {
        Assert.Contains("SH-29", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_V2169()
    {
        Assert.Contains("v2.16.9", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void Backlog_DoesNotContain_SH29AsOpenItem()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.DoesNotContain("| SH-29 |", backlog);
    }

    // ── SH-30: Shell コマンドの有効/無効理由ツールチップ統一 ────────────────
    // 注意: SH-30 という ID は v2.13.3（Shell ステータスバー同期修正）でも使われている。
    // 単純な "SH-30" だけの Contains では旧エントリと区別できないため、
    // このバージョンでは新エントリの見出し文字列そのものを確認する。

    [Fact]
    public void ReleaseNotes_Contains_V21610Header_ForSH30()
    {
        Assert.Contains("v2.16.10 — SH-30", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_V21610()
    {
        Assert.Contains("v2.16.10", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void Backlog_DoesNotContain_SH30AsOpenItem()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.DoesNotContain("| SH-30 |", backlog);
    }

    // ── SH-1: 起動時エラー時の案内改善 ───────────────────────────────────
    // 注意: 単純な "SH-1" の Contains は "SH-15" / "SH-19" 等の部分文字列としても
    // 一致してしまうため、見出し文字列 "v2.16.11 — SH-1:" で厳密に確認する。

    [Fact]
    public void ReleaseNotes_Contains_V21611Header_ForSH1()
    {
        Assert.Contains("v2.16.11 — SH-1:", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_V21611()
    {
        Assert.Contains("v2.16.11", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void Backlog_DoesNotContain_SH1AsOpenItem()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.DoesNotContain("| SH-1 |", backlog);
    }

    // ── RJ-10: M2 見送り・タスク縮退方針整理 ────────────────────────────────

    [Fact]
    public void Backlog_DoesNotContain_M2AsOpenItem()
    {
        // M2 は完了ではなく見送り。RJ-10 として backlog.md の見送り表へ移した。
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.DoesNotContain("| M2 |", backlog);
    }

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
    public void ReleaseNotes_Contains_V21612()
    {
        Assert.Contains("v2.16.12", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_RJ10()
    {
        Assert.Contains("RJ-10", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
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

    // ── TD-63: 巨大テストクラスのシナリオ単位分割 ────────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21613()
    {
        Assert.Contains("v2.16.13", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_TD63()
    {
        Assert.Contains("TD-63", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void Backlog_DoesNotContain_TD63AsOpenItem()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.DoesNotContain("| TD-63 |", backlog);
    }
}
