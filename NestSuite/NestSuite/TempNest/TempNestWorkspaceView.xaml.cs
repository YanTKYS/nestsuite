using System.Windows;
using System.Windows.Controls;

namespace NestSuite.TempNest;

public partial class TempNestWorkspaceView : UserControl
{
    public TempNestWorkspaceView()
    {
        InitializeComponent();
        DataContextChanged += TempNestWorkspaceView_DataContextChanged;
    }

    private void TempNestWorkspaceView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not TempNestWorkspaceViewModel vm) return;
        foreach (var slot in new[] { vm.Slot1, vm.Slot2, vm.Slot3, vm.Slot4 })
            slot.ConfirmClear = () =>
                MessageBox.Show("スロットの内容をクリアしますか？", "スロットのクリア",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question)
                == MessageBoxResult.OK;
    }
}
