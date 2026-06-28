using System.Collections.Specialized;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NestSuite.Services;

namespace NestSuite.ChatNest;

/// <summary>
/// ChatNest Workspace ビュー。参照ソース ChatNest v0.4.1
/// Views/ChatNestWorkspaceView.xaml(.cs) より、Workspace 部分を中心に取り込み。
/// メッセージ追加時の自動スクロールと、入力欄のショートカット（Ctrl+Enter 投稿、
/// Ctrl+←→ 発言者切替）を処理する。AppShell（NestSuiteShellWindow）は移植しない。
///
/// <para><b>v2.3.0 変更点</b><br/>
/// CH-3: 最下部付近なら自動スクロール。遡り閲覧中は「最新へ」ボタンを表示する。<br/>
/// CH-6: EditBox の PreviewKeyDown / IsVisibleChanged でインライン編集キー操作を処理する。</para>
///
/// <para><b>v2.7.11 追加</b><br/>
/// CH-5: Ctrl+F で検索バーを開く。Esc で閉じる。ScrollToMessageRequested で BringIntoView。<br/>
/// CH-10: 発言単体コピーは ViewModel 側で処理するためビュー側の追加なし。</para>
/// </summary>
public partial class ChatNestWorkspaceView : UserControl
{
    private const double NearBottomThreshold = 100.0;

    // CH-13: ドラッグ並び替え状態
    private Point _dragStartPoint;
    private MessageViewModel? _pendingDragSource;
    private FrameworkElement? _pendingDragSourceElement;

