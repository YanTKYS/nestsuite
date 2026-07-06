using System.IO;
using System.Security;
using System.Text.Json;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.7.6 TD-3-1: WorkspaceFileHelper 共通化後の回帰確認テスト。
/// Shell 上の private ヘルパーは WPF ウィンドウに依存するため直接テストしない。
/// FileErrorMessages と NestSuiteOpenFilePolicy の動作が共通化後も維持されることを確認する。
/// </summary>
public class WorkspaceFileOperationHelperTests
{
    // ── FileErrorMessages 回帰 (LogAndShowLoadError / LogAndShowSaveError 内で使用) ─

    [Fact]
    public void ForLoad_FileNotFoundException_ReturnsFileNotFoundMessage()
    {
        Assert.Contains("見つかりません", FileErrorMessages.ForLoad(new FileNotFoundException()));
    }

    [Fact]
    public void ForLoad_DirectoryNotFoundException_ReturnsFileNotFoundMessage()
    {
        Assert.Contains("見つかりません", FileErrorMessages.ForLoad(new DirectoryNotFoundException()));
    }

    [Fact]
    public void ForLoad_JsonException_ReturnsFormatMessage()
    {
        Assert.Contains("形式", FileErrorMessages.ForLoad(new JsonException()));
    }

    [Fact]
    public void ForLoad_IOException_ReturnsIoMessage()
    {
        Assert.Contains("入出力エラー", FileErrorMessages.ForLoad(new IOException()));
    }

    [Fact]
    public void ForLoad_UnauthorizedAccessException_ReturnsAccessMessage()
    {
        Assert.Contains("権限", FileErrorMessages.ForLoad(new UnauthorizedAccessException()));
    }

    [Fact]
    public void ForLoad_SecurityException_ReturnsAccessMessage()
    {
        Assert.Contains("権限", FileErrorMessages.ForLoad(new SecurityException()));
    }

    [Fact]
    public void ForLoad_PathTooLongException_ReturnsPathMessage()
    {
        Assert.Contains("パス", FileErrorMessages.ForLoad(new PathTooLongException()));
    }

    [Fact]
    public void ForLoad_UnknownException_ReturnsFallbackAndNotEmpty()
    {
        Assert.NotEmpty(FileErrorMessages.ForLoad(new InvalidOperationException("unknown")));
    }

    // ── v2.14.4 FM-4: SchemaVersionTooNewException 専用文言 ────────────────

    [Fact]
    public void ForLoad_SchemaVersionTooNewException_ReturnsNewerVersionMessage_NotCorruptionMessage()
    {
        var message = FileErrorMessages.ForLoad(new SchemaVersionTooNewException("x"));
        Assert.Contains("より新しいバージョン", message);
        Assert.DoesNotContain("破損", message);
    }

    // ── v2.14.7 SH-31: ForKindDetectionFailure 文言 ────────────────────────

    [Fact]
    public void ForKindDetectionFailure_FileNotFound_ContainsNotFoundWording()
    {
        Assert.Contains(
            "見つかりません",
            FileErrorMessages.ForKindDetectionFailure(WorkspaceKindDetectionFailure.FileNotFound));
    }

    [Fact]
    public void ForKindDetectionFailure_InvalidFormat_ContainsFormatWording_AndDoesNotAssertCorruption()
    {
        var message = FileErrorMessages.ForKindDetectionFailure(WorkspaceKindDetectionFailure.InvalidFormat);

        Assert.Contains("形式", message);
        Assert.Contains("とは限りません", message);
        Assert.DoesNotContain("破損", message);
        Assert.DoesNotContain("壊れています。", message);
    }

    [Fact]
    public void ForKindDetectionFailure_SchemaVersionTooNew_ContainsNewerVersionWording_NotCorruption()
    {
        var message = FileErrorMessages.ForKindDetectionFailure(WorkspaceKindDetectionFailure.SchemaVersionTooNew);

        Assert.Contains("より新しいバージョン", message);
        Assert.DoesNotContain("破損", message);
    }

    [Fact]
    public void ForKindDetectionFailure_FileNotFoundAndInvalidFormat_HaveDifferentWording()
    {
        var fileNotFound = FileErrorMessages.ForKindDetectionFailure(WorkspaceKindDetectionFailure.FileNotFound);
        var invalidFormat = FileErrorMessages.ForKindDetectionFailure(WorkspaceKindDetectionFailure.InvalidFormat);

        Assert.NotEqual(fileNotFound, invalidFormat);
    }

    [Fact]
    public void ForKindDetectionFailure_UnsupportedExtension_ListsSupportedFormats()
    {
        Assert.Contains(
            ".nestsuite",
            FileErrorMessages.ForKindDetectionFailure(WorkspaceKindDetectionFailure.UnsupportedExtension));
    }

    [Fact]
    public void ForSave_IOException_ReturnsIoMessage()
    {
        Assert.Contains("入出力エラー", FileErrorMessages.ForSave(new IOException()));
    }

