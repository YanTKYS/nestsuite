using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.10.13 TD-26: GuardNest 第一段階整理の回帰テスト。
/// AtomicFileWriter / CloseConfirmationService / FileErrorMessages / ErrorLogService
/// の責務境界をテストで固定する。
/// </summary>
public class GuardNestTD26Tests : IDisposable
{
    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private readonly string _tempDir;

    public GuardNestTD26Tests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "GuardNestTD26_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    // ── バージョン ────────────────────────────────────────────────────────

    // ── AtomicFileWriter: tmp cleanup ─────────────────────────────────────

    [Fact]
    public void AtomicFileWriter_NoTmpRemaining_AfterSuccessfulWrite()
    {
        var path = Path.Combine(_tempDir, "test.notenest");
        AtomicFileWriter.WriteAllText(path, "content", Encoding.UTF8);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void AtomicFileWriter_NoTmpRemaining_AfterOverwrite()
    {
        var path = Path.Combine(_tempDir, "overwrite.notenest");
        File.WriteAllText(path, "original", Encoding.UTF8);
        AtomicFileWriter.WriteAllText(path, "updated", Encoding.UTF8);
        Assert.False(File.Exists(path + ".tmp"));
        Assert.Equal("updated", File.ReadAllText(path, Encoding.UTF8));
    }

    [Fact]
    public void AtomicFileWriter_CreatesBakFile_WhenBackupPathProvided()
    {
        var path    = Path.Combine(_tempDir, "withbak.notenest");
        var bakPath = path + ".bak";
        File.WriteAllText(path, "original", Encoding.UTF8);

        AtomicFileWriter.WriteAllText(path, "updated", Encoding.UTF8, bakPath);

        Assert.True(File.Exists(bakPath));
        Assert.Equal("original", File.ReadAllText(bakPath, Encoding.UTF8));
        Assert.Equal("updated",  File.ReadAllText(path,    Encoding.UTF8));
    }

    [Fact]
    public void AtomicFileWriter_CreatesDirectory_IfNotExist()
    {
        var path = Path.Combine(_tempDir, "sub", "deep", "file.notenest");
        AtomicFileWriter.WriteAllText(path, "nested", Encoding.UTF8);
        Assert.True(File.Exists(path));
    }

    // ── CloseConfirmationService: Save / Discard / Cancel ─────────────────

    [Fact]
    public void CloseConfirmation_WhenNotDirty_ReturnsNoActionNeeded()
    {
        var result = CloseConfirmationService.EvaluateSingle(
            hasUnsavedChanges: false,
            requestDecision:   () => UnsavedChangeDecision.Save,
            save:              () => true);

        Assert.Equal(UnsavedChangeDecision.NoActionNeeded, result);
    }

    [Fact]
    public void CloseConfirmation_SaveSuccess_CanClose()
    {
        var result = CloseConfirmationService.EvaluateSingle(
            hasUnsavedChanges: true,
            requestDecision:   () => UnsavedChangeDecision.Save,
            save:              () => true);

        Assert.Equal(UnsavedChangeDecision.Save, result);
        Assert.True(CloseConfirmationService.CanCloseSingle(
            true, () => UnsavedChangeDecision.Save, () => true));
    }

    [Fact]
    public void CloseConfirmation_SaveFailure_PreventClose()
    {
        // 保存失敗時はクローズを阻止する（dirty 維持の方針）
        var result = CloseConfirmationService.EvaluateSingle(
            hasUnsavedChanges: true,
            requestDecision:   () => UnsavedChangeDecision.Save,
            save:              () => false);

        Assert.Equal(UnsavedChangeDecision.Cancel, result);
        Assert.False(CloseConfirmationService.CanCloseSingle(
            true, () => UnsavedChangeDecision.Save, () => false));
    }

    [Fact]
    public void CloseConfirmation_Discard_AllowsClose()
    {
        var canClose = CloseConfirmationService.CanCloseSingle(
            true, () => UnsavedChangeDecision.Discard, null);
        Assert.True(canClose);
    }

    [Fact]
    public void CloseConfirmation_Cancel_PreventClose()
    {
        var canClose = CloseConfirmationService.CanCloseSingle(
            true, () => UnsavedChangeDecision.Cancel, null);
        Assert.False(canClose);
    }

    [Fact]
    public void CloseConfirmation_TempTab_SkippedInEvaluateMany()
    {
        // CanClose=false タブ（TempNest）は EvaluateMany で除外される
        var targets = new[]
        {
            new CloseConfirmationTarget("note",  CanClose: true,  HasUnsavedChanges: false),
            new CloseConfirmationTarget("temp",  CanClose: false, HasUnsavedChanges: false),
        };

        var result = CloseConfirmationService.EvaluateMany(
            targets,
            _ => UnsavedChangeDecision.Cancel,
            null);

        Assert.True(result.CanContinue);
    }

    // ── FileErrorMessages: 例外種別 → メッセージ ──────────────────────────

    [Fact]
    public void FileErrorMessages_ForLoad_FileNotFound_ReturnsJapaneseMessage()
    {
        var msg = FileErrorMessages.ForLoad(new FileNotFoundException("test"));
        Assert.False(string.IsNullOrWhiteSpace(msg));
        Assert.DoesNotContain("test", msg); // ex.Message をそのまま出さない
    }

    [Fact]
    public void FileErrorMessages_ForLoad_UnauthorizedAccess_ReturnsDifferentMessage()
    {
        var fileNotFound = FileErrorMessages.ForLoad(new FileNotFoundException("x"));
        var unauthorized = FileErrorMessages.ForLoad(new UnauthorizedAccessException("x"));
        Assert.NotEqual(fileNotFound, unauthorized);
    }

    [Fact]
    public void FileErrorMessages_ForSave_IOException_ReturnsJapaneseMessage()
    {
        var msg = FileErrorMessages.ForSave(new IOException("disk error"));
        Assert.False(string.IsNullOrWhiteSpace(msg));
        Assert.DoesNotContain("disk error", msg); // ex.Message をそのまま出さない
    }

    [Fact]
    public void FileErrorMessages_ForSave_UnauthorizedAccess_ReturnsDifferentMessage()
    {
        var io           = FileErrorMessages.ForSave(new IOException("x"));
        var unauthorized = FileErrorMessages.ForSave(new UnauthorizedAccessException("x"));
        Assert.NotEqual(io, unauthorized);
    }

    // ── ErrorLogService: Error のみ、Info / Warning なし ──────────────────

    [Fact]
    public void ErrorLogService_HasNoLogInfoMethod()
    {
        var type = typeof(MainViewModel).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == "ErrorLogService");

        Assert.NotNull(type);
        var infoMethod = type.GetMethod("LogInfo",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.Null(infoMethod);
    }

    [Fact]
    public void ErrorLogService_HasNoLogWarningMethod()
    {
        var type = typeof(MainViewModel).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == "ErrorLogService");

        Assert.NotNull(type);
        var warnMethod = type.GetMethod("LogWarning",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.Null(warnMethod);
    }

    [Fact]
    public void ErrorLogService_HasLogMethod_WithOperationAndException()
    {
        var type = typeof(MainViewModel).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == "ErrorLogService");

        Assert.NotNull(type);
        var logMethod = type.GetMethod("Log",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(logMethod);

        var parameters = logMethod!.GetParameters();
        Assert.True(parameters.Length >= 2);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(Exception), parameters[1].ParameterType);
    }

