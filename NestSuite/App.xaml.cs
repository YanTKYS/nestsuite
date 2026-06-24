using System.Windows;
using System.Windows.Threading;
using NestSuite.Services;

namespace NestSuite;

public partial class App : Application
{
    private NestSuiteSingleInstance? _singleInstance;

    private void App_Startup(object sender, StartupEventArgs e)
    {
        // v2.9.5 SH-21 hotfix: UIイベント起因の未処理例外を ErrorLog に記録する。
        // ユーザーデータ（ノート本文等）はスタックトレースに含まれないよう provider 側で除外済み。
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += App_CurrentDomainUnhandledException;

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // v1.11.0: 既定起動は NestSuite。--nestsuite フラグも互換として維持（同じ動作）。
        // v1.19.3: --classic-notenest は廃止。未知フラグは無視して NestSuite を起動する。
        // ファイルパス指定時は拡張子に応じて NoteNest / ChatNest / IdeaNest タブを開く。
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

        // v1.18.2: ファイルパスをコンストラクターへ渡すことで ShouldCreateInitialTab が
        // 復元失敗時の無題タブ作成を抑止する（無セッション＋引数ファイルで無題タブが混入しない）。
        // セッション復元はコンストラクター内で引数有無を問わず常に試みる（v1.18.2 fix）。
        // 起動引数ファイルは復元完了後に LoadInitialFile で追加タブとして開く（v1.18.2）。
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

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ErrorLogService.Log("DispatcherUnhandledException", e.Exception);
    }

    private static void App_CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            ErrorLogService.Log("UnhandledException", ex);
    }
}
