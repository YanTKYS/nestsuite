using System.IO;
using System.Security;
using System.Text.Json;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.7.5: ErrorLogService と FileErrorMessages の動作確認テスト。
/// </summary>
public class ErrorLogServiceTests : IDisposable
{
    // テスト専用の一時ログパスを注入するため、リフレクションを避けて
    // テスト可能な設計にするには LogPath を外部から渡せる必要がある。
    // ErrorLogService は static のため、テストでは一時ディレクトリへ書き込む
    // ラッパーメソッドを直接呼び出して副作用を検証する。

    private readonly string _tempDir;
    private readonly string _logPath;

    public ErrorLogServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"nestsuite-test-{Guid.NewGuid():N}");
        _logPath = Path.Combine(_tempDir, "nestsuite-error.log");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    // ── ErrorLogService (ログファイル書き込みのテスト) ───────────────────

    [Fact]
    public void Log_WritesToFile_ContainsExceptionTypeAndMessage()
    {
        var ex = new FileNotFoundException("テストエラー", "test.notenest");

        bool result = ErrorLogServiceTestHelper.Log(_logPath, "TestOp", ex);

        Assert.True(result);
        var content = File.ReadAllText(_logPath);
        Assert.Contains("System.IO.FileNotFoundException", content);
        Assert.Contains("テストエラー", content);
    }

    [Fact]
    public void Log_WritesToFile_ContainsOperationName()
    {
        var ex = new IOException("io error");

        ErrorLogServiceTestHelper.Log(_logPath, "ChatNestSave", ex);

        var content = File.ReadAllText(_logPath);
        Assert.Contains("ChatNestSave", content);
    }

    [Fact]
    public void Log_WritesToFile_ContainsWorkspaceKindAndFilePath()
    {
        var ex = new UnauthorizedAccessException("access denied");

        ErrorLogServiceTestHelper.Log(_logPath, "IdeaNestSave", ex,
            workspaceKind: "IdeaNest", filePath: @"C:\Projects\ideas.ideanest");

        var content = File.ReadAllText(_logPath);
        Assert.Contains("IdeaNest", content);
        Assert.Contains("ideas.ideanest", content);
    }

    [Fact]
    public void Log_DoesNotThrow_WhenLogPathIsInvalid()
    {
        var invalidPath = Path.Combine(
            "Z:\\nonexistent\\deeply\\nested\\path", "nestsuite-error.log");
        var ex = new Exception("test");

        var result = ErrorLogServiceTestHelper.Log(invalidPath, "TestOp", ex);

        Assert.False(result);
    }

    [Fact]
    public void Log_DoesNotContainUserContent_OnlyMetadata()
    {
        var ex = new JsonException("unexpected token");
        const string userContent = "秘密のノート本文テスト1234";

        ErrorLogServiceTestHelper.Log(_logPath, "NoteNestLoad", ex,
            filePath: "notes.notenest");

        var content = File.ReadAllText(_logPath);
        Assert.DoesNotContain(userContent, content);
        Assert.Contains("NoteNestLoad", content);
    }

    [Fact]
    public void Log_Appends_WhenCalledMultipleTimes()
    {
        var ex1 = new FileNotFoundException("first");
        var ex2 = new IOException("second");

        ErrorLogServiceTestHelper.Log(_logPath, "Op1", ex1);
        ErrorLogServiceTestHelper.Log(_logPath, "Op2", ex2);

        var content = File.ReadAllText(_logPath);
        Assert.Contains("Op1", content);
        Assert.Contains("Op2", content);
        Assert.Contains("first", content);
        Assert.Contains("second", content);
    }

    [Fact]
    public void Log_IncludesInnerException_WhenPresent()
    {
        var inner = new JsonException("inner json error");
        var ex = new InvalidOperationException("outer error", inner);

        ErrorLogServiceTestHelper.Log(_logPath, "TestOp", ex);

        var content = File.ReadAllText(_logPath);
        Assert.Contains("inner json error", content);
    }

    // ── v2.14.0 TD-57 (LT-12): ローテーション連携 ─────────────────────────

    [Fact]
    public void Log_RotatesOversizedLog_AndWritesToFreshCurrentFile()
    {
        ErrorLogServiceTestHelper.Log(_logPath, "OldOperation", new IOException("old"));
        var oldContent = File.ReadAllText(_logPath);

        // 閾値 1 バイトで再度ログ → 旧内容は第1世代へ退避され、現行ログは新エントリのみ
        ErrorLogServiceTestHelper.Log(_logPath, "NewOperation", new IOException("new"),
            maxSizeBytes: 1, maxGenerations: 3);

        var archived = File.ReadAllText(ErrorLogRotation.ArchivePath(_logPath, 1));
        var current = File.ReadAllText(_logPath);
        Assert.Equal(oldContent, archived);
        Assert.Contains("NewOperation", current);
        Assert.DoesNotContain("OldOperation", current);
    }

    [Fact]
    public void ErrorLogService_LogPath_KeepsCompatibleLocationAndFileName()
    {
        // LT-3 方針: ログ保存先（%APPDATA%\NoteNest\logs\nestsuite-error.log）は互換性のため変更しない。
        // ErrorLogService は internal のためアセンブリ経由で private 定数を検査する。
        var serviceType = typeof(ErrorLogRotation).Assembly.GetType("NestSuite.Services.ErrorLogService");
        Assert.NotNull(serviceType);
        var field = serviceType!.GetField("LogPath",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        var logPath = (string?)field!.GetValue(null);
        Assert.NotNull(logPath);
        Assert.Contains("NoteNest", logPath);
        Assert.EndsWith("nestsuite-error.log", logPath);
    }

    // ── FileErrorMessages (ユーザー向けメッセージ分岐テスト) ──────────────

    [Fact]
    public void ForLoad_FileNotFoundException_ReturnsSpecificMessage()
    {
        var ex = new FileNotFoundException("not found");
        var msg = FileErrorMessages.ForLoad(ex);
        Assert.Contains("見つかりません", msg);
    }

    [Fact]
    public void ForLoad_DirectoryNotFoundException_ReturnsSpecificMessage()
    {
        var ex = new DirectoryNotFoundException("dir not found");
        var msg = FileErrorMessages.ForLoad(ex);
        Assert.Contains("見つかりません", msg);
    }

    [Fact]
    public void ForLoad_UnauthorizedAccessException_ReturnsAccessMessage()
    {
        var ex = new UnauthorizedAccessException("access denied");
        var msg = FileErrorMessages.ForLoad(ex);
        Assert.Contains("権限", msg);
    }

    [Fact]
    public void ForLoad_SecurityException_ReturnsAccessMessage()
    {
        var ex = new SecurityException("security error");
        var msg = FileErrorMessages.ForLoad(ex);
        Assert.Contains("権限", msg);
    }

    [Fact]
    public void ForLoad_JsonException_ReturnsFormatMessage()
    {
        var ex = new JsonException("invalid json");
        var msg = FileErrorMessages.ForLoad(ex);
        Assert.Contains("形式", msg);
    }

    [Fact]
    public void ForLoad_IOException_ReturnsIoMessage()
    {
        var ex = new IOException("disk error");
        var msg = FileErrorMessages.ForLoad(ex);
        Assert.Contains("入出力エラー", msg);
    }

    [Fact]
    public void ForSave_UnauthorizedAccessException_ReturnsAccessMessage()
    {
        var ex = new UnauthorizedAccessException("access denied");
        var msg = FileErrorMessages.ForSave(ex);
        Assert.Contains("権限", msg);
    }

    [Fact]
    public void ForSave_IOException_ReturnsIoMessage()
    {
        var ex = new IOException("disk full");
        var msg = FileErrorMessages.ForSave(ex);
        Assert.Contains("入出力エラー", msg);
    }

    [Fact]
    public void ForLoad_UnknownException_ReturnsFallbackMessage()
    {
        var ex = new InvalidOperationException("unexpected");
        var msg = FileErrorMessages.ForLoad(ex);
        Assert.NotEmpty(msg);
    }

    [Fact]
    public void ForSave_UnknownException_ReturnsFallbackMessage()
    {
        var ex = new InvalidOperationException("unexpected");
        var msg = FileErrorMessages.ForSave(ex);
        Assert.NotEmpty(msg);
    }
}

