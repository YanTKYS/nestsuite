namespace NoteNest;

/// <summary>
/// 起動引数解析。App_Startup から呼び出す。
///
/// <para><b>引数仕様（v1.19.3）</b></para>
/// <list type="bullet">
///   <item>引数なし         → NestSuite 起動（無題 NoteNest タブ）</item>
///   <item>ファイルパス     → NestSuite 起動し、拡張子に応じてタブを開く</item>
///   <item>--nestsuite      → NestSuite 起動（v1.6.1 互換。既定と同じ動作）</item>
///   <item>--nestsuite + パス → NestSuite 起動し、拡張子に応じてタブを開く</item>
///   <item>その他のフラグ   → 無視して NestSuite 起動（--classic-notenest は v1.19.3 で廃止）</item>
/// </list>
/// </summary>
public static class StartupArgParser
{
    /// <summary>
    /// --nestsuite フラグが含まれているか（大文字小文字を区別しない）。
    /// v1.11.0 以降は既定が NestSuite のため、このフラグは互換として維持する。
    /// </summary>
    public static bool IsNestSuiteMode(string[] args) =>
        args.Contains("--nestsuite", StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 引数リストからファイルパス候補を返す。
    /// '-' で始まらない最初の引数をファイルパス候補として返す。見つからない場合は null。
    /// 拡張子・存在確認は呼び出し側（LoadInitialFile）が担当する。
    /// </summary>
    public static string? GetFilePath(string[] args) =>
        args.FirstOrDefault(a => !a.StartsWith('-'));
}
