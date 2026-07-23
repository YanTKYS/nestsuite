using NestSuite;
using Xunit;

namespace NestSuite.Tests;

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

    // ── v2.19.2 TD-87 (state-data-protection-boundary-review.md L1): 破損時の診断性 ──

    [Fact]
    public void Load_CorruptedJson_QuarantinesOriginalFile_ToCorruptSuffix()
    {
        var dataPath = Path.Combine(_dir, "corrupt.json");
        const string corruptContent = "this is not valid json }{";
        File.WriteAllText(dataPath, corruptContent);
        var svc = new NestSuiteRecentFilesService(dataPath);

        svc.Load();

        Assert.False(File.Exists(dataPath));
        var quarantinePath = dataPath + ".corrupt";
        Assert.True(File.Exists(quarantinePath));
        Assert.Equal(corruptContent, File.ReadAllText(quarantinePath));
    }

    [Fact]
    public void Load_CorruptedJson_SecondFailure_UsesTimestampedQuarantineName()
    {
        var dataPath = Path.Combine(_dir, "corrupt.json");
        var svc = new NestSuiteRecentFilesService(dataPath);

        File.WriteAllText(dataPath, "broken-1 }{");
        svc.Load();
        Assert.True(File.Exists(dataPath + ".corrupt"));

        // 2 回目の破損: 既存の .corrupt を上書きせず、別名（日時付き）で退避する。
        File.WriteAllText(dataPath, "broken-2 }{");
        svc.Load();

        Assert.False(File.Exists(dataPath));
        Assert.Equal("broken-1 }{", File.ReadAllText(dataPath + ".corrupt"));
        var timestamped = Directory.GetFiles(_dir, "corrupt.json.corrupt-*");
        var extra = Assert.Single(timestamped);
        Assert.Equal("broken-2 }{", File.ReadAllText(extra));
    }

    [Fact]
    public void Load_CorruptedJson_QuarantineFailure_StillReturnsEmpty_AndDoesNotThrow()
    {
        var dataPath = Path.Combine(_dir, "corrupt.json");
        File.WriteAllText(dataPath, "this is not valid json }{");
        // 退避先と同名のディレクトリを先に作り、File.Move による退避を失敗させる。
        Directory.CreateDirectory(dataPath + ".corrupt");
        var svc = new NestSuiteRecentFilesService(dataPath);

        var list = svc.Load();

        Assert.Empty(list);
        // 退避に失敗したので元ファイルは残ったままでよい（起動継続が優先）。
        Assert.True(File.Exists(dataPath));
    }

    [Fact]
    public void Load_MissingFile_DoesNotAttemptQuarantine()
    {
        var dataPath = Path.Combine(_dir, "never-existed.json");
        var svc = new NestSuiteRecentFilesService(dataPath);

        var list = svc.Load();

        Assert.Empty(list);
        Assert.False(File.Exists(dataPath + ".corrupt"));
    }

    [Fact]
    public void Load_AfterQuarantine_CanSaveNormallyToOriginalPath()
    {
        var dataPath = Path.Combine(_dir, "corrupt.json");
        File.WriteAllText(dataPath, "this is not valid json }{");
        var svc = new NestSuiteRecentFilesService(dataPath);
        svc.Load();

        var updated = svc.Add("path/after-recovery");

        Assert.Equal(new[] { "path/after-recovery" }, updated);
        Assert.Equal(updated, svc.Load());
        Assert.True(File.Exists(dataPath + ".corrupt"));
    }
}
