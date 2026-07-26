using System.IO;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// ID-7: IdeaNestカード内検索ハイライトのXAML配線を静的に確認する。
/// タイトル・本文プレビュー（固定行数モード・自動高さモード両方）が
/// <c>IdeaNestSearchHighlightBehavior</c> を利用していること、タグ表示・プレビュー画面へは
/// 範囲を広げていないこと、RichTextBox等の重い部品を追加していないことを固定する。
/// </summary>
public class IdeaNestSearchHighlightXamlTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadWorkspaceViewXaml()
    {
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "IdeaNest", "Views", "IdeaNestWorkspaceView.xaml");
        Assert.True(File.Exists(path), $"IdeaNestWorkspaceView.xaml not found: {path}");
        return File.ReadAllText(path);
    }

    private static string ReadPreviewWindowXaml()
    {
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "IdeaNest", "Views", "PreviewIdeaWindow.xaml");
        Assert.True(File.Exists(path), $"PreviewIdeaWindow.xaml not found: {path}");
        return File.ReadAllText(path);
    }

    [Fact]
    public void WorkspaceView_TitleTextBlock_UsesHighlightBehavior()
    {
        var xaml = ReadWorkspaceViewXaml();

        Assert.Contains("beh:IdeaNestSearchHighlightBehavior.Text=\"{Binding DisplayTitle}\"", xaml);
    }

    [Fact]
    public void WorkspaceView_BodyPreviewTextBlocks_UseHighlightBehavior()
    {
        var xaml = ReadWorkspaceViewXaml();

        Assert.Contains("<beh:IdeaNestSearchHighlightBehavior.Text>", xaml);
        Assert.Contains("beh:IdeaNestSearchHighlightBehavior.Text=\"{Binding Body}\"", xaml);
    }

    [Fact]
    public void WorkspaceView_HighlightQuery_BindsToWorkspaceSearchText()
    {
        var xaml = ReadWorkspaceViewXaml();

        var occurrences = 0;
        var index = 0;
        const string needle = "beh:IdeaNestSearchHighlightBehavior.Query=\"{Binding DataContext.SearchText, RelativeSource={RelativeSource AncestorType=UserControl}}\"";
        while ((index = xaml.IndexOf(needle, index, System.StringComparison.Ordinal)) >= 0)
        {
            occurrences++;
            index += needle.Length;
        }

        // タイトル・本文プレビュー（固定行数）・本文プレビュー（自動高さ）の3箇所。
        Assert.Equal(3, occurrences);
    }

    [Fact]
    public void WorkspaceView_TagChips_AreUnchanged_NoHighlightBehavior()
    {
        var xaml = ReadWorkspaceViewXaml();
        var tagChipStart = xaml.IndexOf("<!-- Tag chips -->", System.StringComparison.Ordinal);
        Assert.True(tagChipStart >= 0);
        var tagChipEnd = xaml.IndexOf("</ItemsControl>", tagChipStart, System.StringComparison.Ordinal);
        Assert.True(tagChipEnd >= 0);
        var tagChipSection = xaml.Substring(tagChipStart, tagChipEnd - tagChipStart);

        Assert.DoesNotContain("IdeaNestSearchHighlightBehavior", tagChipSection);
        Assert.Contains("{Binding StringFormat='#{0}'}", tagChipSection);
    }

    [Fact]
    public void WorkspaceView_DoesNotIntroduceRichTextOrExternalRendering()
    {
        var xaml = ReadWorkspaceViewXaml();

        Assert.DoesNotContain("RichTextBox", xaml);
        Assert.DoesNotContain("FlowDocument", xaml);
        Assert.DoesNotContain("WebView", xaml);
    }

    [Fact]
    public void PreviewWindow_DoesNotUseHighlightBehavior()
    {
        var xaml = ReadPreviewWindowXaml();

        Assert.DoesNotContain("IdeaNestSearchHighlightBehavior", xaml);
    }

    [Fact]
    public void HighlightBehavior_ReusesExistingThreeRunSplitHelper_NotDuplicatingIndexOfLogic()
    {
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "IdeaNest", "Services", "IdeaNestSearchHighlightHelper.cs");
        Assert.True(File.Exists(path), $"IdeaNestSearchHighlightHelper.cs not found: {path}");
        var source = File.ReadAllText(path);

        Assert.Contains("SearchMatchSegments.Split(", source);
        Assert.Contains("StringComparison.OrdinalIgnoreCase", source);
    }

    [Fact]
    public void UserGuide_ExplainsIdeaNestSearchHighlight()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "guide", "nestsuite-user-guide.md"));

        Assert.Contains("IdeaNestカード内検索ハイライト", text);
        Assert.Contains("タグだけが検索語に一致した場合", text);
        Assert.Contains("通常表示へ戻ります", text);
        Assert.Contains("保存データには一切影響しません", text);
    }
}
