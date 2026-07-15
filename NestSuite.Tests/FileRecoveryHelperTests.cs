using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// M19: <see cref="FileRecoveryHelper.QuarantineCorruptFile"/> の退避処理（命名・衝突回避・
/// ファイル移動・結果返却のみ）を確認する。設定サービス統合・JSON読込・通知・ErrorLog出力は
/// このヘルパーの責務外のため確認しない。
/// </summary>
public class FileRecoveryHelperTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "FileRecoveryHelperTests_" + Guid.NewGuid().ToString("N"));

    public FileRecoveryHelperTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void QuarantineCorruptFile_MovesFileToCorruptNamedTarget()
    {
        var path = Path.Combine(_dir, "settings.json");
        File.WriteAllText(path, "broken");
        var now = new DateTime(2026, 7, 15, 10, 30, 0);

        var result = FileRecoveryHelper.QuarantineCorruptFile(path, now);

        Assert.True(result.Succeeded);
        Assert.Equal(path + ".corrupt-20260715-103000", result.BackupPath);
        Assert.True(File.Exists(result.BackupPath));
    }

    [Fact]
    public void QuarantineCorruptFile_OriginalFileNoLongerExists()
    {
        var path = Path.Combine(_dir, "settings.json");
        File.WriteAllText(path, "broken");

        FileRecoveryHelper.QuarantineCorruptFile(path);

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void QuarantineCorruptFile_BackupContentMatchesOriginal()
    {
        var path = Path.Combine(_dir, "settings.json");
        File.WriteAllText(path, "original-broken-content");

        var result = FileRecoveryHelper.QuarantineCorruptFile(path);

        Assert.Equal("original-broken-content", File.ReadAllText(result.BackupPath!));
    }

    [Fact]
    public void QuarantineCorruptFile_CollisionWithExistingBackup_DoesNotOverwrite_AppendsSuffix()
    {
        var path = Path.Combine(_dir, "settings.json");
        var now = new DateTime(2026, 7, 15, 10, 30, 0);
        var existingBackup = path + ".corrupt-20260715-103000";
        File.WriteAllText(existingBackup, "already-quarantined");
        File.WriteAllText(path, "new-broken-content");

        var result = FileRecoveryHelper.QuarantineCorruptFile(path, now);

        Assert.True(result.Succeeded);
        Assert.NotEqual(existingBackup, result.BackupPath);
        Assert.Equal(existingBackup + "-1", result.BackupPath);
        Assert.Equal("already-quarantined", File.ReadAllText(existingBackup));
        Assert.Equal("new-broken-content", File.ReadAllText(result.BackupPath!));
    }

    [Fact]
    public void QuarantineCorruptFile_SourceMissing_ReturnsFailureResult_DoesNotThrow()
    {
        var path = Path.Combine(_dir, "does-not-exist.json");

        var result = FileRecoveryHelper.QuarantineCorruptFile(path);

        Assert.False(result.Succeeded);
        Assert.Null(result.BackupPath);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public void QuarantineCorruptFile_DestinationCannotBeCreated_ReturnsFailureResult_OriginalFileRemains()
    {
        // 退避先ディレクトリの代わりにファイルを置くことで File.Move を失敗させる。
        var path = Path.Combine(_dir, "settings.json");
        File.WriteAllText(path, "broken");
        var now = new DateTime(2026, 7, 15, 10, 30, 0);
        var blockedTarget = path + ".corrupt-20260715-103000";
        Directory.CreateDirectory(blockedTarget); // ファイルの代わりにディレクトリを作り移動を失敗させる

        var result = FileRecoveryHelper.QuarantineCorruptFile(path, now);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Exception);
        Assert.True(File.Exists(path), "退避に失敗した場合、元ファイルを削除してはならない");
    }
}
