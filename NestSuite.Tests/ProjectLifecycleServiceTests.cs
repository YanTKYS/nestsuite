using NestSuite;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

public class ProjectLifecycleServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"notenest-lifecycle-{Guid.NewGuid()}");

    [Fact]
    public void CreateNewLoadsWorkspaceAndSessionWithoutUnsavedChange()
    {
        var context = CreateContext();

        context.Lifecycle.CreateNew();

        Assert.NotEmpty(context.Notes.Notebooks);
        Assert.NotNull(context.Editor.SelectedNote);
        Assert.True(context.Session.IsSampleProject);
        Assert.False(context.Session.IsModified);
        Assert.NotEmpty(context.Markers.Markers);
    }

    [Fact]
    public void SaveAndOpenRoundTripOwnsSessionAndRecentFiles()
    {
        var context = CreateContext();
        context.Lifecycle.CreateNew();
        context.Notes.AddNotebook("Added");
        context.Session.IsModified = true;
        var path = Path.Combine(_directory, "roundtrip.notenest");

        context.Lifecycle.Save(path);
        context.Notes.AddNotebook("Temporary");
        context.Lifecycle.Open(path);

        Assert.Equal(path, context.Session.CurrentFilePath);
        Assert.Equal("roundtrip.notenest", context.Session.ProjectDisplayName);
        Assert.False(context.Session.IsModified);
        Assert.False(context.Session.IsSampleProject);
        Assert.Contains(context.Session.RecentFiles, file => file.FullPath == path);
        Assert.Contains(context.Notes.Notebooks, notebook => notebook.Title == "Added");
        Assert.DoesNotContain(context.Notes.Notebooks, notebook => notebook.Title == "Temporary");
    }

    [Fact]
    public void CreateSnapshotBuildsCurrentDocumentWithoutSavingOrChangingSession()
    {
        var context = CreateContext();
        context.Lifecycle.CreateNew();
        context.Notes.AddNotebook("Snapshot only");
        context.Session.IsModified = true;

        var snapshot = context.Lifecycle.CreateSnapshot();

        Assert.Contains(snapshot.Notebooks, notebook => notebook.Title == "Snapshot only");
        Assert.Null(context.Session.CurrentFilePath);
        Assert.True(context.Session.IsModified);
    }

    [Fact]
    public void CreateEmptyLoadsEmptyWorkspaceWithoutSampleData()
    {
        // v1.20.0: サンプル無題.notenest で「新規プロジェクト」を押した場合の期待動作
        var context = CreateContext();

        context.Lifecycle.CreateEmpty();

        Assert.Empty(context.Notes.Notebooks);
        Assert.False(context.Session.IsSampleProject);   // バナーが消える
        Assert.False(context.Session.IsModified);
        Assert.Null(context.Session.CurrentFilePath);    // 保存先未設定のまま
    }

    [Fact]
    public void ClearRecentFilesSynchronizesSessionWithRecentFilesService()
    {
        var context = CreateContext();
        context.Lifecycle.CreateNew();
        var path = Path.Combine(_directory, "recent.notenest");
        context.Lifecycle.Save(path);

        context.Lifecycle.ClearRecentFiles();

        Assert.Empty(context.Session.RecentFiles);
        Assert.Empty(new RecentFilesService(Path.Combine(_directory, "recent.json")).Load());
    }

    [Fact]
    public void SaveDoesNotShowRecentFileWhenRecentHistoryPersistenceFails()
    {
        Directory.CreateDirectory(_directory);
        var invalidRecentDataPath = Path.Combine(_directory, "recent-path-is-directory");
        Directory.CreateDirectory(invalidRecentDataPath);
        var context = CreateContext(new RecentFilesService(invalidRecentDataPath));
        context.Lifecycle.CreateNew();
        var projectPath = Path.Combine(_directory, "saved.notenest");

        context.Lifecycle.Save(projectPath);

        Assert.True(File.Exists(projectPath));
        Assert.Empty(context.Session.RecentFiles);
    }

    // ── M19: 最近使ったファイル履歴の読込失敗からの復旧経路 ─────────────────

    [Fact]
    public void InitializeRecentFiles_NormalFile_NoRecovery_SessionGetsFiles()
    {
        var recentFiles = new RecentFilesService(Path.Combine(_directory, "recent.json"));
        Directory.CreateDirectory(_directory);
        recentFiles.Add(Path.Combine(_directory, "existing.notenest"));
        var context = CreateContext(recentFiles);

        var result = context.Lifecycle.InitializeRecentFiles();

        Assert.Null(result.Recovery);
        Assert.Contains(context.Session.RecentFiles, f => f.FullPath == Path.Combine(_directory, "existing.notenest"));
    }

    [Fact]
    public void InitializeRecentFiles_CorruptFile_ReturnsRecovery_SessionGetsEmptyHistory()
    {
        Directory.CreateDirectory(_directory);
        var dataPath = Path.Combine(_directory, "recent.json");
        File.WriteAllText(dataPath, "{ not a list");
        var context = CreateContext(new RecentFilesService(dataPath));

        var result = context.Lifecycle.InitializeRecentFiles();

        Assert.NotNull(result.Recovery);
        Assert.True(result.Recovery!.Succeeded);
        Assert.Empty(context.Session.RecentFiles);
        Assert.False(File.Exists(dataPath));
    }

    // ── v2.16.35 TD-59b-2: OpenPrepared（設計文書 §8.6） ────────────────────

    [Fact]
    public void OpenPrepared_SetsCurrentFilePathToContextFilePath_AndTracksRecentFile()
    {
        var context = CreateContext();
        context.Lifecycle.CreateNew();
        context.Notes.AddNotebook("Added");
        var path = Path.Combine(_directory, "prepared.notenest");
        context.Lifecycle.Save(path);

        Assert.True(NestSuiteTabFactory.TryPrepareOpen(path, out var openContext, out _));
        context.Notes.AddNotebook("Temporary");
        context.Lifecycle.OpenPrepared(openContext);

        Assert.Equal(openContext.FilePath, context.Session.CurrentFilePath);
        Assert.Contains(context.Session.RecentFiles, file => file.FullPath == openContext.FilePath);
    }

    [Fact]
    public void OpenPrepared_LoadsProjectContentIntoViewModels_AndClearsModifiedFlag()
    {
        var context = CreateContext();
        context.Lifecycle.CreateNew();
        context.Notes.AddNotebook("Added");
        context.Session.IsModified = true;
        var path = Path.Combine(_directory, "prepared-content.notenest");
        context.Lifecycle.Save(path);

        Assert.True(NestSuiteTabFactory.TryPrepareOpen(path, out var openContext, out _));
        context.Notes.AddNotebook("Temporary");
        context.Lifecycle.OpenPrepared(openContext);

        Assert.False(context.Session.IsModified);
        Assert.Contains(context.Notes.Notebooks, notebook => notebook.Title == "Added");
        Assert.DoesNotContain(context.Notes.Notebooks, notebook => notebook.Title == "Temporary");
    }

    [Fact]
    public void OpenPrepared_NestSuitePath_LoadsWithoutAdditionalFileRead()
    {
        var context = CreateContext();
        context.Lifecycle.CreateNew();
        var path = Path.Combine(_directory, "prepared.nestsuite");
        context.Lifecycle.Save(path);
        var wrapped = File.ReadAllText(path);
        File.Delete(path);

        var success = NestSuiteTabFactory.TryPrepareOpen(
            path, out var openContext, out _,
            fileExists: _ => true,
            readAllText: _ => wrapped);
        Assert.True(success);
        Assert.False(File.Exists(path));

        context.Lifecycle.OpenPrepared(openContext);

        Assert.Equal(path, context.Session.CurrentFilePath);
    }

    [Fact]
    public void OpenPrepared_MatchesOpenResult_ForSamePath()
    {
        var contextA = CreateContext();
        contextA.Lifecycle.CreateNew();
        contextA.Notes.AddNotebook("Shared");
        var path = Path.Combine(_directory, "compare.notenest");
        contextA.Lifecycle.Save(path);

        var contextB = CreateContext();
        Assert.True(NestSuiteTabFactory.TryPrepareOpen(path, out var openContext, out _));
        contextB.Lifecycle.OpenPrepared(openContext);

        var contextC = CreateContext();
        contextC.Lifecycle.Open(path);

        Assert.Equal(
            contextC.Notes.Notebooks.Select(n => n.Title),
            contextB.Notes.Notebooks.Select(n => n.Title));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }

    private Context CreateContext(RecentFilesService? recentFiles = null)
    {
        Directory.CreateDirectory(_directory);
        var session = new ProjectSessionViewModel();
        var notes = new NoteWorkspaceViewModel();
        var tasks = new TaskBoardViewModel();
        var markers = new MarkerPanelViewModel(new MarkerExtractorService());
        var editor = new EditorStateViewModel();
        var lifecycle = new ProjectLifecycleService(
            session, notes, tasks, markers, editor,
            recentFiles: recentFiles ?? new RecentFilesService(Path.Combine(_directory, "recent.json")));
        return new Context(session, notes, markers, editor, lifecycle);
    }

    private sealed record Context(
        ProjectSessionViewModel Session,
        NoteWorkspaceViewModel Notes,
        MarkerPanelViewModel Markers,
        EditorStateViewModel Editor,
        ProjectLifecycleService Lifecycle);
}
