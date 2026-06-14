using System.Windows;
using System.Windows.Input;
using ChatNest.ViewModels;

namespace ChatNest
{
    public partial class FinishDialog : Window
    {
        public FinishAction SelectedAction =>
            (DataContext as FinishDialogViewModel)?.SelectedAction ?? FinishAction.None;

        public FinishDialog()
        {
            InitializeComponent();
            var vm = new FinishDialogViewModel();
            DataContext = vm;
            vm.CloseRequested += (_, _) => Close();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}
