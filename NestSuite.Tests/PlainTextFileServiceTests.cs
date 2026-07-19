using System.IO;
using System.Text;
using NestSuite;
using NestSuite.PlainText;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.19.0 SH-43: PlainTextFileService の文字コード判定・改行コード判定・保存動作を確認する。
/// テスト文字列だけでなく、明示したバイト列で BOM・不正バイト列を再現する。
/// </summary>
public class PlainTextFileServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public PlainTextFileServiceTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string TempPath(string name) => Path.Combine(_tempDir, name);

    [Fact]
    public void FileExtension_IsExpected() => Assert.Equal(".txt", PlainTextFileService.FileExtension);

    // ── 読込: 文字コード判定 ─────────────────────────────────────────────

    [Fact]
    public void DecodeBytes_Utf8NoBom_AsciiOnly_IsUtf8NoBom()
    {
        var bytes = Encoding.UTF8.GetBytes("hello");
        var result = PlainTextFileService.DecodeBytes(bytes);
        Assert.Equal("hello", result.Text);
        Assert.Equal(PlainTextEncodingKind.Utf8NoBom, result.EncodingKind);
    }

    [Fact]
    public void DecodeBytes_Utf8NoBom_JapaneseText_DecodesCorrectly()
    {
        var bytes = new UTF8Encoding(false).GetBytes("こんにちは");
        var result = PlainTextFileService.DecodeBytes(bytes);
        Assert.Equal("こんにちは", result.Text);
        Assert.Equal(PlainTextEncodingKind.Utf8NoBom, result.EncodingKind);
    }

    [Fact]
    public void DecodeBytes_Utf8Bom_StripsBomAndDetectsKind()
    {
        var bytes = new UTF8Encoding(true).GetBytes("テスト");
        var result = PlainTextFileService.DecodeBytes(bytes);
        Assert.Equal("テスト", result.Text);
        Assert.Equal(PlainTextEncodingKind.Utf8Bom, result.EncodingKind);
        Assert.DoesNotContain('﻿', result.Text);
    }

    [Fact]
    public void DecodeBytes_Utf16LE_StripsBomAndDetectsKind()
    {
        var bytes = new UnicodeEncoding(bigEndian: false, byteOrderMark: true).GetBytes("abc");
        var result = PlainTextFileService.DecodeBytes(bytes);
        Assert.Equal("abc", result.Text);
        Assert.Equal(PlainTextEncodingKind.Utf16LE, result.EncodingKind);
    }

    [Fact]
    public void DecodeBytes_Utf16BE_StripsBomAndDetectsKind()
    {
        var bytes = new UnicodeEncoding(bigEndian: true, byteOrderMark: true).GetBytes("abc");
        var result = PlainTextFileService.DecodeBytes(bytes);
        Assert.Equal("abc", result.Text);
        Assert.Equal(PlainTextEncodingKind.Utf16BE, result.EncodingKind);
    }

    [Fact]
    public void DecodeBytes_Utf32LE_StripsBomAndDetectsKind()
    {
        var bytes = new UTF32Encoding(bigEndian: false, byteOrderMark: true).GetBytes("abc");
        var result = PlainTextFileService.DecodeBytes(bytes);
        Assert.Equal("abc", result.Text);
        Assert.Equal(PlainTextEncodingKind.Utf32LE, result.EncodingKind);
    }

    [Fact]
    public void DecodeBytes_Utf32BE_StripsBomAndDetectsKind()
    {
        var bytes = new UTF32Encoding(bigEndian: true, byteOrderMark: true).GetBytes("abc");
        var result = PlainTextFileService.DecodeBytes(bytes);
        Assert.Equal("abc", result.Text);
        Assert.Equal(PlainTextEncodingKind.Utf32BE, result.EncodingKind);
    }

    [Fact]
    public void DecodeBytes_Utf32LEBom_IsNotMisdetectedAsUtf16LE()
    {
        // UTF-32 LE BOM (FF FE 00 00) は UTF-16 LE BOM (FF FE) を接頭辞として含むため、
        // 長い BOM から先に判定する必要がある。
        var bytes = new UTF32Encoding(bigEndian: false, byteOrderMark: true).GetBytes("x");
        var result = PlainTextFileService.DecodeBytes(bytes);
        Assert.Equal(PlainTextEncodingKind.Utf32LE, result.EncodingKind);
    }

    [Fact]
    public void DecodeBytes_EmptyFile_IsUtf8NoBomEmptyString()
    {
        var result = PlainTextFileService.DecodeBytes(Array.Empty<byte>());
        Assert.Equal(string.Empty, result.Text);
        Assert.Equal(PlainTextEncodingKind.Utf8NoBom, result.EncodingKind);
    }

    [Fact]
    public void DecodeBytes_InvalidUtf8Bytes_ThrowsUnsupportedEncoding_NotReplacementCharacter()
    {
        // 0x80 は単独では不正な UTF-8 継続バイト。置換文字への黙った変換ではなく例外にする。
        var invalidBytes = new byte[] { 0x41, 0x80, 0x42 };
        var ex = Assert.Throws<PlainTextUnsupportedEncodingException>(
            () => PlainTextFileService.DecodeBytes(invalidBytes));
        Assert.DoesNotContain('�', ex.Message);
    }

    [Fact]
    public void DecodeBytes_ShiftJisBytes_IsRejectedAsUnsupported()
    {
        // Shift_JIS の「あ」は UTF-8 として不正なバイト列になる。初期実装は Shift_JIS 非対応。
        var shiftJisLikeBytes = new byte[] { 0x82, 0xA0 };
        Assert.Throws<PlainTextUnsupportedEncodingException>(
            () => PlainTextFileService.DecodeBytes(shiftJisLikeBytes));
    }

    // ── 読込: ファイル経由 ──────────────────────────────────────────────

    [Fact]
    public void Load_MissingFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => PlainTextFileService.Load(TempPath("missing.txt")));
    }

    [Fact]
    public void Load_EmptyFile_ReturnsEmptyText()
    {
        var path = TempPath("empty.txt");
        File.WriteAllBytes(path, Array.Empty<byte>());
        var result = PlainTextFileService.Load(path);
        Assert.Equal(string.Empty, result.Text);
        Assert.Equal(PlainTextNewlineKind.None, result.NewlineKind);
    }

    [Fact]
    public void Load_DoesNotModifyOriginalFile()
    {
        var path = TempPath("readonly-check.txt");
        var original = new UTF8Encoding(false).GetBytes("original content");
        File.WriteAllBytes(path, original);
        var beforeWrite = File.GetLastWriteTimeUtc(path);

        PlainTextFileService.Load(path);

        Assert.Equal(beforeWrite, File.GetLastWriteTimeUtc(path));
        Assert.Equal(original, File.ReadAllBytes(path));
    }

    [Fact]
    public void Load_UnsupportedEncoding_DoesNotModifyOriginalFile()
    {
        var path = TempPath("bad-encoding.txt");
        var invalidBytes = new byte[] { 0x41, 0x80, 0x42 };
        File.WriteAllBytes(path, invalidBytes);

        Assert.Throws<PlainTextUnsupportedEncodingException>(() => PlainTextFileService.Load(path));

        Assert.Equal(invalidBytes, File.ReadAllBytes(path));
    }

    // ── 改行コード判定 ──────────────────────────────────────────────────

    [Theory]
    [InlineData("a\r\nb\r\nc", PlainTextNewlineKind.Crlf)]
    [InlineData("a\nb\nc", PlainTextNewlineKind.Lf)]
    [InlineData("a\rb\rc", PlainTextNewlineKind.Cr)]
    [InlineData("no newline here", PlainTextNewlineKind.None)]
    [InlineData("a\r\nb\nc", PlainTextNewlineKind.Mixed)]
    [InlineData("a\nb\rc", PlainTextNewlineKind.Mixed)]
    public void DetectNewline_ReturnsExpectedKind(string text, PlainTextNewlineKind expected) =>
        Assert.Equal(expected, PlainTextFileService.DetectNewline(text));

    // ── 保存: 文字コード round-trip ──────────────────────────────────────

    [Theory]
    [InlineData(PlainTextEncodingKind.Utf8NoBom)]
    [InlineData(PlainTextEncodingKind.Utf8Bom)]
    [InlineData(PlainTextEncodingKind.Utf16LE)]
    [InlineData(PlainTextEncodingKind.Utf16BE)]
    [InlineData(PlainTextEncodingKind.Utf32LE)]
    [InlineData(PlainTextEncodingKind.Utf32BE)]
    public void SaveThenLoad_RoundTrips_SameEncodingAndText(PlainTextEncodingKind kind)
    {
        var path = TempPath($"roundtrip-{kind}.txt");
        PlainTextFileService.Save(path, "本文テキスト123", kind, PlainTextNewlineKind.None);

        var result = PlainTextFileService.Load(path);

        Assert.Equal("本文テキスト123", result.Text);
        Assert.Equal(kind, result.EncodingKind);
    }

    [Fact]
    public void Save_NewFile_DefaultIsUtf8NoBom()
    {
        var path = TempPath("new.txt");
        PlainTextFileService.Save(path, "hello", PlainTextEncodingKind.Utf8NoBom, PlainTextNewlineKind.None);

        var bytes = File.ReadAllBytes(path);
        // UTF-8 BOM (EF BB BF) が先頭に無いこと
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        Assert.Equal("hello", PlainTextFileService.Load(path).Text);
    }

    [Fact]
    public void Save_Utf8Bom_WritesBomBytes()
    {
        var path = TempPath("bom.txt");
        PlainTextFileService.Save(path, "x", PlainTextEncodingKind.Utf8Bom, PlainTextNewlineKind.None);

        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
    }

    // ── 保存: 改行コード保持 ─────────────────────────────────────────────

    [Fact]
    public void Save_CrlfDetected_NormalizesEditedTextToCrlf()
    {
        // 読込時に検出した CRLF を、編集後（LF混入）でも書き込み時に揃える。
        var path = TempPath("crlf.txt");
        var editedText = "line1\nline2\r\nline3"; // LF と CRLF が混じった編集結果を想定
        PlainTextFileService.Save(path, editedText, PlainTextEncodingKind.Utf8NoBom, PlainTextNewlineKind.Crlf);

        var raw = File.ReadAllText(path, new UTF8Encoding(false));
        Assert.Equal("line1\r\nline2\r\nline3", raw);
    }

    [Fact]
    public void Save_LfDetected_DoesNotIntroduceCrlf()
    {
        // LF のみのファイルを編集・保存しても、全体が CRLF へ変わらないことを確認する
        // （意図せず全ファイルを CRLF へ変更しないことの回帰テスト）。
        var path = TempPath("lf.txt");
        var editedText = "line1\nline2\r\nline3"; // 混入した CRLF も LF へ揃う
        PlainTextFileService.Save(path, editedText, PlainTextEncodingKind.Utf8NoBom, PlainTextNewlineKind.Lf);

        var raw = File.ReadAllText(path, new UTF8Encoding(false));
        Assert.Equal("line1\nline2\nline3", raw);
        Assert.DoesNotContain("\r\n", raw);
    }

    [Fact]
    public void Save_MixedNewline_PassesThroughTextUnchanged()
    {
        var path = TempPath("mixed.txt");
        var text = "a\r\nb\nc";
        PlainTextFileService.Save(path, text, PlainTextEncodingKind.Utf8NoBom, PlainTextNewlineKind.Mixed);

        var raw = File.ReadAllText(path, new UTF8Encoding(false));
        Assert.Equal(text, raw);
    }

    [Fact]
    public void Save_NoneNewline_PassesThroughTextUnchanged()
    {
        var path = TempPath("none.txt");
        PlainTextFileService.Save(path, "single line", PlainTextEncodingKind.Utf8NoBom, PlainTextNewlineKind.None);

        Assert.Equal("single line", File.ReadAllText(path, new UTF8Encoding(false)));
    }

    // ── 保存: atomic write / .bak ────────────────────────────────────────

    [Fact]
    public void Save_ExistingFile_CreatesBakByDefault()
    {
        var path = TempPath("withbak.txt");
        PlainTextFileService.Save(path, "v1", PlainTextEncodingKind.Utf8NoBom, PlainTextNewlineKind.None);
        PlainTextFileService.Save(path, "v2", PlainTextEncodingKind.Utf8NoBom, PlainTextNewlineKind.None);

        Assert.True(File.Exists(path + ".bak"));
        Assert.Equal("v1", File.ReadAllText(path + ".bak", new UTF8Encoding(false)));
        Assert.Equal("v2", File.ReadAllText(path, new UTF8Encoding(false)));
    }

    [Fact]
    public void Save_CreateBackupFalse_DoesNotCreateOrUpdateBak()
    {
        // v2.16.6 TD-64 と同方針: 自動保存は正本のみ更新し既存の .bak を更新しない。
        var path = TempPath("nobak.txt");
        PlainTextFileService.Save(path, "v1", PlainTextEncodingKind.Utf8NoBom, PlainTextNewlineKind.None);
        PlainTextFileService.Save(path, "v2", PlainTextEncodingKind.Utf8NoBom, PlainTextNewlineKind.None, createBackup: true);
        PlainTextFileService.Save(path, "v3", PlainTextEncodingKind.Utf8NoBom, PlainTextNewlineKind.None, createBackup: false);

        Assert.Equal("v1", File.ReadAllText(path + ".bak", new UTF8Encoding(false)));
        Assert.Equal("v3", File.ReadAllText(path, new UTF8Encoding(false)));
    }

    [Fact]
    public void Save_UsesTempFileBeforeReplacing_NoLeftoverTmpFile()
    {
        var path = TempPath("atomic.txt");
        PlainTextFileService.Save(path, "content", PlainTextEncodingKind.Utf8NoBom, PlainTextNewlineKind.None);

        Assert.False(File.Exists(path + ".tmp"));
        Assert.True(File.Exists(path));
    }

    // ── LoadPrepared 契約 ────────────────────────────────────────────────

    [Fact]
    public void LoadPrepared_NullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => PlainTextFileService.LoadPrepared(null!));
    }

    [Fact]
    public void LoadPrepared_WrongWorkspaceKind_ThrowsArgumentException()
    {
        // .notenest 拡張子から probe した context（WorkspaceKind=NoteNest）を PlainText の
        // LoadPrepared へ渡すと、kind 不一致を検出して拒否すること。
        var path = TempPath("kindcheck.notenest");
        File.WriteAllText(path, "{}");
        var success = NestSuiteTabFactory.TryPrepareOpen(path, out var context, out _);
        Assert.True(success);
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, context.WorkspaceKind);

        Assert.Throws<ArgumentException>(() => PlainTextFileService.LoadPrepared(context));
    }

    [Fact]
    public void LoadPrepared_ValidTextContext_LoadsSameAsDirectLoad()
    {
        var path = TempPath("prepared.txt");
        PlainTextFileService.Save(path, "prepared content", PlainTextEncodingKind.Utf8NoBom, PlainTextNewlineKind.None);

        var success = NestSuiteTabFactory.TryPrepareOpen(path, out var context, out var failure);
        Assert.True(success);
        Assert.Equal(NestSuiteWorkspaceKind.PlainText, context.WorkspaceKind);
        Assert.Null(context.Preloaded);

        var result = PlainTextFileService.LoadPrepared(context);
        Assert.Equal("prepared content", result.Text);
    }
}
