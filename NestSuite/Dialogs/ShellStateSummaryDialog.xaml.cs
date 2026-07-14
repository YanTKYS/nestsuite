using System.Windows;

namespace NestSuite.Dialogs;

/// <summary>
/// SH-37: Shell が現在保持している状態を読み取り専用で表示するダイアログ。
/// 表示するのは <see cref="ShellStateSummary"/> のスナップショットのみで、
/// このダイアログ自身は状態を変更する操作を持たない（閉じるのみ）。
/// </summary>
public partial class ShellStateSummaryDialog : Window
{
    public ShellStateSummaryDialog(ShellStateSummary summary)
    {
        InitializeComponent();

        OpenTabsValue.Text = $"{summary.OpenTabCount}件";
        UnsavedTabsValue.Text = $"{summary.UnsavedTabCount}件";
        PendingRestoreValue.Text = $"{summary.PendingRestoreCount}件";
        DraftRecoveryValue.Text = summary.DraftRecoveryCandidateCount is int draftCount
            ? $"{draftCount}件"
            : "取得できません";
        TempNestSlotsValue.Text = $"{summary.NonEmptyTempNestSlotCount}スロット";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
