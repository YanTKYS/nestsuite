using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // ── v1.7.6: タブを閉じる操作 ──────────────────────────────────────────

    /// <summary>
    /// タブの × ボタンクリックハンドラ。Button.Tag にバインドされたタブモデルを取り出し、
    /// <see cref="CloseTab"/> に委譲する。e.Handled = true で ListBoxItem 選択変更の
    /// 余分な伝播を抑制する。
    /// </summary>
    private void TabClose_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is NestSuiteDocumentTab tab)
            CloseTab(tab);
        e.Handled = true;
    }

    // ── v2.4.0 SH-2: タブコンテキストメニュー ────────────────────────────

    private void TabContextClose_Click(object sender, RoutedEventArgs e)
    {
        if (GetTabFromContextMenuItem(sender) is { } tab)
            CloseTab(tab);
    }

    private void TabContextPin_Click(object sender, RoutedEventArgs e)
    {
        if (GetTabFromContextMenuItem(sender) is { } tab)
            SetTabPinned(tab, isPinned: true);
    }

    private void TabContextUnpin_Click(object sender, RoutedEventArgs e)
    {
        if (GetTabFromContextMenuItem(sender) is { } tab)
            SetTabPinned(tab, isPinned: false);
    }

    private void TabContextCloseOthers_Click(object sender, RoutedEventArgs e)
    {
        if (GetTabFromContextMenuItem(sender) is { } keepTab)
            CloseOtherTabs(keepTab);
    }

    private void TabContextCloseRight_Click(object sender, RoutedEventArgs e)
    {
        if (GetTabFromContextMenuItem(sender) is { } pivotTab)
            CloseTabsToRight(pivotTab);
    }

    private static NestSuiteDocumentTab? GetTabFromContextMenuItem(object sender)
    {
        if (sender is MenuItem mi &&
            mi.Parent is ContextMenu cm &&
            cm.PlacementTarget is FrameworkElement el &&
            el.DataContext is NestSuiteDocumentTab tab)
            return tab;
        return null;
    }

    /// <summary>
    /// v2.4.0 SH-2: keepTab 以外のすべてのタブを順に閉じる。未保存確認を各タブで行う。
    /// いずれかのタブでユーザーがキャンセルした場合、そのタブ以降の処理を中断する。
    /// </summary>
    private void CloseOtherTabs(NestSuiteDocumentTab keepTab)
    {
        foreach (var tab in _tabs.Where(t => t.Id != keepTab.Id && t.CanClose).ToList())
        {
            if (!CloseTab(tab)) break;
        }
    }

    /// <summary>
    /// v2.4.0 SH-2: pivotTab より右側（インデックスが大きい）のタブを順に閉じる。未保存確認を各タブで行う。
    /// いずれかのタブでユーザーがキャンセルした場合、そのタブ以降の処理を中断する。
    /// </summary>
    private void CloseTabsToRight(NestSuiteDocumentTab pivotTab)
    {
        var idx = _tabs.IndexOf(pivotTab);
        if (idx < 0) return;
        foreach (var tab in _tabs.Skip(idx + 1).ToList())
        {
            if (!CloseTab(tab)) break;
        }
    }

    // ── v2.4.0 SH-3: 中クリックでタブを閉じる ────────────────────────────

    /// <summary>v2.4.0 SH-3: 中ボタンクリックで対象タブを閉じる。未保存確認を通す。</summary>
    private void TabStrip_PreviewMouseButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle) return;
        var tab = GetTabFromVisualTree(e.OriginalSource as DependencyObject);
        if (tab == null) return;
        if (!tab.CanClose) return;
        CloseTab(tab);
        e.Handled = true;
    }
}
