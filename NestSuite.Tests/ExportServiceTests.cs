using System.IO;
using System.Text;
using NoteNest.Models;
using NoteNest.Services;
using Xunit;

namespace NoteNest.Tests;

public class ExportServiceTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public ExportServiceTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── SanitizeFileName ───────────────────────────────────────────────────

    [Fact]
    public void SanitizeFileName_ReplacesForwardSlash()
        => Assert.Equal("プロジェクト_管理", ExportService.SanitizeFileName("プロジェクト/管理"));

    [Fact]
    public void SanitizeFileName_ReplacesColon()
        => Assert.Equal("要件_整理", ExportService.SanitizeFileName("要件:整理"));

    [Fact]
    public void SanitizeFileName_ReplacesMultipleInvalidChars()
        => Assert.Equal("a_b_c_d", ExportService.SanitizeFileName("a/b\\c*d"));

    [Fact]
    public void SanitizeFileName_ReturnsDefault_WhenEmpty()
        => Assert.Equal("notebook", ExportService.SanitizeFileName(""));

    [Fact]
    public void SanitizeFileName_ReturnsDefault_WhenWhitespaceOnly()
        => Assert.Equal("notebook", ExportService.SanitizeFileName("   "));

    [Fact]
    public void SanitizeFileName_TrimsWhitespace()
        => Assert.Equal("メモ", ExportService.SanitizeFileName("  メモ  "));

    [Fact]
    public void SanitizeFileName_PreservesJapanese()
        => Assert.Equal("会議メモ", ExportService.SanitizeFileName("会議メモ"));

    [Theory]
    [InlineData("CON")]
    [InlineData("PRN")]
    [InlineData("AUX")]
    [InlineData("NUL")]
    [InlineData("COM1")]
    [InlineData("LPT9")]
    public void SanitizeFileName_AppendsUnderscore_ForWindowsReservedNames(string reserved)
        => Assert.Equal(reserved + "_", ExportService.SanitizeFileName(reserved));

    [Fact]
    public void SanitizeFileName_ReservedNameCheck_IsCaseInsensitive()
        => Assert.Equal("con_", ExportService.SanitizeFileName("con"));

    [Theory]
    [InlineData("CON.txt",     "CON_.txt")]
    [InlineData("aux.backup",  "aux_.backup")]
    [InlineData("LPT1.export", "LPT1_.export")]
    public void SanitizeFileName_DottedReservedName_InsertsUnderscoreAfterStem(
        string input, string expected)
        => Assert.Equal(expected, ExportService.SanitizeFileName(input));

    // ── GetUniqueFilePath ──────────────────────────────────────────────────

    [Fact]
    public void GetUniqueFilePath_ReturnsBasePath_WhenNoConflict()
    {
        var result = ExportService.GetUniqueFilePath(_tempDir, "test", ".txt");
        Assert.Equal(Path.Combine(_tempDir, "test.txt"), result);
    }

    [Fact]
    public void GetUniqueFilePath_AddsCounter_WhenConflict()
    {
        File.WriteAllText(Path.Combine(_tempDir, "test.txt"), "");
        var result = ExportService.GetUniqueFilePath(_tempDir, "test", ".txt");
        Assert.Equal(Path.Combine(_tempDir, "test_2.txt"), result);
    }

    [Fact]
    public void GetUniqueFilePath_Increments_WhenMultipleConflicts()
    {
        File.WriteAllText(Path.Combine(_tempDir, "test.txt"), "");
        File.WriteAllText(Path.Combine(_tempDir, "test_2.txt"), "");
        var result = ExportService.GetUniqueFilePath(_tempDir, "test", ".txt");
        Assert.Equal(Path.Combine(_tempDir, "test_3.txt"), result);
    }

    [Fact]
    public void GetUniqueFilePath_WorksWithJapaneseName()
    {
        var result = ExportService.GetUniqueFilePath(_tempDir, "同名", ".txt");
        Assert.Equal(Path.Combine(_tempDir, "同名.txt"), result);
    }

    // ── BuildProjectText ───────────────────────────────────────────────────

    [Fact]
    public void BuildProjectText_ContainsProjectName()
    {
        var project = MakeProject("テストプロジェクト");
        var text = ExportService.BuildProjectText(project);
        Assert.Contains("テストプロジェクト", text);
    }

    [Fact]
    public void BuildProjectText_ContainsNotebookAndNoteInfo()
    {
        var project = MakeProjectWithNote("P", "NB1", "ノートA", "本文テスト");
        var text = ExportService.BuildProjectText(project);
        Assert.Contains("Notebook: NB1", text);
        Assert.Contains("Note: ノートA", text);
        Assert.Contains("本文テスト", text);
    }

    [Fact]
    public void BuildProjectText_ContainsAllNotebooks()
    {
        var project = MakeProject("P");
        AddNote(project, "NB1", "N1", "C1");
        AddNote(project, "NB2", "N2", "C2");
        var text = ExportService.BuildProjectText(project);
        Assert.Contains("Notebook: NB1", text);
        Assert.Contains("Notebook: NB2", text);
    }

    [Fact]
    public void BuildProjectText_EmptyNotebook_StillHasSeparator()
    {
        var project = MakeProject("P");
        project.Notebooks.Add(new Notebook { Title = "空のノートブック" });
        var text = ExportService.BuildProjectText(project);
        Assert.Contains("Notebook: 空のノートブック", text);
    }

    [Fact]
    public void BuildProjectText_NoteLinkSyntaxPassedThrough()
    {
        var project = MakeProjectWithNote("P", "NB", "N", "[[リンク先ノート]] の参照");
        var text = ExportService.BuildProjectText(project);
        Assert.Contains("[[リンク先ノート]]", text);
    }

    [Fact]
    public void BuildProjectText_MarkerSyntaxPassedThrough()
    {
        var project = MakeProjectWithNote("P", "NB", "N", "[TODO] 確認事項");
        var text = ExportService.BuildProjectText(project);
        Assert.Contains("[TODO] 確認事項", text);
    }

    // ── BuildNotebookText ──────────────────────────────────────────────────

    [Fact]
    public void BuildNotebookText_ContainsNotebookName()
    {
        var project = MakeProjectWithNote("P", "NB1", "Note1", "Content1");
        var text = ExportService.BuildNotebookText(project, project.Notebooks[0]);
        Assert.Contains("Notebook: NB1", text);
    }

    [Fact]
    public void BuildNotebookText_ExcludesOtherNotebooks()
    {
        var project = MakeProject("P");
        AddNote(project, "NB1", "Note1", "Content1");
        AddNote(project, "NB2", "Note2", "Content2");

        var text = ExportService.BuildNotebookText(project, project.Notebooks[0]);
        Assert.Contains("NB1", text);
        Assert.Contains("Note1", text);
        Assert.DoesNotContain("NB2", text);
        Assert.DoesNotContain("Note2", text);
    }

    // ── File I/O ───────────────────────────────────────────────────────────

    [Fact]
    public void ExportProjectToText_WritesReadableUtf8File()
    {
        var svc     = new ExportService();
        var project = MakeProjectWithNote("P", "NB", "日本語ノート", "日本語本文テスト");
        var path    = Path.Combine(_tempDir, "export.txt");

        svc.ExportProjectToText(project, path);

        Assert.True(File.Exists(path));
        var text = File.ReadAllText(path, Encoding.UTF8);
        Assert.Contains("日本語ノート", text);
        Assert.Contains("日本語本文テスト", text);
    }

    [Fact]
    public void ExportNotebooksToTextFiles_CreatesOneFilePerNotebook()
    {
        var svc     = new ExportService();
        var project = MakeProject("P");
        AddNote(project, "マイノートブック", "N1", "C1");
        AddNote(project, "技術メモ",         "N2", "C2");

        svc.ExportNotebooksToTextFiles(project, _tempDir);

        Assert.True(File.Exists(Path.Combine(_tempDir, "マイノートブック.txt")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "技術メモ.txt")));
    }

    [Fact]
    public void ExportNotebooksToTextFiles_UniqueFilesForSameNotebookName()
    {
        var svc     = new ExportService();
        var project = MakeProject("P");
        project.Notebooks.Add(new Notebook { Title = "同名" });
        project.Notebooks.Add(new Notebook { Title = "同名" });

        svc.ExportNotebooksToTextFiles(project, _tempDir);

        Assert.True(File.Exists(Path.Combine(_tempDir, "同名.txt")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "同名_2.txt")));
    }

    [Fact]
    public void ExportNotebooksToTextFiles_SanitizesUnsafeCharactersInFileName()
    {
        var svc     = new ExportService();
        var project = MakeProject("P");
        AddNote(project, "プロジェクト/管理", "N1", "C1");

        svc.ExportNotebooksToTextFiles(project, _tempDir);

        Assert.True(File.Exists(Path.Combine(_tempDir, "プロジェクト_管理.txt")));
    }

    [Fact]
    public void ExportNotebooksToTextFiles_EmptyProject_CreatesNoFiles()
    {
        var svc     = new ExportService();
        var project = MakeProject("P");

        svc.ExportNotebooksToTextFiles(project, _tempDir);

        Assert.Empty(Directory.GetFiles(_tempDir));
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static Project MakeProject(string name) =>
        new() { ProjectName = name };

    private static Project MakeProjectWithNote(
        string projectName, string nbTitle, string noteTitle, string noteContent)
    {
        var project = new Project { ProjectName = projectName };
        AddNote(project, nbTitle, noteTitle, noteContent);
        return project;
    }

    private static void AddNote(Project project, string nbTitle, string noteTitle, string noteContent)
    {
        var nb = project.Notebooks.FirstOrDefault(n => n.Title == nbTitle);
        if (nb == null)
        {
            nb = new Notebook { Title = nbTitle };
            project.Notebooks.Add(nb);
        }
        nb.Notes.Add(new Note { Title = noteTitle, Content = noteContent });
    }
}
