using System.IO;
using System.Text;
using NestSuite.Services;

namespace NestSuite.PlainText;

/// <summary>
/// v2.19.0 SH-43: `.txt` の読込・保存・文字コード/改行コード判定を担当する。
///
/// <para><b>位置づけ</b><br/>
/// `.txt` は <see cref="NestSuiteWorkspaceEnvelope"/>（`.nestsuite` wrapper）へ格納しない。
/// ファイル内容そのものが正本であり、NestSuite 独自のメタデータ（文字コード・改行コード等）は
/// 本文へ埋め込まない・session や recent files へも永続化しない（読込のたびに再判定する）。</para>
///
/// <para><b>文字コード判定方針</b><br/>
/// BOM で判定できる範囲（UTF-8 BOM あり／UTF-16 LE・BE／UTF-32 LE・BE）と、BOM なしの厳密 UTF-8 のみに対応する。
/// 曖昧な推測（文字頻度による Shift_JIS 判定等）は行わない。対応できない場合は
/// <see cref="PlainTextUnsupportedEncodingException"/> で読込を止め、元ファイルは変更しない。</para>
/// </summary>
public static class PlainTextFileService
{
    public const string FileExtension = ".txt";

    private static readonly byte[] Utf32LEBom = { 0xFF, 0xFE, 0x00, 0x00 };
    private static readonly byte[] Utf32BEBom = { 0x00, 0x00, 0xFE, 0xFF };
    private static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };
    private static readonly byte[] Utf16LEBom = { 0xFF, 0xFE };
    private static readonly byte[] Utf16BEBom = { 0xFE, 0xFF };

    // ── 読込 ────────────────────────────────────────────────────────────

    /// <exception cref="FileNotFoundException">ファイルが存在しない場合。</exception>
    /// <exception cref="UnauthorizedAccessException">アクセス拒否の場合。</exception>
    /// <exception cref="PlainTextUnsupportedEncodingException">文字コードを安全に判定できない場合。</exception>
    public static PlainTextLoadResult Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("テキストファイルが見つかりません。", path);
        var bytes = File.ReadAllBytes(path);
        var decoded = DecodeBytes(bytes);
        var newline = DetectNewline(decoded.Text);
        return new PlainTextLoadResult(decoded.Text, decoded.EncodingKind, newline);
    }

    /// <summary>
    /// v2.19.0 SH-43: 共通 Open 計画（<see cref="NestSuiteTabFactory.TryPrepareOpen"/>）が
    /// probe 済みの <see cref="WorkspaceFileOpenContext"/> から読み込む。`.txt` は wrapper を
    /// 持たないため <c>Preloaded</c> は常に null で、実体は <see cref="Load(string)"/> と同じ
    /// 1 回のファイル読込になる（他 FileService の LoadPrepared と契約の形を揃えるための薄いラッパー）。
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> が null の場合。</exception>
    /// <exception cref="ArgumentException">
    /// FilePath が空・Temp・path/拡張子/kind の組み合わせが呼び出し契約に反する場合。
    /// </exception>
    public static PlainTextLoadResult LoadPrepared(WorkspaceFileOpenContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(context.FilePath))
            throw new ArgumentException("FilePath が空です。", nameof(context));
        if (context.WorkspaceKind == NestSuiteWorkspaceKind.Temp)
            throw new ArgumentException("TempNest はファイル型 Workspace ではありません。", nameof(context));
        if (context.WorkspaceKind != NestSuiteWorkspaceKind.PlainText)
            throw new ArgumentException("WorkspaceKind が読込先と一致しません。", nameof(context));
        if (context.Preloaded != null)
            throw new ArgumentException("解析済み envelope は .txt には使えません。", nameof(context));
        if (!string.Equals(Path.GetExtension(context.FilePath), FileExtension, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"prepared 読込の拡張子は {FileExtension} である必要があります。", nameof(context));
        return Load(context.FilePath);
    }

    // ── 保存 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 指定した文字コード・改行コードで保存する。<see cref="AtomicFileWriter"/> による
    /// tmp 経由 atomic write を使う。手動保存では 1 世代 <c>.bak</c> を作成する
    /// （<paramref name="createBackup"/>=false の自動保存では作成しない。TD-64 と同方針）。
    /// </summary>
    public static void Save(
        string path,
        string text,
        PlainTextEncodingKind encodingKind,
        PlainTextNewlineKind newlineKind,
        bool createBackup = true)
    {
        var outputText = NormalizeForSave(text, newlineKind);
        var encoding = ResolveEncoding(encodingKind);
        if (createBackup)
            AtomicFileWriter.WriteAllTextWithBackup(path, outputText, encoding);
        else
            AtomicFileWriter.WriteAllText(path, outputText, encoding);
    }

    /// <summary>
    /// <paramref name="encoding"/> の <see cref="Encoding.GetPreamble"/> が、<see cref="File.WriteAllText(string,string,Encoding)"/>
    /// 経由で書き込み時に自動付与される BOM と一致するインスタンスを返す。
    /// </summary>
    public static Encoding ResolveEncoding(PlainTextEncodingKind kind) => kind switch
    {
        PlainTextEncodingKind.Utf8NoBom => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        PlainTextEncodingKind.Utf8Bom => new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
        PlainTextEncodingKind.Utf16LE => new UnicodeEncoding(bigEndian: false, byteOrderMark: true),
        PlainTextEncodingKind.Utf16BE => new UnicodeEncoding(bigEndian: true, byteOrderMark: true),
        PlainTextEncodingKind.Utf32LE => new UTF32Encoding(bigEndian: false, byteOrderMark: true),
        PlainTextEncodingKind.Utf32BE => new UTF32Encoding(bigEndian: true, byteOrderMark: true),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>
    /// 改行コードが単一種類（CRLF/LF/CR）と判定されたテキストのみ、読込時の改行コードへ揃えて保存する。
    /// 混在（Mixed）・改行なし（None）の場合は編集コントロールが返した内容をそのまま書き込む
    /// （意図せず全ファイルを特定の改行コードへ変更しないため）。
    /// </summary>
    public static string NormalizeForSave(string text, PlainTextNewlineKind newlineKind) =>
        newlineKind switch
        {
            PlainTextNewlineKind.Crlf or PlainTextNewlineKind.Lf or PlainTextNewlineKind.Cr =>
                ApplyNewline(ToLineFeedOnly(text), newlineKind),
            _ => text,
        };

    private static string ToLineFeedOnly(string text) => text.Replace("\r\n", "\n").Replace("\r", "\n");

    private static string ApplyNewline(string lineFeedOnlyText, PlainTextNewlineKind newlineKind) => newlineKind switch
    {
        PlainTextNewlineKind.Crlf => lineFeedOnlyText.Replace("\n", "\r\n"),
        PlainTextNewlineKind.Cr => lineFeedOnlyText.Replace("\n", "\r"),
        _ => lineFeedOnlyText,
    };

    // ── 文字コード判定 ──────────────────────────────────────────────────

    /// <exception cref="PlainTextUnsupportedEncodingException">文字コードを安全に判定できない場合。</exception>
    public static PlainTextDecodeResult DecodeBytes(byte[] bytes)
    {
        // 4 byte BOM は 2 byte BOM の接頭辞（UTF-32 LE の FF FE 00 00 は UTF-16 LE の FF FE を含む）
        // のため、長い BOM から先に判定する。
        if (StartsWith(bytes, Utf32LEBom))
            return DecodeWith(bytes, Utf32LEBom.Length,
                new UTF32Encoding(bigEndian: false, byteOrderMark: false, throwOnInvalidCharacters: true),
                PlainTextEncodingKind.Utf32LE);
        if (StartsWith(bytes, Utf32BEBom))
            return DecodeWith(bytes, Utf32BEBom.Length,
                new UTF32Encoding(bigEndian: true, byteOrderMark: false, throwOnInvalidCharacters: true),
                PlainTextEncodingKind.Utf32BE);
        if (StartsWith(bytes, Utf8Bom))
            return DecodeWith(bytes, Utf8Bom.Length,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
                PlainTextEncodingKind.Utf8Bom);
        if (StartsWith(bytes, Utf16LEBom))
            return DecodeWith(bytes, Utf16LEBom.Length,
                new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true),
                PlainTextEncodingKind.Utf16LE);
        if (StartsWith(bytes, Utf16BEBom))
            return DecodeWith(bytes, Utf16BEBom.Length,
                new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true),
                PlainTextEncodingKind.Utf16BE);

        // BOM なし: 厳密 UTF-8 のみを許容する。ASCII のみのファイルも UTF-8 BOM なしとして扱う
        // （Shift_JIS かの曖昧な推測はしない）。不正なバイト列は置換文字へ変換せず例外にする。
        try
        {
            var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            var text = strictUtf8.GetString(bytes);
            return new PlainTextDecodeResult(text, PlainTextEncodingKind.Utf8NoBom);
        }
        catch (Exception ex) when (ex is DecoderFallbackException or ArgumentException)
        {
            throw new PlainTextUnsupportedEncodingException(ex);
        }
    }

    private static PlainTextDecodeResult DecodeWith(byte[] bytes, int bomLength, Encoding encoding, PlainTextEncodingKind kind)
    {
        try
        {
            var text = encoding.GetString(bytes, bomLength, bytes.Length - bomLength);
            return new PlainTextDecodeResult(text, kind);
        }
        catch (Exception ex) when (ex is DecoderFallbackException or ArgumentException)
        {
            throw new PlainTextUnsupportedEncodingException(ex);
        }
    }

    private static bool StartsWith(byte[] bytes, byte[] prefix)
    {
        if (bytes.Length < prefix.Length) return false;
        for (var i = 0; i < prefix.Length; i++)
            if (bytes[i] != prefix[i]) return false;
        return true;
    }

    // ── 改行コード判定 ──────────────────────────────────────────────────

    /// <summary>
    /// 単一ファイル内の改行コード構成を判定する。CRLF/LF/CR のいずれか単一種類のみを検出した場合は
    /// そのまま返し、複数種類が混在する場合は <see cref="PlainTextNewlineKind.Mixed"/>、
    /// 改行を含まない場合は <see cref="PlainTextNewlineKind.None"/> を返す。
    /// </summary>
    public static PlainTextNewlineKind DetectNewline(string text)
    {
        var sawCrlf = false;
        var sawLf = false;
        var sawCr = false;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n') { sawCrlf = true; i++; }
                else sawCr = true;
            }
            else if (text[i] == '\n')
            {
                sawLf = true;
            }
        }

        var distinctCount = (sawCrlf ? 1 : 0) + (sawLf ? 1 : 0) + (sawCr ? 1 : 0);
        if (distinctCount == 0) return PlainTextNewlineKind.None;
        if (distinctCount > 1) return PlainTextNewlineKind.Mixed;
        return sawCrlf ? PlainTextNewlineKind.Crlf : sawLf ? PlainTextNewlineKind.Lf : PlainTextNewlineKind.Cr;
    }
}
