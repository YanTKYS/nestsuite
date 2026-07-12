using System.Text.Json;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

public class DraftStoreTests
{
    [Fact]
    public void TryGetTabId_AcceptsGuidNAndUppercaseAndRejectsSidecars()
    {
        var id = Guid.NewGuid().ToString("N");
        Assert.True(DraftStore.TryGetTabId($"draft-{id}.nestsuite", out var parsed));
        Assert.Equal(id, parsed);
        Assert.True(DraftStore.TryGetTabId($"draft-{id.ToUpperInvariant()}.nestsuite", out parsed));
        Assert.Equal(id, parsed);
        Assert.False(DraftStore.TryGetTabId($"draft-{id}.state.json", out _));
        Assert.False(DraftStore.TryGetTabId($"draft-{id}.nestsuite.tmp", out _));
        Assert.False(DraftStore.TryGetTabId($"draft-{id}.nestsuite.corrupt-20260712-120000", out _));
        Assert.False(DraftStore.TryGetTabId("draft-tempnest-fixed.nestsuite", out _));
        Assert.False(DraftStore.TryGetTabId("../draft-" + id + ".nestsuite", out _));
    }

    [Fact]
    public void ListDraftFiles_ReturnsOnlyValidDraftsInStableOrder()
    {
        var root = NewRoot();
        var a = Guid.NewGuid().ToString("N");
        var b = Guid.NewGuid().ToString("N");
        File.WriteAllText(Path.Combine(root, $"draft-{b}.nestsuite"), "{}");
        File.WriteAllText(Path.Combine(root, $"draft-{a}.nestsuite"), "{}");
        File.WriteAllText(Path.Combine(root, $"draft-{a}.state.json"), "{}");
        File.WriteAllText(Path.Combine(root, $"draft-{a}.nestsuite.tmp"), "{}");
        File.WriteAllText(Path.Combine(root, "draft-not-a-guid.nestsuite"), "{}");

        var files = DraftStore.ListDraftFiles(root);

        Assert.Equal(files.OrderBy(x => x, StringComparer.Ordinal), files);
        Assert.Equal(2, files.Count);
        Assert.All(files, f => Assert.EndsWith(".nestsuite", f));
    }

    [Fact]
    public void WriteWorkspaceDraft_WithTransientState_WritesBodySidecarHashAndNoBak()
    {
        var root = NewRoot();
        var id = Guid.NewGuid().ToString("N");
        var state = new ChatNestTransientDraftState("hello", "自分", Guid.NewGuid(), "edit");

        DraftStore.WriteWorkspaceDraft(id, "{\"format\":\"nestsuite.workspace\"}", state, root);

        var body = Path.Combine(root, $"draft-{id}.nestsuite");
        var sidecar = Path.Combine(root, $"draft-{id}.state.json");
        Assert.True(File.Exists(body));
        Assert.True(File.Exists(sidecar));
        Assert.False(File.Exists(body + ".bak"));
        using var json = JsonDocument.Parse(File.ReadAllText(sidecar));
        Assert.Equal(DraftStore.CurrentDraftFormatVersion, json.RootElement.GetProperty("draftFormatVersion").GetString());
        Assert.Equal("ChatNest", json.RootElement.GetProperty("workspaceKind").GetString());
        Assert.Equal("hello", json.RootElement.GetProperty("transientState").GetProperty("inputText").GetString());
    }

    [Fact]
    public void WriteWorkspaceDraft_WithoutTransientState_RemovesActiveSidecarButKeepsCorrupt()
    {
        var root = NewRoot();
        var id = Guid.NewGuid().ToString("N");
        DraftStore.WriteWorkspaceDraft(id, "{}", new ChatNestTransientDraftState("hello", "自分", null, ""), root);
        var corrupt = Path.Combine(root, $"draft-{id}.state.json.corrupt-20260712-120000");
        File.WriteAllText(corrupt, "old");

        DraftStore.WriteWorkspaceDraft(id, "{}", null, root);

        Assert.False(File.Exists(Path.Combine(root, $"draft-{id}.state.json")));
        Assert.True(File.Exists(corrupt));
    }

    [Fact]
    public void Delete_RemovesOnlyActivePair()
    {
        var root = NewRoot();
        var id = Guid.NewGuid().ToString("N");
        var other = Guid.NewGuid().ToString("N");
        DraftStore.WriteWorkspaceDraft(id, "{}", new ChatNestTransientDraftState("hello", "自分", null, ""), root);
        DraftStore.WriteWorkspaceDraft(other, "{}", null, root);
        var corrupt = Path.Combine(root, $"draft-{id}.nestsuite.corrupt-20260712-120000");
        File.WriteAllText(corrupt, "old");

        DraftStore.Delete(id, root);
        DraftStore.Delete(id, root);

        Assert.False(File.Exists(Path.Combine(root, $"draft-{id}.nestsuite")));
        Assert.False(File.Exists(Path.Combine(root, $"draft-{id}.state.json")));
        Assert.True(File.Exists(Path.Combine(root, $"draft-{other}.nestsuite")));
        Assert.True(File.Exists(corrupt));
    }

    [Fact]
    public void ReadTransientState_ClassifiesLoadedNotPresentHashMismatchAndInvalid()
    {
        var root = NewRoot();
        var id = Guid.NewGuid().ToString("N");
        var draft = Path.Combine(root, $"draft-{id}.nestsuite");
        DraftStore.WriteWorkspaceDraft(id, "{}", null, root);
        Assert.Equal(TransientDraftReadStatus.NotPresent, DraftStore.ReadTransientState(draft, root).Status);

        DraftStore.WriteWorkspaceDraft(id, "{}", new ChatNestTransientDraftState("hello", "自分", null, ""), root);
        Assert.Equal(TransientDraftReadStatus.Loaded, DraftStore.ReadTransientState(draft, root).Status);

        File.WriteAllText(draft, "changed");
        Assert.Equal(TransientDraftReadStatus.HashMismatch, DraftStore.ReadTransientState(draft, root).Status);

        File.WriteAllText(Path.Combine(root, $"draft-{id}.state.json"), "{");
        Assert.Equal(TransientDraftReadStatus.InvalidFormat, DraftStore.ReadTransientState(draft, root).Status);
    }

    [Fact]
    public void Quarantine_RenamesWithoutDeletingAndExcludesFromList()
    {
        var root = NewRoot();
        var id = Guid.NewGuid().ToString("N");
        DraftStore.WriteWorkspaceDraft(id, "body", new ChatNestTransientDraftState("hello", "自分", null, ""), root);
        var draft = Path.Combine(root, $"draft-{id}.nestsuite");

        var quarantined = DraftStore.QuarantineWorkspaceDraft(draft);

        Assert.False(File.Exists(draft));
        Assert.True(File.Exists(quarantined));
        Assert.Contains("body", File.ReadAllText(quarantined));
        Assert.Empty(DraftStore.ListDraftFiles(root));
    }

    private static string NewRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "nestsuite-draft-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
