using NestSuite.ViewModels;

namespace NestSuite.Services;

/// <summary>
/// M14: 左ペインのノート一覧の表示順を計算する。WPF・保存処理に依存しない純粋関数として、
/// 単体テストしやすい形にしている。並べ替えは表示専用（<see cref="NotebookViewModel.DisplayNotes"/>）
/// に適用し、保存対象の <see cref="NotebookViewModel.Notes"/> は変更しない。
/// </summary>
public static class NoteSortService
{
    /// <summary>
    /// <paramref name="notes"/> を <paramref name="mode"/> に従って並べ替えた新しいリストを返す。
    /// 入力コレクション自体は変更しない。同値の場合は入力順（安定ソート）を維持する。
    /// </summary>
    public static IReadOnlyList<NoteViewModel> Sort(IReadOnlyList<NoteViewModel> notes, NoteSortMode mode) =>
        mode switch
        {
            NoteSortMode.Updated => notes.OrderByDescending(EffectiveUpdatedAt).ToList(),
            NoteSortMode.Title => notes.OrderBy(n => n.Title, StringComparer.CurrentCultureIgnoreCase).ToList(),
            _ => notes.ToList(),
        };

    /// <summary>
    /// 更新日順の並べ替えキー。<see cref="NoteViewModel.UpdatedAt"/> が既定値（未設定・不正値相当）の場合は
    /// <see cref="NoteViewModel.CreatedAt"/> へフォールバックする。現在時刻を推測で補完しない。
    /// </summary>
    private static DateTime EffectiveUpdatedAt(NoteViewModel note) =>
        note.UpdatedAt != default ? note.UpdatedAt : note.CreatedAt;
}
