namespace NestSuite.Services;

/// <summary>
/// v2.14.12 SH-33: 自動保存対象タブの判定を表す、UI 非依存の純粋ロジック。
/// <see cref="NestSuiteOpenFilePolicy"/> と同じ方針で、WPF ウィンドウを生成せずに
/// 自動保存タイマーの判定条件だけを単体テストできるようにする。
///
/// <para>対象: 保存先パスを持ち、未保存の変更があり、TempNest ではない
/// NoteNest / IdeaNest / ChatNest タブ。新規未保存タブ（FilePath なし）と
/// TempNest は対象外（新規に保存場所を作らない・TempNest は専用の保存機構を持つため）。</para>
/// </summary>
public static class AutoSaveCandidatePolicy
{
    public static bool IsCandidate(NestSuiteWorkspaceKind kind, string? filePath, bool isModified) =>
        kind is NestSuiteWorkspaceKind.NoteNest or NestSuiteWorkspaceKind.IdeaNest or NestSuiteWorkspaceKind.ChatNest
        && filePath != null
        && isModified;
}
