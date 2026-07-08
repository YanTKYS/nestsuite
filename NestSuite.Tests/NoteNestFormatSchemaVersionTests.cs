using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.13 TD-63: NoteNestFormatSchemaRegressionTests から、schema version 定数・
/// 前方互換ガード（v2.14.4 FM-4）に関するテストを分割した。
/// NoteNest (.notenest) / .nestsuite の schema version 判定の非変更を固定する。
/// </summary>
public class NoteNestFormatSchemaVersionTests : IDisposable
{
    private readonly string _tempDir;

    public NoteNestFormatSchemaVersionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NoteNestFormatSchemaVersionTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── バージョン定数 ────────────────────────────────────────────────────

    [Fact]
    public void NoteNest_SchemaVersionConstant_MatchesBuiltProject()
    {
        var path = Path.Combine(_tempDir, "constant.notenest");
        var project = new Project { ProjectName = "ConstantCheck", Version = Project.CurrentSchemaVersion };
        var svc = new ProjectFileService();
        svc.Save(path, project);
        var loaded = svc.Load(path);
        Assert.Equal(Project.CurrentSchemaVersion, loaded.Version);
    }

    // ── 保存スキーマバージョン ────────────────────────────────────────────

    [Fact]
    public void CurrentSchemaVersion_IsAValidVersionString()
    {
        Assert.True(System.Version.TryParse(Project.CurrentSchemaVersion, out _));
    }

    [Fact]
    public void SavedFile_ContainsCurrentSchemaVersion()
    {
        var (lc, session, _, _, _, _) = CreateV146Context();
        lc.CreateNew();
        var path = Path.Combine(_tempDir, "schema.notenest");
        lc.Save(path);
        Assert.Contains($"\"{Project.CurrentSchemaVersion}\"", File.ReadAllText(path));
    }

    // ── v2.14.4 FM-4: schema version 前方互換ガード ───────────────────────
    //
    // LegacyNoteWithoutStarField_LoadsAsNotStarred / LegacySchemaNote_LoadThenSave_PreservesContentAndTasks
    // （NoteNestFormatTimestampAndStarTests / NoteNestFormatRoundTripTests）が既に legacy .notenest
    // （"version":"1.4.1"）の正常読込を確認済みのため、ここでは重複させない。

    [Fact]
    public void Load_NoteNestNewerVersion_ThrowsSchemaVersionTooNewException()
    {
        Directory.CreateDirectory(_tempDir);
        var path = Path.Combine(_tempDir, "toonew.notenest");
        File.WriteAllText(path, """{"version":"1.4.3","projectName":"TooNew","notebooks":[]}""");

        Assert.Throws<SchemaVersionTooNewException>(() => new ProjectFileService().Load(path));
    }

    [Fact]
    public void Load_NoteNestNumericallyNewerVersion_ThrowsSchemaVersionTooNewException()
    {
        // "1.4.10" は文字列比較では現行 schema version より小さく見えるが、数値としては大きい。
        // 数値比較（文字列比較ではない）で新しいと判定されることを確認する。
        Directory.CreateDirectory(_tempDir);
        var path = Path.Combine(_tempDir, "toonew-numeric.notenest");
        File.WriteAllText(path, """{"version":"1.4.10","projectName":"TooNew","notebooks":[]}""");

        Assert.Throws<SchemaVersionTooNewException>(() => new ProjectFileService().Load(path));
    }

    [Fact]
    public void Load_NestSuiteEnvelope_PayloadSchemaVersionNewerThanCurrent_ThrowsSchemaVersionTooNewException()
    {
        Directory.CreateDirectory(_tempDir);
        var path = Path.Combine(_tempDir, "toonew-wrapper.nestsuite");
        var payloadJson = $$"""{"version":"{{Project.CurrentSchemaVersion}}","projectName":"TooNewWrapper","notebooks":[]}""";
        File.WriteAllText(path, NestSuiteWorkspaceEnvelope.Wrap(
            NestSuiteWorkspaceEnvelope.KindNoteNest, "1.4.3", payloadJson));

        Assert.Throws<SchemaVersionTooNewException>(() => new ProjectFileService().Load(path));
    }

    [Fact]
    public void Load_NestSuiteEnvelope_PayloadNewerThanWrapper_ThrowsInvalidDataException()
    {
        // payload 内 version（現行 schema）がラッパーの payloadSchemaVersion（1.4.1）より新しい
        // 矛盾方向のみ失敗させる。
        Directory.CreateDirectory(_tempDir);
        var path = Path.Combine(_tempDir, "inconsistent.nestsuite");
        var payloadJson = $$"""{"version":"{{Project.CurrentSchemaVersion}}","projectName":"Inconsistent","notebooks":[]}""";
        File.WriteAllText(path, NestSuiteWorkspaceEnvelope.Wrap(
            NestSuiteWorkspaceEnvelope.KindNoteNest, "1.4.1", payloadJson));

        Assert.Throws<InvalidDataException>(() => new ProjectFileService().Load(path));
    }

    [Fact]
    public void Load_NestSuiteEnvelope_WrapperNewerThanPayload_LoadsFine()
    {
        // v2.14.1〜v2.14.3 のアプリが旧 payload（1.4.1）を現行 payloadSchemaVersion で包んだ
        // 正当な既存ファイル形状。この方向は意図的に許容される。
        Directory.CreateDirectory(_tempDir);
        var path = Path.Combine(_tempDir, "wrapper-newer.nestsuite");
        var payloadJson = """{"version":"1.4.1","projectName":"WrapperNewer","notebooks":[]}""";
        File.WriteAllText(path, NestSuiteWorkspaceEnvelope.Wrap(
            NestSuiteWorkspaceEnvelope.KindNoteNest, Project.CurrentSchemaVersion, payloadJson));

        var loaded = new ProjectFileService().Load(path);

        Assert.Equal("WrapperNewer", loaded.ProjectName);
        Assert.Equal("1.4.1", loaded.Version);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private (ProjectLifecycleService Lifecycle, ProjectSessionViewModel Session,
             NoteWorkspaceViewModel Notes, TaskBoardViewModel Tasks,
             MarkerPanelViewModel Markers, EditorStateViewModel Editor) CreateV146Context()
    {
        var session = new ProjectSessionViewModel();
        var notes   = new NoteWorkspaceViewModel();
        var tasks   = new TaskBoardViewModel();
        var markers = new MarkerPanelViewModel(new MarkerExtractorService());
        var editor  = new EditorStateViewModel();
        var lifecycle = new ProjectLifecycleService(
            session, notes, tasks, markers, editor,
            recentFiles: new RecentFilesService(Path.Combine(_tempDir, "recent.json")));
        Directory.CreateDirectory(_tempDir);
        return (lifecycle, session, notes, tasks, markers, editor);
    }
}
