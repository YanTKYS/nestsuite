using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NestSuite.ChatNest;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.21.0 LK-4: ChatNest 発言 → IdeaNest カード化。
/// 設計正本 docs/planning/workspace-manual-transfer-helper-design.md（TD-92 / v2.20.1）に対する
/// 実装の回帰確認。
///
/// <para>Shell（<see cref="NestSuiteShellWindow"/>）は WPF <c>Window</c> であり、本リポジトリの
/// 既存テスト（例: <c>WorkspaceTabHelperTests</c>）と同じ方針で、インスタンス化を伴わないリフレクション
/// ベースの契約確認に限定する。実際の転送先解決・0/1/複数件分岐・タブ dirty 反映・保存は、
/// 既存の TN-3 と同様に実機／UI Smoke での確認に委ねる（本ファイルではその境界を超えない）。
/// 本文マッピング・IdeaNest 側受入 API・共通型の契約は Window に依存しないため、ここで直接検証する。</para>
/// </summary>
public class LK4ChatNestToIdeaNestTransferTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;
    private static readonly BindingFlags InstanceNonPublic =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    // ── 1. DTO: WorkspaceTransferContent は Title / Body のみ ────────────────

    [Fact]
    public void WorkspaceTransferContent_HasOnlyTitleAndBody()
    {
        var type = typeof(NestSuiteShellWindow).GetNestedType("WorkspaceTransferContent", BindingFlags.NonPublic);
        Assert.NotNull(type);

        var properties = type!.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToArray();

        Assert.Equal(new[] { "Body", "Title" }, properties);
        Assert.Equal(typeof(string), type.GetProperty("Title")!.PropertyType);
        Assert.Equal(typeof(string), type.GetProperty("Body")!.PropertyType);
    }

    // ── 2. WorkspaceTransferTarget: Kind + TabId + DisplayName ───────────────

    [Fact]
    public void WorkspaceTransferTarget_HasKindTabIdAndDisplayName()
    {
        var type = typeof(NestSuiteShellWindow).GetNestedType("WorkspaceTransferTarget", BindingFlags.NonPublic);
        Assert.NotNull(type);

        var properties = type!.GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(p => p.Name).ToArray();
        Assert.Contains("Kind", properties);
        Assert.Contains("TabId", properties);
        Assert.Contains("DisplayName", properties);
    }

    // ── 3. WorkspaceTransferResult: 5値のみ（Canceled 等を増やさない） ───────

    [Fact]
    public void WorkspaceTransferResult_HasExactlyFiveValues()
    {
        var type = typeof(NestSuiteShellWindow).GetNestedType("WorkspaceTransferResult", BindingFlags.NonPublic);
        Assert.NotNull(type);
        Assert.True(type!.IsEnum);

        var names = Enum.GetNames(type);
        Assert.Equal(
            new[] { "Success", "NoTarget", "InvalidContent", "TargetRejected", "Failed" },
            names);
    }

    // ── 4. 共通ヘルパーのメソッド存在確認（Window非依存の契約のみ） ─────────

    [Fact]
    public void NestSuiteShellWindow_HasEnumerateTransferTargetsMethod()
    {
        var method = typeof(NestSuiteShellWindow).GetMethod("EnumerateTransferTargets", InstanceNonPublic);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasTransferToWorkspaceTabMethod()
    {
        var method = typeof(NestSuiteShellWindow).GetMethod("TransferToWorkspaceTab", InstanceNonPublic);
        Assert.NotNull(method);
        Assert.True(method!.IsGenericMethodDefinition);
    }

    [Fact]
    public void NestSuiteShellWindow_HasChatNestToIdeaNestWiringMethods()
    {
        Assert.NotNull(typeof(NestSuiteShellWindow).GetMethod("WireChatNestToIdeaNestTransfer", InstanceNonPublic));
        Assert.NotNull(typeof(NestSuiteShellWindow).GetMethod("TransferChatNestMessageToIdeaNest", InstanceNonPublic));
    }

    [Fact]
    public void WorkspaceTransferTargetDialog_TypeExists_WithExpectedMembers()
    {
        // 複数件選択ダイアログ自体も WPF Window のため、他の Dialog 型と同じ方針でインスタンス化はせず
        // 型・メンバーの存在のみを確認する。SelectedTarget は WorkspaceTransferTarget（internal）を
        // 公開型より外へ露出させないため internal（CS0053 回避）。
        var type = typeof(NestSuite.Dialogs.WorkspaceTransferTargetDialog);
        Assert.NotNull(type.GetProperty("SelectedTarget", InstanceNonPublic));
    }

    // ── 4-1. LK-4-1 (v2.21.1): WorkspaceTransferTargetDialog の AutomationName 補正 ──

    private static string ReadWorkspaceTransferTargetDialogXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "Dialogs", "WorkspaceTransferTargetDialog.xaml"));

    [Fact]
    public void TargetListBox_AutomationId_IsUnchanged()
    {
        var xaml = ReadWorkspaceTransferTargetDialogXaml();
        Assert.Contains("AutomationProperties.AutomationId=\"Dialog.WorkspaceTransferTargetList\"", xaml);
    }

    [Fact]
    public void TargetListBox_AutomationName_IsNotTheInternalId_AndIsMeaningful()
    {
        var xaml = ReadWorkspaceTransferTargetDialogXaml();
        Assert.DoesNotContain("AutomationProperties.Name=\"Dialog.WorkspaceTransferTargetList\"", xaml);

        var start = xaml.IndexOf("x:Name=\"TargetList\"", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = xaml.IndexOf("/>", start, StringComparison.Ordinal);
        var element = xaml.Substring(start, end - start);

        var nameStart = element.IndexOf("AutomationProperties.Name=\"", StringComparison.Ordinal);
        Assert.True(nameStart >= 0, "ListBox に AutomationProperties.Name が設定されていない");
        nameStart += "AutomationProperties.Name=\"".Length;
        var nameEnd = element.IndexOf('"', nameStart);
        var name = element.Substring(nameStart, nameEnd - nameStart);

        Assert.NotEqual("Dialog.WorkspaceTransferTargetList", name);
        Assert.DoesNotContain("Dialog.", name, StringComparison.Ordinal);
        Assert.NotEmpty(name);
    }

    [Fact]
    public void OkButton_AutomationId_IsUnchanged_AndAutomationNameIsNotInternalId()
    {
        var xaml = ReadWorkspaceTransferTargetDialogXaml();
        Assert.Contains("AutomationProperties.AutomationId=\"Dialog.OkButton\"", xaml);
        Assert.DoesNotContain("AutomationProperties.Name=\"Dialog.OkButton\"", xaml);
        Assert.Contains("Content=\"OK\"", xaml);
    }

    [Fact]
    public void CancelButton_AutomationId_IsUnchanged_AndAutomationNameIsNotInternalId()
    {
        var xaml = ReadWorkspaceTransferTargetDialogXaml();
        Assert.Contains("AutomationProperties.AutomationId=\"Dialog.CancelButton\"", xaml);
        Assert.DoesNotContain("AutomationProperties.Name=\"Dialog.CancelButton\"", xaml);
        Assert.Contains("Content=\"キャンセル\"", xaml);
    }

    [Fact]
    public void ExistingAccessibilityAndKeyboardContract_IsUnchanged()
    {
        var xaml = ReadWorkspaceTransferTargetDialogXaml();

        // OK = IsDefault、キャンセル = IsCancel（Enter/Escape の既存動作を担保する契約）
        Assert.Contains("IsDefault=\"True\"", xaml);
        Assert.Contains("IsCancel=\"True\"", xaml);
        Assert.Contains("MouseDoubleClick=\"TargetList_MouseDoubleClick\"", xaml);
    }

    [Fact]
    public void WorkspaceTransferTargetDialog_InitialFocusGoesToListBox_Unchanged()
    {
        var src = File.ReadAllText(Path.Combine(
            RepoRoot, "NestSuite", "Dialogs", "WorkspaceTransferTargetDialog.xaml.cs"));
        Assert.Contains("TargetList.Focus()", src);
    }

    // ── 5. TN-3 非干渉: 既存シグネチャが変更されていないこと ─────────────────

    [Fact]
    public void NestSuiteShellWindow_Tn3PromotionMethod_StillExists_Unchanged()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("PromoteTempNestSlotToNoteNest", InstanceNonPublic, null,
                [typeof(NestSuite.TempNest.TempNestSlotViewModel)], null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool?), method!.ReturnType);
    }

    // ── 6. ChatNest 本文マッピング: Title=null、Body=本文全文のみ ────────────

    [Fact]
    public void TransferToIdeaNestCommand_PassesMessageTextOnly_NoSpeakerOrTimestampOrHeader()
    {
        var vm = new ChatNestWorkspaceViewModel();
        string? captured = null;
        vm.TransferMessageToIdeaNestRequested = body => captured = body;

        vm.LoadMessages(new[]
        {
            new Message { Speaker = Speaker.自分, Text = "アイデアの断片" },
        });

        vm.Messages[0].TransferToIdeaNestCommand.Execute(null);

        Assert.Equal("アイデアの断片", captured);
        Assert.DoesNotContain("自分", captured);
        Assert.DoesNotContain("[NOTE]", captured);
        Assert.DoesNotContain("ChatNestからの転記", captured);
    }

    [Fact]
    public void TransferToIdeaNestCommand_PassesFullMultilineBody()
    {
        var vm = new ChatNestWorkspaceViewModel();
        string? captured = null;
        vm.TransferMessageToIdeaNestRequested = body => captured = body;

        vm.LoadMessages(new[]
        {
            new Message { Speaker = Speaker.補足, Text = "1行目\n2行目\n3行目" },
        });

        vm.Messages[0].TransferToIdeaNestCommand.Execute(null);

        Assert.Equal("1行目\n2行目\n3行目", captured);
    }

    [Fact]
    public void TransferToIdeaNestCommand_IsAlwaysExecutable()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages(new[] { new Message { Speaker = Speaker.自分, Text = "本文" } });

        Assert.True(vm.Messages[0].TransferToIdeaNestCommand.CanExecute(null));
    }

    [Fact]
    public void ChatNestWorkspaceViewModel_DoesNotReferenceIdeaNestTypes()
    {
        // コマンド名・コメントに「IdeaNest」の語自体が出るのは許容する（利用者向け操作名として自然）。
        // ここで禁じるのは IdeaNest の実際の型参照（using / 具体型名）であり、
        // ChatNest 側が IdeaNest の内部構造・ViewModel を直接触っていないことを確認する。
        var src = File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "ChatNest", "ChatNestWorkspaceViewModel.cs"));
        Assert.DoesNotContain("IdeaNestWorkspaceViewModel", src);
        Assert.DoesNotContain("using NestSuite.IdeaNest", src);

        var msgSrc = File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "ChatNest", "MessageViewModel.cs"));
        Assert.DoesNotContain("IdeaNestWorkspaceViewModel", msgSrc);
        Assert.DoesNotContain("using NestSuite.IdeaNest", msgSrc);
    }

    // ── 7. IdeaNest 受入 API: AddCardFromTransfer ────────────────────────────

    [Fact]
    public void AddCardFromTransfer_TitleNull_UsesIdeaNestExistingTitleGeneration_FromBodyFirstLine()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace());

        var ok = vm.AddCardFromTransfer(null, "先頭行がタイトルになる\n本文2行目");

        Assert.True(ok);
        var card = Assert.Single(vm.AllCards);
        Assert.Equal("先頭行がタイトルになる", card.Title);
        Assert.Equal("先頭行がタイトルになる\n本文2行目", card.Body);
    }

    [Fact]
    public void AddCardFromTransfer_MarksIdeaNestDirty_OnSuccess()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace());
        Assert.False(vm.HasChanges);

        var ok = vm.AddCardFromTransfer(null, "本文");

        Assert.True(ok);
        Assert.True(vm.HasChanges);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\n")]
    public void AddCardFromTransfer_EmptyOrWhitespaceBody_DoesNotAddCard_AndStaysNotDirty(string body)
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace());

        var ok = vm.AddCardFromTransfer(null, body);

        Assert.False(ok);
        Assert.Empty(vm.AllCards);
        Assert.False(vm.HasChanges);
    }

    [Fact]
    public void AddCardFromTransfer_DoesNotUseCommitAddFromText_NoAutoPasteTitleGeneration()
    {
        // LK-4 は CommitAddFromText（Paste_yyyyMMddHHmm 等のタイトル自動生成、[NOTE]ヘッダ検出）を
        // 使わない。AddCardFromTransfer のソース自体がそれを呼んでいないことを確認する。
        // AddCardFromTransfer は式形式（=>）のメンバーのため、次の宣言（次の public/private）までの
        // 範囲を対象にする（ExtractMethodBody のブレースバランス方式は式形式には使えない）。
        var src = File.ReadAllText(Path.Combine(
            RepoRoot, "NestSuite", "NestSuite", "IdeaNest", "ViewModels", "IdeaNestWorkspaceViewModel.cs"));
        var start = src.IndexOf("public bool AddCardFromTransfer", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = src.IndexOf(';', start);
        Assert.True(end >= 0);
        var body = src.Substring(start, end - start + 1);

        Assert.DoesNotContain("CommitAddFromText", body);
        Assert.Contains("CommitAdd(", body);
    }

    // ── 8. ChatNest ContextMenu: 項目追加・アクセスキー一意性・配置 ──────────

    private static string ReadChatNestXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "ChatNest", "ChatNestWorkspaceView.xaml"));

    private static string ExtractMessageContextMenu(string xaml)
    {
        var start = xaml.IndexOf("<StackPanel.ContextMenu>", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = xaml.IndexOf("</StackPanel.ContextMenu>", start, StringComparison.Ordinal);
        Assert.True(end >= 0);
        return xaml.Substring(start, end - start + "</StackPanel.ContextMenu>".Length);
    }

    [Fact]
    public void MessageContextMenu_ContainsAddToIdeaNestItem()
    {
        var menu = ExtractMessageContextMenu(ReadChatNestXaml());
        Assert.Contains("Header=\"IdeaNestカードに追加(_I)\"", menu);
        Assert.Contains("Command=\"{Binding TransferToIdeaNestCommand}\"", menu);
    }

    [Fact]
    public void MessageContextMenu_AddToIdeaNestItem_IsPlacedInSingleMessageOperationGroup()
    {
        var menu = ExtractMessageContextMenu(ReadChatNestXaml());

        var deleteIdx = menu.IndexOf("Header=\"削除(_D)\"", StringComparison.Ordinal);
        var transferIdx = menu.IndexOf("Header=\"IdeaNestカードに追加(_I)\"", StringComparison.Ordinal);
        var separatorIdx = menu.IndexOf("<Separator/>", StringComparison.Ordinal);
        var copyConversationIdx = menu.IndexOf("Header=\"会話をコピー(_K)\"", StringComparison.Ordinal);

        Assert.True(deleteIdx >= 0 && transferIdx >= 0 && separatorIdx >= 0 && copyConversationIdx >= 0);
        // 削除(_D) < IdeaNestカードに追加(_I) < 最初のSeparator < 会話をコピー(_K)
        Assert.True(deleteIdx < transferIdx);
        Assert.True(transferIdx < separatorIdx);
        Assert.True(separatorIdx < copyConversationIdx);
    }

    [Fact]
    public void MessageContextMenu_ExistingItemsHeaderCommandOrder_Unchanged()
    {
        var menu = ExtractMessageContextMenu(ReadChatNestXaml());

        Assert.Contains("Header=\"本文をコピー(_C)\" Command=\"{Binding CopyMessageCommand}\"", menu);
        Assert.Contains("Header=\"編集(_E)\" Command=\"{Binding BeginEditCommand}\"", menu);
        Assert.Contains("Header=\"削除(_D)\" Command=\"{Binding RequestDeleteCommand}\"", menu);
        Assert.Contains("Header=\"会話をMarkdownでコピー(_M)\"", menu);
        Assert.Contains("Header=\"NestSuite形式でコピー(_N)\"", menu);
        Assert.Contains("Header=\"会話を保存...(_S)\"", menu);
        Assert.Contains("Header=\"時刻を表示(_T)\"", menu);
    }

    [Fact]
    public void MessageContextMenu_AccessKeys_AreUnique()
    {
        var menu = ExtractMessageContextMenu(ReadChatNestXaml());
        var keys = ExtractAccessKeys(menu);

        Assert.Contains('I', keys);
        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    // ── 9. CH-19 契約維持: フォーカス可能化・既存ContextMenu共用 ────────────

    [Fact]
    public void CH19FocusabilityContract_StillHolds_AfterMenuItemAdded()
    {
        var xaml = ReadChatNestXaml();
        var anchor = xaml.IndexOf("MaxWidth=\"580\"", StringComparison.Ordinal);
        Assert.True(anchor >= 0);
        var start = xaml.LastIndexOf("<StackPanel", anchor, StringComparison.Ordinal);
        var end = xaml.IndexOf("</StackPanel.ContextMenu>", anchor, StringComparison.Ordinal);
        var element = xaml.Substring(start, end - start + "</StackPanel.ContextMenu>".Length);

        Assert.Contains("Focusable=\"True\"", element);
        Assert.Contains("KeyboardNavigation.IsTabStop=\"True\"", element);
        Assert.Contains(
            "<ContextMenu DataContext=\"{Binding PlacementTarget.DataContext, RelativeSource={RelativeSource Self}}\">",
            element);
        // ContextMenu定義が1つのままであること（キーボード専用の別ContextMenuを新設していない）
        Assert.Equal(1, CountOccurrences(element, "<ContextMenu"));
    }

    // ── 10. 保存形式・schema 非変更（IdeaNest 側） ───────────────────────────

    [Fact]
    public void AddCardFromTransfer_DoesNotChangeIdeaSchemaShape()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace());
        vm.AddCardFromTransfer(null, "本文");

        var saved = vm.BuildWorkspaceForSave();
        Assert.Equal(IdeaNestSchema.CurrentVersion, saved.Version);
        Assert.Single(saved.Ideas);
        Assert.Empty(saved.Ideas[0].Tags);
        Assert.Equal("yellow", saved.Ideas[0].Color);
    }

    // ── 11. docs: TD-92 → LK-4 の関係・backlog・release notes ───────────────

    [Fact]
    public void DesignDoc_RecordsLk4ImplementationResult()
    {
        var doc = File.ReadAllText(Path.Combine(
            RepoRoot, "docs", "planning", "workspace-manual-transfer-helper-design.md"));

        Assert.Contains("v2.21.0", doc);
        Assert.Contains("LK-4", doc);
        Assert.Contains("実装結果", doc);
    }

    [Fact]
    public void ReleaseNotes_RecordsV2210Lk4()
    {
        var notes = TestPaths.ReadReleaseNotes();

        Assert.Contains("v2.21.0", notes);
        Assert.Contains("LK-4", notes);
    }

    [Fact]
    public void Backlog_DoesNotContainLk4AsOpenItem()
    {
        var backlog = TestPaths.ReadBacklog();

        Assert.False(backlog.Contains("| LK-4 |", StringComparison.Ordinal),
            "backlog.md に LK-4 が open item の表行として残っている");
    }

    [Fact]
    public void Backlog_Lk2_NoLongerOpenItem_ImplementedInLaterVersion()
    {
        // LK-3 は v2.22.0 で実装済みとなり open backlog から削除された
        // （LK3TempNestToIdeaNestTransferTests.Backlog_DoesNotContainLk3AsOpenItem 側で確認する）。
        // LK-2 はその後 v2.23.0 で実装され、同様に open backlog から削除された
        // （LK2TempNestToNoteNestTransferTests.Backlog_DoesNotContainLk2AsOpenItem 側で確認する）。
        var backlog = TestPaths.ReadBacklog();

        Assert.False(backlog.Contains("| LK-2 |", StringComparison.Ordinal));
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static System.Collections.Generic.List<char> ExtractAccessKeys(string region)
    {
        var keys = new System.Collections.Generic.List<char>();
        var idx = 0;
        while (true)
        {
            idx = region.IndexOf("(_", idx, StringComparison.Ordinal);
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
        while ((index = source.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }
}
