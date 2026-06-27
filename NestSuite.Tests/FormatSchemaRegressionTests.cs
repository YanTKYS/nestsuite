using System.Text.Json;
using NestSuite.ChatNest;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.Services;
using NestSuite.Models;
using NestSuite.Services;
using NestSuite.TempNest;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.9.9: 保存形式・スキーマの非変更を自動テストで固定する。
/// v2.9.7〜v2.9.9 の保存処理変更がファイル形式を壊していないことを確認する。
/// </summary>
public class FormatSchemaRegressionTests : IDisposable
{
    private readonly string _tempDir;

    public FormatSchemaRegressionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "FormatSchemaRegressionTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── バージョン定数 ────────────────────────────────────────────────────

    [Fact]
    public void NoteNest_SchemaVersionConstant_Is_1_4_1()
    {
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }

    [Fact]
    public void IdeaNest_SchemaVersionConstant_Is_1_1_4()
    {
        Assert.Equal("1.1.4", IdeaNestFileService.SchemaVersion);
    }

    [Fact]
    public void ChatNest_FileVersionConstant_Is_0_4_1()
    {
        Assert.Equal("0.4.1", ChatNestFileService.FileVersionString);
    }

    [Fact]
    public void ChatNest_FileExtensionConstant_Is_chatnest()
    {
        Assert.Equal(".chatnest", ChatNestFileService.FileExtension);
    }

    [Fact]
    public void IdeaNest_FileExtensionConstant_Is_ideanest()
    {
        Assert.Equal(".ideanest", IdeaNestFileService.FileExtension);
    }

    [Fact]
    public void TempNest_DefaultJsonVersion_Is_1()
    {
        var data = new TempNestStoreData();
        Assert.Equal(1, data.Version);
    }

    // ── NoteNest JSON 構造 ────────────────────────────────────────────────

    [Fact]
    public void NoteNest_SerializedJson_ContainsProjectNameKey()
    {
        var path = Path.Combine(_tempDir, "test.notenest");
        new ProjectFileService().Save(path, new Project { ProjectName = "TestProject" });
        var json = File.ReadAllText(path);
        Assert.Contains("\"projectName\"", json);
        Assert.Contains("TestProject", json);
    }

    [Fact]
    public void NoteNest_SerializedJson_ContainsNotebooksKey()
    {
        var path = Path.Combine(_tempDir, "test.notenest");
        new ProjectFileService().Save(path, new Project());
        var json = File.ReadAllText(path);
        Assert.Contains("\"notebooks\"", json);
    }

    [Fact]
    public void NoteNest_SerializedJson_ContainsTasksKey()
    {
        var path = Path.Combine(_tempDir, "test.notenest");
        new ProjectFileService().Save(path, new Project());
        var json = File.ReadAllText(path);
        Assert.Contains("\"tasks\"", json);
    }

    [Fact]
    public void NoteNest_SerializedJson_ContainsSettingsKey()
    {
        var path = Path.Combine(_tempDir, "test.notenest");
        new ProjectFileService().Save(path, new Project());
        var json = File.ReadAllText(path);
        Assert.Contains("\"settings\"", json);
    }

    [Fact]
    public void NoteNest_SavedJson_IsValidJson()
    {
        var path = Path.Combine(_tempDir, "test.notenest");
        new ProjectFileService().Save(path, new Project { ProjectName = "Test" });
        var json = File.ReadAllText(path);
        Assert.True(IsValidJson(json), "保存された .notenest ファイルは有効な JSON である必要がある");
    }

    [Fact]
    public void NoteNest_SaveLoad_PreservesSchemaVersion()
    {
        // スキーマバージョン定数は load/save を経ても変わらない
        var path = Path.Combine(_tempDir, "schema.notenest");
        var svc = new ProjectFileService();
        svc.Save(path, new Project());
        var loaded = svc.Load(path);
        // Project.Version はデフォルト "0.1.0" だが CurrentSchemaVersion は "1.4.1"
        // スキーマバージョン定数が変わらないことを確認する
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
        Assert.NotEqual("1.4.1", loaded.Version); // Version プロパティはスキーマバージョンではない
    }

    [Fact]
    public void NoteNest_SaveLoad_RoundTrip_PreservesNotebookAndNoteStructure()
    {
        var path = Path.Combine(_tempDir, "roundtrip.notenest");
        var svc = new ProjectFileService();
        var nb = new Notebook { Title = "TestBook" };
        nb.Notes.Add(new Note { Title = "TestNote", Content = "本文テスト" });
        var project = new Project { ProjectName = "RoundTrip" };
        project.Notebooks.Add(nb);

        svc.Save(path, project);
        var loaded = svc.Load(path);

        Assert.Equal("RoundTrip", loaded.ProjectName);
        Assert.Single(loaded.Notebooks);
        Assert.Equal("TestBook", loaded.Notebooks[0].Title);
        Assert.Single(loaded.Notebooks[0].Notes);
        Assert.Equal("TestNote",  loaded.Notebooks[0].Notes[0].Title);
        Assert.Equal("本文テスト", loaded.Notebooks[0].Notes[0].Content);
    }

    // ── IdeaNest JSON 構造 ────────────────────────────────────────────────

    [Fact]
    public void IdeaNest_SerializedJson_ContainsVersionField()
    {
        var path = Path.Combine(_tempDir, "test.ideanest");
        IdeaNestFileService.Save(path, new Workspace());
        var json = File.ReadAllText(path);
        Assert.Contains("\"version\"", json);
        Assert.Contains("1.1.4", json);
    }

