using System.IO;
using System.Windows;

namespace ChatNest
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length > 0)
            {
                // ファイル関連付けからの起動
                var path = e.Args[0];
                if (!File.Exists(path))
                {
                    MessageBox.Show(
                        $"ファイルが見つかりません。\n{path}",
                        "ファイルが見つかりません",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    OpenMainWindow(null);
                }
                else if (!path.EndsWith(".chatnest", System.StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(
                        $"サポートされていないファイル形式です。\n{path}",
                        "ファイル形式エラー",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    OpenMainWindow(null);
                }
                else
                {
                    OpenMainWindow(path);
                }
            }
            else
            {
                // 通常起動: スタートダイアログを表示
                var dialog = new StartDialog();
                dialog.ShowDialog();

                var vm = dialog.DataContext as ViewModels.StartDialogViewModel;
                var result = vm?.ResultPath;

                if (result == null)
                {
                    // ダイアログをキャンセル → アプリ終了
                    Shutdown();
                    return;
                }

                OpenMainWindow(result == string.Empty ? null : result);
            }
        }

        private void OpenMainWindow(string? filePath)
        {
            var window = new MainWindow(filePath);
            MainWindow = window;
            window.Closed += (_, _) => Shutdown();
            window.Show();
        }
    }
}
