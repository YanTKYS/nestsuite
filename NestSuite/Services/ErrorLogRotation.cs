using System.IO;

namespace NestSuite.Services;

/// <summary>
/// v2.14.0 TD-57 (LT-12): エラーログのサイズベース最小ローテーション。
/// <see cref="ErrorLogService"/> が追記直前に呼び出す。
/// ローテーション失敗はアプリ本体・ログ追記を止めない（例外を外へ投げない）。
/// 方針は docs/development/error-log-policy.md 参照。
/// </summary>
public static class ErrorLogRotation
{
    /// <summary>
    /// 現行ログが maxSizeBytes 以上ならローテーションする。
    /// 現行 → .1、.1 → .2 … と世代を1つずつ後ろへずらし、maxGenerations を超える最古世代は削除する。
    /// 現行ログファイル名・保存先は変更しない（追記側が同じパスへ書き続ける）。
    /// </summary>
    public static void RotateIfNeeded(string logPath, long maxSizeBytes, int maxGenerations)
    {
        try
        {
            if (maxGenerations < 1) return;
            var current = new FileInfo(logPath);
            if (!current.Exists || current.Length < maxSizeBytes) return;

            var oldest = ArchivePath(logPath, maxGenerations);
            if (File.Exists(oldest)) File.Delete(oldest);
            for (var generation = maxGenerations - 1; generation >= 1; generation--)
            {
                var source = ArchivePath(logPath, generation);
                if (File.Exists(source)) File.Move(source, ArchivePath(logPath, generation + 1));
            }
            File.Move(logPath, ArchivePath(logPath, 1));
        }
        catch (Exception ex)
        {
            // 診断用サービス自身の失敗で本体を止めない。ErrorLogService への再帰記録もしない。
            System.Diagnostics.Debug.WriteLine(
                $"[ErrorLogRotation] ローテーション失敗: {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>世代ファイル名を返す。例: nestsuite-error.log の第2世代 → nestsuite-error.2.log</summary>
    public static string ArchivePath(string logPath, int generation)
    {
        var dir = Path.GetDirectoryName(logPath) ?? "";
        var name = Path.GetFileNameWithoutExtension(logPath);
        var ext = Path.GetExtension(logPath);
        return Path.Combine(dir, $"{name}.{generation}{ext}");
    }
}
