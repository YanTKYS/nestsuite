using System.Windows.Controls;
using System.Windows.Input;
using NestSuite.ViewModels;

namespace NestSuite.Views;

/// <summary>
/// マーカー一覧（ListBox）のキーボード操作。選択自体はジャンプせず、Enterキーのみ
/// 既存の MarkerClickCommand（マウスクリックと共通のジャンプ処理）を呼び出す。
/// </summary>
public partial class NoteNestWorkspaceView
{
    private void MarkerList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.Handled) return;
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not MarkerViewModel marker) return;

        ViewModel.MarkerClickCommand.Execute(marker);
        e.Handled = true;
    }
}
