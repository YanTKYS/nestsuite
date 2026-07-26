using System.Collections.Generic;
using System.IO;
using System.Linq;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.20.0 TD-91: キーボード・アクセシビリティ横断レビュー（K-1〜K-6、v2.19.9〜v2.19.15）の
/// 総点検・回帰確認。個別対応の契約テスト（SH44/L26/L27/L28TaskKeyboardTests/
/// CH19MessageFocusContextMenuTests/SH45IconButtonAutomationNameTests/SH46MenuAccessKeyTests等）は
/// 削除・統合せずそのまま維持し、ここではそれらが現在のmain上で同時に成立していること・
/// 個別対応間でキー処理が競合していないこと・非対象キーが追加されていないことを横断的に確認する。
/// XAML全体の完全一致や行番号には依存せず、重要なコントロール種別・キー契約・AutomationName・
/// アクセスキーの一意性・docs/versionの状態のみを固定する。
/// </summary>
public class TD91KeyboardAccessibilityCloseoutTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadShellXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.xaml"));

    private static string ReadNoteNestXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml"));

    private static string ReadChatNestXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "ChatNest", "ChatNestWorkspaceView.xaml"));

    private static string ReadAllProductCSharp()
    {
        var nestSuiteDir = Path.Combine(RepoRoot, "NestSuite");
        var sb = new System.Text.StringBuilder();
        foreach (var file in Directory.GetFiles(nestSuiteDir, "*.cs", SearchOption.AllDirectories))
        {
            sb.Append(File.ReadAllText(file));
            sb.Append('\n');
        }
        return sb.ToString();
    }

    // ── 1. K-1〜K-6の完了契約（keyboard review文書） ─────────────────────

    [Fact]
    public void KeyboardReviewDoc_RecordsAllSixItemsAsComplete_WithVersionAndId()
    {
        var doc = File.ReadAllText(Path.Combine(RepoRoot, "docs", "planning", "keyboard-accessibility-cross-review.md"));

        Assert.Contains("K-1", doc);
        Assert.Contains("v2.19.9", doc);
        Assert.Contains("SH-44", doc);

        Assert.Contains("K-2", doc);
        Assert.Contains("v2.19.10", doc);
        Assert.Contains("L26", doc);
        Assert.Contains("v2.19.11", doc);
        Assert.Contains("L27", doc);

        Assert.Contains("K-3", doc);
        Assert.Contains("v2.19.12", doc);
        Assert.Contains("L28", doc);

        Assert.Contains("K-4", doc);
        Assert.Contains("v2.19.13", doc);
        Assert.Contains("CH-19", doc);

        Assert.Contains("K-5", doc);
        Assert.Contains("v2.19.14", doc);
        Assert.Contains("SH-45", doc);

        Assert.Contains("K-6", doc);
        Assert.Contains("v2.19.15", doc);
        Assert.Contains("SH-46", doc);

        Assert.Contains("TD-91", doc);
        Assert.Contains("v2.20.0", doc);
    }

    // ── 2. 対応要素の存在確認 ─────────────────────────────────────────────

    [Fact]
    public void K1_CrossSearchEscapeHandling_StillExists()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.CrossSearch.cs"));
        Assert.Contains("TryHandleCrossSearchEscape", src);
        Assert.Contains("CloseCrossSearchPanel", src);
    }

    [Fact]
    public void K2_MarkerAndLinkListBoxes_StillExist()
    {
        var xaml = ReadNoteNestXaml();
        Assert.Contains("<ListBox", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"NoteNest.MarkerList\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"NoteNest.OutboundLinkList\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"NoteNest.BacklinkList\"", xaml);
        Assert.Contains("ItemsSource=\"{Binding FilteredMarkers}\"", xaml);
    }

    [Fact]
    public void K3_TaskToggleButtonsAndTitleButton_StillExist()
    {
        var xaml = ReadNoteNestXaml();
        Assert.Contains("Command=\"{Binding DataContext.ToggleGroupCommand,", xaml);
        Assert.Contains("Command=\"{Binding ToggleCompletedSectionCommand}\"", xaml);
        Assert.Contains("PreviewKeyDown=\"TaskTitle_PreviewKeyDown\"", xaml);
    }

    [Fact]
    public void K4_ChatNestMessageFocusability_StillExists()
    {
        var xaml = ReadChatNestXaml();
        Assert.Contains("Focusable=\"True\"", xaml);
        Assert.Contains("KeyboardNavigation.IsTabStop=\"True\"", xaml);
    }

    [Fact]
    public void K5_KeyAutomationNames_StillExist()
    {
        var xaml = ReadShellXaml();
        Assert.Contains("AutomationProperties.Name=\"このタブを閉じる\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"横断検索を閉じる\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"新規タブを追加\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"タブ一覧を開く\"", xaml);
    }

    [Fact]
    public void K6_UniqueAccessKeys_StillExist()
    {
        var xaml = ReadShellXaml();
        Assert.Contains("Header=\"名前を付けて保存(_V)...\"", xaml);
        Assert.Contains("Header=\"このタブへ戻す(_B)\"", xaml);
        Assert.Contains("Header=\"右側のタブを閉じる(_R)\" Click=\"TabContextCloseRight_Click\"/>", xaml);
        Assert.DoesNotContain("Header=\"名前を付けて保存(_N)...\"", xaml);
        Assert.DoesNotContain("Header=\"このタブへ戻す(_R)\"", xaml);
    }

    // ── 3. キー競合の非存在 ──────────────────────────────────────────────

    [Fact]
    public void NoGlobalAppsOrF10Handling_WasAddedAcrossAnyK1ToK6Work()
    {
        // Shift+F10／コンテキストメニューキーはWPF標準のContextMenuOpeningバブリングに委ねる
        // 設計（K-2/K-4）であり、Key.Apps・Key.F10を明示的に処理するコードはどこにも存在しない。
        var src = ReadAllProductCSharp();
        Assert.DoesNotContain("Key.Apps", src);
        Assert.DoesNotContain("Key.F10", src);
    }

    [Fact]
    public void OnlyOneGlobalPreviewKeyDownHandler_IsRegisteredInChatNestView()
    {
        // ChatNestWorkspaceViewのコンストラクタで登録されるUIElement.PreviewKeyDownEventの
        // クラス外AddHandlerは1件のみで、他のK対応（K-2/K-3のNoteNest側等）が同種のグローバル
        // ハンドラを重ねて追加していないことを確認する。
        var src = ReadAllProductCSharp();
        var occurrences = CountOccurrences(src, "AddHandler(UIElement.PreviewKeyDownEvent");
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void EnterKeyHandlers_AreEachScopedToTheirOwnNamedMethod_NotSharedOrDuplicated()
    {
        // K-2（マーカー/リンク）・K-3（タスクタイトル）のEnter処理が、それぞれ独立したメソッド名で
        // 定義されており、同一メソッドへ統合されたり重複定義されたりしていないことを確認する。
        var src = ReadAllProductCSharp();
        Assert.Equal(1, CountOccurrences(src, "private void MarkerList_KeyDown"));
        Assert.Equal(1, CountOccurrences(src, "private void OutboundLinkList_KeyDown"));
        Assert.Equal(1, CountOccurrences(src, "private void BacklinkList_KeyDown"));
        Assert.Equal(1, CountOccurrences(src, "private void TaskTitle_PreviewKeyDown"));
    }

    // ── 4. 非対象キーの未追加 ────────────────────────────────────────────

    [Fact]
    public void IdeaNestCard_HasNoDeleteOrSpacePinShortcut()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "IdeaNest", "Views", "IdeaNestWorkspaceView.xaml.cs"));
        Assert.DoesNotContain("Key.Delete", src);
        Assert.DoesNotContain("Key.Space", src);
        Assert.DoesNotContain("Key.Up", src);
        Assert.DoesNotContain("Key.Down", src);
    }

    [Fact]
    public void ChatNest_HasNoEnterEditOrDeleteOrCustomReorderShortcut()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "ChatNest", "ChatNestWorkspaceView.xaml.cs"));
        Assert.DoesNotContain("Key.Delete", src);
        Assert.DoesNotContain("ModifierKeys.Alt", src);
    }

    [Fact]
    public void NoSpaceOrDeleteKeyHandling_ExistsAnywhereInProductCode()
    {
        var src = ReadAllProductCSharp();
        Assert.DoesNotContain("Key.Space", src);
        Assert.DoesNotContain("Key.Delete", src);
    }

    // ── 5. dirty非影響 ───────────────────────────────────────────────────

    [Fact]
    public void K1ToK6EventFiles_DoNotReferenceIsModifiedOrSaveApis()
    {
        var files = new[]
        {
            Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.MarkerEvents.cs"),
            Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.LinkEvents.cs"),
            Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.TaskEvents.cs"),
        };
        foreach (var file in files)
        {
            var src = File.ReadAllText(file);
            Assert.DoesNotContain("IsModified", src);
            Assert.DoesNotContain(".Save(", src);
            Assert.DoesNotContain("AutoSave", src);
            Assert.DoesNotContain("UiSettings", src);
        }
    }

    // ── 6. 保存形式・schema非影響 ────────────────────────────────────────

    [Fact]
    public void NoteNestSchemaVersion_IsUnchangedByTd91()
    {
        // schemaリテラルの直接比較はApplicationVersionTests側の一元管理テストと重複するため、
        // ここではTD-91対応ファイル群がschemaやProject関連のシリアライズ処理へ触れていないことのみ確認する。
        var files = new[]
        {
            Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.MarkerEvents.cs"),
            Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.LinkEvents.cs"),
            Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.TaskEvents.cs"),
        };
        foreach (var file in files)
        {
            var src = File.ReadAllText(file);
            Assert.DoesNotContain("CurrentSchemaVersion", src);
        }
    }

    // ── 7. AutomationName（状態依存の両状態を確認） ──────────────────────

    [Fact]
    public void NoteNestRightPaneExpandButton_SetsBothStateNames()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml.cs"));
        Assert.Contains("AutomationProperties.SetName(RightPaneExpandButton, \"右ペインを表示\");", src);
        Assert.Contains("AutomationProperties.SetName(RightPaneExpandButton, \"右ペインを閉じる\");", src);
    }

    [Fact]
    public void IdeaCardViewModel_PinAndArchiveActionNames_ReflectBothStates()
    {
        var card = new IdeaCardViewModel(new Idea { Title = "T" });
        Assert.Equal("カードをピン留め", card.PinActionName);
        Assert.Equal("カードをアーカイブ", card.ArchiveActionName);

        card.IsPinned = true;
        card.IsArchived = true;
        Assert.Equal("カードのピン留めを解除", card.PinActionName);
        Assert.Equal("カードをアーカイブから戻す", card.ArchiveActionName);
    }

    // ── 8. アクセスキー一意性（ファイルメニュー・タブContextMenu） ──────

    [Fact]
    public void FileMenuAndTabContextMenu_AccessKeys_RemainUnique()
    {
        var xaml = ReadShellXaml();

        var fileMenuRegion = ExtractRegion(xaml, "Header=\"ファイル(_F)\"", "Header=\"終了(_X)\" Click=\"MenuExit_Click\"/>");
        var afterNewMenu = fileMenuRegion.Substring(fileMenuRegion.IndexOf("</MenuItem>", System.StringComparison.Ordinal) + "</MenuItem>".Length);
        var fileKeys = ExtractAccessKeys(afterNewMenu);
        Assert.Equal(fileKeys.Count, fileKeys.Distinct().Count());

        var tabMenuRegion = ExtractRegion(xaml, "<Grid.ContextMenu>", "</Grid.ContextMenu>");
        var tabKeys = ExtractAccessKeys(tabMenuRegion);
        Assert.Equal(tabKeys.Count, tabKeys.Distinct().Count());
    }

    // ── 9/10. docs・version ──────────────────────────────────────────────

    [Fact]
    public void ReleaseNotes_RecordsV2200Td91()
    {
        var notes = TestPaths.ReadReleaseNotes();
        Assert.Contains("v2.20.0", notes);
        Assert.Contains("TD-91", notes);
    }

    [Fact]
    public void Backlog_DoesNotContainTd91AsOpenItem()
    {
        var backlog = TestPaths.ReadBacklog();
        Assert.False(backlog.Contains("| TD-91 |", System.StringComparison.Ordinal));
    }

    // バージョン値自体の確認はApplicationVersionTestsが一元的に担うため、ここでは重複させない。

    // ── helpers ─────────────────────────────────────────────────────────────

    private static string ExtractRegion(string xaml, string startAnchor, string endAnchor)
    {
        var start = xaml.IndexOf(startAnchor, System.StringComparison.Ordinal);
        Assert.True(start >= 0, $"start anchor not found: {startAnchor}");
        var end = xaml.IndexOf(endAnchor, start, System.StringComparison.Ordinal);
        Assert.True(end >= 0, $"end anchor not found: {endAnchor}");
        return xaml.Substring(start, end - start + endAnchor.Length);
    }

    private static List<char> ExtractAccessKeys(string region)
    {
        var keys = new List<char>();
        var idx = 0;
        while (true)
        {
            idx = region.IndexOf("(_", idx, System.StringComparison.Ordinal);
            if (idx < 0) break;
            keys.Add(char.ToUpperInvariant(region[idx + 2]));
            idx += 2;
        }
        return keys;
    }

    private static int CountOccurrences(string source, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(needle, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }
}
