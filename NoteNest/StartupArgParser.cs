namespace NoteNest;

/// <summary>
/// v1.6.3 起動引数解析。App_Startup から呼び出す。
///
/// <para><b>引数仕様（v1.6.3）</b></para>
/// <list type="bullet">
///   <item>引数なし     → NoteNest 単体版通常起動（StartDialog 表示）</item>
///   <item>ファイルパス → NoteNest 単体版でそのファイルを開く</item>
///   <item>--nestsuite  → 開発・検証用 NestSuiteShellWindow を起動</item>
///   <item>--nestsuite + .notenest パス → v1.6.3 以降対応。NestSuite モードでそのファイルを開く</item>
/// </list>
/// </summary>
public static class StartupArgParser
{
    /// <summary>
    /// --nestsuite フラグが含まれているか（大文字小文字を区別しない）。
    /// true のとき、App_Startup は NestSuiteShellWindow を起動する。
    /// </summary>
    public static bool IsNestSuiteMode(string[] args) =>
        args.Contains("--nestsuite", StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 引数リストからファイルパス候補を返す。
    /// '-' で始まらない最初の引数をファイルパス候補として返す。見つからない場合は null。
    /// 拡張子・存在確認は呼び出し側（LoadInitialFile / OpenStartupFile）が担当する。
    /// </summary>
    public static string? GetFilePath(string[] args) =>
        args.FirstOrDefault(a => !a.StartsWith('-'));
}
