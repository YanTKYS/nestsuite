using System.Windows;
using NoteNest.NestSuite;
using NoteNest.Services;

namespace NoteNest;

public partial class App : Application
{
    private void App_Startup(object sender, StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // v1.6.1: --nestsuite 引数指定時は開発・検証用 NestSuiteShellWindow を起動
        if (StartupArgParser.IsNestSuiteMode(e.Args))
        {
            var shell = new NestSuiteShellWindow();
            MainWindow = shell;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            shell.Show();
            // v1.6.3: --nestsuite + ファイルパス指定時はそのファイルを開く
            var nestSuiteFilePath = StartupArgParser.GetFilePath(e.Args);
            if (nestSuiteFilePath != null)
                shell.LoadInitialFile(nestSuiteFilePath);
            return;
        }

        // 通常起動: NoteNest 単体版（変更なし）
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
