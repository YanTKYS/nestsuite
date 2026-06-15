using System.Text.Json.Serialization;
using System.Reflection;
using NoteNest.NestSuite.IdeaNest.Models;
using NoteNest.NestSuite.IdeaNest.Services;
using Xunit;
using System.Text.Json;

namespace NoteNest.Tests;

/// <summary>
/// v1.8.2: IdeaNestFileService 定数の確認および
/// IdeaNest モデルの [JsonPropertyName] 属性適用確認テスト。
/// JSON キー名が IdeaNest v1.1.4 の camelCase 形式と互換であることを保証する。
/// </summary>
public class IdeaNestFileServiceTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsCardsTagsOrderAndVersion()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.ideanest");
        var created = new DateTime(2026, 2, 3, 4, 5, 6);
        var updated = created.AddDays(1);
        try
        {
            var workspace = new Workspace
            {
                Ideas = new()
                {
                    new Idea { Id = "first", Title = "A", Body = "本文", Tags = new() { "tag-a" }, CreatedAt = created, UpdatedAt = updated },
                    new Idea { Id = "second", Body = "B", Tags = new() { "tag-b" } },
                }
            };
            IdeaNestFileService.Save(path, workspace);
            Assert.True(File.Exists(path));
            using var json = JsonDocument.Parse(File.ReadAllText(path));
            Assert.Equal(IdeaNestSchema.CurrentVersion, json.RootElement.GetProperty("version").GetString());

            var loaded = IdeaNestFileService.Load(path);
            Assert.Equal(IdeaNestSchema.CurrentVersion, loaded.Version);
            Assert.Equal(new[] { "first", "second" }, loaded.Ideas.Select(i => i.Id));
            Assert.Equal("本文", loaded.Ideas[0].Body);
            Assert.Equal("tag-a", loaded.Ideas[0].Tags.Single());
            Assert.Equal(created, loaded.Ideas[0].CreatedAt);
            Assert.Equal(updated, loaded.Ideas[0].UpdatedAt);
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally { File.Delete(path); File.Delete(path + ".bak"); File.Delete(path + ".tmp"); }
    }

    [Fact]
    public void Load_RejectsWrongExtensionBrokenJsonAndUnsupportedVersion()
    {
        var wrong = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        Assert.Throws<NotSupportedException>(() => IdeaNestFileService.Load(wrong));
        var path = Path.ChangeExtension(wrong, ".ideanest");
        try
        {
            File.WriteAllText(path, "{broken");
            Assert.Throws<JsonException>(() => IdeaNestFileService.Load(path));
            File.WriteAllText(path, """{"version":"99.0","ideas":[],"settings":{}}""");
            Assert.Throws<NotSupportedException>(() => IdeaNestFileService.Load(path));
            File.WriteAllText(path, """{"ideas":[],"settings":{}}""");
            Assert.Throws<InvalidDataException>(() => IdeaNestFileService.Load(path));
        }
        finally { File.Delete(path); }
    }
    // ── IdeaNestFileService 定数 ─────────────────────────────────────────

    [Fact]
    public void FileExtension_IsExpected()
    {
        Assert.Equal(".ideanest", IdeaNestFileService.FileExtension);
    }

    [Fact]
    public void SchemaVersion_IsExpected()
    {
        Assert.Equal("1.1.4", IdeaNestFileService.SchemaVersion);
    }

    [Fact]
    public void NewWorkspace_UsesCurrentSchemaVersion()
    {
        // Workspace.Version default must stay in sync with IdeaNestFileService.SchemaVersion.
        // This catches the case where one is updated without the other.
        Assert.Equal(IdeaNestFileService.SchemaVersion, new Workspace().Version);
    }

    // ── Idea モデルの [JsonPropertyName] 属性確認 ───────────────────────

    [Theory]
    [InlineData("Id", "id")]
    [InlineData("Title", "title")]
    [InlineData("Body", "body")]
    [InlineData("Tags", "tags")]
    [InlineData("Color", "color")]
    [InlineData("IsPinned", "isPinned")]
    [InlineData("IsArchived", "isArchived")]
    [InlineData("CreatedAt", "createdAt")]
    [InlineData("UpdatedAt", "updatedAt")]
    public void Idea_Property_HasJsonPropertyNameAttribute(string propertyName, string expectedJsonName)
    {
        var prop = typeof(Idea).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        var attr = prop!.GetCustomAttribute<JsonPropertyNameAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(expectedJsonName, attr!.Name);
    }

    // ── Workspace モデルの [JsonPropertyName] 属性確認 ───────────────────

    [Theory]
    [InlineData("Version", "version")]
    [InlineData("WorkspaceName", "workspaceName")]
    [InlineData("Ideas", "ideas")]
    [InlineData("Settings", "settings")]
    public void Workspace_Property_HasJsonPropertyNameAttribute(string propertyName, string expectedJsonName)
    {
        var prop = typeof(Workspace).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        var attr = prop!.GetCustomAttribute<JsonPropertyNameAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(expectedJsonName, attr!.Name);
    }

    // ── WorkspaceSettings モデルの [JsonPropertyName] 属性確認 ───────────

    [Theory]
    [InlineData("SearchText", "searchText")]
    [InlineData("SelectedTag", "selectedTag")]
    [InlineData("SelectedColor", "selectedColor")]
    [InlineData("ShowArchived", "showArchived")]
    [InlineData("TagPanelOpen", "tagPanelOpen")]
    [InlineData("CardSize", "cardSize")]
    [InlineData("CardHeightMode", "cardHeightMode")]
    [InlineData("SortMode", "sortMode")]
    [InlineData("WindowWidth", "windowWidth")]
    [InlineData("WindowHeight", "windowHeight")]
    public void WorkspaceSettings_Property_HasJsonPropertyNameAttribute(string propertyName, string expectedJsonName)
    {
        var prop = typeof(WorkspaceSettings).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        var attr = prop!.GetCustomAttribute<JsonPropertyNameAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(expectedJsonName, attr!.Name);
    }
}
