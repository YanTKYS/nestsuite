using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using NestSuite.ChatNest;
using NestSuite.FileAssociation;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.IdeaNest.Services;
using NestSuite.NoteNest.Editor;
using NestSuite.Services;
using NestSuite.TempNest;
using NestSuite.ViewModels;
using NestSuite.Views;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // ── NestSuite メニューハンドラ ──────────────────────────────────────

    private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();

    // ── ツール選択ハンドラ（サイドバー・ツールメニュー共通） ────────────

    // v1.7.3: サイドバーとメニューはタブランチャーとして機能する
    private void ToolBorder_MouseDown(object sender, MouseButtonEventArgs e)
        => EnsureTabForToolId((string)((FrameworkElement)sender).Tag);

    private void MenuTool_Click(object sender, RoutedEventArgs e)
        => EnsureTabForToolId((string)((FrameworkElement)sender).Tag);

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

    private void MenuAbout_Click(object sender, RoutedEventArgs e)
        => _dialogs.ShowInfo(
            $"NestSuite v{MainViewModel.ApplicationVersion}\n\n" +
            "NoteNest / ChatNest / IdeaNest を搭載\n" +
            "ファイル単位タブで 3 ツールを並行利用できます。",
            "NestSuite について");
}
