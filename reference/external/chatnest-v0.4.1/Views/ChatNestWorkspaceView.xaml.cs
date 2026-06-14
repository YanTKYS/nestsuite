using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ChatNest.ViewModels;

namespace ChatNest.Views
{
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
}
