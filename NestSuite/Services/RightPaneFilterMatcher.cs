namespace NestSuite.Services;

/// <summary>
/// NoteNest右ペイン（マーカー一覧・互換タスク一覧）共通の文字列絞り込み判定。
/// 通常の <see cref="string.IndexOf(string, StringComparison)"/> による大文字小文字を
/// 区別しない部分一致のみを行い、正規表現は使用しない。
/// </summary>
public static class RightPaneFilterMatcher
{
    /// <summary>filterText が空の場合は常に一致（絞り込みなし）として扱う。</summary>
    public static bool Matches(string? candidate, string? filterText)
    {
        if (string.IsNullOrEmpty(filterText)) return true;
        if (string.IsNullOrEmpty(candidate)) return false;
        return candidate.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>候補のいずれか1つでも一致すれば true。</summary>
    public static bool MatchesAny(string? filterText, params string?[] candidates)
    {
        if (string.IsNullOrEmpty(filterText)) return true;
        foreach (var candidate in candidates)
            if (Matches(candidate, filterText)) return true;
        return false;
    }
}
