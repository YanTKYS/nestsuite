using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.15 TD-67 (review1-fable5.md R-7): MultipleOpenFailureMessageBuilder の単体テスト。
/// UI 非依存の string builder のため、WPF Window を起動せず実際の挙動を検証できる。
/// </summary>
public class MultipleOpenFailureMessageBuilderTests
{
    [Fact]
    public void Build_IncludesFailureCount()
    {
        var failures = new[]
        {
            new OpenFileFailure(@"C:\notes\a.txt", WorkspaceKindDetectionFailure.UnsupportedExtension),
            new OpenFileFailure(@"C:\notes\b.txt", WorkspaceKindDetectionFailure.UnsupportedExtension),
        };

        var message = MultipleOpenFailureMessageBuilder.Build(failures);

        Assert.Contains("2 件", message);
    }

    [Fact]
    public void Build_IncludesFileNames()
    {
        var failures = new[]
        {
            new OpenFileFailure(@"C:\notes\plan.txt", WorkspaceKindDetectionFailure.UnsupportedExtension),
            new OpenFileFailure(@"C:\old\old-project.nestsuite", WorkspaceKindDetectionFailure.InvalidFormat),
        };

        var message = MultipleOpenFailureMessageBuilder.Build(failures);

        Assert.Contains("plan.txt", message);
        Assert.Contains("old-project.nestsuite", message);
    }

    [Fact]
    public void Build_IncludesFailureReasons()
    {
        var failures = new[] { new OpenFileFailure(@"C:\shared.notenest", WorkspaceKindDetectionFailure.FileNotFound) };

        var message = MultipleOpenFailureMessageBuilder.Build(failures);

        var expectedReason = FileErrorMessages.ForKindDetectionFailure(WorkspaceKindDetectionFailure.FileNotFound).Split('\n')[0];
        Assert.Contains(expectedReason, message);
    }

    [Fact]
    public void Build_UnsupportedExtension_UsesUserFacingWording()
    {
        var failures = new[] { new OpenFileFailure(@"C:\a.txt", WorkspaceKindDetectionFailure.UnsupportedExtension) };

        var message = MultipleOpenFailureMessageBuilder.Build(failures);

        Assert.Contains("このファイル形式は NestSuite では開けません", message);
    }

    [Fact]
    public void Build_FileNotFound_MentionsExternalNetworkDriveAndMovedFile()
    {
        var failures = new[] { new OpenFileFailure(@"C:\a.notenest", WorkspaceKindDetectionFailure.FileNotFound) };

        var message = MultipleOpenFailureMessageBuilder.Build(failures);

        Assert.Contains("外部ドライブ", message);
        Assert.Contains("ネットワークドライブ", message);
        Assert.Contains("移動済み", message);
    }

    [Fact]
    public void Build_AccessDenied_MentionsPermissionOrOtherAppUsage()
    {
        var failures = new[] { new OpenFileFailure(@"C:\a.nestsuite", WorkspaceKindDetectionFailure.AccessDenied) };

        var message = MultipleOpenFailureMessageBuilder.Build(failures);

        Assert.Contains("権限", message);
        Assert.Contains("他のアプリ", message);
    }

    [Fact]
    public void Build_SchemaVersionTooNew_MentionsNewerNestSuiteVersionPossibility()
    {
        var failures = new[] { new OpenFileFailure(@"C:\a.nestsuite", WorkspaceKindDetectionFailure.SchemaVersionTooNew) };

        var message = MultipleOpenFailureMessageBuilder.Build(failures);

        Assert.Contains("新しいバージョンの NestSuite", message);
    }

    [Fact]
    public void Build_ExceedingDisplayLimit_SummarizesRemainingAsOthersCount()
    {
        var failures = Enumerable.Range(0, 7)
            .Select(i => new OpenFileFailure($@"C:\file{i}.txt", WorkspaceKindDetectionFailure.UnsupportedExtension))
            .ToArray();

        var message = MultipleOpenFailureMessageBuilder.Build(failures);

        Assert.Contains("ほか 2 件", message);
        Assert.DoesNotContain("file5.txt", message);
        Assert.DoesNotContain("file6.txt", message);
    }

