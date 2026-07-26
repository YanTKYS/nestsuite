namespace NestSuite.Services;

/// <summary>
/// SH-44: 横断検索パネルを閉じた後にフォーカスを復帰する際、保存しておいた要素が
/// 復帰先として使えるかどうかを表す状態。WPF の実 UIElement に依存しないプレーンな値型にし、
/// 実際の Focus() 成功可否を UI テストで固定せずに判定ロジックだけを単体テストできるようにする。
/// </summary>
public readonly record struct FocusRestoreCandidateState(
    bool Exists,
    bool IsVisible,
    bool IsEnabled,
    bool IsFocusable)
{
    /// <summary>保存されていた要素が null（保存なし、または破棄済み参照）だった場合。</summary>
    public static readonly FocusRestoreCandidateState Missing = new(false, false, false, false);
}

/// <summary>SH-44: フォーカス復帰先の判定ロジック（純粋関数）。</summary>
public static class CrossSearchFocusRestoreLogic
{
    /// <summary>
    /// 保存しておいた要素を復帰先としてそのまま使ってよいかどうかを判定する。
    /// 存在し、表示中で、有効で、フォーカス可能な場合のみ true。
    /// いずれか1つでも欠けていれば、呼び出し側は次のフォールバック候補へ進む。
    /// </summary>
    public static bool ShouldUseSavedFocus(FocusRestoreCandidateState state) =>
        state.Exists && state.IsVisible && state.IsEnabled && state.IsFocusable;
}
