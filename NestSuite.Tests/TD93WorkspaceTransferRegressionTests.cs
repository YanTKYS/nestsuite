using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NestSuite.Dialogs;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.TempNest;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.24.0 TD-93: Workspace 間手動転送シリーズ（TN-3・LK-4・LK-3・LK-2）の横断総点検・回帰確認。
/// 設計正本 docs/planning/workspace-manual-transfer-helper-design.md §25。
///
/// <para>本クラスは個別機能テスト（<see cref="TempNestTests"/>・<see cref="LK4ChatNestToIdeaNestTransferTests"/>・
/// <see cref="LK3TempNestToIdeaNestTransferTests"/>・<see cref="LK2TempNestToNoteNestTransferTests"/>・
/// <see cref="TD92WorkspaceTransferDesignDocsTests"/>）を置き換えず、4 本の転送を横断する契約
/// （共通ヘルパーの責務境界・TN-3 との差異・自動保存/自動タブ切替非実装・AutomationName 補正・
/// docs/version 整合）だけを対象にする。</para>
/// </summary>
public class TD93WorkspaceTransferRegressionTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;
    private static readonly BindingFlags InstanceNonPublic =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static string ReadSource(params string[] relativeParts) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot }.Concat(relativeParts).ToArray()));

    // ── A. 共通型: WorkspaceTransferContent / WorkspaceTransferResult ────────

    [Fact]
    public void WorkspaceTransferContent_StillHasOnlyTitleAndBody()
    {
        var type = typeof(NestSuiteShellWindow).GetNestedType("WorkspaceTransferContent", BindingFlags.NonPublic);
        Assert.NotNull(type);

        var properties = type!.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToArray();

        Assert.Equal(new[] { "Body", "Title" }, properties);
    }

    [Fact]
    public void WorkspaceTransferResult_StillHasExactlyFiveValues()
    {
        var type = typeof(NestSuiteShellWindow).GetNestedType("WorkspaceTransferResult", BindingFlags.NonPublic);
        Assert.NotNull(type);
        Assert.True(type!.IsEnum);
        Assert.Equal(
            new[] { "Success", "NoTarget", "InvalidContent", "TargetRejected", "Failed" },
            Enum.GetNames(type));
    }

    [Fact]
    public void WorkspaceTransferTarget_IdentifiesByTabId_NotByDisplayName()
    {
        var type = typeof(NestSuiteShellWindow).GetNestedType("WorkspaceTransferTarget", BindingFlags.NonPublic);
        Assert.NotNull(type);
        var properties = type!.GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(p => p.Name).ToArray();
        Assert.Contains("TabId", properties);
        Assert.Contains("Kind", properties);
        Assert.Contains("DisplayName", properties);
    }

    // ── B. 共通ヘルパー: 3本(LK-4/LK-3/LK-2)が同一ヘルパーを再利用している ──

    [Fact]
    public void CommonHelper_File_DefinesEnumerateTargetsAndTransferMethod_Once()
    {
        // 共通ヘルパー本体は NestSuiteShellWindow.WorkspaceTransfer.cs の1箇所だけに定義され、
        // LK-4/LK-3/LK-2 の各導線ファイルは呼び出すだけで再実装しない。
        var helperSrc = ReadSource("NestSuite", "NestSuite", "NestSuiteShellWindow.WorkspaceTransfer.cs");
        Assert.Contains("private IReadOnlyList<WorkspaceTransferTarget> EnumerateTransferTargets(", helperSrc);
        Assert.Contains("private WorkspaceTransferResult TransferToWorkspaceTab<TViewModel>(", helperSrc);

        foreach (var file in new[]
        {
            "NestSuiteShellWindow.ChatNestToIdeaNest.cs",
            "NestSuiteShellWindow.TempNestToIdeaNest.cs",
            "NestSuiteShellWindow.TempNestToNoteNest.cs",
        })
        {
            var src = ReadSource("NestSuite", "NestSuite", file);
            Assert.DoesNotContain("private IReadOnlyList<WorkspaceTransferTarget> EnumerateTransferTargets(", src);
            Assert.DoesNotContain("private WorkspaceTransferResult TransferToWorkspaceTab<TViewModel>(", src);
            Assert.Contains("EnumerateTransferTargets(", src);
            Assert.Contains("TransferToWorkspaceTab<", src);
        }
    }

    [Fact]
    public void CommonHelper_HasNoFileIOOrSaveOrActivateTab()
    {
        var src = ReadSource("NestSuite", "NestSuite", "NestSuiteShellWindow.WorkspaceTransfer.cs");
        Assert.DoesNotContain("ActivateTab", src);
        Assert.DoesNotContain(".Save(", src);
        Assert.DoesNotContain(".SaveAs(", src);
        Assert.DoesNotContain("SaveSessionAfterTabChange", src);
        Assert.DoesNotContain("File.Write", src);
        Assert.DoesNotContain("AtomicFileWriter", src);
        Assert.DoesNotContain("IsModified =", src);
        Assert.DoesNotContain("HasChanges =", src);
    }

    [Theory]
    [InlineData("NestSuiteShellWindow.ChatNestToIdeaNest.cs")]
    [InlineData("NestSuiteShellWindow.TempNestToIdeaNest.cs")]
    [InlineData("NestSuiteShellWindow.TempNestToNoteNest.cs")]
    public void TransferLine_HasNoAutoSaveOrAutoTabSwitch(string file)
    {
        // LK-4/LK-3/LK-2 の各導線に、自動保存・自動タブ切替の呼び出しがないことを確認する。
        var src = ReadSource("NestSuite", "NestSuite", file);
        Assert.DoesNotContain("ActivateTab", src);
        Assert.DoesNotContain(".Save(", src);
        Assert.DoesNotContain(".SaveAs(", src);
        Assert.DoesNotContain("SaveSessionAfterTabChange", src);
        Assert.DoesNotContain("File.Write", src);
    }

    [Theory]
    [InlineData("NestSuiteShellWindow.ChatNestToIdeaNest.cs")]
    [InlineData("NestSuiteShellWindow.TempNestToIdeaNest.cs")]
    [InlineData("NestSuiteShellWindow.TempNestToNoteNest.cs")]
    public void TransferLine_ZeroCandidates_DoesNotCreateNewTab(string file)
    {
        // 0件時に NestSuiteTabFactory を使って新規タブを自動生成していないことを確認する。
        var src = ReadSource("NestSuite", "NestSuite", file);
        Assert.DoesNotContain("NestSuiteTabFactory", src);
        Assert.Contains("candidates.Count == 0", src);
    }

    // ── C. TN-3 は共通ヘルパーへ統合されていない ─────────────────────────

    [Fact]
    public void TN3_DoesNotUseCommonHelper_AndStillUsesTabFactoryRollbackActivateTab()
    {
        var src = ReadSource("NestSuite", "NestSuite", "NestSuiteShellWindow.TempNestPromotion.cs");

        // TN-3 は共通ヘルパー(EnumerateTransferTargets/TransferToWorkspaceTab)を使わない。
        Assert.DoesNotContain("EnumerateTransferTargets(", src);
        Assert.DoesNotContain("TransferToWorkspaceTab<", src);

        // TN-3 固有の新規タブ生成・rollback・session登録・ActivateTab はそのまま残っている。
        Assert.Contains("NestSuiteTabFactory.CreateUntitled", src);
        Assert.Contains("RollbackFailedNoteNestPromotion", src);
        Assert.Contains("_sessionManager.Add(session)", src);
        Assert.Contains("ActivateTab(currentTab)", src);
        Assert.Contains("SaveSessionAfterTabChange()", src);
    }

    [Fact]
    public void NestSuiteShellWindow_HasAllFourWiringMethods()
    {
        // TN-3・LK-4・LK-3・LK-2 のいずれも VM 生成時に個別配線され、共有の汎用配線メソッドへ
        // 統合されていないことを確認する（TN-3 を無理に共通化していないことの間接確認）。
        Assert.NotNull(typeof(NestSuiteShellWindow).GetMethod("WireTempNestPromotion", InstanceNonPublic));
        Assert.NotNull(typeof(NestSuiteShellWindow).GetMethod("WireChatNestToIdeaNestTransfer", InstanceNonPublic));
        Assert.NotNull(typeof(NestSuiteShellWindow).GetMethod("WireTempNestToIdeaNestTransfer", InstanceNonPublic));
        Assert.NotNull(typeof(NestSuiteShellWindow).GetMethod("WireTempNestToNoteNestTransfer", InstanceNonPublic));
    }

    [Fact]
    public void ShellXamlCs_WiresAllTempNestTransfersTogether_AtVmCreation()
    {
        var src = ReadSource("NestSuite", "NestSuite", "NestSuiteShellWindow.xaml.cs");
        Assert.Contains("WireTempNestPromotion(vm)", src);
        Assert.Contains("WireTempNestToIdeaNestTransfer(vm)", src);
        Assert.Contains("WireTempNestToNoteNestTransfer(vm)", src);
    }

    [Fact]
    public void ChatNestToIdeaNestTransfer_IsWired_AtEveryChatNestViewModelCreationSite()
    {
        // ChatNestWorkspaceViewModel はメイン生成箇所に加えて FileOpen 経路でも生成されるため、
        // すべての生成箇所で転送要求が配線されていることを確認する（配線漏れは無言の機能欠落になる）。
        var xamlCs = ReadSource("NestSuite", "NestSuite", "NestSuiteShellWindow.xaml.cs");
        var fileOpen = ReadSource("NestSuite", "NestSuite", "NestSuiteShellWindow.FileOpen.cs");

        var creationSites = new[] { xamlCs, fileOpen }
            .SelectMany(src => System.Text.RegularExpressions.Regex.Matches(src, "new ChatNestWorkspaceViewModel\\("))
            .Count();
        var wiringCalls = new[] { xamlCs, fileOpen }
            .SelectMany(src => System.Text.RegularExpressions.Regex.Matches(src, "WireChatNestToIdeaNestTransfer\\("))
            .Count();

        Assert.True(creationSites >= 1);
        Assert.Equal(creationSites, wiringCalls);
    }

    // ── D. TempNest スロットのコマンド契約: TN-3/LK-3/LK-2 は同じ形の delegate ──

    [Fact]
    public void TempNestSlotViewModel_HasAllThreeRequestDelegates_WithSameShape()
    {
        var type = typeof(TempNestSlotViewModel);
        var promote = type.GetProperty("PromoteRequested");
        var toIdea = type.GetProperty("TransferToIdeaNestRequested");
        var toNote = type.GetProperty("TransferToNoteNestRequested");

        Assert.NotNull(promote);
        Assert.NotNull(toIdea);
        Assert.NotNull(toNote);
        Assert.Equal(promote!.PropertyType, toIdea!.PropertyType);
        Assert.Equal(promote.PropertyType, toNote!.PropertyType);
        Assert.Equal(typeof(Func<TempNestSlotViewModel, bool?>), promote.PropertyType);
    }

    [Fact]
    public void TempNestSlotViewModel_Dispose_ClearsAllThreeRequestDelegates()
    {
        var slot = new TempNestSlotViewModel();
        slot.PromoteRequested = _ => null;
        slot.TransferToIdeaNestRequested = _ => null;
        slot.TransferToNoteNestRequested = _ => null;

        slot.Dispose();

        Assert.Null(slot.PromoteRequested);
        Assert.Null(slot.TransferToIdeaNestRequested);
        Assert.Null(slot.TransferToNoteNestRequested);
    }

    // ── E. 受入側の契約: IdeaNest / NoteNest ──────────────────────────────

    [Fact]
    public void IdeaNest_AddCardFromTransfer_MarksDirty_OnSuccess_NotOnRejection()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        Assert.False(vm.HasChanges);

        var rejected = vm.AddCardFromTransfer(null, "   ");
        Assert.False(rejected);
        Assert.False(vm.HasChanges);

        var accepted = vm.AddCardFromTransfer(null, "本文");
        Assert.True(accepted);
        Assert.True(vm.HasChanges);
    }

    [Fact]
    public void NoteNest_CreateNoteFromTransfer_TwoArgOverload_And_OneArgWrapper_BothExist()
    {
        var type = typeof(MainViewModel);
        var oneArg = type.GetMethod("CreateNoteFromTransfer", new[] { typeof(string) });
        var twoArg = type.GetMethod("CreateNoteFromTransfer", new[] { typeof(string), typeof(string) });
        Assert.NotNull(oneArg);
        Assert.NotNull(twoArg);
    }

    [Fact]
    public void NoteNest_CreateNoteFromTransfer_OneArgOverload_DelegatesToTwoArg_WithNullTitle()
    {
        var main = new MainViewModel();
        var note = main.CreateNoteFromTransfer("本文行1\n本文行2");

        Assert.NotNull(note);
        Assert.Equal("本文行1", note!.Title);
        Assert.Equal("本文行1\n本文行2", note.Content);
    }

    // ── F. WorkspaceTransferTargetDialog: 転送先種別ごとの AutomationName ────

    [Fact]
    public void WorkspaceTransferTargetDialog_Constructor_HasListAutomationNameParameter()
    {
        var ctor = typeof(WorkspaceTransferTargetDialog).GetConstructors(InstanceNonPublic).FirstOrDefault();
        Assert.NotNull(ctor);
        var paramNames = ctor!.GetParameters().Select(p => p.Name).ToArray();
        Assert.Contains("listAutomationName", paramNames);
        // 既存パラメーター(windowTitle/promptText)は維持されている。
        Assert.Contains("windowTitle", paramNames);
        Assert.Contains("promptText", paramNames);
    }

    [Theory]
    [InlineData("NestSuiteShellWindow.ChatNestToIdeaNest.cs", "転送先のIdeaNestタブ一覧")]
    [InlineData("NestSuiteShellWindow.TempNestToIdeaNest.cs", "転送先のIdeaNestタブ一覧")]
    [InlineData("NestSuiteShellWindow.TempNestToNoteNest.cs", "転送先のNoteNestタブ一覧")]
    public void TransferLine_PassesTargetSpecificListAutomationName(string file, string expectedName)
    {
        // LK-2 で WorkspaceTransferTargetDialog が NoteNest 転送にも再利用されるようになったため、
        // 各呼出元は転送先種別に応じた listAutomationName を明示していなければならない
        // （固定文字列のままだと NoteNest 転送でも「IdeaNest」と読み上げられてしまう）。
        var src = ReadSource("NestSuite", "NestSuite", file);
        Assert.Contains($"listAutomationName: \"{expectedName}\"", src);
    }

    [Fact]
    public void WorkspaceTransferTargetDialogXaml_ListBoxAutomationId_IsUnchanged()
    {
        var xaml = ReadSource("NestSuite", "Dialogs", "WorkspaceTransferTargetDialog.xaml");
        Assert.Contains("AutomationProperties.AutomationId=\"Dialog.WorkspaceTransferTargetList\"", xaml);
    }

    // ── G. TempNest スロットボタンの AutomationName: 内部IDを流用しない ──────

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void TempNestSlotButtons_VisibleTextButtons_DoNotSetAutomationNameToInternalId(int slot)
    {
        var xaml = ReadSource("NestSuite", "NestSuite", "TempNest", "TempNestWorkspaceView.xaml");

        foreach (var button in new[] { "CopyButton", "ClearButton", "PromoteButton", "TransferToIdeaNestButton", "TransferToNoteNestButton" })
        {
            Assert.DoesNotContain(
                $"AutomationProperties.Name=\"TempNest.Slot{slot}.{button}\"", xaml);
            // AutomationId 自体はテスト・UI Automation 用の内部識別子として維持されている。
            Assert.Contains(
                $"AutomationProperties.AutomationId=\"TempNest.Slot{slot}.{button}\"", xaml);
        }
    }

    [Fact]
    public void TempNestSlotButtons_ContentAndCommandAndToolTip_Unchanged()
    {
        var xaml = ReadSource("NestSuite", "NestSuite", "TempNest", "TempNestWorkspaceView.xaml");
        Assert.Contains("Content=\"コピー\"", xaml);
        Assert.Contains("Content=\"クリア\"", xaml);
        Assert.Contains("Content=\"新規NoteNestへ昇格\"", xaml);
        Assert.Contains("Content=\"IdeaNestカードに追加\"", xaml);
        Assert.Contains("Content=\"既存NoteNestへ追加\"", xaml);
        Assert.Contains("Command=\"{Binding Slot1.CopyBodyCommand}\"", xaml);
        Assert.Contains("Command=\"{Binding Slot1.ClearCommand}\"", xaml);
        Assert.Contains("Command=\"{Binding Slot1.PromoteToNoteCommand}\"", xaml);
    }

    // ── H. ErrorLog: Error のみ、通常失敗では記録しない ───────────────────

    [Theory]
    [InlineData("NestSuiteShellWindow.ChatNestToIdeaNest.cs")]
    [InlineData("NestSuiteShellWindow.TempNestToIdeaNest.cs")]
    [InlineData("NestSuiteShellWindow.TempNestToNoteNest.cs")]
    public void TransferLine_OnlyLogsErrorOnFailedResult(string file)
    {
        var src = ReadSource("NestSuite", "NestSuite", file);
        var failedCaseIndex = src.IndexOf("case WorkspaceTransferResult.Failed:", StringComparison.Ordinal);
        Assert.True(failedCaseIndex >= 0);

        // ErrorLogService への直接呼び出しは導線ファイル自体には無く(共通ヘルパー側の catch のみ)、
        // Failed ケースではエラーダイアログ表示のみを行う。
        Assert.Contains("_dialogs.ShowError(", src.Substring(failedCaseIndex));
    }

    [Fact]
    public void CommonHelper_LogsErrorOnlyInsideCatchBlock()
    {
        var src = ReadSource("NestSuite", "NestSuite", "NestSuiteShellWindow.WorkspaceTransfer.cs");
        var catchIndex = src.IndexOf("catch (Exception ex)", StringComparison.Ordinal);
        Assert.True(catchIndex >= 0);
        Assert.Contains("ErrorLogService.Log(\"WorkspaceTransfer\", ex)", src.Substring(catchIndex));

        // catch ブロック以外に ErrorLogService 呼び出しがないことを確認する
        // （NoTarget/InvalidContent/TargetRejected の通常失敗では記録しない）。
        var occurrences = System.Text.RegularExpressions.Regex.Matches(src, "ErrorLogService\\.Log").Count;
        Assert.Equal(1, occurrences);
    }

    // ── I. docs 記録の整合性 ──────────────────────────────────────────
    // (アプリバージョンの確認は ApplicationVersionTests.cs に集約する方針のため、
    //  本クラスでは対象プロパティを直接参照しない。)

    [Fact]
    public void ReleaseNotes_RecordsV2240_TD93()
    {
        var text = TestPaths.ReadReleaseNotes();
        Assert.Contains("v2.24.0 — TD-93", text);
        Assert.Contains("Workspace間手動転送の総点検", text);
    }

    [Fact]
    public void Backlog_DoesNotContainTD93AsOpenItem()
    {
        var backlog = TestPaths.ReadBacklog();
        Assert.DoesNotContain("| TD-93 |", backlog);
        Assert.Contains("TD-93", backlog);
    }

    [Fact]
    public void Backlog_TabInterworkingSection_StillHasNoOpenItems()
    {
        var backlog = TestPaths.ReadBacklog();
        Assert.Contains("「タブ間連携」単独の未完了項目はない", backlog);
    }

    [Fact]
    public void Backlog_TD10Section_DoesNotClaimLK2StillUnimplemented()
    {
        // TD-92 (v2.20.1) 時点の記述「LK-2 は本 version でも未着手のまま。」が、
        // LK-2 が v2.23.0 で実際に実装された後も §10 に取り残されていないことを確認する
        // （TD-93 の「docs/backlog が現在地と一致しているか」の総点検対象そのもの）。
        var backlog = TestPaths.ReadBacklog();
        Assert.DoesNotContain("LK-2 は本 version でも未着手のまま", backlog);
    }

    [Fact]
    public void Backlog_LK5_TriggerStillNotEstablished_RequiresActualUsage()
    {
        // LK-4/LK-3/LK-2 の3本が実装されたという事実だけではLK-5相当のトリガーが
        // 成立していないことを明記していることを確認する（実装本数だけでは成立させない）。
        var backlog = TestPaths.ReadBacklog();
        Assert.Contains("実際に利用された実績", backlog);
    }

    [Fact]
    public void DesignDoc_HasTD93RegressionSection()
    {
        var path = Path.Combine(RepoRoot, "docs", "planning", "workspace-manual-transfer-helper-design.md");
        Assert.True(File.Exists(path));
        var text = File.ReadAllText(path);
        Assert.Contains("総点検結果（v2.24.0 / TD-93）", text);
        Assert.Contains("TD-93 は総点検 version であり、新しい転送機能は追加していない", text);
    }

    [Fact]
    public void ReleaseChecklist_HasTD93RegressionSection()
    {
        var path = Path.Combine(RepoRoot, "docs", "testing", "nestsuite-release-checklist.md");
        var text = File.ReadAllText(path);
        Assert.Contains("Workspace間手動転送の総点検・回帰確認（v2.24.0 TD-93）", text);
    }

    // ── J. 既存テストクラスがそのまま存在する（削除・置換していない） ────

    [Theory]
    [InlineData("TempNestTests.cs")]
    [InlineData("LK4ChatNestToIdeaNestTransferTests.cs")]
    [InlineData("LK3TempNestToIdeaNestTransferTests.cs")]
    [InlineData("LK2TempNestToNoteNestTransferTests.cs")]
    [InlineData("TD92WorkspaceTransferDesignDocsTests.cs")]
    public void ExistingTransferTestFile_StillExists(string fileName)
    {
        var path = Path.Combine(RepoRoot, "NestSuite.Tests", fileName);
        Assert.True(File.Exists(path), $"既存テストファイルが見つからない: {path}");
    }
}
