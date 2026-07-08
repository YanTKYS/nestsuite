namespace NestSuite.Services;

/// <summary>
/// v2.16.15 TD-67 (review1-fable5.md R-7): 複数ファイルオープンで開けなかった 1 件分の情報。
/// 通知文言の組み立ては <see cref="MultipleOpenFailureMessageBuilder"/> に委譲する（UI 非依存）。
/// <see cref="SessionRestoreFailure"/>（TD-65）と同じ形（Path + 失敗理由）に揃える。
/// </summary>
public sealed record OpenFileFailure(string Path, WorkspaceKindDetectionFailure Failure);
