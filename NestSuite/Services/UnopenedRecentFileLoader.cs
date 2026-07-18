using System.IO;
using System.Threading;
using NestSuite.ChatNest;
using NestSuite.IdeaNest.Services;

namespace NestSuite.Services;

/// <summary>
/// SH-41 (AT-2 フェーズ1): 未オープンrecent filesを、Workspace ViewModelを生成せず
/// 保存モデルまでだけ読み込む。既存の <see cref="NestSuiteTabFactory.TryPrepareOpen"/> と
/// 各 <c>*FileService.LoadPrepared</c>（NoteNest/IdeaNest/ChatNestで既に確立済みの、
/// UI非依存・副作用なし・schema検証込みの読込経路）をそのまま再利用する。
/// 検索専用のReaderを新設しない（設計レビュー §6 の判断）。
/// </summary>
public static class UnopenedRecentFileLoader
{
    /// <summary>
    /// 各ファイルを順に読み込む（原則逐次。ネットワーク・ディスク負荷とcancellation管理の単純さを優先）。
    /// 1件の失敗は他のファイルの読込を止めない。ファイルの合間で <paramref name="cancellationToken"/> を
    /// 確認するが、1ファイルの同期読込そのものを途中で中断することはできない。
    /// </summary>
    public static IReadOnlyList<UnopenedFileLoadResult> Load(
        IEnumerable<string> filePaths,
        CancellationToken cancellationToken,
        Func<string, bool>? fileExists = null,
        Func<string, string>? readAllText = null)
    {
        var results = new List<UnopenedFileLoadResult>();
        foreach (var path in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(LoadOne(path, fileExists, readAllText));
        }
        return results;
    }

    private static UnopenedFileLoadResult LoadOne(
        string path, Func<string, bool>? fileExists, Func<string, string>? readAllText)
    {
        try
        {
            if (!NestSuiteTabFactory.TryPrepareOpen(path, out var context, out _, fileExists, readAllText))
                return new UnopenedFileLoadResult(path, null);

            object? model = context.WorkspaceKind switch
            {
                NestSuiteWorkspaceKind.NoteNest => new ProjectFileService().LoadPrepared(context),
                NestSuiteWorkspaceKind.IdeaNest => IdeaNestFileService.LoadPrepared(context),
                NestSuiteWorkspaceKind.ChatNest => ChatNestFileService.LoadPrepared(context),
                _ => null,
            };
            if (model == null) return new UnopenedFileLoadResult(path, null);

            var fileName = Path.GetFileName(path);
            return new UnopenedFileLoadResult(
                path, new UnopenedSearchDocument(context.WorkspaceKind, path, fileName, model));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // SH-41: 破損・未対応形式・アクセス不可等、理由を問わず該当ファイルだけスキップする。
            // recent files からの削除・個別通知は行わない（呼び出し側が失敗件数として集計する）。
            return new UnopenedFileLoadResult(path, null);
        }
    }
}
