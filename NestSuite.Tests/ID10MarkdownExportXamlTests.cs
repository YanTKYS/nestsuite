using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// ID-10: IdeaNest表示中カードのMarkdown出力の静的UI契約確認。
/// 既存の空実装メニュー項目・ショートカットへ処理を実装しただけであり、
/// 新規メニュー・ボタン・ショートカットは追加していないことを確認する。
/// Markdown整形・コマンドのCanExecute/成功失敗ロジックは
/// IdeaNestMarkdownExporterTests / IdeaNestMarkdownExportCommandTests で別途確認する。
/// </summary>
public class ID10MarkdownExportXamlTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadIdeaNestWorkspaceViewXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "IdeaNest", "Views", "IdeaNestWorkspaceView.xaml"));

    [Fact]
    public void ExportMarkdownCommand_MenuItemExists_UnderFileExportMenu()
    {
        var src = ReadIdeaNestWorkspaceViewXaml();
        Assert.Contains("Command=\"{Binding ExportMarkdownCommand}\"", src);
        // v2.18.19 SH-42: ID-10で実際にMarkdown出力するようになったため、
        // 「風」（それらしい、の意）を含む旧文言から実態に合う文言へ修正した。
        Assert.Contains("Markdownとして保存", src);
    }

    [Fact]
    public void CopyAllMarkdownCommand_MenuItemExists_WithExistingCtrlShiftCShortcut()
    {
        var src = ReadIdeaNestWorkspaceViewXaml();
        Assert.Contains("Command=\"{Binding CopyAllMarkdownCommand}\"", src);
        Assert.Contains("表示中カードをMarkdown形式でコピー", src);
        Assert.Contains("<KeyBinding Modifiers=\"Ctrl+Shift\" Key=\"C\" Command=\"{Binding CopyAllMarkdownCommand}\" />", src);
    }

    [Fact]
    public void NoNewKeyboardShortcut_OnlyThreeExistingCtrlShiftBindingsPresent()
    {
        var src = ReadIdeaNestWorkspaceViewXaml();
        var bindingCount = src.Split("<KeyBinding ").Length - 1;
        Assert.Equal(3, bindingCount);
    }

    [Fact]
    public void CopyCardMarkdownCommand_PerCardContextMenuItem_IsUnchanged_AndOutOfScope()
    {
        var src = ReadIdeaNestWorkspaceViewXaml();
        Assert.Contains("Command=\"{Binding DataContext.CopyCardMarkdownCommand, Source={x:Reference CardArea}}\"", src);
    }

    [Fact]
    public void NoNewExportWindowOrToolbarButton_Introduced()
    {
        var src = ReadIdeaNestWorkspaceViewXaml();
        Assert.DoesNotContain("ExportWindow", src);
        Assert.DoesNotContain("Markdownをコピー\"", src);
        Assert.DoesNotContain("Markdownとして保存\"", src);
    }
}
