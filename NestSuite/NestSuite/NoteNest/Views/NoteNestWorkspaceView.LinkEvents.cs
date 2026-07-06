using System.Windows.Input;
using NestSuite.ViewModels;

namespace NestSuite.Views;

public partial class NoteNestWorkspaceView
{
    private void OutboundLink_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement { DataContext: OutboundLinkEntry entry }
            && entry.Target != null)
        {
            ViewModel.NavigateToNote(entry.Target);
            e.Handled = true;
        }
    }

    private void Backlink_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement { DataContext: BacklinkEntry entry })
        {
            ViewModel.NavigateToNote(entry.SourceNote);
            e.Handled = true;
        }
    }
}
