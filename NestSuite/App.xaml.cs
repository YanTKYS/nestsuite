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
}
