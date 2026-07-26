using System.Text.RegularExpressions;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.19.7 TD-83: 完了済みeditor設計文書・旧入口READMEのarchive/削除候補再判定の回帰テスト。
/// 新パスの存在・旧パスの不在・現行文書からの参照更新・旧入口READMEを削除していないことを確認する。
/// PRレビュー指摘（v2.19.7-1）を受け、archive移動後の相対Markdownリンクが実際に解決できることも固定する。
/// </summary>
public class DocsArchiveTd83Tests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;
    private static readonly Regex MarkdownLinkPattern = new(@"\[[^\]]*\]\(([^)]+)\)", RegexOptions.Compiled);

    private static string DocsPath(params string[] segments) =>
        Path.Combine(new[] { RepoRoot, "docs" }.Concat(segments).ToArray());

    /// <summary>
    /// ファイル内の相対Markdownリンク（http(s)://・アンカーのみ・メールリンクは除く）が、
    /// そのファイルからの相対パスとして実際に解決できることを確認する。
    /// </summary>
    private static void AssertRelativeMarkdownLinksResolve(string filePath)
    {
        Assert.True(File.Exists(filePath), $"File not found: {filePath}");
        var text = File.ReadAllText(filePath);
        var directory = Path.GetDirectoryName(filePath)!;

        foreach (Match match in MarkdownLinkPattern.Matches(text))
        {
            var target = match.Groups[1].Value.Trim();
            if (target.Length == 0) continue;
            if (target.StartsWith('#')) continue;
            if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                target.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                continue;

            var withoutAnchor = target.Split('#')[0];
            var resolved = Path.GetFullPath(Path.Combine(directory, withoutAnchor));
            Assert.True(File.Exists(resolved) || Directory.Exists(resolved),
                $"Relative link target does not resolve in {filePath}: \"{target}\" -> {resolved}");
        }
    }

    // ── Archive: notenest-editor-h0-reassessment.md ────────────────────────

    [Fact]
    public void EditorH0Reassessment_ExistsAtNewArchivePath()
    {
        var path = DocsPath("archive", "completed-designs", "notenest-editor-h0-reassessment.md");
        Assert.True(File.Exists(path), $"Not found at new path: {path}");
    }

    [Fact]
    public void EditorH0Reassessment_DoesNotExistAtOldPath()
    {
        var path = DocsPath("design", "notenest-editor-h0-reassessment.md");
        Assert.False(File.Exists(path), $"Old path should no longer exist: {path}");
    }

    [Fact]
    public void EditorH0Reassessment_RetainsHistoricalBanner()
    {
        var path = DocsPath("archive", "completed-designs", "notenest-editor-h0-reassessment.md");
        var text = File.ReadAllText(path);
        Assert.Contains("[履歴文書]", text);
    }

    [Theory]
    [InlineData("notenest-editor-host-design.md")]
    [InlineData("notenest-editor-textbox-dependencies.md")]
    [InlineData("notenest-editor-adapter-design.md")]
    public void SiblingEditorDesignDocs_ReferenceNewArchivePath_NotOldPath(string fileName)
    {
        var path = DocsPath("design", fileName);
        Assert.True(File.Exists(path), $"Sibling doc not found (should remain in docs/design/): {path}");
        var text = File.ReadAllText(path);

        Assert.Contains("docs/archive/completed-designs/notenest-editor-h0-reassessment.md", text);
        Assert.DoesNotContain("docs/design/notenest-editor-h0-reassessment.md", text);
    }

    // ── Archive: operation-note.md ──────────────────────────────────────────

    [Fact]
    public void OperationNote_ExistsAtNewArchivePath()
    {
        var path = DocsPath("archive", "completed-designs", "operation-note.md");
        Assert.True(File.Exists(path), $"Not found at new path: {path}");
    }

    [Fact]
    public void OperationNote_DoesNotExistAtOldPath()
    {
        var path = DocsPath("operations", "operation-note.md");
        Assert.False(File.Exists(path), $"Old path should no longer exist: {path}");
    }

    [Fact]
    public void OperationNote_RetainsHistoricalBanner()
    {
        var path = DocsPath("archive", "completed-designs", "operation-note.md");
        var text = File.ReadAllText(path);
        Assert.Contains("[履歴文書]", text);
    }

    // ── Kept: file-association.md (operations/ の現行 Canonical 文書) ───────

    [Fact]
    public void FileAssociationDoc_StillExists_AsCurrentOperationsDoc()
    {
        var path = DocsPath("operations", "file-association.md");
        Assert.True(File.Exists(path), $"Not found: {path}");
    }

    // ── Kept (not deleted): integration/README.md, migration/README.md ─────

    [Fact]
    public void IntegrationReadme_StillExists_NotDeleted()
    {
        var path = DocsPath("integration", "README.md");
        Assert.True(File.Exists(path), $"docs/integration/README.md was a Delete Candidate but must be kept (linked from archived docs): {path}");
    }

    [Fact]
    public void MigrationReadme_StillExists_NotDeleted()
    {
        var path = DocsPath("migration", "README.md");
        Assert.True(File.Exists(path), $"docs/migration/README.md was a Delete Candidate but must be kept (linked from archived docs): {path}");
    }

    [Theory]
    [InlineData("nestsuite-preparation.md")]
    [InlineData("ideanest-save-load-plan.md")]
    [InlineData("nestsuite-multi-file-tabs-plan.md")]
    [InlineData("nestsuite-notenest-multi-file-plan.md")]
    public void ArchivedCompletedDesignDocs_StillLinkToIntegrationReadme(string fileName)
    {
        var path = DocsPath("archive", "completed-designs", fileName);
        Assert.True(File.Exists(path), $"Archived doc not found: {path}");
        var text = File.ReadAllText(path);
        Assert.Contains("integration/README.md", text);
    }

    [Fact]
    public void ArchivedMigrationPlan_StillLinksToMigrationReadme()
    {
        var path = DocsPath("archive", "migrations", "nestsuite-default-startup-plan.md");
        Assert.True(File.Exists(path), $"Archived doc not found: {path}");
        var text = File.ReadAllText(path);
        Assert.Contains("migration/README.md", text);
    }

    // ── repository root README.md ───────────────────────────────────────────

    [Fact]
    public void RootReadme_DoesNotLinkToArchivedOperationNote()
    {
        var path = Path.Combine(RepoRoot, "README.md");
        Assert.True(File.Exists(path), $"Not found: {path}");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("docs/operations/operation-note.md", text);
    }

    [Fact]
    public void RootReadme_BakRestoreLink_PointsToUserGuide()
    {
        var path = Path.Combine(RepoRoot, "README.md");
        var text = File.ReadAllText(path);

        Assert.Contains("[docs/guide/nestsuite-user-guide.md](docs/guide/nestsuite-user-guide.md)", text);
    }

    // ── backlog / release notes / inventory ─────────────────────────────────

    [Fact]
    public void Backlog_DoesNotContainTD83AsOpenItem()
    {
        var backlog = TestPaths.ReadBacklog();
        Assert.DoesNotContain("| TD-83 |", backlog);
    }

    [Fact]
    public void ReleaseNotes_RecordsV2197TD83()
    {
        var releaseNotes = TestPaths.ReadReleaseNotes();
        Assert.Contains("v2.19.7 — TD-83", releaseNotes);
    }

    [Fact]
    public void InventoryPolicyDoc_VersionUpdated()
    {
        var path = DocsPath("planning", "docs-inventory-and-archive-policy.md");
        var text = File.ReadAllText(path);
        Assert.Contains("v2.19.7", text);
    }

    // ── PRレビュー指摘: archive移動後の相対リンク解決確認 ───────────────────
    // docs/archive/completed-designs/notenest-editor-h0-reassessment.md の冒頭バナーが
    // 移動前の相対パス "README.md"（実際には docs/archive/completed-designs/README.md へ
    // 誤って解決していた）のまま残っていた問題（PR #588 レビュー指摘）を受けて追加。

    [Theory]
    [InlineData("archive/completed-designs/notenest-editor-h0-reassessment.md")]
    [InlineData("archive/completed-designs/operation-note.md")]
    [InlineData("archive/completed-designs/nestsuite-preparation.md")]
    [InlineData("archive/completed-designs/ideanest-save-load-plan.md")]
    [InlineData("archive/completed-designs/nestsuite-multi-file-tabs-plan.md")]
    [InlineData("archive/completed-designs/nestsuite-notenest-multi-file-plan.md")]
    [InlineData("archive/migrations/nestsuite-default-startup-plan.md")]
    [InlineData("design/README.md")]
    [InlineData("design/notenest-editor-host-design.md")]
    [InlineData("design/notenest-editor-textbox-dependencies.md")]
    [InlineData("design/notenest-editor-adapter-design.md")]
    [InlineData("integration/README.md")]
    [InlineData("migration/README.md")]
    [InlineData("README.md")]
    public void RelativeMarkdownLinks_Resolve(string relativePath) =>
        AssertRelativeMarkdownLinksResolve(Path.Combine(RepoRoot, "docs", relativePath));

    [Fact]
    public void RootReadme_RelativeMarkdownLinks_Resolve() =>
        AssertRelativeMarkdownLinksResolve(Path.Combine(RepoRoot, "README.md"));
}
