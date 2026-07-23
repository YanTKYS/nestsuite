using System.Windows;
using System.Windows.Controls;

namespace NestSuite.NoteNest.Editor;

/// <summary>
/// v2.19.3 L4: NoteNest 本文エディタの折り返し設定（bool）を WPF TextBox の設定値へ変換する。
/// 純粋関数のみを持ち、WPF コントロールの生成・イベント購読は行わない
/// （<see cref="NoteEditorHost"/> 側から呼び出すためのテスト可能境界）。
/// </summary>
public static class NoteEditorWordWrapSettings
{
    public static TextWrapping ToTextWrapping(bool wordWrap) =>
        wordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap;

    public static ScrollBarVisibility ToHorizontalScrollBarVisibility(bool wordWrap) =>
        wordWrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
}
