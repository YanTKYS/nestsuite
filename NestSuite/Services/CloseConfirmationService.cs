namespace NestSuite.Services;

/// <summary>
/// 終了時・タブクローズ時の未保存確認結果を、WPF UI に依存しない形で表す。
/// </summary>
public enum UnsavedChangeDecision
{
    NoActionNeeded,
    Save,
    Discard,
    Cancel
}

/// <summary>
/// 複数タブを閉じる際の対象情報。
/// </summary>
public sealed record CloseConfirmationTarget(string Id, bool CanClose, bool HasUnsavedChanges);

/// <summary>
/// 複数タブを閉じる際の判断結果。
/// </summary>
public sealed record CloseConfirmationResult(
    bool CanContinue,
    IReadOnlyList<string> SavedTabs,
    IReadOnlyList<string> DiscardedTabs,
    IReadOnlyList<string> FailedTabs)
{
    public bool Cancelled => !CanContinue;
}

/// <summary>
/// 終了時・タブクローズ時の未保存確認フローの純粋な判断部分。
/// ダイアログ表示と保存処理は呼び出し側から delegate として渡す。
/// </summary>
public static class CloseConfirmationService
{
    public static bool RequiresConfirmation(bool hasUnsavedChanges) => hasUnsavedChanges;

    public static bool CanCloseSingle(
        bool hasUnsavedChanges,
        Func<UnsavedChangeDecision> requestDecision,
        Func<bool>? save = null) =>
        EvaluateSingle(hasUnsavedChanges, requestDecision, save) != UnsavedChangeDecision.Cancel;

    public static UnsavedChangeDecision EvaluateSingle(
        bool hasUnsavedChanges,
        Func<UnsavedChangeDecision> requestDecision,
        Func<bool>? save = null)
    {
        if (!hasUnsavedChanges) return UnsavedChangeDecision.NoActionNeeded;

        var decision = requestDecision();
        return decision switch
        {
            UnsavedChangeDecision.Save => save?.Invoke() == true
                ? UnsavedChangeDecision.Save
                : UnsavedChangeDecision.Cancel,
            UnsavedChangeDecision.Discard => UnsavedChangeDecision.Discard,
            UnsavedChangeDecision.Cancel => UnsavedChangeDecision.Cancel,
            UnsavedChangeDecision.NoActionNeeded => UnsavedChangeDecision.NoActionNeeded,
            _ => UnsavedChangeDecision.Cancel
        };
    }

    public static CloseConfirmationResult EvaluateMany(
        IEnumerable<CloseConfirmationTarget> targets,
        Func<CloseConfirmationTarget, UnsavedChangeDecision> requestDecision,
        Func<CloseConfirmationTarget, bool>? save = null)
    {
        var saved = new List<string>();
        var discarded = new List<string>();
        var failed = new List<string>();

        foreach (var target in targets)
        {
            if (!target.CanClose) continue;

            var decision = EvaluateSingle(
                target.HasUnsavedChanges,
                () => requestDecision(target),
                save == null ? null : () => save(target));

            switch (decision)
            {
                case UnsavedChangeDecision.Save:
                    saved.Add(target.Id);
                    break;
                case UnsavedChangeDecision.Discard:
                case UnsavedChangeDecision.NoActionNeeded:
                    discarded.Add(target.Id);
                    break;
                case UnsavedChangeDecision.Cancel:
                    failed.Add(target.Id);
                    return new CloseConfirmationResult(false, saved, discarded, failed);
            }
        }

        return new CloseConfirmationResult(true, saved, discarded, failed);
    }
}
