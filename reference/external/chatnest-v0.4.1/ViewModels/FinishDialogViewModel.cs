using System;
using System.Windows.Input;

namespace ChatNest.ViewModels
{
    public enum FinishAction { None, CopyMarkdown, CopyIdeaNest, StartNew }

    public class FinishDialogViewModel
    {
        public FinishAction SelectedAction { get; private set; } = FinishAction.None;
        public event EventHandler? CloseRequested;

        public ICommand CopyMarkdownCommand { get; }
        public ICommand CopyIdeaNestCommand { get; }
        public ICommand StartNewCommand { get; }
        public ICommand CancelCommand { get; }

        public FinishDialogViewModel()
        {
            CopyMarkdownCommand = new RelayCommand(() => Select(FinishAction.CopyMarkdown));
            CopyIdeaNestCommand = new RelayCommand(() => Select(FinishAction.CopyIdeaNest));
            StartNewCommand     = new RelayCommand(() => Select(FinishAction.StartNew));
            CancelCommand       = new RelayCommand(() => Select(FinishAction.None));
        }

        private void Select(FinishAction action)
        {
            SelectedAction = action;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
