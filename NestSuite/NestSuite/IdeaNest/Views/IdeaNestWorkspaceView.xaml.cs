using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NestSuite.IdeaNest.ViewModels;

namespace NestSuite.IdeaNest.Views;

public partial class IdeaNestWorkspaceView : UserControl
{
    public static readonly DependencyProperty ShowMenuProperty = DependencyProperty.Register(
        nameof(ShowMenu), typeof(bool), typeof(IdeaNestWorkspaceView),
        new PropertyMetadata(true));

    public bool ShowMenu
    {
        get => (bool)GetValue(ShowMenuProperty);
        set => SetValue(ShowMenuProperty, value);
    }

    private IdeaNestWorkspaceViewModel? Workspace => DataContext as IdeaNestWorkspaceViewModel;
    private IdeaNestWorkspaceViewModel? _wiredWorkspace;

    public IdeaNestWorkspaceView()
    {
        InitializeComponent();
        PreviewKeyDown += OnWindowPreviewKeyDown;
        DataContextChanged += (_, _) => ConfigureWorkspace();
        Loaded += (_, _) =>
        {
            ConfigureWorkspace();
            FocusWorkspace();
        };
    }

    public void FocusWorkspace() => CardArea.Focus();

    private void ConfigureWorkspace()
    {
        Workspace?.SetOwnerResolver(() => Window.GetWindow(this));

        // ID-15: DataContext が切り替わった場合に、以前の Workspace の購読を残さない。
        if (!ReferenceEquals(_wiredWorkspace, Workspace))
        {
            if (_wiredWorkspace != null) _wiredWorkspace.ScrollRequested -= OnCardScrollRequested;
            _wiredWorkspace = Workspace;
            if (_wiredWorkspace != null) _wiredWorkspace.ScrollRequested += OnCardScrollRequested;
        }
    }

    /// <summary>
    /// ID-15: 新規カード作成直後の一時的なスクロール要求を、コンテナ生成後に一度だけ処理する。
    /// 固定時間の待機や再試行タイマーは使わず、DispatcherPriority.Loaded で 1 回だけ実行する。
    /// </summary>
    private void OnCardScrollRequested(object? sender, IdeaCardViewModel card)
    {
        if (!ReferenceEquals(sender, Workspace)) return; // 既に切り替わった Workspace からの遅延通知は無視する

        Dispatcher.BeginInvoke(
            () =>
            {
                if (!ReferenceEquals(sender, Workspace)) return;
                if (CardsItemsControl.ItemContainerGenerator.ContainerFromItem(card) is FrameworkElement container)
                    container.BringIntoView();
            },
            DispatcherPriority.Loaded);
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            FocusSearch();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.OriginalSource is TextBoxBase) return;
            if (!IsDescendantOrSelf(e.OriginalSource as DependencyObject, CardArea)) return;
            if (Workspace?.PasteAsNewCard() == true) e.Handled = true;
        }
    }

    private void OnFocusSearchClick(object sender, RoutedEventArgs e)
    {
        FocusSearch();
    }

    private void FocusSearch()
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void OnSearchBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (!string.IsNullOrEmpty(SearchBox.Text))
            {
                if (Workspace is { } workspace) workspace.SearchText = string.Empty;
            }
            else
            {
                Keyboard.ClearFocus();
            }
            e.Handled = true;
        }
    }

    private void OnCardMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not IdeaCardViewModel card)
            return;

        if (e.OriginalSource is DependencyObject src && IsInsideButton(src))
            return;

        Workspace?.PreviewIdeaCommand.Execute(card);
    }

    /// <summary>
    /// v2.19.1 ID-4 (TD-88必須範囲): フォーカス中のカードで Enter を押したとき、
    /// マウスクリックと同じ <see cref="OnCardMouseLeftButtonUp"/> の遷移先（PreviewIdeaCommand）を
    /// そのまま呼び出す。プレビュー処理自体は複製しない。
    /// フッターボタン（ピン留め等）にフォーカスがある場合の Enter は、既存の
    /// <see cref="IsInsideButton"/> 判定（マウスクリック時と同一）でここでは処理せず、
    /// Button 標準の Enter/Click 動作へそのまま委ねる（横取りしない）。
    /// </summary>
    private void OnCardKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Handled || e.Key != Key.Enter) return;
        if (sender is not FrameworkElement fe || fe.DataContext is not IdeaCardViewModel card) return;
        if (e.OriginalSource is DependencyObject src && IsInsideButton(src)) return;

        Workspace?.PreviewIdeaCommand.Execute(card);
        e.Handled = true;
    }

    private static bool IsInsideButton(DependencyObject src)
    {
        for (var d = src; d != null; d = VisualTreeHelper.GetParent(d))
        {
            if (d is ButtonBase) return true;
        }
        return false;
    }

    private static bool IsDescendantOrSelf(DependencyObject? element, DependencyObject ancestor)
    {
        for (var d = element; d != null; d = VisualTreeHelper.GetParent(d))
        {
            if (d == ancestor) return true;
        }
        return false;
    }

    private void OnCardAreaMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!CardArea.IsKeyboardFocusWithin) CardArea.Focus();
    }

    private void OnCardAreaDragOver(object sender, DragEventArgs e)
    {
        e.Effects = HasAcceptableDropPayload(e.Data) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnCardAreaDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var paths = e.Data.GetData(DataFormats.FileDrop) as string[] ?? Array.Empty<string>();
        // v1.16.6 fix: 拡張子のみで絞り込む。存在確認は CreateCardsFromFiles の catch に委ねることで
        // 削除済みファイルなど読み込み失敗時にも警告ダイアログを表示できる
        var textFiles = paths.Where(HasAcceptableExtension).ToArray();
        if (textFiles.Length == 0) return;
        Workspace?.CreateCardsFromFiles(textFiles);
        e.Handled = true;
    }

    private static bool HasAcceptableDropPayload(IDataObject data)
    {
        if (!data.GetDataPresent(DataFormats.FileDrop)) return false;
        var paths = data.GetData(DataFormats.FileDrop) as string[];
        return paths != null && paths.Any(HasAcceptableExtension);
    }

    private static bool HasAcceptableExtension(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        var ext = Path.GetExtension(path);
        return string.Equals(ext, ".txt", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ext, ".md", StringComparison.OrdinalIgnoreCase);
    }
}
