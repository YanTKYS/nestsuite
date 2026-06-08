using System.Windows;
using NoteNest.Services;

namespace NoteNest;

public partial class App : Application
{
    private void App_Startup(object sender, StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var startupPath = e.Args.Length > 0 ? e.Args[0] : null;

        if (startupPath == null)
        {
            var uiSettings = new UiSettingsService().Load();
            new ThemeService().Apply(uiSettings.Theme);

            startupPath = DialogService.ShowStartupDialog();
        }

        var window = new MainWindow(startupPath);
        MainWindow = window;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        window.Show();
    }
}
