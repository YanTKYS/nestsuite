namespace NestSuite;

/// <summary>
/// SH-37: 利用者が明示的に開く「現在の状態」ダイアログへ渡す読み取り専用サマリー。
/// Shell が既に保持している状態から生成する表示専用のスナップショットであり、保存対象にはしない。
/// <see cref="DraftRecoveryCandidateCount"/> のみ、下書きファイルの列挙に失敗した場合 <c>null</c>
/// （「取得できません」表示）を許容する。それ以外はメモリ上の状態から取得するため常に値を持つ。
/// </summary>
public sealed record ShellStateSummary(
    int OpenTabCount,
    int UnsavedTabCount,
    int PendingRestoreCount,
    int? DraftRecoveryCandidateCount,
    int NonEmptyTempNestSlotCount);
