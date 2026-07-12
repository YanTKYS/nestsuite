using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

public class DraftStoreTests
{
    [Fact]
    public void TryGetTabId_AcceptsGuidNUppercaseFullPathAndRejectsInvalidNames()
    {
        var id = Guid.NewGuid().ToString("N");
        Assert.True(DraftStore.TryGetTabId($"draft-{id}.nestsuite", out var parsed));
        Assert.Equal(id, parsed);
        Assert.True(DraftStore.TryGetTabId($"draft-{id.ToUpperInvariant()}.nestsuite", out parsed));
        Assert.Equal(id, parsed);
        Assert.True(DraftStore.TryGetTabId(Path.Combine(Path.GetTempPath(), "parent.with.dots", $"draft-{id}.nestsuite"), out parsed));
        Assert.Equal(id, parsed);
        Assert.False(DraftStore.TryGetTabId($"draft-{id}.state.json", out _));
        Assert.False(DraftStore.TryGetTabId($"draft-{id}.nestsuite.tmp", out _));
        Assert.False(DraftStore.TryGetTabId($"draft-{id}.nestsuite.corrupt-20260712-120000", out _));
        Assert.False(DraftStore.TryGetTabId("draft-tempnest-fixed.nestsuite", out _));
        Assert.False(DraftStore.TryGetTabId("draft-.." + id + ".nestsuite", out _));
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
    public void ReadTransientState_ClassifiesLoadedNotPresentHashMismatchInvalidUnsupportedAndIoError()
    {
        var root = NewRoot();
        var id = Guid.NewGuid().ToString("N");
        var draft = Path.Combine(root, $"draft-{id}.nestsuite");
        var sidecar = Path.Combine(root, $"draft-{id}.state.json");
        DraftStore.WriteWorkspaceDraft(id, "{}", null, root);
        Assert.Equal(TransientDraftReadStatus.NotPresent, DraftStore.ReadTransientState(draft, root).Status);

        DraftStore.WriteWorkspaceDraft(id, "{}", new ChatNestTransientDraftState("hello", "自分", null, ""), root);
        Assert.Equal(TransientDraftReadStatus.Loaded, DraftStore.ReadTransientState(draft, root).Status);

        SetSidecarProperty(sidecar, "draftFormatVersion", "9.9");
        var unsupported = DraftStore.ReadTransientState(draft, root);
        Assert.Equal(TransientDraftReadStatus.UnsupportedVersion, unsupported.Status);
        Assert.Null(unsupported.State);

        DraftStore.WriteWorkspaceDraft(id, "{}", new ChatNestTransientDraftState("hello", "自分", null, ""), root);
        File.WriteAllText(draft, "changed");
        Assert.Equal(TransientDraftReadStatus.HashMismatch, DraftStore.ReadTransientState(draft, root).Status);

        File.WriteAllText(sidecar, "{");
        Assert.Equal(TransientDraftReadStatus.InvalidFormat, DraftStore.ReadTransientState(draft, root).Status);

        File.Delete(sidecar);
        Directory.CreateDirectory(sidecar);
        Assert.Equal(TransientDraftReadStatus.IoError, DraftStore.ReadTransientState(draft, root).Status);
    }

    [Theory]
    [InlineData("自分", "自分")]
    [InlineData("反論", "反論")]
    [InlineData("補足", "補足")]
    [InlineData("結論", "結論")]
    [InlineData(null, "自分")]
    [InlineData("", "自分")]
    [InlineData("   ", "自分")]
    [InlineData("UnknownSpeaker", "自分")]
    [InlineData("0", "自分")]
    [InlineData("self", "自分")]
    public void ReadTransientState_NormalizesSelectedSpeakerButKeepsLoaded(string? speaker, string expected)
    {
        var root = NewRoot();
        var id = Guid.NewGuid().ToString("N");
        var draft = Path.Combine(root, $"draft-{id}.nestsuite");
        var sidecar = Path.Combine(root, $"draft-{id}.state.json");
        DraftStore.WriteWorkspaceDraft(id, "{}", new ChatNestTransientDraftState("hello", "自分", null, ""), root);
        SetSidecarProperty(sidecar, "transientState", "selectedSpeaker", speaker);

        var result = DraftStore.ReadTransientState(draft, root);

        Assert.Equal(TransientDraftReadStatus.Loaded, result.Status);
        Assert.NotNull(result.State);
        Assert.Equal(expected, result.State!.SelectedSpeaker);
    }

    [Fact]
    public void QuarantineWorkspaceDraft_RenamesWithoutDeletingAndExcludesFromList()
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

    [Fact]
    public void QuarantineTransientState_RenamesOnlySidecarAndKeepsBody()
    {
        var root = NewRoot();
        var id = Guid.NewGuid().ToString("N");
        DraftStore.WriteWorkspaceDraft(id, "body", new ChatNestTransientDraftState("hello", "自分", null, ""), root);
        var draft = Path.Combine(root, $"draft-{id}.nestsuite");
        var sidecar = Path.Combine(root, $"draft-{id}.state.json");
        File.WriteAllText(draft, "changed");
        Assert.Equal(TransientDraftReadStatus.HashMismatch, DraftStore.ReadTransientState(draft, root).Status);

        var quarantinedSidecar = DraftStore.QuarantineTransientState(draft);

        Assert.NotNull(quarantinedSidecar);
        Assert.True(File.Exists(draft));
        Assert.False(File.Exists(sidecar));
        Assert.Contains("hello", File.ReadAllText(quarantinedSidecar!));
        Assert.Empty(DraftStore.ListDraftFiles(root));
        Assert.Null(DraftStore.QuarantineTransientState(draft));
    }

    [Fact]
    public void Quarantine_DoesNotOverwriteExistingCorruptTarget()
    {
        var root = NewRoot();
        var id = Guid.NewGuid().ToString("N");
        var draft = Path.Combine(root, $"draft-{id}.nestsuite");
        var fixedNow = new DateTime(2026, 7, 12, 12, 0, 0);
        var expectedCollision = draft + ".corrupt-20260712-120000";
        File.WriteAllText(expectedCollision, "existing");
        DraftStore.WriteWorkspaceDraft(id, "new-body", null, root);

        WithNowProvider(fixedNow, () =>
        {
            var quarantined = DraftStore.QuarantineWorkspaceDraft(draft);
            Assert.EndsWith("-1", quarantined);
            Assert.Equal("existing", File.ReadAllText(expectedCollision));
            Assert.Equal("new-body", File.ReadAllText(quarantined));
        });
    }

    private static void SetSidecarProperty(string sidecarPath, string propertyName, string value)
    {
        var node = JsonNode.Parse(File.ReadAllText(sidecarPath))!.AsObject();
        node[propertyName] = value;
        File.WriteAllText(sidecarPath, node.ToJsonString());
    }

    private static void SetSidecarProperty(string sidecarPath, string objectName, string propertyName, string? value)
    {
        var node = JsonNode.Parse(File.ReadAllText(sidecarPath))!.AsObject();
        node[objectName]![propertyName] = value is null ? null : JsonValue.Create(value);
        File.WriteAllText(sidecarPath, node.ToJsonString());
    }

    private static void WithNowProvider(DateTime now, Action action)
    {
        var property = typeof(DraftStore).GetProperty("NowProvider", BindingFlags.NonPublic | BindingFlags.Static)!;
        var original = property.GetValue(null);
        property.SetValue(null, (Func<DateTime>)(() => now));
        try { action(); }
        finally { property.SetValue(null, original); }
    }

    private static string NewRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "nestsuite-draft-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
