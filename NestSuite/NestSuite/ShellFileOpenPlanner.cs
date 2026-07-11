using System.IO;
using NestSuite.Services;

namespace NestSuite;

/// <summary>
/// Shell の各ファイルオープン入口で共通利用する、UI 非依存の判定結果。
/// 実際のタブ追加・選択・通知・読込は <see cref="NestSuiteShellWindow"/> 側に残す。
///
/// <para>v2.16.37 TD-59b-3 (nestsuite-double-read-design-review.md §9): <see cref="OpenContext"/> は
/// <see cref="ShellFileOpenDecisionKind.LoadWorkspace"/> のときだけ非 null になる（<c>.nestsuite</c> の
/// 読込元パス・wrapper 内容を open operation 全体で使い回すため）。<c>MissingFile</c> /
/// <c>KindDetectionFailed</c> は判定失敗のため、<c>ActivateExistingTab</c> は probe 結果を
/// これ以上保持する必要がないため、いずれも null のままにする。</para>
/// </summary>
public sealed record ShellFileOpenDecision(
    ShellFileOpenDecisionKind DecisionKind,
    string Path,
    NestSuiteWorkspaceKind? WorkspaceKind = null,
    NestSuiteDocumentTab? ExistingTab = null,
    WorkspaceKindDetectionFailure Failure = WorkspaceKindDetectionFailure.None,
    WorkspaceFileOpenContext? OpenContext = null);

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

    /// <summary>
    /// v2.16.37 TD-59b-3 / v2.16.38 TD-59b-4 (nestsuite-double-read-design-review.md §9): 種別判定は
    /// <see cref="NestSuiteTabFactory.TryPrepareOpen(string, out WorkspaceFileOpenContext, out WorkspaceKindDetectionFailure, Func{string, bool}?, Func{string, string}?)"/>
    /// で行う。<c>.nestsuite</c> の wrapper 読込は 1 回に集約され、<see cref="ShellFileOpenDecision.OpenContext"/>
    /// として本読込まで運ばれる。共通・種別別 Open、起動引数、最近ファイル、pipe、session 復元の
    /// すべての経路がこの prepared context 経路を使う（session 復元専用の暫定 <c>detectKind</c> seam は
    /// TD-59b-4 で撤去した）。
    ///
    /// <para><paramref name="prepareOpen"/> はテスト用の delegate 注入シーム（省略時は既定の <c>TryPrepareOpen</c> 呼び出し）。
    /// 契約違反（成功を返したのに <c>Context</c> が null、または <c>Context.FilePath</c> が対象 path と
    /// 一致しない）は <see cref="InvalidOperationException"/> で即座に検出し、path ベース読込への
    /// 暗黙フォールバックや任意の別 path context の混入を許さない。</para>
    /// </summary>
    public static ShellFileOpenDecision Plan(
        string rawPath,
        IEnumerable<NestSuiteDocumentTab> openedTabs,
        Func<string, bool>? fileExists = null,
        Func<string, (bool Success, WorkspaceFileOpenContext? Context, WorkspaceKindDetectionFailure Failure)>? prepareOpen = null)
    {
        var path = NormalizePath(rawPath);
        fileExists ??= File.Exists;

        // v2.16.2 TD-62 以来の既存確認を維持する（TryPrepareOpen 内でも .nestsuite の存在確認は行われるが、
        // ファイル内容を二重に読むわけではないため、TOCTOU・failure 分類を変えないことを優先し
        // ここでの事前確認を省略する最適化はしない）。
        if (!fileExists(path))
        {
            return new ShellFileOpenDecision(ShellFileOpenDecisionKind.MissingFile, path,
                Failure: WorkspaceKindDetectionFailure.FileNotFound);
        }

        prepareOpen ??= DefaultPrepareOpen;
        var prepared = prepareOpen(path);
        if (!prepared.Success)
        {
            return new ShellFileOpenDecision(ShellFileOpenDecisionKind.KindDetectionFailed, path,
                Failure: prepared.Failure);
        }

        // v2.16.38 TD-59b-4 §11: prepareOpen delegate の誤実装（テスト用シームの契約違反）を早期に検出する。
        // 既定の TryPrepareOpen 呼び出しはこれらを満たすため、正常動作には影響しない。
        if (prepared.Context is null)
        {
            throw new InvalidOperationException(
                "prepareOpen が成功を返しましたが、OpenContext がありません。");
        }
        if (!NestSuiteOpenFilePolicy.IsSameFile(path, prepared.Context.FilePath))
        {
            throw new InvalidOperationException(
                "prepareOpen が返した OpenContext の path が対象 path と一致しません。");
        }

        var context = prepared.Context;
        var existingContextTab = FindOpenedFileTab(openedTabs, context.WorkspaceKind, context.FilePath);
        return existingContextTab != null
            ? new ShellFileOpenDecision(
                ShellFileOpenDecisionKind.ActivateExistingTab, context.FilePath, context.WorkspaceKind, existingContextTab)
            : new ShellFileOpenDecision(
                ShellFileOpenDecisionKind.LoadWorkspace, context.FilePath, context.WorkspaceKind, OpenContext: context);
    }

    private static (bool Success, WorkspaceFileOpenContext? Context, WorkspaceKindDetectionFailure Failure) DefaultPrepareOpen(
        string path)
    {
        var success = NestSuiteTabFactory.TryPrepareOpen(path, out var context, out var failure);
        return (success, success ? context : null, failure);
    }
}
