using System.IO;
using System.Reflection;
using System.Text.Json;
using NestSuite;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.34 TD-59b-1 (nestsuite-double-read-design-review.md §8.1, §8.3, §8.4, §16, §17):
/// <see cref="WorkspaceFileOpenContext"/> / <see cref="PreloadedWorkspaceEnvelope"/> の生成境界、
/// <see cref="NestSuiteTabFactory.TryPrepareOpen"/> の読込回数・failure 分類、
/// <see cref="NestSuiteTabFactory.FromResolvedKind"/> の非読込タブ生成を確認する。
/// 今回は基盤 API のみの追加であり、Shell / FileService 経路の切替は対象外
/// （実利用経路の読込回数はまだ減らない）。
/// </summary>
public class WorkspaceFileOpenContextTests
{
    // ── 生成境界: 危険な public API になっていないことの確認 ──────────────────
    // ソース文字列の完全一致確認ではなく、コンパイル時の型情報（public コンストラクター・
    // public setter の有無）で確認する。

    [Fact]
    public void WorkspaceFileOpenContext_HasNoPublicConstructor()
    {
        var publicConstructors = typeof(WorkspaceFileOpenContext)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        Assert.Empty(publicConstructors);
    }

    [Fact]
    public void WorkspaceFileOpenContext_PropertiesHaveNoPublicSetter()
    {
        foreach (var property in typeof(WorkspaceFileOpenContext).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var setter = property.GetSetMethod(nonPublic: false);
            Assert.True(setter is null, $"{property.Name} に public setter がある");
        }
    }

    [Fact]
    public void PreloadedWorkspaceEnvelope_HasNoPublicConstructor()
    {
        var publicConstructors = typeof(PreloadedWorkspaceEnvelope)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        Assert.Empty(publicConstructors);
    }

    [Fact]
    public void PreloadedWorkspaceEnvelope_PropertiesHaveNoPublicSetter()
    {
        foreach (var property in typeof(PreloadedWorkspaceEnvelope).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var setter = property.GetSetMethod(nonPublic: false);
            Assert.True(setter is null, $"{property.Name} に public setter がある");
        }
    }

    // ── TryPrepareOpen: レガシー拡張子（読込 0 回） ────────────────────────

    [Theory]
    [InlineData("project.notenest", NestSuiteWorkspaceKind.NoteNest)]
    [InlineData("board.ideanest", NestSuiteWorkspaceKind.IdeaNest)]
    [InlineData("session.chatnest", NestSuiteWorkspaceKind.ChatNest)]
    public void TryPrepareOpen_LegacyExtension_Succeeds_WithZeroReadCalls_AndNullPreloaded(
        string fileName, NestSuiteWorkspaceKind expectedKind)
    {
        var existsCalls = 0;
        var readCalls = 0;

        var success = NestSuiteTabFactory.TryPrepareOpen(
            fileName, out var context, out var failure,
            fileExists: _ => { existsCalls++; return true; },
            readAllText: _ => { readCalls++; return "unused"; });

        Assert.True(success);
        Assert.Equal(WorkspaceKindDetectionFailure.None, failure);
        Assert.Equal(0, existsCalls);
        Assert.Equal(0, readCalls);
        Assert.Equal(expectedKind, context.WorkspaceKind);
        Assert.Null(context.Preloaded);
    }

    [Fact]
    public void TryPrepareOpen_UnsupportedExtension_Fails_WithZeroReadCalls()
    {
        var readCalls = 0;

        var success = NestSuiteTabFactory.TryPrepareOpen(
            @"C:\data\file.txt", out _, out var failure,
            fileExists: _ => true,
            readAllText: _ => { readCalls++; return "unused"; });

        Assert.False(success);
        Assert.Equal(WorkspaceKindDetectionFailure.UnsupportedExtension, failure);
        Assert.Equal(0, readCalls);
    }

    // ── TryPrepareOpen: .nestsuite（read delegate は常にちょうど 1 回） ────────

    [Fact]
    public void TryPrepareOpen_NestSuiteValid_Succeeds_WithExactlyOneReadCall_AndPreloadedEnvelope()
    {
        var wrapped = NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "1.4.2", """{"a":1}""");
        var readCalls = 0;

