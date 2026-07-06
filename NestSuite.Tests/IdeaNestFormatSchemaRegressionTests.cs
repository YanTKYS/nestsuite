using System.Text.Json;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// IdeaNest (.ideanest) 保存形式・スキーマの非変更を自動テストで固定する。
/// </summary>
public class IdeaNestFormatSchemaRegressionTests : IDisposable
{
    private readonly string _tempDir;

    public IdeaNestFormatSchemaRegressionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "IdeaNestFormatSchemaRegressionTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── バージョン定数 ────────────────────────────────────────────────────

    [Fact]
    public void IdeaNest_SchemaVersionConstant_Is_1_1_4()
    {
        Assert.Equal("1.1.4", IdeaNestFileService.SchemaVersion);
    }

    [Fact]
    public void IdeaNest_FileExtensionConstant_Is_ideanest()
    {
        Assert.Equal(".ideanest", IdeaNestFileService.FileExtension);
    }

    // ── JSON 構造 ─────────────────────────────────────────────────────────

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

    // ── helpers ──────────────────────────────────────────────────────────

    private static bool IsValidJson(string json)
    {
        try { JsonDocument.Parse(json); return true; }
        catch { return false; }
    }
}
