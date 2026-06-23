using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

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

    public ChatNestWorkspaceView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnGlobalPreviewKeyDown));
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ChatNestWorkspaceViewModel oldVm)
        {
            oldVm.Messages.CollectionChanged -= OnMessagesChanged;
            oldVm.ScrollToMessageRequested  -= OnScrollToMessageRequested;
        }
        if (e.NewValue is ChatNestWorkspaceViewModel newVm)
        {
            newVm.Messages.CollectionChanged += OnMessagesChanged;
            newVm.ScrollToMessageRequested  += OnScrollToMessageRequested;
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
