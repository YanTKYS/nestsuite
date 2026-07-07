using System.Windows;
using NestSuite;

namespace NestSuite.Dialogs;

public partial class ShortcutHelpDialog : Window
{
    public ShortcutHelpDialog()
    {
        InitializeComponent();
        DataContext = new ShortcutHelpDialogViewModel(ShortcutHelpProvider.GetGroups());
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

public sealed class ShortcutHelpDialogViewModel(IReadOnlyList<ShortcutHelpGroup> groups)
{
    public IReadOnlyList<ShortcutHelpGroup> Groups { get; } = groups;
}