    // ── GuardNest 方針文書の確認 ─────────────────────────────────────────

    [Fact]
    public void PolicyDocument_DescribesAtomicFileWriter()
    {
        Assert.Contains("AtomicFileWriter", ReadPolicyDocument());
    }

    [Fact]
    public void PolicyDocument_DescribesCloseConfirmationService()
    {
        Assert.Contains("CloseConfirmationService", ReadPolicyDocument());
    }

    [Fact]
    public void PolicyDocument_DescribesErrorLogService()
    {
        Assert.Contains("ErrorLogService", ReadPolicyDocument());
    }

    [Fact]
    public void PolicyDocument_StatesErrorOnlyPolicy()
    {
        var text = ReadPolicyDocument();
        Assert.Contains("Error", text);
        Assert.Contains("Info", text); // "Info / Warning 不可" という形で含まれる
    }

    [Fact]
    public void PolicyDocument_StatesPersonalDataNotLogged()
    {
        var text = ReadPolicyDocument();
        Assert.Contains("本文", text);
        Assert.Contains("個人情報", text);
    }

    // ── backlog / release-notes ───────────────────────────────────────────

    [Fact]
    public void Backlog_TD26_IsMarkedComplete()
    {
        var path = Path.Combine(RepoRoot, "docs", "backlog.md");
        Assert.True(File.Exists(path), $"backlog.md not found: {path}");
        Assert.Contains("~~TD-26~~", File.ReadAllText(path));
    }

    [Fact]
    public void ReleaseNotes_Contains_V2_10_13()
    {
        var path = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(path));
        Assert.Contains("v2.10.13", File.ReadAllText(path));
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private string ReadPolicyDocument()
    {
        var path = Path.Combine(RepoRoot, "docs", "architecture", "sessionnest-guardnest-policy.md");
        Assert.True(File.Exists(path), $"Policy document not found: {path}");
        return File.ReadAllText(path);
    }
}
