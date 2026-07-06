using System.Runtime.InteropServices;
using System.Windows.Interop;
using NestSuite.Models;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // v2.14.11 SH-32: ダークテーマ選択時、OS 標準タイトルバーが白いままだと Shell 全体の
    // 見た目と不釣り合いになるため、DWM の immersive dark mode 属性でタイトルバーを
    // ダークモードへ寄せる。カスタムタイトルバー化（WindowChrome 全面作り直し）は行わない。

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd, int attribute, ref int value, int size);

    // Windows 10 20H1 (build 19041) 以降で有効な値。古い Windows では呼び出しが失敗するだけで、
    // 通常の白いタイトルバーのまま安全にフォールバックする（例外を投げない・機能に影響しない）。
    private const int DwmwaUseImmersiveDarkMode = 20;

    /// <summary>
    /// 現在のテーマに応じてタイトルバーの immersive dark mode を切り替える。
    /// ウィンドウハンドルが未生成の場合や OS が非対応の場合は何もしない（安全側フォールバック）。
    /// </summary>
    private void ApplyTitleBarTheme(AppTheme theme)
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            var useDark = theme == AppTheme.Dark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref useDark, sizeof(int));
        }
        catch
        {
            // タイトルバー装飾は見た目の調整のみであり、失敗してもアプリ機能に影響させない。
        }
    }
}
