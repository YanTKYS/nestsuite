using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.Services;
using NestSuite.IdeaNest.ViewModels;
using Xunit;

namespace NestSuite.Tests;

public class IdeaNestMarkdownExporterTests
{
    [Fact]
    public void Build_EmptyList_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, IdeaNestMarkdownExporter.Build(new List<IdeaCardViewModel>()));
    }

    [Fact]
    public void Build_SingleCard_ContainsHeadingAndMetadataAndBody()
    {
        var card = new IdeaCardViewModel(new Idea
        {
            Title = "企画メモ",
            Body = "本文一行目\n本文二行目",
            Tags = ["DX", "調達"],
            Color = "blue",
            IsPinned = true,
            IsArchived = false,
        });

        var md = IdeaNestMarkdownExporter.Build([card]);

        Assert.Contains("## 企画メモ", md);
        Assert.Contains("- タグ: DX, 調達", md);
        Assert.Contains("- 色: blue", md);
        Assert.Contains("- ピン留め: あり", md);
        Assert.Contains("- アーカイブ: なし", md);
        Assert.Contains("本文一行目", md);
        Assert.Contains("本文二行目", md);
    }

    [Fact]
    public void Build_EndsWithExactlyOneTrailingNewline()
    {
        var withBody = new IdeaCardViewModel(new Idea { Title = "A", Body = "本文" });
        var withoutBody = new IdeaCardViewModel(new Idea { Title = "B" });

        foreach (var md in new[]
                 {
                     IdeaNestMarkdownExporter.Build([withBody]),
                     IdeaNestMarkdownExporter.Build([withoutBody]),
                     IdeaNestMarkdownExporter.Build([withBody, withoutBody]),
                 })
        {
            Assert.EndsWith(Environment.NewLine, md);
            Assert.False(md.EndsWith(Environment.NewLine + Environment.NewLine));
        }
    }

    [Fact]
    public void Build_MultiCard_PreservesGivenOrderAndSeparatesWithHorizontalRuleOnlyBetweenCards()
    {
        var first = new IdeaCardViewModel(new Idea { Title = "先頭" });
        var second = new IdeaCardViewModel(new Idea { Title = "次" });
        var third = new IdeaCardViewModel(new Idea { Title = "最後" });

        var md = IdeaNestMarkdownExporter.Build([first, second, third]);

        var firstIndex = md.IndexOf("## 先頭", StringComparison.Ordinal);
        var secondIndex = md.IndexOf("## 次", StringComparison.Ordinal);
        var thirdIndex = md.IndexOf("## 最後", StringComparison.Ordinal);
        Assert.True(firstIndex >= 0 && firstIndex < secondIndex && secondIndex < thirdIndex);

        var separatorCount = md.Split("---").Length - 1;
        Assert.Equal(2, separatorCount);
        Assert.False(md.TrimEnd('\r', '\n').EndsWith("---"));
    }

    [Fact]
    public void Build_UntitledCard_UsesDisplayTitlePlaceholder_AndDoesNotMutateModel()
    {
        var idea = new Idea { Title = "", Body = "" };
        var card = new IdeaCardViewModel(idea);

        var md = IdeaNestMarkdownExporter.Build([card]);

        Assert.Contains("## " + card.DisplayTitle, md);
        Assert.Equal("", idea.Title);
    }

    [Fact]
    public void Build_TitleWithEmbeddedNewlines_IsNormalizedToSingleHeadingLine()
    {
        var card = new IdeaCardViewModel(new Idea { Title = "1行目\n2行目\r\n3行目" });

        var md = IdeaNestMarkdownExporter.Build([card]);
        var headingLine = md.Split(Environment.NewLine).First(l => l.StartsWith("## "));

        Assert.Equal("## 1行目 2行目 3行目", headingLine);
    }

    [Fact]
    public void Build_Body_PreservesNewlinesAndDoesNotEscapeMarkdownSymbols()
    {
        var card = new IdeaCardViewModel(new Idea
        {
            Title = "書式確認",
            Body = "# 見出し風\n- 箇条書き\n> 引用\n```code```\n[link](http://example.com)",
        });

        var md = IdeaNestMarkdownExporter.Build([card]);

        Assert.Contains("# 見出し風", md);
        Assert.Contains("- 箇条書き", md);
        Assert.Contains("> 引用", md);
        Assert.Contains("```code```", md);
        Assert.Contains("[link](http://example.com)", md);
    }

    [Fact]
    public void Build_EmptyBody_SkipsBodySection()
    {
        var card = new IdeaCardViewModel(new Idea { Title = "本文なし", Body = "" });

        var md = IdeaNestMarkdownExporter.Build([card]);
        var lines = md.Split(Environment.NewLine);
        var archiveLineIndex = Array.FindIndex(lines, l => l.StartsWith("- アーカイブ"));

        Assert.True(archiveLineIndex >= 0);
        for (var i = archiveLineIndex + 1; i < lines.Length; i++)
        {
            Assert.True(string.IsNullOrEmpty(lines[i]));
        }
    }

    [Fact]
    public void Build_NoTags_ShowsNoneLabel()
    {
        var card = new IdeaCardViewModel(new Idea { Title = "タグなし", Tags = [] });

        var md = IdeaNestMarkdownExporter.Build([card]);

        Assert.Contains("- タグ: なし", md);
    }

    [Fact]
    public void Build_UnsetColor_ShowsDefaultLabel()
    {
        var card = new IdeaCardViewModel(new Idea { Title = "色未設定", Color = "" });

        var md = IdeaNestMarkdownExporter.Build([card]);

        Assert.Contains("- 色: 既定", md);
    }

    [Fact]
    public void Build_PinnedAndArchivedFalse_ShowsNashiLabels()
    {
        var card = new IdeaCardViewModel(new Idea { Title = "通常", IsPinned = false, IsArchived = false });

        var md = IdeaNestMarkdownExporter.Build([card]);

        Assert.Contains("- ピン留め: なし", md);
        Assert.Contains("- アーカイブ: なし", md);
    }

    [Fact]
    public void Build_ArchivedCard_ShowsArchivedLabel()
    {
        var card = new IdeaCardViewModel(new Idea { Title = "保管済み", IsArchived = true });

        var md = IdeaNestMarkdownExporter.Build([card]);

        Assert.Contains("- アーカイブ: あり", md);
    }

    [Fact]
    public void Build_JapaneseTextIsPreservedVerbatim()
    {
        var card = new IdeaCardViewModel(new Idea
        {
            Title = "日本語のタイトル",
            Body = "日本語の本文。句読点や「かぎ括弧」も含む。",
            Tags = ["日本語タグ"],
        });

        var md = IdeaNestMarkdownExporter.Build([card]);

        Assert.Contains("日本語のタイトル", md);
        Assert.Contains("日本語の本文。句読点や「かぎ括弧」も含む。", md);
        Assert.Contains("日本語タグ", md);
    }
}