    [Fact]
    public void Build_AtExactlyDisplayLimit_ListsAllWithoutOthersSummary()
    {
        var failures = Enumerable.Range(0, 5)
            .Select(i => new OpenFileFailure($@"C:\file{i}.txt", WorkspaceKindDetectionFailure.UnsupportedExtension))
            .ToArray();

        var message = MultipleOpenFailureMessageBuilder.Build(failures);

        Assert.DoesNotContain("ほか", message);
        for (int i = 0; i < 5; i++) Assert.Contains($"file{i}.txt", message);
    }

    [Fact]
    public void Build_DoesNotExposeInternalExceptionOrTypeNamesOrNull()
    {
        var failures = new[] { new OpenFileFailure(@"C:\a.txt", WorkspaceKindDetectionFailure.Unknown) };

        var message = MultipleOpenFailureMessageBuilder.Build(failures);

        Assert.DoesNotContain("Exception", message);
        Assert.DoesNotContain("System.", message);
        Assert.DoesNotContain("null", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_SingleFailure_StillReportsCountFileNameAndReason()
    {
        var failures = new[] { new OpenFileFailure(@"C:\a.txt", WorkspaceKindDetectionFailure.UnsupportedExtension) };

        var message = MultipleOpenFailureMessageBuilder.Build(failures);

        Assert.Contains("1 件", message);
        Assert.Contains("a.txt", message);
        Assert.Contains("このファイル形式は NestSuite では開けません", message);
    }

    // ── v2.16.19 TD-71 (review2-fable5.md 新リスク②): InvalidFormat 時の .bak 詳細案内 ──────

    [Fact]
    public void Build_ContainsInvalidFormat_AppendsBakDetailHint()
    {
        var failures = new[]
        {
            new OpenFileFailure(@"C:\a.txt", WorkspaceKindDetectionFailure.UnsupportedExtension),
            new OpenFileFailure(@"C:\broken.nestsuite", WorkspaceKindDetectionFailure.InvalidFormat),
        };

        var message = MultipleOpenFailureMessageBuilder.Build(failures);

        Assert.Contains(FileErrorMessages.MultipleFailuresBakDetailHint, message);
    }

    [Fact]
    public void Build_DoesNotContainInvalidFormat_DoesNotAppendBakDetailHint()
    {
        var failures = new[]
        {
            new OpenFileFailure(@"C:\a.notenest", WorkspaceKindDetectionFailure.FileNotFound),
            new OpenFileFailure(@"C:\b.nestsuite", WorkspaceKindDetectionFailure.AccessDenied),
            new OpenFileFailure(@"C:\c.nestsuite", WorkspaceKindDetectionFailure.SchemaVersionTooNew),
        };

        var message = MultipleOpenFailureMessageBuilder.Build(failures);

        Assert.DoesNotContain(FileErrorMessages.MultipleFailuresBakDetailHint, message);
    }

    [Fact]
    public void Build_MultipleInvalidFormatEntries_AppendsBakDetailHintOnlyOnce()
    {
        var failures = new[]
        {
            new OpenFileFailure(@"C:\a.nestsuite", WorkspaceKindDetectionFailure.InvalidFormat),
            new OpenFileFailure(@"C:\b.nestsuite", WorkspaceKindDetectionFailure.InvalidFormat),
            new OpenFileFailure(@"C:\c.notenest", WorkspaceKindDetectionFailure.FileNotFound),
        };

        var message = MultipleOpenFailureMessageBuilder.Build(failures);

        var occurrences = message.Split(
            [FileErrorMessages.MultipleFailuresBakDetailHint], StringSplitOptions.None).Length - 1;
        Assert.Equal(1, occurrences);
    }
}
