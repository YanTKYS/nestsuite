using NestSuite;
using Xunit;

namespace NestSuite.Tests;

public class ShortcutHelpProviderTests
{
    [Fact]
    public void GetItems_ContainsImplementedShellShortcuts()
    {
        var items = ShortcutHelpProvider.GetItems();

        Assert.Contains(items, item => item.Category == "Shell共通" && item.Shortcut == "Ctrl+S");
        Assert.Contains(items, item => item.Category == "Shell共通" && item.Shortcut == "Ctrl+Shift+F");
        Assert.Contains(items, item => item.Category == "タブ操作" && item.Shortcut == "Ctrl+Tab");
    }

    [Fact]
    public void GetItems_ContainsWorkspaceShortcutsFoundInCode()
    {
        var items = ShortcutHelpProvider.GetItems();

        Assert.Contains(items, item => item.Category == "NoteNest" && item.Shortcut == "Ctrl+F");
        Assert.Contains(items, item => item.Category == "IdeaNest" && item.Shortcut == "Ctrl+Shift+N");
        Assert.Contains(items, item => item.Category == "ChatNest" && item.Shortcut == "Ctrl+Enter");
    }

    [Fact]
    public void GetItems_DoesNotListRemovedShiftArrowTabSwitchShortcut()
    {
        var items = ShortcutHelpProvider.GetItems();

        Assert.DoesNotContain(items, item => item.Category == "タブ操作" && item.Shortcut.Contains("Shift+← / →"));
    }

    [Fact]
    public void GetGroups_ReturnsExpectedCategories()
    {
        var groups = ShortcutHelpProvider.GetGroups();

        Assert.Contains(groups, group => group.Category == "Shell共通");
        Assert.Contains(groups, group => group.Category == "タブ操作");
        Assert.Contains(groups, group => group.Category == "NoteNest");
        Assert.Contains(groups, group => group.Category == "IdeaNest");
        Assert.Contains(groups, group => group.Category == "ChatNest");
    }
}
