using System.Windows;
using NoteNest.NestSuite;
using NoteNest.Services;

namespace NoteNest;

public partial class App : Application
{
    private NestSuiteSingleInstance? _singleInstance;

    private void App_Startup(object sender, StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // v1.11.0: --classic-notenest 指定時のみ従来 NoteNest 単体版（互換ルート）を起動
        if (StartupArgParser.IsClassicMode(e.Args))
        {
            var startupPath = StartupArgParser.GetFilePath(e.Args);
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
            return;
        }

        // v1.11.0: 既定起動は NestSuite。--nestsuite フラグも互換として維持（同じ動作）。
        // ファイルパス指定時は拡張子に応じて NoteNest / ChatNest / IdeaNest タブを開く。
        // LoadInitialFile を Show() より前に呼ぶことで起動時ちらつきを防ぐ（v1.10.2）。
        var nestSuiteFilePath = StartupArgParser.GetFilePath(e.Args);

        // v1.18.1: シングルインスタンス — 2 つ目以降のプロセスはファイルを転送して終了する
        _singleInstance = new NestSuiteSingleInstance();
        if (!_singleInstance.TryBecomePrimary())
        {
            if (nestSuiteFilePath != null)
                NestSuiteSingleInstance.TrySendFiles([nestSuiteFilePath]);
            _singleInstance.Dispose();
            Shutdown(0);
            return;
        }

        var shell = new NestSuiteShellWindow(nestSuiteFilePath);
        MainWindow = shell;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        if (nestSuiteFilePath != null)
            shell.LoadInitialFile(nestSuiteFilePath);
        _singleInstance.StartServer(path => shell.OpenFileFromPipe(path));
        shell.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
