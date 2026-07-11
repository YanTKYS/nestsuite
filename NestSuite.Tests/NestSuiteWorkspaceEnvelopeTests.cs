using System.IO;
using System.Text.Json;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

// v2.14.1 FM-1: .nestsuite wrapper 形式の読み書き確認
public class NestSuiteWorkspaceEnvelopeTests
{
    [Fact]
    public void WrapAndRead_RoundTripsKindSchemaVersionAndPayload()
    {
        var payloadJson = """{"a":1,"nested":{"b":"日本語"}}""";

        var wrapped = NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "1.4.1", payloadJson);
        var envelope = NestSuiteWorkspaceEnvelope.Read(wrapped);

        Assert.Equal("NoteNest", envelope.WorkspaceKind);
        Assert.Equal("1.4.1", envelope.PayloadSchemaVersion);
        Assert.Equal("1.0", envelope.FormatVersion);

        using var payload = JsonDocument.Parse(envelope.PayloadJson);
        Assert.Equal(1, payload.RootElement.GetProperty("a").GetInt32());
        Assert.Equal("日本語", payload.RootElement.GetProperty("nested").GetProperty("b").GetString());
    }

    [Fact]
    public void Read_UnknownExtraProperties_AreIgnored()
    {
        var json = """
            {
              "format": "NestSuiteWorkspace",
              "formatVersion": "1.0",
              "workspaceKind": "IdeaNest",
              "payloadSchemaVersion": "1.1.4",
              "payload": {},
              "createdAt": "2026-01-01",
              "metadata": { "x": 1 }
            }
            """;

        var envelope = NestSuiteWorkspaceEnvelope.Read(json);

        Assert.Equal("IdeaNest", envelope.WorkspaceKind);
    }

    [Fact]
    public void Read_MissingFormatVersionAndPayloadSchemaVersion_AreTolerated()
    {
        var json = """
            {
              "format": "NestSuiteWorkspace",
              "workspaceKind": "ChatNest",
              "payload": {}
            }
            """;

        var envelope = NestSuiteWorkspaceEnvelope.Read(json);

        Assert.Equal("", envelope.FormatVersion);
        Assert.Equal("", envelope.PayloadSchemaVersion);
    }

    [Fact]
    public void Read_MissingFormat_Throws()
    {
        var json = """{"workspaceKind":"NoteNest","payload":{}}""";

        Assert.Throws<InvalidDataException>(() => NestSuiteWorkspaceEnvelope.Read(json));
    }

    [Fact]
    public void Read_WrongFormatName_Throws()
    {
        var json = """{"format":"SomethingElse","workspaceKind":"NoteNest","payload":{}}""";

        Assert.Throws<InvalidDataException>(() => NestSuiteWorkspaceEnvelope.Read(json));
    }

    [Fact]
    public void Read_MissingWorkspaceKind_Throws()
    {
        var json = """{"format":"NestSuiteWorkspace","payload":{}}""";

        Assert.Throws<InvalidDataException>(() => NestSuiteWorkspaceEnvelope.Read(json));
    }

    [Fact]
    public void Read_MissingPayload_Throws()
    {
        var json = """{"format":"NestSuiteWorkspace","workspaceKind":"NoteNest"}""";

        Assert.Throws<InvalidDataException>(() => NestSuiteWorkspaceEnvelope.Read(json));
    }

    [Fact]
    public void Read_PayloadNotObject_Throws()
    {
        var json = """{"format":"NestSuiteWorkspace","workspaceKind":"NoteNest","payload":"text"}""";

        Assert.Throws<InvalidDataException>(() => NestSuiteWorkspaceEnvelope.Read(json));
    }

    [Fact]
    public void Read_BrokenJson_Throws()
    {
        Assert.Throws<InvalidDataException>(() => NestSuiteWorkspaceEnvelope.Read("{ broken"));
    }

    [Fact]
    public void EnsureKind_Mismatch_Throws()
    {
        var envelope = NestSuiteWorkspaceEnvelope.Read(
            NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "1.4.1", "{}"));

        Assert.Throws<InvalidDataException>(() =>
            NestSuiteWorkspaceEnvelope.EnsureKind(envelope, "ChatNest"));
    }

    [Fact]
    public void EnsureKind_Match_DoesNotThrow()
    {
        var envelope = NestSuiteWorkspaceEnvelope.Read(
            NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "1.4.1", "{}"));

        NestSuiteWorkspaceEnvelope.EnsureKind(envelope, "NoteNest");
    }

    [Fact]
    public void IsEnvelopePath_MatchesCaseInsensitive_AndRejectsOthers()
    {
        Assert.True(NestSuiteWorkspaceEnvelope.IsEnvelopePath("file.nestsuite"));
        Assert.True(NestSuiteWorkspaceEnvelope.IsEnvelopePath("file.NESTSUITE"));
        Assert.False(NestSuiteWorkspaceEnvelope.IsEnvelopePath("file.notenest"));
        Assert.False(NestSuiteWorkspaceEnvelope.IsEnvelopePath(null));
        Assert.False(NestSuiteWorkspaceEnvelope.IsEnvelopePath(""));
    }

    [Fact]
    public void TryDetectKindFromFile_ReturnsKind_ForValidFile_AndNullOtherwise()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"nestsuite-envelope-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var validPath = Path.Combine(tempDir, "a.nestsuite");
            File.WriteAllText(validPath, NestSuiteWorkspaceEnvelope.Wrap("IdeaNest", "1.1.4", "{}"));
            Assert.Equal("IdeaNest", NestSuiteWorkspaceEnvelope.TryDetectKindFromFile(validPath));

            var missingPath = Path.Combine(tempDir, "missing.nestsuite");
            Assert.Null(NestSuiteWorkspaceEnvelope.TryDetectKindFromFile(missingPath));

            var brokenPath = Path.Combine(tempDir, "broken.nestsuite");
            File.WriteAllText(brokenPath, "not json");
            Assert.Null(NestSuiteWorkspaceEnvelope.TryDetectKindFromFile(brokenPath));
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    // ── v2.14.7 SH-31: DetectKindFromFile（理由つき判定） ──────────────

    [Fact]
    public void DetectKindFromFile_ValidEnvelope_ReturnsNoneFailure_AndKindAndSchemaVersion()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        try
        {
            File.WriteAllText(path, NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "1.4.1", "{}"));

            var result = NestSuiteWorkspaceEnvelope.DetectKindFromFile(path);

            Assert.Equal(WorkspaceKindDetectionFailure.None, result.Failure);
            Assert.Equal("NoteNest", result.WorkspaceKind);
            Assert.Equal("1.4.1", result.PayloadSchemaVersion);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void DetectKindFromFile_MissingFile_ReturnsFileNotFound()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");

        var result = NestSuiteWorkspaceEnvelope.DetectKindFromFile(path);

        Assert.Equal(WorkspaceKindDetectionFailure.FileNotFound, result.Failure);
        Assert.Null(result.WorkspaceKind);
    }

    [Fact]
    public void DetectKindFromFile_BrokenJson_ReturnsInvalidFormat()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        try
        {
            File.WriteAllText(path, "not json");

            var result = NestSuiteWorkspaceEnvelope.DetectKindFromFile(path);

            Assert.Equal(WorkspaceKindDetectionFailure.InvalidFormat, result.Failure);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void DetectKindFromFile_ValidJsonButNotWrapperFormat_ReturnsInvalidFormat()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        try
        {
            File.WriteAllText(path, """{"foo":1}""");

            var result = NestSuiteWorkspaceEnvelope.DetectKindFromFile(path);

            Assert.Equal(WorkspaceKindDetectionFailure.InvalidFormat, result.Failure);
        }
        finally { File.Delete(path); }
    }

    // ── v2.16.34 TD-59b-1: ReadFromFile（読込回数・failure 分類の test seam） ──

    [Fact]
    public void ReadFromFile_ValidEnvelope_ReturnsEnvelope_AndNoneFailure_WithExactlyOneReadCall()
    {
        var wrapped = NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "1.4.1", "{}");
        var readCalls = 0;

        var result = NestSuiteWorkspaceEnvelope.ReadFromFile(
            "fake/path.nestsuite",
            fileExists: _ => true,
            readAllText: _ => { readCalls++; return wrapped; });

        Assert.Equal(1, readCalls);
        Assert.Equal(WorkspaceKindDetectionFailure.None, result.Failure);
        Assert.NotNull(result.Envelope);
        Assert.Equal("NoteNest", result.Envelope!.WorkspaceKind);
        Assert.Equal("1.4.1", result.Envelope.PayloadSchemaVersion);
    }

    [Fact]
    public void ReadFromFile_FileDoesNotExist_ReturnsFileNotFound_AndDoesNotCallReadAllText()
    {
        var readCalls = 0;

        var result = NestSuiteWorkspaceEnvelope.ReadFromFile(
            "fake/missing.nestsuite",
            fileExists: _ => false,
            readAllText: _ => { readCalls++; return "unused"; });

        Assert.Equal(0, readCalls);
        Assert.Equal(WorkspaceKindDetectionFailure.FileNotFound, result.Failure);
        Assert.Null(result.Envelope);
    }

    [Fact]
    public void ReadFromFile_InvalidJson_ReturnsInvalidFormat_WithOneReadCall()
    {
        var readCalls = 0;

        var result = NestSuiteWorkspaceEnvelope.ReadFromFile(
            "fake/broken.nestsuite",
            fileExists: _ => true,
            readAllText: _ => { readCalls++; return "not json"; });

        Assert.Equal(1, readCalls);
        Assert.Equal(WorkspaceKindDetectionFailure.InvalidFormat, result.Failure);
        Assert.Null(result.Envelope);
    }

    [Fact]
    public void ReadFromFile_ValidJsonButNotWrapperFormat_ReturnsInvalidFormat()
    {
        var result = NestSuiteWorkspaceEnvelope.ReadFromFile(
            "fake/not-wrapper.nestsuite",
            fileExists: _ => true,
            readAllText: _ => """{"foo":1}""");

        Assert.Equal(WorkspaceKindDetectionFailure.InvalidFormat, result.Failure);
    }

    [Fact]
    public void ReadFromFile_UnauthorizedAccessException_ReturnsAccessDenied()
    {
        var result = NestSuiteWorkspaceEnvelope.ReadFromFile(
            "fake/locked.nestsuite",
            fileExists: _ => true,
            readAllText: _ => throw new UnauthorizedAccessException());

        Assert.Equal(WorkspaceKindDetectionFailure.AccessDenied, result.Failure);
        Assert.Null(result.Envelope);
    }

    [Fact]
    public void ReadFromFile_SecurityException_ReturnsAccessDenied()
    {
        var result = NestSuiteWorkspaceEnvelope.ReadFromFile(
            "fake/restricted.nestsuite",
            fileExists: _ => true,
            readAllText: _ => throw new System.Security.SecurityException());

        Assert.Equal(WorkspaceKindDetectionFailure.AccessDenied, result.Failure);
    }

    [Fact]
    public void ReadFromFile_IOException_ReturnsIoError()
    {
        var result = NestSuiteWorkspaceEnvelope.ReadFromFile(
            "fake/network-drive.nestsuite",
            fileExists: _ => true,
            readAllText: _ => throw new IOException());

        Assert.Equal(WorkspaceKindDetectionFailure.IoError, result.Failure);
    }

    [Fact]
    public void ReadFromFile_UnexpectedException_ReturnsUnknown()
    {
        var result = NestSuiteWorkspaceEnvelope.ReadFromFile(
            "fake/weird.nestsuite",
            fileExists: _ => true,
            readAllText: _ => throw new InvalidOperationException());

        Assert.Equal(WorkspaceKindDetectionFailure.Unknown, result.Failure);
    }

    [Fact]
    public void ReadFromFile_DefaultDelegates_ReadsRealFileFromDisk()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        try
        {
            File.WriteAllText(path, NestSuiteWorkspaceEnvelope.Wrap("ChatNest", "0.4.1", "{}"));

            var result = NestSuiteWorkspaceEnvelope.ReadFromFile(path);

            Assert.Equal(WorkspaceKindDetectionFailure.None, result.Failure);
            Assert.Equal("ChatNest", result.Envelope!.WorkspaceKind);
        }
        finally { File.Delete(path); }
    }
}
