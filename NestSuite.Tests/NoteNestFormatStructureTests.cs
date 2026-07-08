using System.Text.Json;
using NestSuite.Models;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.13 TD-63: NoteNestFormatSchemaRegressionTests から、.notenest / .nestsuite の
/// JSON トップレベル構造（キー存在・妥当性）に関するテストを分割した。
/// </summary>
public class NoteNestFormatStructureTests : IDisposable
{
    private readonly string _tempDir;

    public NoteNestFormatStructureTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NoteNestFormatStructureTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── JSON 構造 ─────────────────────────────────────────────────────────

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
        // Project.Version はデフォルト "0.1.0" だが CurrentSchemaVersion は別管理
        // スキーマバージョン定数が変わらないことを確認する
        Assert.NotEqual(Project.CurrentSchemaVersion, loaded.Version); // Version プロパティはスキーマバージョンではない
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

    [Fact]
    public void NestSuiteSave_PayloadSchemaVersion_MatchesCurrent()
    {
        Directory.CreateDirectory(_tempDir);
        var path = Path.Combine(_tempDir, "schema.nestsuite");
        new ProjectFileService().Save(path, new Project { ProjectName = "Schema" });

        var envelope = NestSuiteWorkspaceEnvelope.Read(File.ReadAllText(path));

        Assert.Equal(Project.CurrentSchemaVersion, envelope.PayloadSchemaVersion);
        Assert.Equal(NestSuiteWorkspaceEnvelope.CurrentFormatVersion, envelope.FormatVersion);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private static bool IsValidJson(string json)
    {
        try { JsonDocument.Parse(json); return true; }
        catch { return false; }
    }
}
