using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace NoteNest.NestSuite.ChatNest;

/// <summary>
/// ChatNest Workspace ビュー。参照ソース ChatNest v0.4.1
/// Views/ChatNestWorkspaceView.xaml(.cs) より、Workspace 部分を中心に取り込み。
/// メッセージ追加時の自動スクロールと、入力欄のショートカット（Ctrl/Shift+Enter 投稿、
/// Ctrl/Shift+←→ 発言者切替）を処理する。AppShell（NestSuiteShellWindow）は移植しない。
/// </summary>
public partial class ChatNestWorkspaceView : UserControl
{
    public ChatNestWorkspaceView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ChatNestWorkspaceViewModel oldVm)
            oldVm.Messages.CollectionChanged -= OnMessagesChanged;
        if (e.NewValue is ChatNestWorkspaceViewModel newVm)
            newVm.Messages.CollectionChanged += OnMessagesChanged;
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.InvokeAsync(() => ChatScrollViewer.ScrollToBottom(), DispatcherPriority.Background);
    }

    private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ChatNestWorkspaceViewModel vm) return;
        var mods = Keyboard.Modifiers;

        if (e.Key == Key.Enter &&
            (mods == ModifierKeys.Control || mods == ModifierKeys.Shift))
        {
            if (vm.PostCommand.CanExecute(null))
                vm.PostCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if ((e.Key == Key.Right || e.Key == Key.Left) &&
            (mods == ModifierKeys.Control || mods == ModifierKeys.Shift))
        {
            vm.CycleSpeaker(e.Key == Key.Right);
            e.Handled = true;
        }
    }
}
