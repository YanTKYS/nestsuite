using System.IO;
using System.Windows;
using NestSuite.Services;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    private readonly MigrationPackService _migrationPackService = new();

    private void MenuExportMigrationPack_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var skipped = _tabs.Count(t => t.WorkspaceKind != NestSuiteWorkspaceKind.Temp && (t.FilePath is null || t.IsModified));
            if (skipped > 0)
            {
                _dialogs.ShowInfo($"未保存または未保存変更のある Workspace {skipped} 件は移行パックに含まれません。\n必要な場合は保存してから再実行してください。", "デバイス移行パック");
            }

            var defaultName = $"NestSuite_Migration_{DateTime.Now:yyyyMMdd_HHmm}.zip";
            var zipPath = _dialogs.SelectMigrationPackExportPath(defaultName);
            if (zipPath == null) return;

            var sources = CollectMigrationWorkspaceSources();
            var result = _migrationPackService.Export(zipPath, sources);
            _dialogs.ShowInfo($"デバイス移行パックを作成しました。\nWorkspace: {result.WorkspaceCount}件\n環境ファイル: {result.EnvironmentCount}件\n保存先: {result.ZipPath}", "デバイス移行パック");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("MigrationPackExport", ex);
            _dialogs.ShowError($"デバイス移行パックを作成できませんでした。\n\n{ex.Message}", "デバイス移行パック");
        }
    }

    private void MenuImportMigrationPack_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var zipPath = _dialogs.SelectMigrationPackOpenPath();
            if (zipPath == null) return;
            var folder = _dialogs.SelectMigrationPackImportFolder();
            if (folder == null) return;

            var packName = Path.GetFileNameWithoutExtension(zipPath);
            var destination = Path.Combine(folder, $"{packName}_{DateTime.Now:yyyyMMdd_HHmmss}");
            var result = _migrationPackService.Import(zipPath, destination);
            _dialogs.ShowInfo($"デバイス移行パックを展開しました。\nWorkspace: {result.WorkspacePaths.Count}件\n環境ファイル: {result.EnvironmentPaths.Count}件\n展開先: {result.DestinationRoot}\n\n環境ファイルは展開のみ行いました。必要に応じて内容を確認してください。", "デバイス移行パック");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("MigrationPackImport", ex);
            _dialogs.ShowError($"デバイス移行パックをインポートできませんでした。\n\n{ex.Message}", "デバイス移行パック");
        }
    }

    private IReadOnlyList<MigrationPackWorkspaceSource> CollectMigrationWorkspaceSources() =>
        _tabs
            .Where(t => t.WorkspaceKind != NestSuiteWorkspaceKind.Temp && t.FilePath != null && !t.IsModified && File.Exists(t.FilePath))
            .Select(t => new MigrationPackWorkspaceSource(t.FilePath!, t.WorkspaceKind, t.DisplayName))
            .ToList();
}
