using System.Windows;
using System.Windows.Controls;
using NestSuite.Dialogs;
using NestSuite.FileAssociation;
using NestSuite.ViewModels;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // ── NestSuite メニューハンドラ ──────────────────────────────────────

    private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();

    private void MenuFileAssociation_Click(object sender, RoutedEventArgs e)
    {
        var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
            ?? string.Empty;
        new FileAssociationDialog(exePath) { Owner = this }.ShowDialog();
        RestoreFocusToWorkspace();
    }

    private void TabListButton_Click(object sender, RoutedEventArgs e)
    {
        var btn = (Button)sender;
        var menu = new ContextMenu
        {
            PlacementTarget = btn,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom
        };
        foreach (var tab in _tabs)
        {
            var item = new MenuItem
            {
                Header      = tab.DisplayName,
                IsCheckable = true,
                IsChecked   = tab.Id == _selectedTab?.Id
            };
            var capturedTab = tab;
            item.Click += (_, _) => ActivateTab(capturedTab);
            menu.Items.Add(item);
        }
        menu.IsOpen = true;
    }

    private void MenuKeyboardShortcuts_Click(object sender, RoutedEventArgs e)
    {
        new ShortcutHelpDialog { Owner = this }.ShowDialog();
        RestoreFocusToWorkspace();
    }

    // v2.16.8 L8 (review1-fable5.md R-5): .bak の手動復元手順を案内するだけのダイアログ。
    // 自動復元・自動コピーは行わない。
    private void MenuBackupRestoreGuide_Click(object sender, RoutedEventArgs e)
    {
        new BackupRestoreGuideDialog { Owner = this }.ShowDialog();
        RestoreFocusToWorkspace();
    }

    private void MenuAbout_Click(object sender, RoutedEventArgs e)
        => _dialogs.ShowInfo(
            $"NestSuite v{MainViewModel.ApplicationVersion}\n\n" +
            "NoteNest / ChatNest / IdeaNest を搭載\n" +
            "ファイル単位タブで 3 ツールを並行利用できます。",
            "NestSuite について");
}
