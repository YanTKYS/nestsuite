using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

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

    // ── M19: 読込失敗時の復旧経路 ────────────────────────────────────────────

    [Fact]
    public void Load_FileDoesNotExist_ReturnsEmpty_NoRecovery()
    {
        var result = _svc.LoadWithRecovery();

        Assert.Null(result.Recovery);
        Assert.Empty(result.Files);
    }

    [Fact]
    public void Load_InvalidJson_ReturnsEmptyList()
    {
        var dataPath = Path.Combine(_dir, "recent-files.json");
        File.WriteAllText(dataPath, "{ not a list");

        var list = _svc.Load();

        Assert.Empty(list);
    }

    [Fact]
    public void Load_InvalidJson_QuarantinesOriginalFile_BackupFileExists()
    {
        var dataPath = Path.Combine(_dir, "recent-files.json");
        File.WriteAllText(dataPath, "{ not a list");

        var result = _svc.LoadWithRecovery();

        Assert.NotNull(result.Recovery);
        Assert.True(result.Recovery!.Succeeded);
        Assert.False(File.Exists(dataPath));
        Assert.True(File.Exists(result.Recovery.BackupPath));
        Assert.Contains(".corrupt-", result.Recovery.BackupPath);
    }

    [Fact]
    public void Load_ExistingPathsThatNoLongerExistOnDisk_IsNotTreatedAsCorruption()
    {
        // 履歴内の存在しないファイルパス自体は破損ではない（現行の除外・整理方針を維持）。
        var dataPath = Path.Combine(_dir, "recent-files.json");
        File.WriteAllText(dataPath, "[\"path/does-not-exist-on-disk\"]");

        var result = _svc.LoadWithRecovery();

        Assert.Null(result.Recovery);
        Assert.Equal(new[] { "path/does-not-exist-on-disk" }, result.Files);
    }

    [Fact]
    public void Load_AfterQuarantine_CanSaveNormallyToOriginalPath()
    {
        var dataPath = Path.Combine(_dir, "recent-files.json");
        File.WriteAllText(dataPath, "{ not a list");
        _svc.LoadWithRecovery();

        var updated = _svc.Add("path/after-recovery");

        Assert.Equal(new[] { "path/after-recovery" }, updated);
        Assert.Equal(updated, _svc.Load());
    }

    [Fact]
    public void Load_MaxItemsAndDedup_StillEnforced_AfterRecoveryPath()
    {
        var dataPath = Path.Combine(_dir, "recent-files.json");
        File.WriteAllText(dataPath, "{ not a list");
        _svc.LoadWithRecovery();

        for (var i = 0; i < 7; i++) _svc.Add($"path/{i}");
        _svc.Add("path/0");

        var list = _svc.Load();
        Assert.True(list.Count <= 5);
        Assert.Equal("path/0", list[0]);
    }
}
