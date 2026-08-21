using System.Windows;
using System.Windows.Controls;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // 新規タブ作成コマンド（NoteNest / IdeaNest / ChatNest）と「＋」ボタンメニューを扱う partial。

    /// <summary>新規 IdeaNest タブを作成する。既存の IdeaNest タブには影響しない。</summary>
    private void NewIdeaNestSession() => NewWorkspaceSession(NestSuiteWorkspaceKind.IdeaNest);

    /// <summary>
    /// 新規 ChatNest タブを作成する。既存の ChatNest タブには影響しない。
    /// 各タブは独立した ViewModel を持つため、破棄確認や Clear() は不要。
    /// </summary>
    private void NewChatNestSession() => NewWorkspaceSession(NestSuiteWorkspaceKind.ChatNest);

    private void MenuNewNoteNest_Click(object sender, RoutedEventArgs e) => NewNoteNestSession();
    private void MenuNewChatNest_Click(object sender, RoutedEventArgs e)  => NewChatNestSession();
    private void MenuNewIdeaNest_Click(object sender, RoutedEventArgs e)  => NewIdeaNestSession();
    private void MenuNewText_Click(object sender, RoutedEventArgs e)      => NewTextSession();

    /// <summary>
    /// 新規 PlainText（無題.txt）タブを作成する。既存の PlainText タブには影響しない。
    /// </summary>
    private void NewTextSession() => NewWorkspaceSession(NestSuiteWorkspaceKind.PlainText);

    /// <summary>「＋」ボタンクリック時に NoteNest/IdeaNest/ChatNest 選択メニューを表示する。</summary>
    private void TabAddButton_Click(object sender, RoutedEventArgs e)
    {
        var btn = (Button)sender;
        btn.ContextMenu!.PlacementTarget = btn;
        btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        btn.ContextMenu.IsOpen = true;
    }

    /// <summary>新規 NoteNest タブを作成する。既存の NoteNest タブには影響しない。</summary>
    private void NewNoteNestSession() => NewWorkspaceSession(NestSuiteWorkspaceKind.NoteNest);
}