    [Fact]
    public void ForSave_UnauthorizedAccessException_ReturnsAccessMessage()
    {
        Assert.Contains("権限", FileErrorMessages.ForSave(new UnauthorizedAccessException()));
    }

    [Fact]
    public void ForSave_JsonException_ReturnsWriteErrorMessage()
    {
        Assert.Contains("書き込み", FileErrorMessages.ForSave(new JsonException()));
    }

    [Fact]
    public void ForSave_UnknownException_ReturnsFallbackAndNotEmpty()
    {
        Assert.NotEmpty(FileErrorMessages.ForSave(new InvalidOperationException("unknown")));
    }

    // ── NestSuiteOpenFilePolicy 回帰 (CheckAndActivateDuplicateTabForSave 内で使用) ─

    [Fact]
    public void IsSameFile_CaseInsensitive_ReturnsTrue()
    {
        Assert.True(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\Projects\notes.notenest",
            @"C:\projects\NOTES.NOTENEST"));
    }

    [Fact]
    public void IsSameFile_NullLeft_ReturnsFalse()
    {
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(null, @"C:\file.notenest"));
    }

    [Fact]
    public void IsSameFile_NullRight_ReturnsFalse()
    {
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(@"C:\file.notenest", null));
    }

    [Fact]
    public void IsSameFile_BothNull_ReturnsFalse()
    {
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(null, null));
    }

    [Fact]
    public void IsSameFile_DifferentFiles_ReturnsFalse()
    {
        Assert.False(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\Projects\notes.notenest",
            @"C:\Projects\other.notenest"));
    }

    [Fact]
    public void IsSameFile_IdenticalPaths_ReturnsTrue()
    {
        Assert.True(NestSuiteOpenFilePolicy.IsSameFile(
            @"C:\Projects\notes.notenest",
            @"C:\Projects\notes.notenest"));
    }

    // ── v2.14.2: IsDuplicateForSave（.nestsuite の WorkspaceKind 横断重複検出）回帰 ─

    [Fact]
    public void IsDuplicateForSave_LegacyExtension_DifferentKind_ReturnsFalse()
    {
        // legacy 拡張子は拡張子自体が WorkspaceKind を確定するため、異なる kind は重複扱いしない
        Assert.False(NestSuiteOpenFilePolicy.IsDuplicateForSave(
            @"C:\Projects\a.chatnest", NestSuiteWorkspaceKind.ChatNest,
            @"C:\Projects\a.chatnest", NestSuiteWorkspaceKind.NoteNest));
    }

    [Fact]
    public void IsDuplicateForSave_LegacyExtension_SameKind_ReturnsTrue()
    {
        Assert.True(NestSuiteOpenFilePolicy.IsDuplicateForSave(
            @"C:\Projects\a.chatnest", NestSuiteWorkspaceKind.ChatNest,
            @"C:\Projects\a.chatnest", NestSuiteWorkspaceKind.ChatNest));
    }

    [Fact]
    public void IsDuplicateForSave_NestSuitePath_ChatNestTabOpen_NoteNestSaveAs_ReturnsTrue()
    {
        // .nestsuite で ChatNest タブが開いている状態で NoteNest として同じパスへ Save As した場合、
        // 拡張子だけでは WorkspaceKind が定まらないため kind に関係なく重複検出する
        Assert.True(NestSuiteOpenFilePolicy.IsDuplicateForSave(
            @"C:\Projects\meeting.nestsuite", NestSuiteWorkspaceKind.ChatNest,
            @"C:\Projects\meeting.nestsuite", NestSuiteWorkspaceKind.NoteNest));
    }

    [Fact]
    public void IsDuplicateForSave_NestSuitePath_IdeaNestTabOpen_ChatNestSaveAs_ReturnsTrue()
    {
        Assert.True(NestSuiteOpenFilePolicy.IsDuplicateForSave(
            @"C:\Projects\plan.nestsuite", NestSuiteWorkspaceKind.IdeaNest,
            @"C:\Projects\plan.nestsuite", NestSuiteWorkspaceKind.ChatNest));
    }

    [Fact]
    public void IsDuplicateForSave_NestSuitePath_DifferentPaths_ReturnsFalse()
    {
        Assert.False(NestSuiteOpenFilePolicy.IsDuplicateForSave(
            @"C:\Projects\a.nestsuite", NestSuiteWorkspaceKind.ChatNest,
            @"C:\Projects\b.nestsuite", NestSuiteWorkspaceKind.NoteNest));
    }

    [Fact]
    public void IsDuplicateForSave_NullExistingTabPath_ReturnsFalse()
    {
        Assert.False(NestSuiteOpenFilePolicy.IsDuplicateForSave(
            null, NestSuiteWorkspaceKind.ChatNest,
            @"C:\Projects\a.nestsuite", NestSuiteWorkspaceKind.NoteNest));
    }
}
