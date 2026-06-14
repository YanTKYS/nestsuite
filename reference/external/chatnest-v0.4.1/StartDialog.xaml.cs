using System.Windows;
using System.Windows.Input;
using ChatNest.ViewModels;

namespace ChatNest
{
    public partial class StartDialog : Window
    {
        public StartDialog()
        {
            InitializeComponent();
            var vm = new StartDialogViewModel();
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
