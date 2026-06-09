using NoteNest.Services;
using Xunit;

namespace NoteNest.Tests;

public class RecentFilesServiceTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly RecentFilesService _svc;

    public RecentFilesServiceTests()
    {
        Directory.CreateDirectory(_dir);
        _svc = new RecentFilesService(Path.Combine(_dir, "recent-files.json"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Add_DuplicatePath_MovedToFront()
    {
        _svc.Add("path/a");
        _svc.Add("path/b");
        _svc.Add("path/a");

        var list = _svc.Load();
        Assert.Equal("path/a", list[0]);
    }

    [Fact]
    public void Add_ExceedsMaxFive_ListTrimmed()
    {
        for (int i = 0; i < 7; i++)
            _svc.Add($"path/{i}");

        var list = _svc.Load();
        Assert.True(list.Count <= 5);
    }

    [Fact]
    public void Add_NewPath_AppearsAtFront()
    {
        _svc.Add("path/existing");

        var updated = _svc.Add("path/newest");

        Assert.Equal("path/newest", updated[0]);
        Assert.Equal(updated, _svc.Load());
    }

    [Fact]
    public void Load_EmptyState_ReturnsEmpty()
    {
        Assert.Empty(_svc.Load());
    }

    [Fact]
    public void Add_WriteFailure_ReturnsPersistedListInsteadOfUnwrittenUpdate()
    {
        var invalidDataPath = Path.Combine(_dir, "data-path-is-directory");
        Directory.CreateDirectory(invalidDataPath);
        var service = new RecentFilesService(invalidDataPath);

        var updated = service.Add("path/not-persisted");

        Assert.Empty(updated);
        Assert.Empty(service.Load());
        AssertNoTemporaryFiles("data-path-is-directory");
    }

    [Fact]
    public void Add_ReplaceFailure_PreservesPersistedListAndRemovesTemporaryFile()
    {
        if (!OperatingSystem.IsWindows()) return;

        _svc.Add("path/persisted");
        var dataPath = Path.Combine(_dir, "recent-files.json");
        using var locked = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        var updated = _svc.Add("path/not-persisted");

        Assert.Equal(new[] { "path/persisted" }, updated);
        Assert.Equal(updated, _svc.Load());
        AssertNoTemporaryFiles("recent-files.json");
    }

    [Fact]
    public void Clear_DeleteFailure_ReturnsPersistedList()
    {
        if (!OperatingSystem.IsWindows()) return;

        _svc.Add("path/persisted");
        var dataPath = Path.Combine(_dir, "recent-files.json");
        using var locked = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        var updated = _svc.ClearAndGetUpdatedList();

        Assert.Equal(new[] { "path/persisted" }, updated);
    }

    [Fact]
    public void Remove_ExistingPath_ReturnsAndPersistsUpdatedList()
    {
        _svc.Add("path/a");
        _svc.Add("path/b");

        var updated = _svc.RemoveAndGetUpdatedList("path/b");

        Assert.Equal(new[] { "path/a" }, updated);
        Assert.Equal(updated, _svc.Load());
    }

    [Fact]
    public void Remove_WriteFailure_ReturnsPersistedList()
    {
        if (!OperatingSystem.IsWindows()) return;

        _svc.Add("path/a");
        _svc.Add("path/b");
        var dataPath = Path.Combine(_dir, "recent-files.json");
        using var locked = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        var updated = _svc.RemoveAndGetUpdatedList("path/b");

        Assert.Equal(new[] { "path/b", "path/a" }, updated);
        Assert.Equal(updated, _svc.Load());
        AssertNoTemporaryFiles("recent-files.json");
    }

    [Fact]
    public void Clear_RemovesAllRecentFiles()
    {
        _svc.Add("path/a");
        _svc.Add("path/b");

        var updated = _svc.ClearAndGetUpdatedList();

        Assert.Empty(updated);
        Assert.Empty(_svc.Load());
    }

    private void AssertNoTemporaryFiles(string dataFileName)
        => Assert.Empty(Directory.GetFiles(_dir, $"{dataFileName}.*.tmp"));
}
