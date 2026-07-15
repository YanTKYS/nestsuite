using System.Globalization;
using System.IO;

namespace NestSuite.Services;

/// <summary>
/// M19: 読込失敗した設定・履歴ファイルの退避結果。
/// </summary>
/// <param name="Succeeded">退避（移動）に成功した場合 true。</param>
/// <param name="OriginalPath">退避対象だった元のファイルパス。</param>
/// <param name="BackupPath">退避先パス。失敗時は null。</param>
/// <param name="Exception">退避に失敗した場合の例外。成功時は null。</param>
public sealed record CorruptFileRecoveryResult(
    bool Succeeded,
    string OriginalPath,
    string? BackupPath,
    Exception? Exception);

/// <summary>
/// M19: 読込に失敗した設定・履歴ファイルを同一ディレクトリ内で退避するだけの小さなヘルパー。
/// 退避先パス生成・衝突回避・ファイル移動・結果返却のみを担当する。
/// 設定サービス統合・session/draft 復旧との共通化・汎用バックアップ基盤・リトライ管理・
/// UI通知・ErrorLog 出力・JSON 読込は行わない（呼び出し側の責務）。
/// </summary>
public static class FileRecoveryHelper
{
    /// <summary>
    /// <paramref name="path"/> を "&lt;元ファイル名&gt;.corrupt-yyyyMMdd-HHmmss" へリネームする。
    /// 同名の退避ファイルが既にある場合は "-1", "-2", ... を付けて衝突を避ける。
    /// 元ファイルが存在しない場合は呼び出し側の責務（本メソッドは常に移動を試みる）。
    /// </summary>
    public static CorruptFileRecoveryResult QuarantineCorruptFile(string path, DateTime? now = null)
    {
        try
        {
            var stamp = (now ?? DateTime.Now).ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var baseTarget = path + ".corrupt-" + stamp;
            var target = baseTarget;
            for (var i = 1; File.Exists(target); i++)
                target = baseTarget + "-" + i.ToString(CultureInfo.InvariantCulture);

            File.Move(path, target);
            return new CorruptFileRecoveryResult(true, path, target, null);
        }
        catch (Exception ex)
        {
            return new CorruptFileRecoveryResult(false, path, null, ex);
        }
    }
}
