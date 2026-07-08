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

    // ── v2.16.7 TD-65: session.json 形式が変わっていないことのゴールデンファイル確認 ──
    // review1-fable5.md の対応（復元失敗 entry の持ち越し・破損 session 診断）は
    // 既存の Tabs[] / FilePaths / ActiveFilePath の範囲内で行い、新しい top-level field は追加しない。

    [Fact]
    public void Session_SerializedJson_TopLevelFieldNames_AreUnchanged()
    {
        var state = new NestSuiteSessionState
        {
            FilePaths = ["/a.notenest"],
            ActiveFilePath = "/a.notenest",
            Tabs = [new NestSuiteSessionTabState { FilePath = "/a.notenest", WorkspaceKind = "NoteNest", IsPinned = true }],
        };

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(state));
        var topLevelNames = doc.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal);

        Assert.Equal(new[] { "ActiveFilePath", "FilePaths", "Tabs" }, topLevelNames);
    }

    [Fact]
    public void Session_SerializedJson_TabEntryFieldNames_AreUnchanged()
    {
        var state = new NestSuiteSessionState
        {
            Tabs = [new NestSuiteSessionTabState { FilePath = "/a.notenest", WorkspaceKind = "NoteNest", IsPinned = true }],
        };

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(state));
        var tabEntry = doc.RootElement.GetProperty("Tabs")[0];
        var fieldNames = tabEntry.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal);

        Assert.Equal(new[] { "FilePath", "IsPinned", "WorkspaceKind" }, fieldNames);
    }

    [Fact]
    public void Session_OldFormatJson_WithoutTabs_StillLoads()
    {
        // v2.16.3 SH-15 以前の旧形式（Tabs[] なし）。
        var path = Path.Combine(_tempDir, "old-session.json");
        File.WriteAllText(path, """{"FilePaths":["/a.notenest","/b.chatnest"],"ActiveFilePath":"/a.notenest"}""");
        var svc = new NestSuiteSessionStateService(path);

        var loaded = svc.Load();

        Assert.Equal(new[] { "/a.notenest", "/b.chatnest" }, loaded.FilePaths);
        Assert.Equal("/a.notenest", loaded.ActiveFilePath);
        Assert.Empty(loaded.Tabs);
    }

    [Fact]
    public void Session_NewFormatJson_WithTabsAndIsPinned_StillLoads()
    {
        var path = Path.Combine(_tempDir, "new-session.json");
        File.WriteAllText(path, """
            {
              "FilePaths": ["/a.notenest"],
              "ActiveFilePath": "/a.notenest",
              "Tabs": [{"FilePath":"/a.notenest","WorkspaceKind":"NoteNest","IsPinned":true}]
            }
            """);
        var svc = new NestSuiteSessionStateService(path);

        var loaded = svc.Load();

        Assert.Single(loaded.Tabs);
        Assert.True(loaded.Tabs[0].IsPinned);
        Assert.Equal("NoteNest", loaded.Tabs[0].WorkspaceKind);
    }

    [Fact]
    public void Session_CorruptJson_LoadsAsEmpty_AndAppStartupIsNotBlocked()
    {
        var path = Path.Combine(_tempDir, "broken-session.json");
        File.WriteAllText(path, "{ this is not valid json");
        var svc = new NestSuiteSessionStateService(path);

        var loaded = svc.Load();

        Assert.Empty(loaded.FilePaths);
        Assert.Empty(loaded.Tabs);
        Assert.Null(loaded.ActiveFilePath);
    }

    [Fact]
    public void Session_WithMissingFileEntry_RestoreTargetsExcludeIt_ButReportFailure()
    {
        var state = new NestSuiteSessionState
        {
            FilePaths = ["/does/not/exist.notenest"],
        };

        var targets = SessionTabMapper.CreateRestoreTargets(state, _ => false, out var failures);

        Assert.Empty(targets);
        var failure = Assert.Single(failures);
        Assert.Equal(WorkspaceKindDetectionFailure.FileNotFound, failure.Failure);
    }
}
