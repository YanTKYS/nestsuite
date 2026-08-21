using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NestSuite.ViewModels;

namespace NestSuite.Views;

public partial class NoteNestWorkspaceView
{
    private void OutboundLink_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: OutboundLinkEntry entry } && TryNavigateOutboundLink(entry))
            e.Handled = true;
    }

    private void Backlink_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BacklinkEntry entry } && TryNavigateBacklink(entry))
            e.Handled = true;
    }

    /// <summary>Enterキーは選択中のアウトバウンドリンクをマウスクリックと同じ処理で実行する。</summary>
    private void OutboundLinkList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.Handled) return;
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not OutboundLinkEntry entry) return;

        TryNavigateOutboundLink(entry);
        e.Handled = true;
    }

    /// <summary>Enterキーは選択中のバックリンクをマウスクリックと同じ処理で実行する。</summary>
    private void BacklinkList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.Handled) return;
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not BacklinkEntry entry) return;

        TryNavigateBacklink(entry);
        e.Handled = true;
    }

    /// <summary>マウスクリック・Enterキー共通のアウトバウンドリンク移動処理。リンク切れ（Target == null）は移動しない。</summary>
    private bool TryNavigateOutboundLink(OutboundLinkEntry? entry)
    {
        if (entry?.Target == null) return false;
        ViewModel.NavigateToNote(entry.Target);
        return true;
    }

    /// <summary>マウスクリック・Enterキー共通のバックリンク移動処理。</summary>
    private bool TryNavigateBacklink(BacklinkEntry? entry)
    {
        if (entry == null) return false;
        ViewModel.NavigateToNote(entry.SourceNote);
        return true;
    }
}
