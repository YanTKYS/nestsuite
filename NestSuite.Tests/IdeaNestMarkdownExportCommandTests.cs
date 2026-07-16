using System.Text;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// ID-10: ExportMarkdownCommand / CopyAllMarkdownCommand のCanExecute・0件時・成功/失敗通知・
/// 読み取り専用性を検証する。実クリップボード・実SaveFileDialogには依存せず、
/// <see cref="IdeaNestWorkspaceUiService"/> を差し替えて検証する。
/// </summary>
public class IdeaNestMarkdownExportCommandTests
{
    private sealed class FakeUiService : IdeaNestWorkspaceUiService
    {
        public string? CopiedText;
        public bool ThrowOnCopy;
        public string? SavePathToReturn;
        public bool ThrowOnSave;
        public string? LastErrorMessage;

        public override void SetClipboardText(string text)
        {
            if (ThrowOnCopy) throw new InvalidOperationException("clipboard busy");
            CopiedText = text;
        }

        public override string? ShowSaveMarkdownDialog(string defaultFileName) => SavePathToReturn;

        public override void ShowError(string message) => LastErrorMessage = message;
    }

    private static IdeaNestWorkspaceViewModel MakeVmWithCards(FakeUiService ui, int cardCount = 2)
    {
        var vm = new IdeaNestWorkspaceViewModel(ui);
        var ideas = new List<Idea>();
        for (var i = 0; i < cardCount; i++)
            ideas.Add(new Idea { Id = $"card-{i}", Title = $"カード{i}" });
        vm.LoadFromWorkspace(new Workspace { Ideas = ideas });
        return vm;
    }

    [Fact]
    public void ExportAndCopyCommands_CanExecute_TrueWhenVisibleCardsExist()
    {
        var ui = new FakeUiService();
        var vm = MakeVmWithCards(ui);

        Assert.True(vm.ExportMarkdownCommand.CanExecute(null));
        Assert.True(vm.CopyAllMarkdownCommand.CanExecute(null));
    }

    [Fact]
    public void ExportAndCopyCommands_CanExecute_FalseWhenNoVisibleCards()
    {
        var ui = new FakeUiService();
        var vm = new IdeaNestWorkspaceViewModel(ui);
        vm.LoadFromWorkspace(new Workspace { Ideas = [] });

        Assert.False(vm.ExportMarkdownCommand.CanExecute(null));
        Assert.False(vm.CopyAllMarkdownCommand.CanExecute(null));
    }

    [Fact]
    public void CanExecute_UpdatesLiveWhenFilterHidesAllCards()
    {
        var ui = new FakeUiService();
        var vm = MakeVmWithCards(ui, 1);
        Assert.True(vm.CopyAllMarkdownCommand.CanExecute(null));

        vm.SearchText = "一致しないはずの検索語xyz";

        Assert.Equal(0, vm.VisibleCards.Count);
        Assert.False(vm.CopyAllMarkdownCommand.CanExecute(null));
    }

    [Fact]
    public void CopyAllMarkdownCommand_Success_SetsClipboardAndShowsCountStatus_AndDoesNotMarkDirty()
    {
        var ui = new FakeUiService();
        var vm = MakeVmWithCards(ui, 2);
        Assert.False(vm.HasChanges);

        vm.CopyAllMarkdownCommand.Execute(null);

        Assert.NotNull(ui.CopiedText);
        Assert.Contains("## カード0", ui.CopiedText);
        Assert.Contains("## カード1", ui.CopiedText);
        Assert.Contains("表示中の2件をMarkdownとしてコピーしました", vm.StatusMessage);
        Assert.False(vm.HasChanges);
    }

    [Fact]
    public void CopyAllMarkdownCommand_Failure_ShowsErrorAndDoesNotMarkDirty_AndNoSuccessStatus()
    {
        var ui = new FakeUiService { ThrowOnCopy = true };
        var vm = MakeVmWithCards(ui, 1);

        vm.CopyAllMarkdownCommand.Execute(null);

        Assert.NotNull(ui.LastErrorMessage);
        Assert.DoesNotContain("コピーしました", vm.StatusMessage);
        Assert.False(vm.HasChanges);
    }

