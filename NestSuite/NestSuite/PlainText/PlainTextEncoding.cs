namespace NestSuite.PlainText;

/// <summary>
/// v2.19.0 SH-43: PlainTextWorkspace が対応する文字コード。
/// BOM の有無で判定できる範囲のみを対象とし、コードページ推測は行わない。
/// </summary>
public enum PlainTextEncodingKind
{
    /// <summary>UTF-8（BOM なし）。BOM を持たないファイルの厳密デコードに成功した場合。新規ファイルの既定。</summary>
    Utf8NoBom,

    /// <summary>UTF-8（BOM あり）。</summary>
    Utf8Bom,

    /// <summary>UTF-16 リトルエンディアン（BOM あり）。</summary>
    Utf16LE,

    /// <summary>UTF-16 ビッグエンディアン（BOM あり）。</summary>
    Utf16BE,

    /// <summary>UTF-32 リトルエンディアン（BOM あり）。</summary>
    Utf32LE,

    /// <summary>UTF-32 ビッグエンディアン（BOM あり）。</summary>
    Utf32BE,
}

/// <summary>v2.19.0 SH-43: 読込時に検出した改行コードの構成。</summary>
public enum PlainTextNewlineKind
{
    /// <summary>改行を含まない（新規ファイル・単一行等）。</summary>
    None,

    /// <summary>CRLF（<c>\r\n</c>）のみ。</summary>
    Crlf,

    /// <summary>LF（<c>\n</c>）のみ。</summary>
    Lf,

    /// <summary>CR（<c>\r</c>）のみ。</summary>
    Cr,

    /// <summary>複数の改行コードが混在。保存時は編集コントロールが返した内容をそのまま書き込む。</summary>
    Mixed,
}

/// <summary>
/// v2.19.0 SH-43: `.txt` の文字コードを外部依存なしで安全に判定できなかったことを表す例外。
/// 「対応外として開かず、文字コードを判定できない旨を通知する」方針（フェーズA）の実装。
/// 元ファイルは一切変更しない（読込専用の判定失敗であり、この例外を投げる前に書込は行わない）。
/// </summary>
public sealed class PlainTextUnsupportedEncodingException : Exception
{
    public PlainTextUnsupportedEncodingException()
        : base("この .txt ファイルの文字コードを安全に判定できませんでした。対応している文字コードは、UTF-8（BOMあり/なし）・UTF-16（LE/BE）・UTF-32（LE/BE）です。")
    {
    }

    public PlainTextUnsupportedEncodingException(Exception innerException)
        : base("この .txt ファイルの文字コードを安全に判定できませんでした。対応している文字コードは、UTF-8（BOMあり/なし）・UTF-16（LE/BE）・UTF-32（LE/BE）です。", innerException)
    {
    }
}

/// <summary>v2.19.0 SH-43: 文字コード判定結果（デコード済み文字列＋判定した文字コード）。</summary>
public sealed record PlainTextDecodeResult(string Text, PlainTextEncodingKind EncodingKind);

/// <summary>v2.19.0 SH-43: `.txt` 読込結果（本文＋文字コード＋改行コード）。</summary>
public sealed record PlainTextLoadResult(string Text, PlainTextEncodingKind EncodingKind, PlainTextNewlineKind NewlineKind);
