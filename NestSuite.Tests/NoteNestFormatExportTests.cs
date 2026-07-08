using NestSuite.Models;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.13 TD-63: NoteNestFormatSchemaRegressionTests から、Txt/Markdown/Html
/// エクスポートに関するテストを分割した。
/// </summary>
public class NoteNestFormatExportTests : IDisposable
{
    private readonly string _tempDir;

    public NoteNestFormatExportTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NoteNestFormatExportTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── エクスポート ─────────────────────────────────────────────────────

    [Fact]
    public void UnifiedExportSupportsTargetsFormatsTasksAndMarkers()
    {
        Directory.CreateDirectory(_tempDir);
        var project = new Project
        {
            ProjectName = "P",
            Notebooks =
            [
                new Notebook { Id = "nb", Title = "NB", Notes = [new Note { Id = "note", Title = "N", Content = "[TODO] marker" }] },
                new Notebook { Id = "other-nb", Title = "Other", Notes = [new Note { Id = "other-note", Title = "OtherNote", Content = "" }] },
            ],
            Tasks = new TaskCollection
            {
                Today =
                [
                    new NoteTask { Title = "Linked Task", LinkedNoteId = "note" },
                    new NoteTask { Title = "Other Task", LinkedNoteId = "other-note" },
                    new NoteTask { Title = "Unlinked Task" },
                ],
            },
        };
        var service = new ExportService();
        var markdown = Path.Combine(_tempDir, "export.md");
        var html = Path.Combine(_tempDir, "export.html");

        service.Export(project, new ExportOptions(ExportTarget.CurrentNote, ExportFormat.Markdown, true, true), markdown, "nb", "note");
        service.Export(project, new ExportOptions(ExportTarget.Project, ExportFormat.Html, true, true), html);

        var markdownText = File.ReadAllText(markdown);
        Assert.Contains("## Tasks", markdownText);
        Assert.Contains("Linked Task", markdownText);
        Assert.DoesNotContain("Other Task", markdownText);
        Assert.DoesNotContain("Unlinked Task", markdownText);
        Assert.Contains("## Markers", markdownText);
        var htmlText = File.ReadAllText(html);
        Assert.Contains("<html>", htmlText);
        Assert.Contains("Other Task", htmlText);
        Assert.Contains("Unlinked Task", htmlText);
        Assert.Equal(".md", ExportService.GetExtension(ExportFormat.Markdown));
    }

    [Fact]
    public void Export_Txt_WritesUtf8()
    {
        Directory.CreateDirectory(_tempDir);
        var project = new Project
        {
            ProjectName = "テスト",
            Notebooks = [new Notebook { Title = "NB", Notes = [new Note { Title = "日本語ノート", Content = "日本語本文" }] }]
        };
        var path = Path.Combine(_tempDir, "export.txt");
        new ExportService().Export(project, new ExportOptions(ExportTarget.Project, ExportFormat.Text, false, false), path);

        var text = File.ReadAllText(path, System.Text.Encoding.UTF8);
        Assert.Contains("日本語ノート", text);
        Assert.Contains("日本語本文", text);
    }

    [Fact]
    public void Export_Markdown_ContainsHeadings()
    {
        Directory.CreateDirectory(_tempDir);
        var project = new Project
        {
            ProjectName = "MD",
            Notebooks = [new Notebook { Title = "NB", Notes = [new Note { Title = "MDNote", Content = "body" }] }]
        };
        var path = Path.Combine(_tempDir, "export.md");
        new ExportService().Export(project, new ExportOptions(ExportTarget.Project, ExportFormat.Markdown, false, false), path);

        var text = File.ReadAllText(path);
        Assert.Contains("# ", text);
        Assert.Contains("MDNote", text);
    }

    [Fact]
    public void Export_Html_ContainsHtmlTags()
    {
        Directory.CreateDirectory(_tempDir);
        var project = new Project
        {
            ProjectName = "HTML",
            Notebooks = [new Notebook { Title = "NB", Notes = [new Note { Title = "HtmlNote", Content = "body" }] }]
        };
        var path = Path.Combine(_tempDir, "export.html");
        new ExportService().Export(project, new ExportOptions(ExportTarget.Project, ExportFormat.Html, false, false), path);

        var text = File.ReadAllText(path);
        Assert.Contains("<html>", text);
        Assert.Contains("HtmlNote", text);
    }
}
