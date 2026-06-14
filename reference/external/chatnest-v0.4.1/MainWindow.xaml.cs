using System.Windows;
using ChatNest.ViewModels;

namespace ChatNest
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow(string? filePath = null)
        {
            InitializeComponent();
            _vm = new MainViewModel();
            DataContext = _vm;

            Closing += (_, e) => { if (!_vm.ConfirmDiscardChanges()) e.Cancel = true; };

            if (filePath != null)
                Loaded += (_, _) => _vm.LoadFromPath(filePath);
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FinishDialog { Owner = this };
            dialog.ShowDialog();

            switch (dialog.SelectedAction)
            {
                case FinishAction.CopyMarkdown:
                    _vm.ExecuteMarkdownCopyWithSave();
                    break;
                case FinishAction.CopyIdeaNest:
                    _vm.ExecuteIdeaNestCopyWithSave();
                    break;
                case FinishAction.StartNew:
                    _vm.NewCommand.Execute(null);
                    break;
            }
        }
    }
}
