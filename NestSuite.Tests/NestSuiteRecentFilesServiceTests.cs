using NoteNest.NestSuite;
using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.14.0: NestSuite 横断最近ファイルサービスのユニットテスト。
/// 最大 10 件・重複排除・先頭挿入・削除・クリア・永続化を確認する。
/// </summary>
public class NestSuiteRecentFilesServiceTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly NestSuiteRecentFilesService _svc;

    public NestSuiteRecentFilesServiceTests()
    {
        Directory.CreateDirectory(_dir);
        _svc = new NestSuiteRecentFilesService(Path.Combine(_dir, "nestsuite-recent-files.json"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Load_EmptyState_ReturnsEmpty()
    {
        Assert.Empty(_svc.Load());
    }

    [Fact]
    public void Add_NewPath_AppearsAtFront()
    {
        _svc.Add("a.notenest");

        var updated = _svc.Add("b.chatnest");

        Assert.Equal("b.chatnest", updated[0]);
        Assert.Equal(updated, _svc.Load());
    }

    [Fact]
    public void Add_DuplicatePath_MovedToFront()
    {
        _svc.Add("a.notenest");
        _svc.Add("b.chatnest");
        _svc.Add("a.notenest");

        var list = _svc.Load();
        Assert.Equal("a.notenest", list[0]);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void Add_ExceedsTenItems_ListTrimmedToTen()
    {
        for (int i = 0; i < 12; i++)
            _svc.Add($"file{i}.notenest");

        var list = _svc.Load();
        Assert.Equal(10, list.Count);
    }

    [Fact]
    public void Add_PersistsBetweenInstances()
    {
        _svc.Add("project.notenest");

        var dataPath = Path.Combine(_dir, "nestsuite-recent-files.json");
        var svc2 = new NestSuiteRecentFilesService(dataPath);

        Assert.Equal(new[] { "project.notenest" }, svc2.Load());
    }

    [Fact]
    public void Remove_ExistingPath_RemovesFromList()
    {
        _svc.Add("a.notenest");
        _svc.Add("b.chatnest");

        var updated = _svc.Remove("b.chatnest");

        Assert.Equal(new[] { "a.notenest" }, updated);
        Assert.Equal(updated, _svc.Load());
    }

    [Fact]
    public void Remove_NonExistentPath_ReturnsUnchangedList()
    {
        _svc.Add("a.notenest");

        var updated = _svc.Remove("not-present.notenest");

        Assert.Equal(new[] { "a.notenest" }, updated);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        _svc.Add("a.notenest");
        _svc.Add("b.chatnest");

        var updated = _svc.Clear();

        Assert.Empty(updated);
        Assert.Empty(_svc.Load());
    }

    [Fact]
    public void Add_WriteFailure_ReturnsPersistedListWithoutCrash()
    {
        var invalidDataPath = Path.Combine(_dir, "data-path-is-directory");
        Directory.CreateDirectory(invalidDataPath);
        var service = new NestSuiteRecentFilesService(invalidDataPath);

        var updated = service.Add("path/not-persisted");

        Assert.Empty(updated);
        Assert.Empty(service.Load());
    }

    [Fact]
    public void Add_NoTmpFileLeft_AfterSuccessfulWrite()
    {
        _svc.Add("project.notenest");

        Assert.Empty(Directory.GetFiles(_dir, "nestsuite-recent-files.json.*.tmp"));
    }

    // ── v1.14.1: 壊れたファイル・未対応拡張子の耐久性 ──

    [Fact]
    public void Load_CorruptedJson_ReturnsEmpty()
    {
        var dataPath = Path.Combine(_dir, "corrupt.json");
        File.WriteAllText(dataPath, "this is not valid json }{");
        var svc = new NestSuiteRecentFilesService(dataPath);

        Assert.Empty(svc.Load());
    }

    [Fact]
    public void Remove_UnsupportedExtensionPath_RemovesFromList()
    {
        _svc.Add("file.notenest");
        _svc.Add("foreign.txt");

        var updated = _svc.Remove("foreign.txt");

        Assert.Equal(new[] { "file.notenest" }, updated);
        Assert.Equal(updated, _svc.Load());
    }
}
