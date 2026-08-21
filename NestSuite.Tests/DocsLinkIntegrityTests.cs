using System.Text.RegularExpressions;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// docs 配下と README の相対 Markdown リンクが実在するファイル／ディレクトリへ解決できることを確認する。
///
/// <para>本クラスは <b>文書本文（日本語の文章・見出し・履歴）を一切 assert しない</b>。
/// 検証するのは「参照先が実在するか」という機械的に判定できる事実だけであり、
/// 文書を書き換えても、リンク先を壊さない限りテストは失敗しない
/// （方針: <c>docs/development/test-suite-policy.md</c>）。</para>
///
/// <para>ファイル一覧をハードコードせず docs 配下を走査するため、文書の追加・移動に自動追従する。
/// 失敗した場合はリンク切れという実際の不具合を示す。</para>
/// </summary>
public class DocsLinkIntegrityTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;
    private static readonly Regex MarkdownLinkPattern = new(@"\[[^\]]*\]\(([^)]+)\)", RegexOptions.Compiled);

    private static IEnumerable<string> AllMarkdownFiles()
    {
        var docs = Path.Combine(RepoRoot, "docs");
        if (Directory.Exists(docs))
        {
            foreach (var file in Directory.GetFiles(docs, "*.md", SearchOption.AllDirectories))
                yield return file;
        }

        var rootReadme = Path.Combine(RepoRoot, "README.md");
        if (File.Exists(rootReadme)) yield return rootReadme;
    }

    private static IEnumerable<string> FindBrokenLinks(string filePath)
    {
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
            if (!File.Exists(resolved) && !Directory.Exists(resolved))
                yield return $"{Path.GetRelativePath(RepoRoot, filePath)}: \"{target}\"";
        }
    }

    [Fact]
    public void AllDocsAndReadme_RelativeMarkdownLinks_Resolve()
    {
        var broken = AllMarkdownFiles().SelectMany(FindBrokenLinks).ToList();

        Assert.True(broken.Count == 0,
            "解決できない相対リンクがある:" + Environment.NewLine + string.Join(Environment.NewLine, broken));
    }

    [Fact]
    public void DocsDirectory_AndRootReadme_Exist()
    {
        // 配布・案内の入口として実在が前提となるファイルのみを確認する。
        Assert.True(Directory.Exists(Path.Combine(RepoRoot, "docs")));
        Assert.True(File.Exists(Path.Combine(RepoRoot, "README.md")));
    }
}
