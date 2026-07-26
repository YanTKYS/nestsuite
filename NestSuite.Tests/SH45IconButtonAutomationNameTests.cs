using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.19.14 SH-45: アイコン・記号のみで表示される操作ボタンへのAutomationProperties.Name補完の
/// 静的 UI/コード契約確認。表示・Click・Command・Tab順・保存形式は一切変更していないため、
/// ここでは対象ボタンへNameが付与されたこと、既存の可視テキスト付き要素・装飾記号・K-6対象を
/// 誤って変更していないことを確認する。
/// </summary>
public class SH45IconButtonAutomationNameTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadShellXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.xaml"));

    private static string ReadNoteNestXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml"));

    private static string ReadNoteNestCodeBehind() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml.cs"));

    private static string ReadIdeaNestXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "IdeaNest", "Views", "IdeaNestWorkspaceView.xaml"));

    private static string ReadIdeaCardViewModelSource() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "IdeaNest", "ViewModels", "IdeaCardViewModel.cs"));

    private static string ReadChatNestXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "ChatNest", "ChatNestWorkspaceView.xaml"));

    private static string ExtractElementEndingAt(string xaml, string anchor, bool selfClosing = true)
    {
        var idx = xaml.IndexOf(anchor, System.StringComparison.Ordinal);
        Assert.True(idx >= 0, $"anchor not found: {anchor}");
        var elementStart = xaml.LastIndexOf("<Button", idx, System.StringComparison.Ordinal);
        Assert.True(elementStart >= 0);
        var end = selfClosing
            ? xaml.IndexOf("/>", idx, System.StringComparison.Ordinal)
            : xaml.IndexOf(">", idx, System.StringComparison.Ordinal);
        return xaml.Substring(elementStart, end - elementStart);
    }

    // ── 2. Shellタブ閉じるボタン ─────────────────────────────────────────

    [Fact]
    public void Shell_TabCloseButton_HasMatchingAutomationName()
    {
        var element = ExtractElementEndingAt(ReadShellXaml(), "ToolTip=\"このタブを閉じる\"");
        Assert.Contains("Content=\"×\"", element);
        Assert.Contains("AutomationProperties.Name=\"このタブを閉じる\"", element);
    }

    [Fact]
    public void Shell_CrossSearchCloseButton_HasMatchingAutomationName()
    {
        var element = ExtractElementEndingAt(ReadShellXaml(), "Click=\"CrossSearchCloseButton_Click\"");
        Assert.Contains("Content=\"×\"", element);
        Assert.Contains("AutomationProperties.Name=\"横断検索を閉じる\"", element);
    }

    // ── 3/4. 新規・タブ一覧ボタン（内部ID文字列だったNameを操作名へ修正） ──

    [Fact]
    public void Shell_TabAddButton_NameIsOperationText_NotInternalId()
    {
        var xaml = ReadShellXaml();
        Assert.Contains("AutomationProperties.AutomationId=\"Shell.TabAddButton\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"新規タブを追加\"", xaml);
        Assert.DoesNotContain("AutomationProperties.Name=\"Shell.TabAddButton\"", xaml);
    }

    [Fact]
    public void Shell_TabListButton_NameIsOperationText_NotInternalId()
    {
        var xaml = ReadShellXaml();
        Assert.Contains("AutomationProperties.AutomationId=\"Shell.TabListButton\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"タブ一覧を開く\"", xaml);
        Assert.DoesNotContain("AutomationProperties.Name=\"Shell.TabListButton\"", xaml);
    }

    // ── 6. NoteNest右ペイン開閉ボタン（状態依存Name） ──────────────────

    [Fact]
    public void NoteNest_RightPaneExpandButton_HasNoStaticInternalIdName()
    {
        var xaml = ReadNoteNestXaml();
        // Content="»" ボタン自体の固定Nameは削除し、状態依存でコードから設定する。
        Assert.DoesNotContain("AutomationProperties.Name=\"NoteNest.RightPaneExpandButton\"", xaml);
    }

    [Fact]
    public void NoteNest_RightPaneExpandButton_SetsAutomationNameForBothStates()
    {
        var src = ReadNoteNestCodeBehind();
        var collapseStart = src.IndexOf("public void CollapseRightPane()", System.StringComparison.Ordinal);
        var expandStart = src.IndexOf("public void ExpandRightPane()", System.StringComparison.Ordinal);
        Assert.True(collapseStart >= 0 && expandStart >= 0);
        var collapseBody = ExtractMethodBody(src, collapseStart);
        var expandBody = ExtractMethodBody(src, expandStart);

        Assert.Contains("RightPaneExpandButton.ToolTip = \"右ペインを表示\";", collapseBody);
        Assert.Contains("AutomationProperties.SetName(RightPaneExpandButton, \"右ペインを表示\");", collapseBody);
        Assert.Contains("RightPaneExpandButton.ToolTip = \"右ペインを閉じる\";", expandBody);
        Assert.Contains("AutomationProperties.SetName(RightPaneExpandButton, \"右ペインを閉じる\");", expandBody);
    }

    // ── NoteNest: コメント編集・保存ボタン ───────────────────────────────

    [Fact]
    public void NoteNest_EditCommentButton_HasMatchingAutomationName()
    {
        // Click="EditTaskComment_Click" は同名のContextMenu MenuItemにも存在するため、
        // Buttonに一意なContent="✏"を目印として使う。
        var element = ExtractElementEndingAt(ReadNoteNestXaml(), "Content=\"✏\"");
        Assert.Contains("Content=\"✏\"", element);
        Assert.Contains("AutomationProperties.Name=\"コメントを編集\"", element);
    }

    [Fact]
    public void NoteNest_SaveButton_HasAutomationName_NotFullToolTipWithShortcut()
    {
        var element = ExtractElementEndingAt(ReadNoteNestXaml(), "Command=\"{Binding SaveProjectCommand}\"");
        Assert.Contains("Content=\"💾\"", element);
        Assert.Contains("ToolTip=\"保存 (Ctrl+S)\"", element);
        Assert.Contains("AutomationProperties.Name=\"保存\"", element);
        // ToolTip全文（ショートカット表記込み）をそのまま複製していないこと。
        Assert.DoesNotContain("AutomationProperties.Name=\"保存 (Ctrl+S)\"", element);
    }

    // ── 5/10. IdeaNest: 状態依存ボタン（タグパネル・ピン・アーカイブ） ──

    [Fact]
    public void IdeaNest_TagPanelToggle_AutomationNameReusesExistingToolTipBinding()
    {
        var element = ExtractElementEndingAt(ReadIdeaNestXaml(), "Command=\"{Binding ToggleTagPanelCommand}\"");
        Assert.Contains("ToolTip=\"{Binding TagPanelButtonTip}\"", element);
        Assert.Contains("AutomationProperties.Name=\"{Binding TagPanelButtonTip}\"", element);
    }

    [Fact]
    public void IdeaNest_PinButton_AutomationNameBoundToStateDependentProperty()
    {
        var element = ExtractElementEndingAt(ReadIdeaNestXaml(), "Command=\"{Binding DataContext.TogglePinCommand, RelativeSource={RelativeSource AncestorType=UserControl}}\"");
        Assert.Contains("Content=\"📌\"", element);
        Assert.Contains("AutomationProperties.Name=\"{Binding PinActionName}\"", element);
    }

    [Fact]
    public void IdeaNest_ArchiveButton_AutomationNameBoundToStateDependentProperty()
    {
        var element = ExtractElementEndingAt(ReadIdeaNestXaml(), "Command=\"{Binding DataContext.ToggleArchiveCommand, RelativeSource={RelativeSource AncestorType=UserControl}}\"");
        Assert.Contains("Content=\"📥\"", element);
        Assert.Contains("AutomationProperties.Name=\"{Binding ArchiveActionName}\"", element);
    }

    [Fact]
    public void IdeaNest_DeleteButton_HasFixedAutomationName()
    {
        var element = ExtractElementEndingAt(ReadIdeaNestXaml(), "Command=\"{Binding DataContext.DeleteIdeaCommand, RelativeSource={RelativeSource AncestorType=UserControl}}\"");
        Assert.Contains("Content=\"🗑\"", element);
        Assert.Contains("AutomationProperties.Name=\"カードを削除\"", element);
    }

    [Fact]
    public void PinActionName_ReflectsCurrentPinnedState_NotFixedRegardlessOfState()
    {
        var unpinned = new IdeaCardViewModel(new Idea { Title = "A" });
        Assert.Equal("カードをピン留め", unpinned.PinActionName);

        unpinned.IsPinned = true;
        Assert.Equal("カードのピン留めを解除", unpinned.PinActionName);
    }

    [Fact]
    public void ArchiveActionName_ReflectsCurrentArchivedState_NotFixedRegardlessOfState()
    {
        var active = new IdeaCardViewModel(new Idea { Title = "A" });
        Assert.Equal("カードをアーカイブ", active.ArchiveActionName);

        active.IsArchived = true;
        Assert.Equal("カードをアーカイブから戻す", active.ArchiveActionName);
    }

    [Fact]
    public void PinAndArchiveActionName_AreNotPersistedOrDirtyRelated()
    {
        var src = ReadIdeaCardViewModelSource();
        var pinStart = src.IndexOf("public string PinActionName", System.StringComparison.Ordinal);
        var archiveStart = src.IndexOf("public string ArchiveActionName", System.StringComparison.Ordinal);
        Assert.True(pinStart >= 0 && archiveStart >= 0);
        // 単一式ボディ（=>）のため、行末までを対象に確認する。
        var pinLineEnd = src.IndexOf('\n', pinStart);
        var archiveLineEnd = src.IndexOf('\n', archiveStart);
        var pinLine = src.Substring(pinStart, pinLineEnd - pinStart);
        var archiveLine = src.Substring(archiveStart, archiveLineEnd - archiveStart);

        Assert.DoesNotContain("IsModified", pinLine);
        Assert.DoesNotContain("IsModified", archiveLine);
    }

    // ── IdeaNest: 固定名の対象（並び替え・サイズ・検索クリア） ──────────

    [Fact]
    public void IdeaNest_ReshuffleAndRandomPreviewButtons_HaveAutomationNames()
    {
        var xaml = ReadIdeaNestXaml();
        Assert.Contains("AutomationProperties.Name=\"カードを再シャッフル\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"カードをランダムに1枚プレビュー\"", xaml);
    }

    [Fact]
    public void IdeaNest_CardSizeButtons_HaveDistinctAutomationNames()
    {
        var xaml = ReadIdeaNestXaml();
        Assert.Contains("AutomationProperties.Name=\"カードサイズを小に設定\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"カードサイズを中に設定\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"カードサイズを大に設定\"", xaml);
    }

    [Fact]
    public void IdeaNest_ClearSearchButton_HasAutomationName()
    {
        var element = ExtractElementEndingAt(ReadIdeaNestXaml(), "Command=\"{Binding ClearSearchCommand}\"");
        Assert.Contains("Content=\"✕\"", element);
        Assert.Contains("AutomationProperties.Name=\"検索をクリア\"", element);
    }

    // ── ChatNest: 検索バーボタン ─────────────────────────────────────────

    [Fact]
    public void ChatNest_SearchBarButtons_HaveAutomationNames()
    {
        var xaml = ReadChatNestXaml();
        Assert.Contains("AutomationProperties.Name=\"検索を閉じる\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"前の検索結果へ移動\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"次の検索結果へ移動\"", xaml);
    }

    // ── 6. 可視テキスト要素の非対象 ──────────────────────────────────────

    [Fact]
    public void NormalTextButtons_AreNotMachinicallyGivenAutomationName()
    {
        // 「保存」「キャンセル」等、可視テキストを持つ通常ボタンへ機械的付与していないことの一例確認。
        // ChatNestの確定/キャンセルボタンはContentが説明文字のため対象外のまま。
        var xaml = ReadChatNestXaml();
        Assert.Contains("Content=\"キャンセル(_C)\"", xaml);
        var idx = xaml.IndexOf("Content=\"キャンセル(_C)\"", System.StringComparison.Ordinal);
        var elementStart = xaml.LastIndexOf("<Button", idx, System.StringComparison.Ordinal);
        var elementEnd = xaml.IndexOf("/>", idx, System.StringComparison.Ordinal);
        var element = xaml.Substring(elementStart, elementEnd - elementStart);
        Assert.DoesNotContain("AutomationProperties.Name", element);
    }

    [Fact]
    public void MenuItemsWithDescriptiveHeaders_AreNotChanged()
    {
        // IdeaNestのContextMenu項目（Header文字あり）は今回対象外のまま。
        var xaml = ReadIdeaNestXaml();
        var idx = xaml.IndexOf("Header=\"ピン留め切替(_P)\"", System.StringComparison.Ordinal);
        Assert.True(idx >= 0);
        var elementStart = xaml.LastIndexOf("<MenuItem", idx, System.StringComparison.Ordinal);
        var elementEnd = xaml.IndexOf("/>", idx, System.StringComparison.Ordinal);
        var element = xaml.Substring(elementStart, elementEnd - elementStart);
        Assert.DoesNotContain("AutomationProperties.Name", element);
    }

    // ── 7. 装飾要素の非対象 ──────────────────────────────────────────────

    [Fact]
    public void DecorativeUnsavedDotAndBrokenLinkWarning_AreNotTreatedAsButtons()
    {
        var shellXaml = ReadShellXaml();
        var idx = shellXaml.IndexOf("Text=\"●\"", System.StringComparison.Ordinal);
        Assert.True(idx >= 0);
        var elementStart = shellXaml.LastIndexOf("<TextBlock", idx, System.StringComparison.Ordinal);
        var elementEnd = shellXaml.IndexOf("/>", idx, System.StringComparison.Ordinal);
        var element = shellXaml.Substring(elementStart, elementEnd - elementStart);
        Assert.DoesNotContain("<Button", element);
        Assert.DoesNotContain("AutomationProperties.Name", element);
    }

    // ── 8. 動作属性の維持 ────────────────────────────────────────────────

    [Fact]
    public void ModifiedButtons_StillHaveOriginalClickOrCommand_AndToolTip()
    {
        var shellXaml = ReadShellXaml();
        Assert.Contains("Click=\"TabClose_Click\"", shellXaml);
        Assert.Contains("Click=\"CrossSearchCloseButton_Click\"", shellXaml);
        Assert.Contains("Click=\"TabAddButton_Click\"", shellXaml);
        Assert.Contains("Click=\"TabListButton_Click\"", shellXaml);

        var noteNestXaml = ReadNoteNestXaml();
        Assert.Contains("Click=\"EditTaskComment_Click\"", noteNestXaml);
        Assert.Contains("Command=\"{Binding SaveProjectCommand}\"", noteNestXaml);
        Assert.Contains("Click=\"ToggleRightPane_Click\"", noteNestXaml);

        var ideaNestXaml = ReadIdeaNestXaml();
        Assert.Contains("Command=\"{Binding ToggleTagPanelCommand}\"", ideaNestXaml);
        Assert.Contains("Command=\"{Binding ClearSearchCommand}\"", ideaNestXaml);
        Assert.Contains("Command=\"{Binding ReshuffleCommand}\"", ideaNestXaml);
        Assert.Contains("Command=\"{Binding RandomPreviewCommand}\"", ideaNestXaml);

        var chatNestXaml = ReadChatNestXaml();
        Assert.Contains("Command=\"{Binding CloseSearchCommand}\"", chatNestXaml);
        Assert.Contains("Command=\"{Binding SearchPreviousCommand}\"", chatNestXaml);
        Assert.Contains("Command=\"{Binding SearchNextCommand}\"", chatNestXaml);
    }

    // ── 9. dirty・保存非影響 ─────────────────────────────────────────────

    [Fact]
    public void RightPaneAutomationNameChanges_DoNotTouchDirtyOrSaveState()
    {
        var src = ReadNoteNestCodeBehind();
        var collapseStart = src.IndexOf("public void CollapseRightPane()", System.StringComparison.Ordinal);
        var expandStart = src.IndexOf("public void ExpandRightPane()", System.StringComparison.Ordinal);
        var collapseBody = ExtractMethodBody(src, collapseStart);
        var expandBody = ExtractMethodBody(src, expandStart);

        Assert.DoesNotContain("IsModified", collapseBody);
        Assert.DoesNotContain("IsModified", expandBody);
        Assert.DoesNotContain(".Save(", collapseBody);
        Assert.DoesNotContain(".Save(", expandBody);
    }

    // ── 11. K-6（アクセスキー重複）はSH-45の対象外だった ──────────────────
    // SH-45時点では新規作成(_N)/名前を付けて保存(_N)、このタブへ戻す(_R)/右側のタブを
    // 閉じる(_R) の重複は意図的に未修正のまま残していた。これらはK-6としてSH-46
    // （v2.19.15）で解消済みのため、このテストはSH-45が新規作成(_N)自体を変更して
    // いないことのみを確認する（重複の有無はSH46MenuAccessKeyTestsが検証する）。

    [Fact]
    public void SH45_DidNotTouchNewMenuMnemonic()
    {
        var shellXaml = ReadShellXaml();
        Assert.Contains("Header=\"新規作成(_N)\"", shellXaml);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static string ExtractMethodBody(string source, int methodStart)
    {
        var braceStart = source.IndexOf('{', methodStart);
        Assert.True(braceStart >= 0);
        var depth = 0;
        var i = braceStart;
        for (; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0) break;
            }
        }
        return source.Substring(braceStart, i - braceStart + 1);
    }
}