    [Fact]
    public void CopyAllMarkdownCommand_ZeroVisibleCards_NoClipboardWriteAndNoOutput()
    {
        var ui = new FakeUiService();
        var vm = new IdeaNestWorkspaceViewModel(ui);
        vm.LoadFromWorkspace(new Workspace { Ideas = [] });

        vm.CopyAllMarkdownCommand.Execute(null);

        Assert.Null(ui.CopiedText);
        Assert.Equal("出力するカードがありません", vm.StatusMessage);
    }

    [Fact]
    public void ExportMarkdownCommand_Cancel_ProducesNoFileAndNoNotificationChange()
    {
        var ui = new FakeUiService { SavePathToReturn = null };
        var vm = MakeVmWithCards(ui, 1);
        var before = vm.StatusMessage;

        vm.ExportMarkdownCommand.Execute(null);

        Assert.Equal(before, vm.StatusMessage);
        Assert.Null(ui.LastErrorMessage);
        Assert.False(vm.HasChanges);
    }

    [Fact]
    public void ExportMarkdownCommand_Success_WritesUtf8FileAndShowsCountStatus_AndDoesNotMarkDirty()
    {
        var path = Path.Combine(Path.GetTempPath(), $"idea-export-{Guid.NewGuid():N}.md");
        try
        {
            var ui = new FakeUiService { SavePathToReturn = path };
            var vm = MakeVmWithCards(ui, 2);

            vm.ExportMarkdownCommand.Execute(null);

            Assert.True(File.Exists(path));
            var content = File.ReadAllText(path, new UTF8Encoding(true));
            Assert.Contains("## カード0", content);
            Assert.Contains("## カード1", content);
            Assert.Contains("表示中の2件をMarkdownへ保存しました", vm.StatusMessage);
            Assert.False(vm.HasChanges);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ExportMarkdownCommand_ZeroVisibleCards_DoesNotOpenDialogOrCreateFile()
    {
        var ui = new FakeUiService { SavePathToReturn = Path.Combine(Path.GetTempPath(), $"unused-{Guid.NewGuid():N}.md") };
        var vm = new IdeaNestWorkspaceViewModel(ui);
        vm.LoadFromWorkspace(new Workspace { Ideas = [] });

        vm.ExportMarkdownCommand.Execute(null);

        Assert.False(File.Exists(ui.SavePathToReturn));
        Assert.Equal("出力するカードがありません", vm.StatusMessage);
    }

    [Fact]
    public void CopyAllMarkdownCommand_OnlyExportsCurrentlyVisibleCards_RespectingFilterAndOrder()
    {
        var ui = new FakeUiService();
        var vm = new IdeaNestWorkspaceViewModel(ui);
        vm.LoadFromWorkspace(new Workspace
        {
            Ideas =
            [
                new Idea { Id = "a", Title = "表示A", IsArchived = false },
                new Idea { Id = "b", Title = "非表示アーカイブ", IsArchived = true },
                new Idea { Id = "c", Title = "表示C", IsArchived = false },
            ],
        });

        vm.CopyAllMarkdownCommand.Execute(null);

        Assert.NotNull(ui.CopiedText);
        Assert.Contains("表示A", ui.CopiedText);
        Assert.Contains("表示C", ui.CopiedText);
        Assert.DoesNotContain("非表示アーカイブ", ui.CopiedText);
    }

    [Fact]
    public void CopyAllMarkdownCommand_MultipleViewModelInstances_DoNotLeakVisibleCardsAcrossEachOther()
    {
        var uiA = new FakeUiService();
        var uiB = new FakeUiService();
        var vmA = new IdeaNestWorkspaceViewModel(uiA);
        vmA.LoadFromWorkspace(new Workspace { Ideas = [new Idea { Id = "x", Title = "タブA専用" }] });
        var vmB = new IdeaNestWorkspaceViewModel(uiB);
        vmB.LoadFromWorkspace(new Workspace { Ideas = [new Idea { Id = "y", Title = "タブB専用" }] });

        vmA.CopyAllMarkdownCommand.Execute(null);
        vmB.CopyAllMarkdownCommand.Execute(null);

        Assert.Contains("タブA専用", uiA.CopiedText);
        Assert.DoesNotContain("タブB専用", uiA.CopiedText);
        Assert.Contains("タブB専用", uiB.CopiedText);
        Assert.DoesNotContain("タブA専用", uiB.CopiedText);
    }
}
