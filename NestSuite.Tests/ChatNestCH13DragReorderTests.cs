using System.IO;
using System.Linq;
using NestSuite.ChatNest;
using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.10.9 CH-13: ChatNest 発言ドラッグ並び替えの単体テスト + 回帰テスト。
/// </summary>
public class ChatNestCH13DragReorderTests
{
    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    // ── バージョン ────────────────────────────────────────────────────────

    [Fact]
    public void ApplicationVersion_Is_2_10_9()
    {
        Assert.Equal("2.10.11", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void NoteNestSchemaVersion_Remains_1_4_1()
    {
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }

    // ── CH-13: MoveMessage 基本動作 ─────────────────────────────────────

    [Fact]
    public void MoveMessage_ChangesOrder()
    {
        var vm = CreateVm(Speaker.自分, Speaker.反論, Speaker.補足);
        vm.MoveMessage(0, 2);
        var models = vm.MessageModels.ToList();
        Assert.Equal(Speaker.反論, models[0].Speaker);
        Assert.Equal(Speaker.補足, models[1].Speaker);
        Assert.Equal(Speaker.自分, models[2].Speaker);
    }

    [Fact]
    public void MoveMessage_FirstToLast()
    {
        var vm = CreateVm(Speaker.自分, Speaker.反論, Speaker.補足, Speaker.結論);
        vm.MoveMessage(0, 3);
        var models = vm.MessageModels.ToList();
        Assert.Equal(Speaker.反論,  models[0].Speaker);
        Assert.Equal(Speaker.補足,  models[1].Speaker);
        Assert.Equal(Speaker.結論,  models[2].Speaker);
        Assert.Equal(Speaker.自分,  models[3].Speaker);
    }

    [Fact]
    public void MoveMessage_LastToFirst()
    {
        var vm = CreateVm(Speaker.自分, Speaker.反論, Speaker.補足, Speaker.結論);
        vm.MoveMessage(3, 0);
        var models = vm.MessageModels.ToList();
        Assert.Equal(Speaker.結論,  models[0].Speaker);
        Assert.Equal(Speaker.自分,  models[1].Speaker);
        Assert.Equal(Speaker.反論,  models[2].Speaker);
        Assert.Equal(Speaker.補足,  models[3].Speaker);
    }

    [Fact]
    public void MoveMessage_SameIndex_DoesNotChangeOrder()
    {
        var vm = CreateVm(Speaker.自分, Speaker.反論, Speaker.補足);
        vm.MoveMessage(1, 1);
        var models = vm.MessageModels.ToList();
        Assert.Equal(Speaker.自分,  models[0].Speaker);
        Assert.Equal(Speaker.反論,  models[1].Speaker);
        Assert.Equal(Speaker.補足,  models[2].Speaker);
    }

    [Fact]
    public void MoveMessage_InvalidOldIndex_DoesNotThrow()
    {
        var vm = CreateVm(Speaker.自分, Speaker.反論);
        var ex = Record.Exception(() => vm.MoveMessage(-1, 0));
        Assert.Null(ex);
        ex = Record.Exception(() => vm.MoveMessage(5, 0));
        Assert.Null(ex);
    }

    [Fact]
    public void MoveMessage_InvalidNewIndex_DoesNotThrow()
    {
        var vm = CreateVm(Speaker.自分, Speaker.反論);
        var ex = Record.Exception(() => vm.MoveMessage(0, -1));
        Assert.Null(ex);
        ex = Record.Exception(() => vm.MoveMessage(0, 5));
        Assert.Null(ex);
    }

    [Fact]
    public void MoveMessage_SingleMessage_DoesNotThrow()
    {
        var vm = CreateVm(Speaker.自分);
        var ex = Record.Exception(() => vm.MoveMessage(0, 0));
        Assert.Null(ex);
    }

    [Fact]
    public void MoveMessage_EmptyCollection_DoesNotThrow()
    {
        var vm = new ChatNestWorkspaceViewModel();
        var ex = Record.Exception(() => vm.MoveMessage(0, 0));
        Assert.Null(ex);
    }

    // ── CH-13: IsDirty / WorkspaceModified ───────────────────────────────

    [Fact]
    public void MoveMessage_SetsDirty()
    {
        var vm = CreateVm(Speaker.自分, Speaker.反論, Speaker.補足);
        vm.MarkSaved();
        Assert.False(vm.IsDirty);
        vm.MoveMessage(0, 2);
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void MoveMessage_SameIndex_DoesNotSetDirty()
    {
        var vm = CreateVm(Speaker.自分, Speaker.反論);
        vm.MarkSaved();
        vm.MoveMessage(0, 0);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void MoveMessage_FiresWorkspaceModified()
    {
        var vm = CreateVm(Speaker.自分, Speaker.反論, Speaker.補足);
        bool fired = false;
        vm.WorkspaceModified += (_, _) => fired = true;
        vm.MoveMessage(0, 2);
        Assert.True(fired);
    }

    // ── CH-13: ID / timestamp / speaker / text 不変 ──────────────────────

    [Fact]
    public void MoveMessage_PreservesId()
    {
        var msg = new Message { Speaker = Speaker.自分, Text = "テスト" };
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages(new[] { msg, new Message { Speaker = Speaker.反論, Text = "B" } });
        vm.MoveMessage(0, 1);
        Assert.Equal(msg.Id, vm.MessageModels.Last().Id);
    }

    [Fact]
    public void MoveMessage_PreservesCreatedAt()
    {
        var msg = new Message { Speaker = Speaker.自分, Text = "テスト" };
        var originalTime = msg.CreatedAt;
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages(new[] { msg, new Message { Speaker = Speaker.反論, Text = "B" } });
        vm.MoveMessage(0, 1);
        Assert.Equal(originalTime, vm.MessageModels.Last().CreatedAt);
    }

    [Fact]
    public void MoveMessage_PreservesSpeakerAndText()
    {
        var vm = CreateVm(Speaker.自分, Speaker.反論, Speaker.補足);
        var originalModels = vm.MessageModels.ToList();
        vm.MoveMessage(0, 2);
        var movedModels = vm.MessageModels.ToList();
        Assert.Equal(Speaker.反論, movedModels[0].Speaker);
        Assert.Equal(Speaker.補足, movedModels[1].Speaker);
        Assert.Equal(Speaker.自分, movedModels[2].Speaker);
        Assert.Equal(originalModels[0].Text, movedModels[2].Text);
    }

    // ── CH-13: MessageModels 出力順との整合 ──────────────────────────────

    [Fact]
    public void MessageModels_ReflectsReorderedSequence()
    {
        var vm = CreateVm(Speaker.自分, Speaker.反論, Speaker.補足, Speaker.結論);
        vm.MoveMessage(3, 0);
        var speakers = vm.MessageModels.Select(m => m.Speaker).ToList();
        Assert.Equal(new[] { Speaker.結論, Speaker.自分, Speaker.反論, Speaker.補足 }, speakers);
    }

    // ── CH-13: backlog / release-notes ───────────────────────────────────

    [Fact]
    public void Backlog_CH13_IsMarkedComplete()
    {
        Assert.Contains("~~CH-13~~", ReadBacklog());
    }

    [Fact]
    public void ReleaseNotes_Contains_V2109()
    {
        var path = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(path));
        Assert.Contains("v2.10.9", File.ReadAllText(path));
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private static ChatNestWorkspaceViewModel CreateVm(params Speaker[] speakers)
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages(speakers.Select((s, i) => new Message { Speaker = s, Text = $"発言{i}" }));
        return vm;
    }

    private string ReadBacklog()
    {
        var path = Path.Combine(RepoRoot, "docs", "backlog.md");
        Assert.True(File.Exists(path), $"backlog.md not found: {path}");
        return File.ReadAllText(path);
    }
}
