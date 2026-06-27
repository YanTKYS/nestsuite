using System.IO;
using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.10.10 TD-23: UIスモークテスト Workspace カバレッジ拡大の回帰テスト。
/// </summary>
public class UiSmokeTD23Tests
{
    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    // ── バージョン ────────────────────────────────────────────────────────

    [Fact]
    public void ApplicationVersion_Is_2_10_10()
    {
        Assert.Equal("2.10.10", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void NoteNestSchemaVersion_Remains_1_4_1()
    {
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }

    // ── backlog / release-notes ───────────────────────────────────────────

    [Fact]
    public void Backlog_TD23_IsMarkedComplete()
    {
        Assert.Contains("~~TD-23~~", ReadBacklog());
    }

    [Fact]
    public void ReleaseNotes_Contains_V2_10_10()
    {
        var path = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(path));
        Assert.Contains("v2.10.10", File.ReadAllText(path));
    }

    // ── smoke test structure ──────────────────────────────────────────────

    [Fact]
    public void SmokeProgram_Exists()
    {
        var path = Path.Combine(RepoRoot, "NestSuite.UiSmoke", "Program.cs");
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void SmokeProgram_HasWaitForMainWindowHelper()
    {
        var src = ReadSmokeProgram();
        Assert.Contains("WaitForMainWindow", src);
    }

    [Fact]
    public void SmokeProgram_HasWaitForElementByAutomationIdHelper()
    {
        var src = ReadSmokeProgram();
        Assert.Contains("WaitForElementByAutomationId", src);
    }

    [Fact]
    public void SmokeProgram_HasCheckRequiredElementsHelper()
    {
        var src = ReadSmokeProgram();
        Assert.Contains("CheckRequiredElements", src);
    }

    [Fact]
    public void SmokeProgram_HasClickElementByPointHelper()
    {
        var src = ReadSmokeProgram();
        Assert.Contains("ClickElementByPoint", src);
    }

    [Fact]
    public void SmokeProgram_CoversNoteNestElements()
    {
        var src = ReadSmokeProgram();
        Assert.Contains("NoteNest.NotebookTree", src);
        Assert.Contains("NoteNest.AddNoteButton", src);
        Assert.Contains("NoteNest.EditorHost", src);
    }

    [Fact]
    public void SmokeProgram_CoversIdeaNestElements()
    {
        var src = ReadSmokeProgram();
        Assert.Contains("IdeaNest.SearchBox", src);
        Assert.Contains("IdeaNest.AddIdeaButton", src);
    }

    [Fact]
    public void SmokeProgram_CoversChatNestElements()
    {
        var src = ReadSmokeProgram();
        Assert.Contains("ChatNest.InputBox", src);
        Assert.Contains("ChatNest.PostButton", src);
        Assert.Contains("ChatNest.ShowTimestampsCheckBox", src);
    }

    [Fact]
    public void SmokeProgram_CoversToolMenuIds()
    {
        var src = ReadSmokeProgram();
        Assert.Contains("Shell.MenuToolNoteNest", src);
        Assert.Contains("Shell.MenuToolIdeaNest", src);
        Assert.Contains("Shell.MenuToolChatNest", src);
    }

    [Fact]
    public void SmokeProgram_CoversTempNestElements()
    {
        var src = ReadSmokeProgram();
        Assert.Contains("TempNest.Slot1.BodyBox", src);
        Assert.Contains("TempNest.Slot1.TitleBox", src);
        Assert.Contains("TempNest.Slot1.CopyButton", src);
        Assert.Contains("TempNest.Slot1.ClearButton", src);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private string ReadBacklog()
    {
        var path = Path.Combine(RepoRoot, "docs", "backlog.md");
        Assert.True(File.Exists(path), $"backlog.md not found: {path}");
        return File.ReadAllText(path);
    }

    private string ReadSmokeProgram()
    {
        var path = Path.Combine(RepoRoot, "NestSuite.UiSmoke", "Program.cs");
        Assert.True(File.Exists(path), $"Program.cs not found: {path}");
        return File.ReadAllText(path);
    }
}