    [Fact]
    public void IdeaNest_SerializedJson_ContainsIdeasField()
    {
        var path = Path.Combine(_tempDir, "test.ideanest");
        IdeaNestFileService.Save(path, new Workspace());
        var json = File.ReadAllText(path);
        Assert.Contains("\"ideas\"", json);
    }

    [Fact]
    public void IdeaNest_SerializedJson_ContainsSettingsField()
    {
        var path = Path.Combine(_tempDir, "test.ideanest");
        IdeaNestFileService.Save(path, new Workspace());
        var json = File.ReadAllText(path);
        Assert.Contains("\"settings\"", json);
    }

    [Fact]
    public void IdeaNest_SavedJson_IsValidJson()
    {
        var path = Path.Combine(_tempDir, "test.ideanest");
        IdeaNestFileService.Save(path, new Workspace());
        var json = File.ReadAllText(path);
        Assert.True(IsValidJson(json), "保存された .ideanest ファイルは有効な JSON である必要がある");
    }

    [Fact]
    public void IdeaNest_SaveLoad_PreservesVersion()
    {
        var path = Path.Combine(_tempDir, "ver.ideanest");
        IdeaNestFileService.Save(path, new Workspace());
        var loaded = IdeaNestFileService.Load(path);
        Assert.Equal("1.1.4", loaded.Version);
    }

    // ── ChatNest JSON 構造 ────────────────────────────────────────────────

    [Fact]
    public void ChatNest_SerializedJson_ContainsVersionField()
    {
        var path = Path.Combine(_tempDir, "test.chatnest");
        ChatNestFileService.Save(path, []);
        var json = File.ReadAllText(path);
        Assert.Contains("\"version\"", json);
        Assert.Contains("0.4.1", json);
    }

    [Fact]
    public void ChatNest_SerializedJson_ContainsMessagesField()
    {
        var path = Path.Combine(_tempDir, "test.chatnest");
        ChatNestFileService.Save(path, []);
        var json = File.ReadAllText(path);
        Assert.Contains("\"messages\"", json);
    }

    [Fact]
    public void ChatNest_SavedJson_IsValidJson()
    {
        var path = Path.Combine(_tempDir, "test.chatnest");
        ChatNestFileService.Save(path, []);
        var json = File.ReadAllText(path);
        Assert.True(IsValidJson(json), "保存された .chatnest ファイルは有効な JSON である必要がある");
    }

    [Fact]
    public void ChatNest_SaveLoad_PreservesMessages()
    {
        var path = Path.Combine(_tempDir, "msgs.chatnest");
        var messages = new[]
        {
            new Message { Speaker = Speaker.自分, Text = "テストメッセージ" },
            new Message { Speaker = Speaker.反論, Text = "反論テスト" },
        };
        ChatNestFileService.Save(path, messages);
        var loaded = ChatNestFileService.Load(path);
        Assert.Equal(2, loaded.Count);
        Assert.Equal("テストメッセージ", loaded[0].Text);
        Assert.Equal("反論テスト",       loaded[1].Text);
    }

    // ── TempNest JSON 構造 ────────────────────────────────────────────────

    [Fact]
    public void TempNest_StoreData_DefaultVersionIs1()
    {
        var data = new TempNestStoreData { Slots = [] };
        var json = JsonSerializer.Serialize(data);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetProperty("Version").GetInt32());
    }

    [Fact]
    public void TempNest_StoreData_HasSlotsArray()
    {
        var data = new TempNestStoreData { Slots = [new TempNestSlot { Title = "A", Body = "B" }] };
        var json = JsonSerializer.Serialize(data);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("Slots").ValueKind);
        Assert.Equal(1, doc.RootElement.GetProperty("Slots").GetArrayLength());
    }

    // ── セッション形式 ────────────────────────────────────────────────────

    [Fact]
    public void Session_DefaultState_HasEmptyFilePathsAndNullActivePath()
    {
        var state = new NestSuiteSessionState();
        Assert.Empty(state.FilePaths);
        Assert.Null(state.ActiveFilePath);
    }

    [Fact]
    public void Session_SerializedJson_ContainsFilePathsField()
    {
        var state = new NestSuiteSessionState { FilePaths = ["/some/path.notenest"] };
        var json = JsonSerializer.Serialize(state);
        Assert.Contains("FilePaths", json);
        Assert.Contains("path.notenest", json);
    }

    [Fact]
    public void Session_SerializedJson_ContainsActiveFilePathField()
    {
        var state = new NestSuiteSessionState { ActiveFilePath = "/active.notenest" };
        var json = JsonSerializer.Serialize(state);
        Assert.Contains("ActiveFilePath", json);
    }

    [Fact]
    public void Session_RoundTrip_PreservesFilePathsAndActivePath()
    {
        var path = Path.Combine(_tempDir, "session.json");
        var state = new NestSuiteSessionState
        {
            FilePaths = ["/a.notenest", "/b.chatnest"],
            ActiveFilePath = "/a.notenest"
        };
        var svc = new NestSuiteSessionStateService(path);
        svc.Save(state);
        var loaded = svc.Load();
        Assert.Equal(state.FilePaths, loaded.FilePaths);
        Assert.Equal(state.ActiveFilePath, loaded.ActiveFilePath);
    }

    // ── バージョン ───────────────────────────────────────────────────────

    [Fact]
    public void ApplicationVersion_Is_2_10_0()
    {
        Assert.Equal("2.10.11", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void NoteNestSchemaVersion_Remains_1_4_1()
    {
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }

    // ── helpers ─────────────────────────────────────────────────────────

    private static bool IsValidJson(string json)
    {
        try { JsonDocument.Parse(json); return true; }
        catch { return false; }
    }
}
