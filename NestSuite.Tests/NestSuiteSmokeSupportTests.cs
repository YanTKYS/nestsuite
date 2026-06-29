using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// NestSuite.UiSmoke/Program.cs の構造確認テスト。
/// UI Smoke テストプログラムが必要なヘルパーと Automation ID カバレッジを持つことを静的に確認する。
/// </summary>
public class NestSuiteSmokeSupportTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

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
        // CH-15: ShowTimestampsCheckBox は右クリックメニューへ移行したため削除
        Assert.DoesNotContain("ChatNest.ShowTimestampsCheckBox", src);
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

    private string ReadSmokeProgram()
    {
        var path = Path.Combine(RepoRoot, "NestSuite.UiSmoke", "Program.cs");
        Assert.True(File.Exists(path), $"Program.cs not found: {path}");
        return File.ReadAllText(path);
    }
}
