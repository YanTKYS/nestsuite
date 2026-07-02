using System.IO;
using System.Reflection;
using System.Text;

namespace NestSuite.Services;

/// <summary>
/// Error 専用の軽量ログサービス。Info / Warning ログは出力しない。
/// ログ書き込み失敗時もアプリ本体を落とさない。
/// ユーザー本文（ノート・カード・チャット本文など）は記録しない。
/// </summary>
internal static class ErrorLogService
{
    private static readonly string LogPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "NoteNest", "logs", "nestsuite-error.log");

    // v2.14.0 TD-57 (LT-12): サイズベース最小ローテーション。
    // 現行ログが 1MB 以上なら追記前に世代退避し、3 世代を超える最古世代を削除する。
    // 値の根拠と方針は docs/development/error-log-policy.md 参照。
    private const long MaxLogSizeBytes = 1024 * 1024;
    private const int MaxArchivedGenerations = 3;

    /// <summary>
    /// エラーをログファイルへ追記する。
    /// </summary>
    /// <param name="operation">操作名（例: "IdeaNestLoad", "ChatNestSave", "SessionRestore"）</param>
    /// <param name="ex">発生した例外</param>
    /// <param name="workspaceKind">ワークスペース種別（省略可）</param>
    /// <param name="filePath">対象ファイルパス（省略可）</param>
    /// <returns>ログ書き込みに成功した場合 true。失敗した場合 false（呼び元はダイアログ表示を調整できる）。</returns>
    public static bool Log(
        string operation,
        Exception ex,
        string? workspaceKind = null,
        string? filePath = null)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath)!;
            Directory.CreateDirectory(dir);
            ErrorLogRotation.RotateIfNeeded(LogPath, MaxLogSizeBytes, MaxArchivedGenerations);

            var sb = new StringBuilder();
            sb.AppendLine("========================================");
            sb.AppendLine($"Timestamp  : {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
            sb.AppendLine($"Version    : {GetAppVersion()}");
            sb.AppendLine($"Operation  : {operation}");
            if (workspaceKind != null)
                sb.AppendLine($"Workspace  : {workspaceKind}");
            if (filePath != null)
                sb.AppendLine($"File       : {filePath}");
            sb.AppendLine($"Exception  : {ex.GetType().FullName}");
            sb.AppendLine($"Message    : {ex.Message}");
            sb.AppendLine("StackTrace :");
            sb.AppendLine(ex.StackTrace ?? "(none)");
            if (ex.InnerException is { } inner)
            {
                sb.AppendLine($"Inner      : {inner.GetType().FullName}: {inner.Message}");
                sb.AppendLine(inner.StackTrace ?? "(none)");
            }
            sb.AppendLine();

            File.AppendAllText(LogPath, sb.ToString(), Encoding.UTF8);
            return true;
        }
        catch (Exception logEx)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[ErrorLogService] ログ書き込み失敗: {logEx.GetType().Name}: {logEx.Message}");
            return false;
        }
    }

    private static string GetAppVersion()
    {
        return typeof(ErrorLogService).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";
    }
}
