namespace NoteNest;

/// <summary>
/// v1.6.1 起動引数解析。App_Startup から呼び出す。
///
/// <para><b>引数仕様（v1.6.1）</b></para>
/// <list type="bullet">
///   <item>引数なし     → NoteNest 単体版通常起動（StartDialog 表示）</item>
///   <item>ファイルパス → NoteNest 単体版でそのファイルを開く</item>
///   <item>--nestsuite  → 開発・検証用 NestSuiteShellWindow を起動</item>
///   <item>--nestsuite + ファイルパス同時指定 → v1.6.1 非対応。NestSuite モードで起動（ファイルは無視）</item>
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
}
