using System.Windows;

namespace NoteNest.Dialogs;

public partial class ProjectInfoDialog : Window
{
    public ProjectInfoDialog(string information)
    {
        InitializeComponent();
        InfoBox.Text = information;
    }
}
