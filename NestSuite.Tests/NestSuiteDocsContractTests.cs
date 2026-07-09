using System.Text.RegularExpressions;
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

    // ── TD-66: タブ変更時の session 随時保存 ────────────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21614()
    {
        Assert.Contains("v2.16.14", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_TD66()
    {
        Assert.Contains("TD-66", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_TD66_MentionsR6AndSessionFormatUnchanged()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var td66Section = text.Substring(text.IndexOf("TD-66", StringComparison.Ordinal));
        var nextSectionIdx = td66Section.IndexOf("\n## ", 1, StringComparison.Ordinal);
        if (nextSectionIdx > 0) td66Section = td66Section.Substring(0, nextSectionIdx);

        Assert.Contains("R-6", td66Section);
        Assert.Contains("session 形式", td66Section);
    }

    [Fact]
    public void Backlog_DoesNotContain_TD66AsOpenItem()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.DoesNotContain("| TD-66 |", backlog);
    }

    [Fact]
    public void Backlog_MentionsTD66InCompletedRange()
    {
        // v2.16.15 TD-67 fix: 「TD-64〜TD-66」が「TD-64〜TD-67」等へ圧縮されると
        // リテラル "TD-66" が range テキストから消えるため、範囲パースで判定する。
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.True(BacklogCompletedRangeCoversTD(backlog, 66), "TD-66 が backlog の完了済み範囲（TD-A〜TD-B）に含まれていない");
        // TD-59 は今回削除しない open item として残っている想定
        Assert.Contains("| TD-59 |", backlog);
    }

    // ── TD-67: 複数ファイルオープン失敗通知の改善 ──────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21615()
    {
        Assert.Contains("v2.16.15", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_TD67()
    {
        Assert.Contains("TD-67", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_TD67_MentionsR7AndNoFormatChange()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var td67Section = text.Substring(text.IndexOf("TD-67", StringComparison.Ordinal));
        var nextSectionIdx = td67Section.IndexOf("\n## ", 1, StringComparison.Ordinal);
        if (nextSectionIdx > 0) td67Section = td67Section.Substring(0, nextSectionIdx);

        Assert.Contains("R-7", td67Section);
        Assert.Contains("保存形式", td67Section);
    }

    [Fact]
    public void Backlog_DoesNotContain_TD67AsOpenItem()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.DoesNotContain("| TD-67 |", backlog);
    }

    [Fact]
    public void Backlog_MentionsTD67InCompletedRange()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.True(BacklogCompletedRangeCoversTD(backlog, 67), "TD-67 が backlog の完了済み範囲（TD-A〜TD-B）に含まれていない");
        // TD-59 は今回削除しない open item として残っている想定
        Assert.Contains("| TD-59 |", backlog);
    }

    // ── TD-68: Tabs[].WorkspaceKind の用途明文化・テスト固定 ─────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21616()
    {
        Assert.Contains("v2.16.16", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_TD68()
    {
        Assert.Contains("TD-68", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_TD68_MentionsR8AndUiHintNotTrustSource()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var td68Section = text.Substring(text.IndexOf("TD-68", StringComparison.Ordinal));
        var nextSectionIdx = td68Section.IndexOf("\n## ", 1, StringComparison.Ordinal);
        if (nextSectionIdx > 0) td68Section = td68Section.Substring(0, nextSectionIdx);

        Assert.Contains("R-8", td68Section);
        Assert.Contains("ヒント", td68Section);
        Assert.Contains("信頼ソース", td68Section);
    }

    [Fact]
    public void Backlog_DoesNotContain_TD68AsOpenItem()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.DoesNotContain("| TD-68 |", backlog);
    }

    [Fact]
    public void Backlog_MentionsTD68InCompletedRange()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.True(BacklogCompletedRangeCoversTD(backlog, 68), "TD-68 が backlog の完了済み範囲（TD-A〜TD-B）に含まれていない");
        // TD-59 は今回削除しない open item として残っている想定
        Assert.Contains("| TD-59 |", backlog);
    }

    // ── TD-69: FilePaths を Tabs から導出する統一 ───────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21617()
    {
        Assert.Contains("v2.16.17", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_TD69()
    {
        Assert.Contains("TD-69", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_TD69_MentionsR14AndDerivedFilePaths()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var td69Section = text.Substring(text.IndexOf("TD-69", StringComparison.Ordinal));
        var nextSectionIdx = td69Section.IndexOf("\n## ", 1, StringComparison.Ordinal);
        if (nextSectionIdx > 0) td69Section = td69Section.Substring(0, nextSectionIdx);

        Assert.Contains("R-14", td69Section);
        Assert.Contains("導出", td69Section);
    }

    [Fact]
    public void Backlog_DoesNotContain_TD69AsOpenItem()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.DoesNotContain("| TD-69 |", backlog);
    }

    [Fact]
    public void Backlog_MentionsTD69InCompletedRange()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.True(BacklogCompletedRangeCoversTD(backlog, 69), "TD-69 が backlog の完了済み範囲（TD-A〜TD-B）に含まれていない");
        // TD-59 は今回削除しない open item として残っている想定
        Assert.Contains("| TD-59 |", backlog);
    }

    // ── TD-70: pending entry の再試行解除手段 ───────────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V21618()
    {
        Assert.Contains("v2.16.18", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_TD70()
    {
        Assert.Contains("TD-70", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_TD70_MentionsFileNotFoundAndUserConfirmation()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var td70Section = text.Substring(text.IndexOf("TD-70", StringComparison.Ordinal));
        var nextSectionIdx = td70Section.IndexOf("\n## ", 1, StringComparison.Ordinal);
        if (nextSectionIdx > 0) td70Section = td70Section.Substring(0, nextSectionIdx);

        Assert.Contains("FileNotFound", td70Section);
        Assert.Contains("利用者", td70Section);
    }

    [Fact]
    public void Backlog_DoesNotContain_TD70AsOpenItem()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.DoesNotContain("| TD-70 |", backlog);
    }

    [Fact]
    public void Backlog_MentionsTD70InCompletedRange()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.True(BacklogCompletedRangeCoversTD(backlog, 70), "TD-70 が backlog の完了済み範囲（TD-A〜TD-B）に含まれていない");
        // TD-59 は今回削除しない open item として残っている想定
        Assert.Contains("| TD-59 |", backlog);
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
}
