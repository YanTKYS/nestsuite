namespace NestSuite.Services;

/// <summary>
/// v2.16.11 SH-1: 起動時・外部オープン時（起動引数・ファイル関連付け・pipe 経由の 2 重起動転送）に
/// ファイルを開けなかった場合、「NestSuite 自体は使える」ことを一言添えるための、UI 非依存の文言 helper。
///
/// 個々の失敗理由別メッセージ（見つからない・権限がない・形式が不正 等）は引き続き
/// <see cref="FileErrorMessages"/> が担う。ここではその内容を書き換えず、起動・外部オープン文脈に
/// 限って短い次行動の一文を付け足すことだけを行う。Open ダイアログ・最近使ったファイルなど、
/// Shell が既に画面に表示され利用者が操作している最中の失敗には使わない
/// （その場合「NestSuite は起動しています」は自明で冗長なため）。
/// </summary>
public static class ShellOpenFailureGuidanceProvider
{
    /// <summary>
    /// 起動引数・ファイル関連付け・pipe 経由のファイルオープンが失敗しても、
    /// NestSuite 自体は起動・利用できることを伝える短い一文。
    /// </summary>
    public const string StillUsableHint =
        "NestSuite は起動しています。別のファイルを開くか、新しいタブで作業を開始できます。";

    /// <summary>
    /// 理由別メッセージ（<see cref="FileErrorMessages"/> の戻り値等）の末尾に、
    /// <see cref="StillUsableHint"/> を 1 行空けて追記する。
    /// </summary>
    public static string AppendStillUsableHint(string baseMessage) =>
        baseMessage + "\n\n" + StillUsableHint;
}
