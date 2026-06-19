using NoteNest.NestSuite;
using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.15.0: NestSuite セッション状態サービスのユニットテスト。
/// 保存・読込・永続化・エラー耐久性を確認する。
/// </summary>
public class NestSuiteSessionStateServiceTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly NestSuiteSessionStateService _svc;

    public NestSuiteSessionStateServiceTests()
    {
        Directory.CreateDirectory(_dir);
        _svc = new NestSuiteSessionStateService(Path.Combine(_dir, "nestsuite-session.json"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Load_EmptyState_ReturnsEmpty()
    {
        var state = _svc.Load();

        Assert.Empty(state.FilePaths);
        Assert.Null(state.ActiveFilePath);
    }

    [Fact]
    public void Load_EmptyState_ReturnsNewInstance()
    {
        // Load() が毎回新しいインスタンスを返すことで、呼び出し側の変更が他の呼び出し結果に影響しないことを確認する
        var state1 = _svc.Load();
        var state2 = _svc.Load();

        Assert.NotSame(state1, state2);
        state1.FilePaths.Add("polluted.notenest");
        Assert.Empty(state2.FilePaths);
    }

    [Fact]
    public void Save_AndLoad_RoundTrip()
    {
        var state = new NestSuiteSessionState
        {
            FilePaths = ["a.notenest", "b.chatnest"],
            ActiveFilePath = "a.notenest"
        };

        _svc.Save(state);
        var loaded = _svc.Load();

        Assert.Equal(new[] { "a.notenest", "b.chatnest" }, loaded.FilePaths);
        Assert.Equal("a.notenest", loaded.ActiveFilePath);
    }

    [Fact]
    public void Save_PersistsBetweenInstances()
    {
        _svc.Save(new NestSuiteSessionState { FilePaths = ["project.notenest"] });

        var svc2 = new NestSuiteSessionStateService(Path.Combine(_dir, "nestsuite-session.json"));
        var loaded = svc2.Load();

        Assert.Equal(new[] { "project.notenest" }, loaded.FilePaths);
    }

    [Fact]
    public void Save_SupportsAllThreeExtensions()
    {
        var state = new NestSuiteSessionState
        {
            FilePaths = ["a.notenest", "b.chatnest", "c.ideanest"],
            ActiveFilePath = "b.chatnest"
        };

        _svc.Save(state);
        var loaded = _svc.Load();

        Assert.Equal(3, loaded.FilePaths.Count);
        Assert.Equal("b.chatnest", loaded.ActiveFilePath);
    }

    [Fact]
    public void Save_NullActiveFilePath_IsPreserved()
    {
        _svc.Save(new NestSuiteSessionState { FilePaths = ["a.notenest"], ActiveFilePath = null });

        var loaded = _svc.Load();

        Assert.Null(loaded.ActiveFilePath);
    }

    [Fact]
    public void Save_EmptyFilePaths_IsPreserved()
    {
        _svc.Save(new NestSuiteSessionState { FilePaths = [], ActiveFilePath = null });

        var loaded = _svc.Load();

        Assert.Empty(loaded.FilePaths);
    }

    [Fact]
    public void Load_CorruptedJson_ReturnsEmpty()
    {
        var dataPath = Path.Combine(_dir, "corrupt.json");
        File.WriteAllText(dataPath, "this is not valid json }{");
        var svc = new NestSuiteSessionStateService(dataPath);

        var state = svc.Load();

        Assert.Empty(state.FilePaths);
        Assert.Null(state.ActiveFilePath);
    }

    [Fact]
    public void Save_WriteFailure_DoesNotCrash()
    {
        var invalidPath = Path.Combine(_dir, "dir-as-file");
        Directory.CreateDirectory(invalidPath);
        var svc = new NestSuiteSessionStateService(invalidPath);

        svc.Save(new NestSuiteSessionState { FilePaths = ["x.notenest"] });
        // 例外が発生しないことを確認する
    }
}
