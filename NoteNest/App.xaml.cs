using System.Windows;
using NoteNest.Dialogs;
using NoteNest.Services;

namespace NoteNest;

public partial class App : Application
{
    private void App_Startup(object sender, StartupEventArgs e)
    {
        var startupPath = e.Args.Length > 0 ? e.Args[0] : null;

        if (startupPath == null)
        {
            var uiSettings = new UiSettingsService().Load();
            new ThemeService().Apply(uiSettings.Theme);

            var dialog = new StartDialog();
            dialog.ShowDialog();
            startupPath = dialog.SelectedPath;
        }

        var window = new MainWindow(startupPath);
        window.Show();
    }
}
