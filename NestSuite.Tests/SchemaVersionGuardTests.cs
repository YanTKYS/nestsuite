using System.IO;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.14.4 FM-4: SchemaVersionGuard の数値比較・前方互換ガード動作を確認するテスト。
///
/// <para>版数リテラルは現行の実 schema version とは無関係な仮の値を使う。
/// 実 schema version をここに固定してしまうと、TD-58 の集約方針（schema version リテラルは
/// ApplicationVersionTests.cs にのみ存在させる）に反し、
/// ApplicationVersionTests.CurrentSchemaVersionLiteral_IsNotHardcoded_InOtherTestClasses が検出する。</para>
/// </summary>
public class SchemaVersionGuardTests
{
    // ── TryParse ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("2.3.7")]
    [InlineData("2.3")]
    public void TryParse_ParseableVersion_ReturnsTrue(string version)
    {
        Assert.True(SchemaVersionGuard.TryParse(version, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("1")]
    public void TryParse_UnparseableVersion_ReturnsFalse(string? version)
    {
        Assert.False(SchemaVersionGuard.TryParse(version, out _));
    }

    // ── IsNewer ──────────────────────────────────────────────────────────

    [Fact]
    public void IsNewer_NumericallyGreaterPatch_ReturnsTrue_NotStringCompare()
    {
        // 数値 vs 文字列比較の証明: "2.3.10" は文字列比較では "2.3.9" より
        // 小さく見えるが（'1'<'9'）、数値としては 10 > 9 で大きい。
        Assert.True(SchemaVersionGuard.IsNewer("2.3.10", "2.3.9"));
    }

    [Fact]
    public void IsNewer_NumericallySmallerPatch_ReturnsFalse()
    {
        Assert.False(SchemaVersionGuard.IsNewer("2.3.9", "2.3.10"));
    }

    [Fact]
    public void IsNewer_EqualVersions_ReturnsFalse()
    {
        Assert.False(SchemaVersionGuard.IsNewer("2.3.7", "2.3.7"));
    }

    [Fact]
    public void IsNewer_OlderVersion_ReturnsFalse()
    {
        Assert.False(SchemaVersionGuard.IsNewer("2.3.6", "2.3.7"));
    }

    [Fact]
    public void IsNewer_UnparseableFileVersion_Throws()
    {
        Assert.Throws<InvalidDataException>(() => SchemaVersionGuard.IsNewer("abc", "2.3.7"));
    }

    [Fact]
    public void IsNewer_UnparseableCurrentVersion_Throws()
    {
        Assert.Throws<InvalidDataException>(() => SchemaVersionGuard.IsNewer("2.3.7", "abc"));
    }

    // ── EnsureNotNewer ───────────────────────────────────────────────────

    [Fact]
    public void EnsureNotNewer_NewerFileVersion_ThrowsSchemaVersionTooNewException()
    {
        Assert.Throws<SchemaVersionTooNewException>(
            () => SchemaVersionGuard.EnsureNotNewer("2.3.10", "2.3.7", "NoteNest"));
    }

    [Fact]
    public void EnsureNotNewer_EqualFileVersion_DoesNotThrow()
    {
        SchemaVersionGuard.EnsureNotNewer("2.3.7", "2.3.7", "NoteNest");
    }

    [Fact]
    public void EnsureNotNewer_OlderFileVersion_DoesNotThrow()
    {
        SchemaVersionGuard.EnsureNotNewer("2.3.6", "2.3.7", "NoteNest");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EnsureNotNewer_NullOrEmptyFileVersion_DoesNotThrow(string? fileVersion)
    {
        SchemaVersionGuard.EnsureNotNewer(fileVersion, "2.3.7", "NoteNest");
    }

    [Fact]
    public void EnsureNotNewer_GarbageFileVersion_ThrowsInvalidDataException()
    {
        Assert.Throws<InvalidDataException>(
            () => SchemaVersionGuard.EnsureNotNewer("not-a-version", "2.3.7", "NoteNest"));
    }

    // ── EnsureEnvelopeConsistent ─────────────────────────────────────────

    [Fact]
    public void EnsureEnvelopeConsistent_PayloadNewerThanWrapper_ThrowsInvalidDataException()
    {
        Assert.Throws<InvalidDataException>(
            () => SchemaVersionGuard.EnsureEnvelopeConsistent("2.3.6", "2.3.10", "NoteNest"));
    }

    [Fact]
    public void EnsureEnvelopeConsistent_WrapperNewerThanPayload_DoesNotThrow()
    {
        // v2.14.1〜v2.14.3 のアプリが旧 payload（wrapper より古い schema version）を
        // 現行 payloadSchemaVersion で包んだ正当な既存ファイル形状。この方向は意図的に許容する。
        SchemaVersionGuard.EnsureEnvelopeConsistent("2.3.10", "2.3.6", "NoteNest");
    }

    [Fact]
    public void EnsureEnvelopeConsistent_EqualVersions_DoesNotThrow()
    {
        SchemaVersionGuard.EnsureEnvelopeConsistent("2.3.7", "2.3.7", "NoteNest");
    }

    [Theory]
    [InlineData(null, "2.3.7")]
    [InlineData("", "2.3.7")]
    [InlineData("2.3.7", null)]
    [InlineData("2.3.7", "")]
    public void EnsureEnvelopeConsistent_EitherSideEmptyOrNull_DoesNotThrow(
        string? payloadSchemaVersion, string? payloadVersion)
    {
        SchemaVersionGuard.EnsureEnvelopeConsistent(payloadSchemaVersion, payloadVersion, "NoteNest");
    }
}
