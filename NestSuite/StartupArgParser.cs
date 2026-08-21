namespace NestSuite;

/// <summary>
/// 起動引数解析。App_Startup から呼び出す。
///
/// <para><b>引数仕様</b></para>
/// <list type="bullet">
///   <item>引数なし         → NestSuite 起動（無題 NoteNest タブ）</item>
///   <item>ファイルパス     → NestSuite 起動し、拡張子に応じてタブを開く</item>
///   <item>--nestsuite      → NestSuite 起動（互換フラグ。既定と同じ動作）</item>
///   <item>--nestsuite + パス → NestSuite 起動し、拡張子に応じてタブを開く</item>
///   <item>その他のフラグ   → 無視して NestSuite 起動</item>
/// </list>
/// </summary>
public static class StartupArgParser
{
    /// <summary>
    /// --nestsuite フラグが含まれているか（大文字小文字を区別しない）。
    /// 既定が NestSuite のため、このフラグは互換として維持する。
    /// </summary>
    public static bool IsNestSuiteMode(string[] args) =>
        args.Contains("--nestsuite", StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 引数リストからファイルパス候補を返す。
    /// '-' で始まらない最初の引数をファイルパス候補として返す。見つからない場合は null。
    /// 拡張子・存在確認は呼び出し側（LoadInitialFile）が担当する。
    /// </summary>
    public static string? GetFilePath(string[] args) =>
        args.FirstOrDefault(a => !a.StartsWith("-"));
}
