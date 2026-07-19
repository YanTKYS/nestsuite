using NestSuite.PlainText;
using Xunit;

namespace NestSuite.Tests;

/// <summary>v2.19.0 SH-43: PlainTextWorkspaceViewModel の dirty 契約を確認する。</summary>
public class PlainTextWorkspaceViewModelTests
{
    [Fact]
    public void InitializeAsNew_IsNotDirty_AndHasUtf8NoBomDefaults()
    {
        var vm = new PlainTextWorkspaceViewModel();
        vm.InitializeAsNew();

        Assert.False(vm.IsDirty);
        Assert.Equal(string.Empty, vm.Text);
        Assert.Equal(PlainTextEncodingKind.Utf8NoBom, vm.EncodingKind);
        Assert.Equal(PlainTextNewlineKind.None, vm.NewlineKind);
    }

    [Fact]
    public void LoadContent_IsNotDirty_RightAfterLoad()
    {
        var vm = new PlainTextWorkspaceViewModel();
        vm.LoadContent(new PlainTextLoadResult("loaded text", PlainTextEncodingKind.Utf16LE, PlainTextNewlineKind.Crlf));

        Assert.False(vm.IsDirty);
        Assert.Equal("loaded text", vm.Text);
        Assert.Equal(PlainTextEncodingKind.Utf16LE, vm.EncodingKind);
        Assert.Equal(PlainTextNewlineKind.Crlf, vm.NewlineKind);
    }

    [Fact]
    public void EditingTextAfterLoad_BecomesDirty()
    {
        var vm = new PlainTextWorkspaceViewModel();
        vm.LoadContent(new PlainTextLoadResult("original", PlainTextEncodingKind.Utf8NoBom, PlainTextNewlineKind.None));

        vm.Text = "original edited";

        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void RevertingToOriginalText_DoesNotClearDirty()
    {
        // 内容比較による dirty 解除は導入しない（既存 Workspace の契約に合わせる）。
        var vm = new PlainTextWorkspaceViewModel();
        vm.LoadContent(new PlainTextLoadResult("original", PlainTextEncodingKind.Utf8NoBom, PlainTextNewlineKind.None));

        vm.Text = "changed";
        Assert.True(vm.IsDirty);
        vm.Text = "original";

        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void MarkSaved_ClearsDirty()
    {
        var vm = new PlainTextWorkspaceViewModel();
        vm.LoadContent(new PlainTextLoadResult("original", PlainTextEncodingKind.Utf8NoBom, PlainTextNewlineKind.None));
        vm.Text = "changed";
        Assert.True(vm.IsDirty);

        vm.MarkSaved();

        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void NewTab_FirstEdit_BecomesDirty()
    {
        var vm = new PlainTextWorkspaceViewModel();
        vm.InitializeAsNew();
        Assert.False(vm.IsDirty);

        vm.Text = "typed content";

        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void PropertyChanged_RaisedForTextAndIsDirty()
    {
        var vm = new PlainTextWorkspaceViewModel();
        vm.InitializeAsNew();
        var raised = new List<string>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName!);

        vm.Text = "x";

        Assert.Contains(nameof(PlainTextWorkspaceViewModel.Text), raised);
        Assert.Contains(nameof(PlainTextWorkspaceViewModel.IsDirty), raised);
    }

    [Fact]
    public void Dispose_ClearsPropertyChangedSubscribers()
    {
        var vm = new PlainTextWorkspaceViewModel();
        var invoked = false;
        vm.PropertyChanged += (_, _) => invoked = true;

        vm.Dispose();
        vm.Text = "after dispose";

        Assert.False(invoked);
    }
}
