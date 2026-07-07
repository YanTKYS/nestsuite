using System.IO;
using NestSuite.Services;

namespace NestSuite;

/// <summary>
/// Shell の各ファイルオープン入口で共通利用する、UI 非依存の判定結果。
/// 実際のタブ追加・選択・通知・読込は <see cref="NestSuiteShellWindow"/> 側に残す。
/// </summary>
public sealed record ShellFileOpenDecision(
    ShellFileOpenDecisionKind DecisionKind,
    string Path,
    NestSuiteWorkspaceKind? WorkspaceKind = null,
    NestSuiteDocumentTab? ExistingTab = null,
    WorkspaceKindDetectionFailure Failure = WorkspaceKindDetectionFailure.None);

public enum ShellFileOpenDecisionKind
{
    MissingFile,
    KindDetectionFailed,
    ActivateExistingTab,
    LoadWorkspace,
}

/// <summary>
/// v2.16.2 TD-62: Open ダイアログ・起動引数・pipe・最近ファイル・session 復元で共有できる
/// パス正規化、存在確認、WorkspaceKind 判定、既存タブ判定を小さく集約する。
/// UI 操作、タブ追加、通知、実ファイル読込は呼び出し側の責務として残す。
/// </summary>
public static class ShellFileOpenPlanner
{
    public static string NormalizePath(string path) => Path.GetFullPath(path);

    public static NestSuiteDocumentTab? FindOpenedFileTab(
        IEnumerable<NestSuiteDocumentTab> tabs,
        NestSuiteWorkspaceKind kind,
        string normalizedPath) =>
        tabs.FirstOrDefault(t =>
            t.WorkspaceKind == kind &&
            NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, normalizedPath));

    public static ShellFileOpenDecision Plan(
        string rawPath,
        IEnumerable<NestSuiteDocumentTab> openedTabs,
        Func<string, bool>? fileExists = null,
        Func<string, (bool Success, NestSuiteWorkspaceKind Kind, WorkspaceKindDetectionFailure Failure)>? detectKind = null)
    {
        var path = NormalizePath(rawPath);
        fileExists ??= File.Exists;
        detectKind ??= DetectKind;

        if (!fileExists(path))
        {
            return new ShellFileOpenDecision(ShellFileOpenDecisionKind.MissingFile, path,
                Failure: WorkspaceKindDetectionFailure.FileNotFound);
        }

        var detected = detectKind(path);
        if (!detected.Success)
        {
            return new ShellFileOpenDecision(ShellFileOpenDecisionKind.KindDetectionFailed, path,
                Failure: detected.Failure);
        }

        var existingTab = FindOpenedFileTab(openedTabs, detected.Kind, path);
        return existingTab != null
            ? new ShellFileOpenDecision(ShellFileOpenDecisionKind.ActivateExistingTab, path, detected.Kind, existingTab)
            : new ShellFileOpenDecision(ShellFileOpenDecisionKind.LoadWorkspace, path, detected.Kind);
    }

    private static (bool Success, NestSuiteWorkspaceKind Kind, WorkspaceKindDetectionFailure Failure) DetectKind(string path)
    {
        var success = NestSuiteTabFactory.TryGetKind(path, out var kind, out var failure);
        return (success, kind, failure);
    }
}
