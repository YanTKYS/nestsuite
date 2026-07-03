using System.Text.Json.Serialization;
using System.Reflection;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.Services;
using Xunit;
using System.Text.Json;

namespace NestSuite.Tests;

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
            Assert.ThrowsAny<JsonException>(() => IdeaNestFileService.Load(path));
            // v2.14.4 FM-4: 数値として解釈できる「より新しい」version は SchemaVersionTooNewException の
            // 対象になるため、ここでは数値比較の対象外（解釈不能）な garbage version で
            // 従来どおりの NotSupportedException 経路を確認する。
            File.WriteAllText(path, """{"version":"unsupported-version","ideas":[],"settings":{}}""");
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

    // ── v2.13.6 TD-45: 保存失敗の契約確認 ────────────────────────────────

    [Fact]
    public void Save_ThrowsWhenParentPathIsAFile()
    {
        // v2.13.6 TD-45: 保存失敗が例外として通知されることを固定する（Shell 共通保存コアの catch がこの契約に依存する）。
        // AtomicFileWriter.WriteAllText は保存先ディレクトリを自動作成するため、
        // 単に「存在しないディレクトリ」を指定しただけでは失敗しない。
        // 既存の「ファイル」を親ディレクトリとして使うことで Directory.CreateDirectory を確実に失敗させる。
        var workspace = new Workspace
        {
            Ideas = new()
            {
                new Idea { Id = "first", Title = "A", Body = "本文", Tags = new() { "tag-a" } },
            }
        };
        var blockingFile = Path.GetTempFileName();
        try
        {
            var path = Path.Combine(blockingFile, "sub", "x.ideanest");
            Assert.ThrowsAny<Exception>(() => IdeaNestFileService.Save(path, workspace));
        }
        finally
        {
            File.Delete(blockingFile);
        }
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

    // ── v2.14.1 FM-1: .nestsuite wrapper 経由の保存・読込 ─────────────────

    [Fact]
    public void SaveLoad_NestSuitePath_RoundTripsViaEnvelope()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.nestsuite");
        try
        {
            var workspace = new Workspace
            {
                Ideas = new()
                {
                    new Idea { Id = "first", Title = "A", Body = "本文", Tags = new() { "tag-a" } },
                    new Idea { Id = "second", Body = "B", Tags = new() { "tag-b" } },
                }
            };
            IdeaNestFileService.Save(path, workspace);

            var loaded = IdeaNestFileService.Load(path);
            Assert.Equal(2, loaded.Ideas.Count);
            Assert.Equal("first", loaded.Ideas[0].Id);
            Assert.Equal("A", loaded.Ideas[0].Title);
            Assert.Equal(IdeaNestFileService.SchemaVersion, loaded.Version);
        }
        finally { File.Delete(path); File.Delete(path + ".bak"); File.Delete(path + ".tmp"); }
    }

    // ── v2.14.4 FM-4: schema version 前方互換ガード ───────────────────────

    [Fact]
    public void Load_NewerSchemaVersion_ThrowsSchemaVersionTooNewException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.ideanest");
        try
        {
            File.WriteAllText(path, """{"version":"9.9.9","ideas":[],"settings":{}}""");
            Assert.Throws<NestSuite.Services.SchemaVersionTooNewException>(() => IdeaNestFileService.Load(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Load_NestSuiteWithWrongKind_Throws()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.nestsuite");
        try
        {
            File.WriteAllText(path, NestSuite.Services.NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "1.4.1", "{}"));

            Assert.Throws<InvalidDataException>(() => IdeaNestFileService.Load(path));
        }
        finally { File.Delete(path); }
    }
}
