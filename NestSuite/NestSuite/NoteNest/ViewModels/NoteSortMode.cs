namespace NestSuite.ViewModels;

/// <summary>
/// M14: 左ペインのノート一覧の表示順。保存データの並び順（<see cref="NotebookViewModel.Notes"/>）は
/// 変更しない派生の表示設定であり、<c>UiSettings.NoteSortMode</c> としてアプリ全体で1つ保存する。
/// </summary>
public enum NoteSortMode
{
    /// <summary>既定値。<see cref="NotebookViewModel.Notes"/> の現行コレクション順をそのまま表示する。</summary>
    Created,

    /// <summary>更新日時の降順（同値・不正値は元のコレクション順へ安定的にフォールバックする）。</summary>
    Updated,

    /// <summary>タイトルの昇順（大文字小文字を区別しない。同名は元のコレクション順を維持する）。</summary>
    Title,
}
