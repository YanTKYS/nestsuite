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
/// 転送契約（NestSuiteShellWindow.WorkspaceTransfer.cs）に対する実装の回帰確認。
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
    public void MessageContextMenu_AccessKeys_AreUnique()
    {
        var menu = ExtractMessageContextMenu(ReadChatNestXaml());
        var keys = ExtractAccessKeys(menu);

        Assert.Contains('I', keys);
        Assert.Equal(keys.Count, keys.Distinct().Count());
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
