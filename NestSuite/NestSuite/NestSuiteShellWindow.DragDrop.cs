using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using NestSuite.ChatNest;
using NestSuite.FileAssociation;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.IdeaNest.Services;
using NestSuite.NoteNest.Editor;
using NestSuite.Services;
using NestSuite.TempNest;
using NestSuite.ViewModels;
using NestSuite.Views;

namespace NestSuite;

partial class NestSuiteShellWindow
{
    // ── v1.17.0: タブドラッグ並び替え ─────────────────────────────────────

    private void TabStrip_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _tabDragSource = null;
        if (IsDescendantOfButton(e.OriginalSource as DependencyObject)) return;
        _tabDragStartPoint = e.GetPosition(null);
        _tabDragSource = GetTabFromVisualTree(e.OriginalSource as DependencyObject);
        if (_tabDragSource?.CanClose == false) _tabDragSource = null;
    }

    private void TabStrip_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _tabDragSource == null) return;
        var pos = e.GetPosition(null);
        var diff = _tabDragStartPoint - pos;
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        var source = _tabDragSource;
        _tabDragSource = null;
        DragDrop.DoDragDrop(TabStrip, source, DragDropEffects.Move);
        // DoDragDrop は同期ブロック。ここに来た時点でドラッグ終了（Drop 済み / Esc キャンセル）
        _tabDropTargetIndex = null;
        _insertionAdorner?.Hide();
    }

    private void TabStrip_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(NestSuiteDocumentTab)))
        {
            e.Effects = DragDropEffects.None;
            _insertionAdorner?.Hide();
            e.Handled = true;
            return;
        }
        e.Effects = DragDropEffects.Move;
        var idx = GetInsertionIndex(e);
        _tabDropTargetIndex = idx;
        ShowInsertionIndicator(idx);
        e.Handled = true;
    }

    private void TabStrip_DragLeave(object sender, DragEventArgs e)
    {
        // DragLeave は子要素からもバブリングするため、ListBox 境界外に出た場合のみ非表示
        var bounds = new Rect(0, 0, TabStrip.ActualWidth, TabStrip.ActualHeight);
        if (bounds.Contains(e.GetPosition(TabStrip))) return;
        _tabDropTargetIndex = null;
        _insertionAdorner?.Hide();
    }

    private void TabStrip_Drop(object sender, DragEventArgs e)
    {
        var insertAt = _tabDropTargetIndex;
        _tabDropTargetIndex = null;
        _insertionAdorner?.Hide();
        if (!e.Data.GetDataPresent(typeof(NestSuiteDocumentTab))) return;
        var sourceTab = (NestSuiteDocumentTab)e.Data.GetData(typeof(NestSuiteDocumentTab));
        if (insertAt == null) return;
        int sourceIdx = _tabs.IndexOf(sourceTab);
        if (sourceIdx < 0) return;
        // Temp タブ（index 0）の左側には挿入しない。insertAt 有効範囲: 1〜Count
        int rawInsert = Math.Max(1, Math.Min(insertAt.Value, _tabs.Count));
        // ObservableCollection.Move(from, to) は「from 削除後の配列の to に挿入」する。
        // 右方向移動（sourceIdx < rawInsert）では削除でインデックスが 1 ずれるため補正する。
        int targetIdx = sourceIdx < rawInsert ? rawInsert - 1 : rawInsert;
        targetIdx = Math.Max(1, Math.Min(targetIdx, _tabs.Count - 1));
        if (targetIdx == sourceIdx) return;
        _tabs.Move(sourceIdx, targetIdx);
    }

    private static NestSuiteDocumentTab? GetTabFromVisualTree(DependencyObject? element)
    {
        for (var d = element; d != null; d = VisualTreeHelper.GetParent(d))
        {
            if (d is FrameworkElement { DataContext: NestSuiteDocumentTab tab })
                return tab;
        }
        return null;
    }

    private static bool IsDescendantOfButton(DependencyObject? element)
    {
        for (var d = element; d != null; d = VisualTreeHelper.GetParent(d))
            if (d is Button) return true;
        return false;
    }

    // ── v2.6.5 SH-17: タブドラッグ挿入位置インジケーター ────────────────────

    /// <summary>マウス位置から挿入インデックスを計算する。Temp タブ左側（index 0）への挿入は排除し、最小 1 を返す。</summary>
    private int GetInsertionIndex(DragEventArgs e)
    {
        var mouseX = e.GetPosition(TabStrip).X;
        for (int i = 1; i < _tabs.Count; i++)
        {
            if (TabStrip.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement item) continue;
            var center = item.TranslatePoint(new Point(item.ActualWidth / 2.0, 0), TabStrip).X;
            if (mouseX < center) return i;
        }
        return _tabs.Count;
    }

    private void ShowInsertionIndicator(int insertionIndex)
    {
        var adorner = GetOrCreateInsertionAdorner();
        if (adorner == null) return;
        double x;
        if (insertionIndex < _tabs.Count)
        {
            if (TabStrip.ItemContainerGenerator.ContainerFromIndex(insertionIndex) is not FrameworkElement item)
            { adorner.Hide(); return; }
            x = item.TranslatePoint(new Point(0, 0), TabStrip).X;
        }
        else
        {
            if (TabStrip.ItemContainerGenerator.ContainerFromIndex(_tabs.Count - 1) is not FrameworkElement item)
            { adorner.Hide(); return; }
            x = item.TranslatePoint(new Point(item.ActualWidth, 0), TabStrip).X;
        }
        adorner.Show(x);
    }

    private TabInsertionAdorner? GetOrCreateInsertionAdorner()
    {
        if (_insertionAdorner != null) return _insertionAdorner;
        var layer = AdornerLayer.GetAdornerLayer(TabStrip);
        if (layer == null) return null;
        // NoteNest アクセントカラー（#4A90D9）をインジケーター色として採用。ライト / ダーク両テーマで視認可能。
        var brush = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9));
        _insertionAdorner = new TabInsertionAdorner(TabStrip, brush);
        layer.Add(_insertionAdorner);
        return _insertionAdorner;
    }

    private sealed class TabInsertionAdorner : Adorner
    {
        private readonly Pen _pen;
        private double _insertX;
        private bool _isVisible;

        public TabInsertionAdorner(UIElement adornedElement, Brush brush) : base(adornedElement)
        {
            IsHitTestVisible = false;
            _pen = new Pen(brush, 2);
        }

        public void Show(double x) { _insertX = x; _isVisible = true; InvalidateVisual(); }
        public void Hide()         { if (!_isVisible) return; _isVisible = false; InvalidateVisual(); }

        protected override void OnRender(DrawingContext dc)
        {
            if (!_isVisible) return;
            dc.DrawLine(_pen, new Point(_insertX, 0), new Point(_insertX, AdornedElement.RenderSize.Height));
        }
    }
}