/// <summary>
/// テスト用: ErrorLogService のログ書き込みをカスタムパスで実行するヘルパー。
/// 本番 ErrorLogService は static で LogPath が固定されているため、
/// テスト専用のパス指定版として同等のロジックを再現する。
/// </summary>
internal static class ErrorLogServiceTestHelper
{
    public static bool Log(
        string logPath,
        string operation,
        Exception ex,
        string? workspaceKind = null,
        string? filePath = null,
        long maxSizeBytes = 1024 * 1024,
        int maxGenerations = 3)
    {
        try
        {
            var dir = Path.GetDirectoryName(logPath)!;
            Directory.CreateDirectory(dir);
            // v2.14.0 TD-57: 本番と同じ ErrorLogRotation を経由する（ローテーションは本物を検証する）
            ErrorLogRotation.RotateIfNeeded(logPath, maxSizeBytes, maxGenerations);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("========================================");
            sb.AppendLine($"Timestamp  : {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
            sb.AppendLine($"Version    : test");
            sb.AppendLine($"Operation  : {operation}");
            if (workspaceKind != null) sb.AppendLine($"Workspace  : {workspaceKind}");
            if (filePath != null) sb.AppendLine($"File       : {filePath}");
            sb.AppendLine($"Exception  : {ex.GetType().FullName}");
            sb.AppendLine($"Message    : {ex.Message}");
            sb.AppendLine("StackTrace :");
            sb.AppendLine(ex.StackTrace ?? "(none)");
            if (ex.InnerException is { } inner)
            {
                sb.AppendLine($"Inner      : {inner.GetType().FullName}: {inner.Message}");
                sb.AppendLine(inner.StackTrace ?? "(none)");
            }
            sb.AppendLine();

            File.AppendAllText(logPath, sb.ToString(), System.Text.Encoding.UTF8);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
