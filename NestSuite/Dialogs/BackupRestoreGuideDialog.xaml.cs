using System.Windows;
using NestSuite;

namespace NestSuite.Dialogs;

public partial class BackupRestoreGuideDialog : Window
{
    public BackupRestoreGuideDialog()
    {
        InitializeComponent();
        DataContext = new BackupRestoreGuideDialogViewModel(BackupRestoreGuideProvider.GetGuideText());
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

public sealed class BackupRestoreGuideDialogViewModel(string guideText)
{
    public string GuideText { get; } = guideText;
}
