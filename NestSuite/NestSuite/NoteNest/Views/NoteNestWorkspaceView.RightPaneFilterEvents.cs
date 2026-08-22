using System.Windows.Input;

namespace NestSuite.Views;

// 右ペイン（マーカー・互換タスク）共通絞り込み欄のクリア・Esc操作。
// 絞り込み文字列自体は MainViewModel.RightPaneFilterText（タブごとに独立）が所有し、
// ここでは View 側のクリア・キーボード操作の配線のみを担う。
public partial class NoteNestWorkspaceView
{
    private void RightPaneFilterClear_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        ViewModel.RightPaneFilterText = string.Empty;
        RightPaneFilterBox.Focus();
    }

    private void RightPaneFilterBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        if (ViewModel.RightPaneFilterText.Length == 0) return;
        ViewModel.RightPaneFilterText = string.Empty;
        e.Handled = true;
    }
}
