using System.Text.Json;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// セッション形式 (session.json) の非変更を自動テストで固定する。
/// </summary>
public class SessionFormatSchemaRegressionTests : IDisposable
{
    private readonly string _tempDir;

    public SessionFormatSchemaRegressionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SessionFormatSchemaRegressionTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
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
}
