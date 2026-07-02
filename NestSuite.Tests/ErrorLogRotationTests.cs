using System.IO;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.14.0 TD-57 (LT-12): ErrorLogRotation のサイズベースローテーションの動作確認。
/// </summary>
public class ErrorLogRotationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _logPath;

    public ErrorLogRotationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"nestsuite-rotation-test-{Guid.NewGuid():N}");
        _logPath = Path.Combine(_tempDir, "nestsuite-error.log");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private string Gen(int generation) => ErrorLogRotation.ArchivePath(_logPath, generation);

    [Fact]
    public void ArchivePath_InsertsGenerationBeforeExtension()
    {
        var path = ErrorLogRotation.ArchivePath(_logPath, 2);

        Assert.Equal(Path.Combine(_tempDir, "nestsuite-error.2.log"), path);
    }

    [Fact]
    public void RotateIfNeeded_BelowThreshold_KeepsCurrentFileAndCreatesNoArchive()
    {
        File.WriteAllText(_logPath, "small");

        ErrorLogRotation.RotateIfNeeded(_logPath, maxSizeBytes: 1024, maxGenerations: 3);

        Assert.Equal("small", File.ReadAllText(_logPath));
        Assert.False(File.Exists(Gen(1)));
    }

    [Fact]
    public void RotateIfNeeded_MissingFile_DoesNotThrowAndCreatesNothing()
    {
        ErrorLogRotation.RotateIfNeeded(_logPath, maxSizeBytes: 1, maxGenerations: 3);

        Assert.False(File.Exists(_logPath));
        Assert.False(File.Exists(Gen(1)));
    }

    [Fact]
    public void RotateIfNeeded_AtThreshold_MovesCurrentToGeneration1()
    {
        File.WriteAllText(_logPath, "oversized content");

        ErrorLogRotation.RotateIfNeeded(_logPath, maxSizeBytes: 1, maxGenerations: 3);

        Assert.False(File.Exists(_logPath));
        Assert.Equal("oversized content", File.ReadAllText(Gen(1)));
    }

    [Fact]
    public void RotateIfNeeded_ShiftsGenerationsAndDeletesOldest()
    {
        File.WriteAllText(_logPath, "current");
        File.WriteAllText(Gen(1), "generation1");
        File.WriteAllText(Gen(2), "generation2");
        File.WriteAllText(Gen(3), "generation3");

        ErrorLogRotation.RotateIfNeeded(_logPath, maxSizeBytes: 1, maxGenerations: 3);

        Assert.False(File.Exists(_logPath));
        Assert.Equal("current", File.ReadAllText(Gen(1)));
        Assert.Equal("generation1", File.ReadAllText(Gen(2)));
        Assert.Equal("generation2", File.ReadAllText(Gen(3)));
        Assert.False(File.Exists(Gen(4)));
        // generation3（最古）は削除されている（どの世代ファイルにも残らない）
        foreach (var generation in new[] { 1, 2, 3 })
            Assert.NotEqual("generation3", File.ReadAllText(Gen(generation)));
    }

    [Fact]
    public void RotateIfNeeded_ZeroGenerations_DoesNothing()
    {
        File.WriteAllText(_logPath, "oversized content");

        ErrorLogRotation.RotateIfNeeded(_logPath, maxSizeBytes: 1, maxGenerations: 0);

        Assert.Equal("oversized content", File.ReadAllText(_logPath));
    }

    [Fact]
    public void RotateIfNeeded_LockedArchive_DoesNotThrow()
    {
        // 世代ファイルを排他ロックし、リネーム失敗を意図的に起こしても例外が外へ出ないことを確認する
        File.WriteAllText(_logPath, "oversized content");
        File.WriteAllText(Gen(1), "locked generation");

        using var _ = new FileStream(Gen(1), FileMode.Open, FileAccess.Read, FileShare.None);
        var exception = Record.Exception(() =>
            ErrorLogRotation.RotateIfNeeded(_logPath, maxSizeBytes: 1, maxGenerations: 3));

        Assert.Null(exception);
    }
}
