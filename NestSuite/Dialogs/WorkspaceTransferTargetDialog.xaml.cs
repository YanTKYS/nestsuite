using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;

namespace NestSuite.Dialogs;

/// <summary>
/// LK-4 (v2.21.0): 転送先候補が複数件のときだけ表示する最小の選択ダイアログ。
/// 一覧・OK・キャンセルのみで、検索・複数選択・新規 Workspace 作成は行わない。
/// 選択対象は内部的に <see cref="NestSuiteShellWindow.WorkspaceTransferTarget.TabId"/> で保持するため、
/// 表示名が同じタブが複数あっても、選んだ行の TabId だけが返る（誤転送しない）。
/// </summary>
public partial class WorkspaceTransferTargetDialog : Window
{
    internal NestSuiteShellWindow.WorkspaceTransferTarget? SelectedTarget { get; private set; }

    private readonly List<NestSuiteShellWindow.WorkspaceTransferTarget> _targets;

    internal WorkspaceTransferTargetDialog(
        IEnumerable<NestSuiteShellWindow.WorkspaceTransferTarget> targets,
        string? windowTitle = null,
        string? promptText = null,
        string? listAutomationName = null)
    {
        _targets = targets.ToList();
        InitializeComponent();
        if (windowTitle != null) Title = windowTitle;
        if (promptText != null) PromptText.Text = promptText;
        // TD-93: このダイアログは LK-2（NoteNest）でも再利用されるため、XAML 既定の
        // 「転送先のIdeaNestタブ一覧」を呼出元の転送先種別に合わせて上書きする。
        if (listAutomationName != null) AutomationProperties.SetName(TargetList, listAutomationName);
        TargetList.ItemsSource = _targets;

        Loaded += (_, _) =>
        {
            if (TargetList.Items.Count > 0) TargetList.SelectedIndex = 0;
            TargetList.Focus();
        };
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        if (TargetList.SelectedItem is not NestSuiteShellWindow.WorkspaceTransferTarget target) return;
        SelectedTarget = target;
        DialogResult = true;
    }

    private void TargetList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TargetList.SelectedItem != null)
            OK_Click(sender, e);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;
}