        var success = NestSuiteTabFactory.TryPrepareOpen(
            "fake/note.nestsuite", out var context, out var failure,
            fileExists: _ => true,
            readAllText: _ => { readCalls++; return wrapped; });

        Assert.True(success);
        Assert.Equal(1, readCalls);
        Assert.Equal(WorkspaceKindDetectionFailure.None, failure);
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, context.WorkspaceKind);
        Assert.NotNull(context.Preloaded);
        using var payload = JsonDocument.Parse(context.Preloaded!.Envelope.PayloadJson);
        Assert.Equal(1, payload.RootElement.GetProperty("a").GetInt32());
    }

    [Fact]
    public void TryPrepareOpen_NestSuiteInvalidWrapper_Fails_WithExactlyOneReadCall()
    {
        var readCalls = 0;

        var success = NestSuiteTabFactory.TryPrepareOpen(
            "fake/broken.nestsuite", out _, out var failure,
            fileExists: _ => true,
            readAllText: _ => { readCalls++; return "not json"; });

        Assert.False(success);
        Assert.Equal(1, readCalls);
        Assert.Equal(WorkspaceKindDetectionFailure.InvalidFormat, failure);
    }

    [Fact]
    public void TryPrepareOpen_NestSuiteUnknownWorkspaceKind_Fails_WithExactlyOneReadCall()
    {
        var wrapped = NestSuiteWorkspaceEnvelope.Wrap("FutureNest", "1.0", "{}");
        var readCalls = 0;

        var success = NestSuiteTabFactory.TryPrepareOpen(
            "fake/future.nestsuite", out _, out var failure,
            fileExists: _ => true,
            readAllText: _ => { readCalls++; return wrapped; });

        Assert.False(success);
        Assert.Equal(1, readCalls);
        Assert.Equal(WorkspaceKindDetectionFailure.UnknownWorkspaceKind, failure);
    }

    [Fact]
    public void TryPrepareOpen_NestSuiteSchemaVersionTooNew_Fails_WithExactlyOneReadCall()
    {
        var wrapped = NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "9.9.9", "{}");
        var readCalls = 0;

        var success = NestSuiteTabFactory.TryPrepareOpen(
            "fake/toonew.nestsuite", out _, out var failure,
            fileExists: _ => true,
            readAllText: _ => { readCalls++; return wrapped; });

        Assert.False(success);
        Assert.Equal(1, readCalls);
        Assert.Equal(WorkspaceKindDetectionFailure.SchemaVersionTooNew, failure);
    }

    [Fact]
    public void TryPrepareOpen_NestSuiteMissingFile_Fails_WithZeroReadCalls_AndFileNotFound()
    {
        var readCalls = 0;

        var success = NestSuiteTabFactory.TryPrepareOpen(
            "fake/missing.nestsuite", out _, out var failure,
            fileExists: _ => false,
            readAllText: _ => { readCalls++; return "unused"; });

        Assert.False(success);
        Assert.Equal(0, readCalls);
        Assert.Equal(WorkspaceKindDetectionFailure.FileNotFound, failure);
    }

    // ── context 生成保証: path / kind / envelope の対応 ──────────────────────

    [Fact]
    public void TryPrepareOpen_NestSuite_ContextFilePath_IsFullPath_AndMatchesPreloadedSourcePath()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        try
        {
            File.WriteAllText(path, NestSuiteWorkspaceEnvelope.Wrap("IdeaNest", "1.1.4", "{}"));

            var success = NestSuiteTabFactory.TryPrepareOpen(path, out var context, out _);

            Assert.True(success);
            Assert.Equal(Path.GetFullPath(path), context.FilePath);
            Assert.NotNull(context.Preloaded);
            Assert.True(NestSuiteOpenFilePolicy.IsSameFile(context.FilePath, context.Preloaded!.SourcePath));
            Assert.Equal(NestSuiteWorkspaceKind.IdeaNest, context.WorkspaceKind);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void TryPrepareOpen_RelativePath_NormalizesToFullPath()
    {
        var success = NestSuiteTabFactory.TryPrepareOpen(
            "relative.notenest", out var context, out _);

        Assert.True(success);
        Assert.True(Path.IsPathRooted(context.FilePath));
    }

    [Fact]
    public void TryPrepareOpen_NestSuite_DefaultDelegates_ReadsRealFileFromDisk()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        try
        {
            File.WriteAllText(path, NestSuiteWorkspaceEnvelope.Wrap("ChatNest", "0.4.1", "{}"));

            var success = NestSuiteTabFactory.TryPrepareOpen(path, out var context, out _);

            Assert.True(success);
            Assert.Equal(NestSuiteWorkspaceKind.ChatNest, context.WorkspaceKind);
        }
        finally { File.Delete(path); }
    }

    // ── TryGetKind 互換確認: 新 API への委譲後も従来と同じ結果 ────────────────

    [Theory]
    [InlineData("a.notenest", NestSuiteWorkspaceKind.NoteNest)]
    [InlineData("b.ideanest", NestSuiteWorkspaceKind.IdeaNest)]
    [InlineData("c.chatnest", NestSuiteWorkspaceKind.ChatNest)]
    public void TryGetKind_LegacyExtension_StillResolvesExpectedKind(string fileName, NestSuiteWorkspaceKind expected)
    {
        Assert.True(NestSuiteTabFactory.TryGetKind(fileName, out var kind));
        Assert.Equal(expected, kind);
    }

    [Fact]
    public void TryGetKind_NestSuiteFile_StillResolvesKindFromEnvelope_AfterDelegation()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        try
        {
            File.WriteAllText(path, NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "1.4.2", "{}"));

            Assert.True(NestSuiteTabFactory.TryGetKind(path, out var kind, out var failure));
            Assert.Equal(NestSuiteWorkspaceKind.NoteNest, kind);
            Assert.Equal(WorkspaceKindDetectionFailure.None, failure);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void TryGetKind_NestSuiteFile_SchemaVersionTooNew_StillDetected_AfterDelegation()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        try
        {
            File.WriteAllText(path, NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "9.9.9", "{}"));

            var result = NestSuiteTabFactory.TryGetKind(path, out _, out var failure);

            Assert.False(result);
            Assert.Equal(WorkspaceKindDetectionFailure.SchemaVersionTooNew, failure);
        }
        finally { File.Delete(path); }
    }

    // ── FromResolvedKind: 非読込タブ生成 ─────────────────────────────────

    [Theory]
    [InlineData(NestSuiteWorkspaceKind.NoteNest)]
    [InlineData(NestSuiteWorkspaceKind.IdeaNest)]
    [InlineData(NestSuiteWorkspaceKind.ChatNest)]
    public void FromResolvedKind_NonExistentPath_StillCreatesTab_WithoutFileIO(NestSuiteWorkspaceKind kind)
    {
        var path = @"C:\definitely\does\not\exist\file" + NestSuiteTabFactory.GetExtension(kind);

        var tab = NestSuiteTabFactory.FromResolvedKind(path, kind);

        Assert.Equal(kind, tab.WorkspaceKind);
        Assert.Equal(path, tab.FilePath);
        Assert.Equal(Path.GetFileName(path), tab.DisplayName);
        Assert.False(tab.IsModified);
        Assert.NotNull(tab.Id);
    }

    [Fact]
    public void FromResolvedKind_Temp_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NestSuiteTabFactory.FromResolvedKind(@"C:\any\path", NestSuiteWorkspaceKind.Temp));
    }

    [Fact]
    public void FromResolvedKind_ProducesSameTabShape_AsFromFilePath_ForSamePath()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".notenest");

        var viaFromFilePath = NestSuiteTabFactory.FromFilePath(path);
        var viaFromResolvedKind = NestSuiteTabFactory.FromResolvedKind(path, NestSuiteWorkspaceKind.NoteNest);

        Assert.Equal(viaFromFilePath.WorkspaceKind, viaFromResolvedKind.WorkspaceKind);
        Assert.Equal(viaFromFilePath.FilePath, viaFromResolvedKind.FilePath);
        Assert.Equal(viaFromFilePath.DisplayName, viaFromResolvedKind.DisplayName);
        Assert.Equal(viaFromFilePath.IsModified, viaFromResolvedKind.IsModified);
    }
}
