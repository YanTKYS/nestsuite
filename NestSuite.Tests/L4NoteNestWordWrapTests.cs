using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.19.3 L4: NoteNest 本文エディタのワードラップ切替の静的 UI 契約確認。
/// M14NoteSortModeXamlTests と同じ静的文字列テストのパターンで、メニュー配線・
/// 全 NoteNest セッションへの伝播・新規タブへの適用・UI settings 永続化を固定する。
/// bool→TextWrapping/HorizontalScrollBarVisibility の変換自体は
/// NoteEditorWordWrapSettingsTests（純粋関数の単体テスト）で別途確認する。
/// </summary>
public class L4NoteNestWordWrapTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadShellXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.xaml"));

    private static string ReadShellCodeBehind() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.xaml.cs"));

    private static string ReadNoteEditorHostXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Editor", "NoteEditorHost.xaml"));

    private static string ReadNoteEditorHostCodeBehind() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Editor", "NoteEditorHost.xaml.cs"));

    [Fact]
    public void ShellMenu_HasWordWrapToggle_UnderViewMenu()
    {
        var src = ReadShellXaml();
        Assert.Contains("Shell.NoteNestWordWrapMenuItem", src);
        Assert.Contains("テキストを折り返す", src);
    }

    [Fact]
    public void ShellMenu_WordWrapItem_IsSingleCheckableToggle()
    {
        var src = ReadShellXaml();
        var occurrences = System.Text.RegularExpressions.Regex.Matches(src, "Click=\"MenuNoteNestWordWrap_Click\"").Count;
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void ShellCodeBehind_UpdatesMenuCheckFromCurrentWordWrapSetting()
    {
        var src = ReadShellCodeBehind();
        Assert.Contains("UpdateNoteNestWordWrapMenuCheck", src);
        Assert.Contains("NoteNestWordWrapMenuItem.IsChecked", src);
    }

    [Fact]
    public void ShellCodeBehind_PropagatesToAllNoteNestSessions_AndPersistsToUiSettings()
    {
        var src = ReadShellCodeBehind();
        Assert.Contains("PropagateNoteNestWordWrap", src);
        Assert.Contains("ui.NoteNestWordWrap = wordWrap", src);
        Assert.Contains("otherNoteVm.EditorWordWrap = wordWrap", src);
    }

    [Fact]
    public void ShellCodeBehind_NewNoteNestViewModel_AppliesCurrentWordWrapSetting()
    {
        var src = ReadShellCodeBehind();
        Assert.Contains("vm.EditorWordWrap = _noteNestWordWrap", src);
    }

    [Fact]
    public void ShellXaml_DoesNotAddNewKeyBindingForWordWrap()
    {
        var xaml = ReadShellXaml();
        Assert.DoesNotContain("Key=\"W\" Modifiers=\"Ctrl\"", xaml);
        Assert.DoesNotContain("Key=\"W\" Modifiers=\"Alt\"", xaml);
        Assert.DoesNotContain("Key=\"W\" Modifiers=\"Ctrl+Shift\"", xaml);
    }

    [Fact]
    public void NoteEditorHostXaml_DoesNotHardBindTextWrapping_ToWrapOnly()
    {
        // 折り返しON/OFFはコードビハインドで EditorBox.TextWrapping を切り替えるため、
        // XAML 側で TextWrapping="Wrap" 固定のバインドは残っていても構わないが、
        // OFF 側の切替を妨げる Binding 固定（Mode=OneTime 等）は追加していないことを確認する。
        var xaml = ReadNoteEditorHostXaml();
        Assert.DoesNotContain("TextWrapping=\"{Binding", xaml);
    }

    [Fact]
    public void NoteEditorHostCodeBehind_AppliesWordWrapSetting_OnLoadAndOnPropertyChanged()
    {
        var src = ReadNoteEditorHostCodeBehind();
        Assert.Contains("ApplyWordWrapSetting", src);
        Assert.Contains("\"EditorWordWrap\"", src);
        Assert.Contains("NoteEditorWordWrapSettings.ToTextWrapping", src);
        Assert.Contains("NoteEditorWordWrapSettings.ToHorizontalScrollBarVisibility", src);
    }
}