    public ChatNestWorkspaceView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnGlobalPreviewKeyDown));
        AddHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler(OnGlobalPreviewMouseMove));
        AddHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(OnGlobalPreviewMouseLeftButtonUp));
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ChatNestWorkspaceViewModel oldVm)
        {
            oldVm.Messages.CollectionChanged    -= OnMessagesChanged;
            oldVm.ScrollToMessageRequested      -= OnScrollToMessageRequested;
            oldVm.ConversationExportRequested   -= OnConversationExportRequested;
        }
        if (e.NewValue is ChatNestWorkspaceViewModel newVm)
        {
            newVm.Messages.CollectionChanged    += OnMessagesChanged;
            newVm.ScrollToMessageRequested      += OnScrollToMessageRequested;
            newVm.ConversationExportRequested   += OnConversationExportRequested;
        }
    }

    // ── CH-5: スクロール ──────────────────────────────────────────────────────

    private void OnScrollToMessageRequested(object? sender, int index)
    {
        Dispatcher.InvokeAsync(() =>
        {
            var container = ChatItemsControl.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
            container?.BringIntoView();
        }, DispatcherPriority.Background);
    }

    // ── CH-5: キーボード ─────────────────────────────────────────────────────

    /// <summary>Ctrl+F で検索バーを開く。Escape で閉じる（SearchBox 以外でも有効）。</summary>
    private void OnGlobalPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ChatNestWorkspaceViewModel vm) return;

        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (vm.OpenSearchCommand.CanExecute(null))
                vm.OpenSearchCommand.Execute(null);
            Dispatcher.InvokeAsync(() => SearchBox.Focus(), DispatcherPriority.Input);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && vm.IsSearchBarVisible)
        {
            if (vm.CloseSearchCommand.CanExecute(null))
                vm.CloseSearchCommand.Execute(null);
            e.Handled = true;
        }
    }

    /// <summary>CH-5: 検索ボックス内キー操作。Enter→次、Shift+Enter→前、Esc→閉じる。</summary>
    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ChatNestWorkspaceViewModel vm) return;

        if (e.Key == Key.Escape)
        {
            if (vm.CloseSearchCommand.CanExecute(null))
                vm.CloseSearchCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (vm.SearchPreviousCommand.CanExecute(null))
                    vm.SearchPreviousCommand.Execute(null);
            }
            else
            {
                if (vm.SearchNextCommand.CanExecute(null))
                    vm.SearchNextCommand.Execute(null);
            }
            e.Handled = true;
        }
    }

    // ── 自動スクロール ───────────────────────────────────────────────────────

    private bool IsNearBottom()
    {
        var sv = ChatScrollViewer;
        return sv.ScrollableHeight <= 0 || sv.ScrollableHeight - sv.VerticalOffset <= NearBottomThreshold;
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add) return;
        bool wasNearBottom = IsNearBottom();
        Dispatcher.InvokeAsync(() =>
        {
            if (wasNearBottom)
                ChatScrollViewer.ScrollToBottom();
            else
                ScrollToBottomButton.Visibility = Visibility.Visible;
        }, DispatcherPriority.Background);
    }

    private void ChatScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (IsNearBottom())
            ScrollToBottomButton.Visibility = Visibility.Collapsed;
    }

    private void ScrollToBottomButton_Click(object sender, RoutedEventArgs e)
    {
        ChatScrollViewer.ScrollToBottom();
    }

    // CH-15: 空エリア右クリックメニューの「最下部へ移動」
    private void ScrollToBottomMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ChatScrollViewer.ScrollToBottom();
    }

    // ── インライン編集 ───────────────────────────────────────────────────────

    private void EditBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not MessageViewModel vm) return;
        if (e.Key == Key.Escape)
        {
            if (vm.CancelEditCommand.CanExecute(null)) vm.CancelEditCommand.Execute(null);
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (vm.CommitEditCommand.CanExecute(null)) vm.CommitEditCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void EditBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && sender is TextBox tb)
        {
            tb.Focus();
            tb.SelectAll();
        }
    }

    // ── CH-9: 会話エクスポート ───────────────────────────────────────────────

    private void OnConversationExportRequested(object? sender, EventArgs e)
    {
        if (sender is not ChatNestWorkspaceViewModel vm) return;
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title    = "会話をエクスポート",
            Filter   = "テキスト (*.txt)|*.txt|Markdown (*.md)|*.md|すべてのファイル (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = "conversation",
        };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;

        var isMarkdown = dlg.FilterIndex == 2;
        var content = isMarkdown
            ? ChatNestExportFormatter.BuildMarkdownConversation(vm.MessageModels)
            : ChatNestExportFormatter.BuildPlainTextConversation(vm.MessageModels);

        try
        {
            AtomicFileWriter.WriteAllText(dlg.FileName, content, Encoding.UTF8);
            vm.ShowExportStatus("会話を保存しました");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("ChatNestExport", ex, "ChatNest", dlg.FileName);
            MessageBox.Show("会話のエクスポートに失敗しました。", "エクスポートエラー",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── CH-13: ドラッグ並び替え ──────────────────────────────────────────────

    private void DragHandle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement el || el.DataContext is not MessageViewModel vm) return;
        _dragStartPoint = e.GetPosition(null);
        _pendingDragSource = vm;
        _pendingDragSourceElement = el;
        e.Handled = true;
    }

    private void OnGlobalPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _pendingDragSource == null) return;

        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        // 編集中はドラッグ無効
        if (DataContext is ChatNestWorkspaceViewModel parentVm && parentVm.Messages.Any(m => m.IsEditing))
        {
            _pendingDragSource = null;
            _pendingDragSourceElement = null;
            return;
        }

        var sourceVm = _pendingDragSource;
        var dragElement = _pendingDragSourceElement ?? ChatItemsControl;
        _pendingDragSource = null;
        _pendingDragSourceElement = null;

        var data = new DataObject();
        data.SetData(typeof(MessageViewModel), sourceVm);

        sourceVm.IsDragging = true;
        try
        {
            DragDrop.DoDragDrop(dragElement, data, DragDropEffects.Move);
        }
        finally
        {
            sourceVm.IsDragging = false;
            DragInsertionIndicator.Visibility = Visibility.Collapsed;
        }
    }

    private void OnGlobalPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _pendingDragSource = null;
        _pendingDragSourceElement = null;
    }

    private void ChatItemsControl_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(MessageViewModel)))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }
        e.Effects = DragDropEffects.Move;
        UpdateInsertionIndicator(e);
        e.Handled = true;
    }

    private void ChatItemsControl_DragLeave(object sender, DragEventArgs e)
    {
        DragInsertionIndicator.Visibility = Visibility.Collapsed;
    }

    private void ChatItemsControl_Drop(object sender, DragEventArgs e)
    {
        DragInsertionIndicator.Visibility = Visibility.Collapsed;
        if (!e.Data.GetDataPresent(typeof(MessageViewModel))) return;
        if (DataContext is not ChatNestWorkspaceViewModel vm) return;
        if (e.Data.GetData(typeof(MessageViewModel)) is not MessageViewModel sourceVm) return;

        int oldIndex = vm.Messages.IndexOf(sourceVm);
        if (oldIndex < 0) return;

        int dropIndex = GetDropIndex(e);
        // dropIndex はドロップ先の「前に挿入」インデックス（元配列基準）。
        // ObservableCollection.Move の第2引数は除去後の配列の挿入位置なので変換する。
        int moveIndex = dropIndex > oldIndex ? dropIndex - 1 : dropIndex;

        vm.MoveMessage(oldIndex, moveIndex);
        e.Handled = true;
    }

    private int GetDropIndex(DragEventArgs e)
    {
        for (int i = 0; i < ChatItemsControl.Items.Count; i++)
        {
            if (ChatItemsControl.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement container)
                continue;
            var pos = e.GetPosition(container);
            if (pos.Y < container.ActualHeight / 2)
                return i;
        }
        return ChatItemsControl.Items.Count;
    }

    private void UpdateInsertionIndicator(DragEventArgs e)
    {
        if (ChatItemsControl.Items.Count == 0)
        {
            DragInsertionIndicator.Visibility = Visibility.Collapsed;
            return;
        }
        int dropIndex = GetDropIndex(e);
        double y;
        if (dropIndex < ChatItemsControl.Items.Count &&
            ChatItemsControl.ItemContainerGenerator.ContainerFromIndex(dropIndex) is FrameworkElement targetContainer)
        {
            y = targetContainer.TransformToAncestor(ChatScrollViewer).Transform(new Point(0, 0)).Y;
        }
        else if (ChatItemsControl.ItemContainerGenerator.ContainerFromIndex(ChatItemsControl.Items.Count - 1) is FrameworkElement lastContainer)
        {
            y = lastContainer.TransformToAncestor(ChatScrollViewer).Transform(new Point(0, lastContainer.ActualHeight)).Y;
        }
        else
        {
            y = e.GetPosition(ChatScrollViewer).Y;
        }

        DragInsertionIndicator.Width = Math.Max(0, ChatScrollViewer.ActualWidth - 32);
        System.Windows.Controls.Canvas.SetTop(DragInsertionIndicator, Math.Max(0, y - 1));
        DragInsertionIndicator.Visibility = Visibility.Visible;
    }

    // ── 入力欄 ───────────────────────────────────────────────────────────────

    private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ChatNestWorkspaceViewModel vm) return;
        var mods = Keyboard.Modifiers;

        if (ChatNestShortcutPolicy.IsSendShortcut(e.Key, mods))
        {
            if (vm.PostCommand.CanExecute(null))
                vm.PostCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (ChatNestShortcutPolicy.IsSpeakerSwitchShortcut(e.Key, mods))
        {
            vm.CycleSpeaker(e.Key == Key.Right);
            e.Handled = true;
        }
    }
}
