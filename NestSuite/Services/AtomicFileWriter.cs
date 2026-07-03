using System.IO;
using System.Text;

namespace NestSuite.Services;

/// <summary>
/// 保存先ディレクトリ作成・tmp 経由 atomic write・finally tmp cleanup を共通化する。
/// NoteNest / IdeaNest / ChatNest の保存処理で共有する。
/// </summary>
public static class AtomicFileWriter
{
    /// <summary>
    /// path へ content を atomic に書き込む。
    /// 保存先ディレクトリがなければ作成し、tmp ファイル経由で置換または移動する。
    /// 保存成功・失敗を問わず finally で tmp を削除する。cleanup 失敗は ErrorLog に記録し
    /// 本来の保存例外を隠さない。
    /// </summary>
    /// <param name="path">保存先ファイルパス。フルパス推奨。</param>
    /// <param name="content">書き込むテキスト。</param>
    /// <param name="encoding">文字エンコーディング。各サービスの既存方針を維持すること。</param>
    /// <param name="backupPath">
    /// 既存ファイル置換時のバックアップパス。null の場合はバックアップを作成しない。
    /// </param>
    public static void WriteAllText(
        string path,
        string content,
        Encoding encoding,
        string? backupPath = null)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var tempPath = path + ".tmp";
        try
        {
            File.WriteAllText(tempPath, content, encoding);
            if (File.Exists(path))
                File.Replace(tempPath, path, backupPath);
            else
                File.Move(tempPath, path);
        }
        finally
        {
            TryDeleteTemp(tempPath);
        }
    }

    /// <summary>
    /// v2.14.8: 保存先パス + ".bak" を単一世代バックアップとして残す atomic write（FM-5 の共通方針）。
    /// <see cref="WriteAllText"/> の backupPath 指定を 3 FileService で重複させないための convenience overload。
    /// </summary>
    public static void WriteAllTextWithBackup(string path, string content, Encoding encoding) =>
        WriteAllText(path, content, encoding, path + ".bak");

    /// <summary>
    /// v2.14.8: ランダムな一時ファイル名を使う atomic write（バックアップなし・既定 UTF-8）。
    /// 固定 tmp 名（path + ".tmp"）と異なり同一ファイルへの並行書き込みで tmp 名が衝突しない。
    /// RecentFiles / SessionState などの補助ファイル保存で共有する
    /// （従来 3 サービスに重複していた実装を移設。挙動は従来と同一）。
    /// </summary>
    public static void WriteAllTextWithRandomTemp(string path, string content)
    {
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(
            directory, $"{Path.GetFileName(path)}.{Path.GetRandomFileName()}.tmp");

        try
        {
            File.WriteAllText(temporaryPath, content);
            if (File.Exists(path))
                File.Replace(temporaryPath, path, destinationBackupFileName: null);
            else
                File.Move(temporaryPath, path);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch
            {
                // 一時ファイルの掃除失敗は致命ではない。正本（path 側）が優先される。
                // （移設元 3 サービスの従来挙動どおり、ここではログも出さない）
            }
        }
    }

    private static void TryDeleteTemp(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("AtomicFileWriterCleanup", ex);
        }
    }
}
