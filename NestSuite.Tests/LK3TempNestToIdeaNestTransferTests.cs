using System;
using System.IO;
using System.Reflection;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.TempNest;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.22.0 LK-3: TempNest スロット → IdeaNest カード化。
/// 設計正本 docs/planning/workspace-manual-transfer-helper-design.md（TD-92 / v2.20.1、§23 実装結果）に対する
/// 実装の回帰確認。
///
/// <para>Shell（<see cref="NestSuiteShellWindow"/>）は WPF <c>Window</c> であり、
/// <see cref="LK4ChatNestToIdeaNestTransferTests"/> と同じ方針で、インスタンス化を伴わないリフレクション
/// ベースの契約確認に限定する。実際の転送先解決・0/1/複数件分岐・タブ dirty 反映・保存・Detached 対応は、
/// LK-4 と同様に実機／UI Smoke での確認に委ねる（本ファイルではその境界を超えない）。
/// TempNest スロット側の delegate 契約・IdeaNest 側受入 API・共通ヘルパーの補正・docs 契約は
/// Window に依存しないため、ここで直接検証する。</para>
/// </summary>
public class LK3TempNestToIdeaNestTransferTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;
    private static readonly BindingFlags InstanceNonPublic =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    // ── 1. Shell 側配線・処理本体の存在確認（Window非依存の契約のみ） ────────

    [Fact]
    public void NestSuiteShellWindow_HasTempNestToIdeaNestWiringMethods()
    {
        Assert.NotNull(typeof(NestSuiteShellWindow).GetMethod("WireTempNestToIdeaNestTransfer", InstanceNonPublic));
        Assert.NotNull(typeof(NestSuiteShellWindow).GetMethod("TransferTempNestSlotToIdeaNest", InstanceNonPublic));
    }

    [Fact]
    public void NestSuiteShellWindow_TransferTempNestSlotToIdeaNest_ReturnsNullableBool_TakesSlot()
    {
        var method = typeof(NestSuiteShellWindow).GetMethod("TransferTempNestSlotToIdeaNest", InstanceNonPublic);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool?), method!.ReturnType);
        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(TempNestSlotViewModel), parameters[0].ParameterType);
    }

    [Fact]
    public void CreateTempNestViewModel_WiresTempNestToIdeaNestTransfer()
    {
        var src = File.ReadAllText(Path.Combine(
            RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.xaml.cs"));
        var start = src.IndexOf("private TempNestWorkspaceViewModel CreateTempNestViewModel", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = src.IndexOf("\n    }", start, StringComparison.Ordinal);
        Assert.True(end >= 0);
        var body = src.Substring(start, end - start);

        Assert.Contains("WireTempNestPromotion(vm)", body);
        Assert.Contains("WireTempNestToIdeaNestTransfer(vm)", body);
    }

    // ── 2. TN-3 非干渉: 既存シグネチャが変更されていないこと ─────────────────

    [Fact]
    public void NestSuiteShellWindow_Tn3PromotionMethod_StillExists_Unchanged()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("PromoteTempNestSlotToNoteNest", InstanceNonPublic, null,
                [typeof(TempNestSlotViewModel)], null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool?), method!.ReturnType);
    }

    // ── 3. 共通ヘルパー InvalidContent 補正: Title または Body があれば有効 ──

    [Fact]
    public void TransferToWorkspaceTab_InvalidContentCheck_AcceptsTitleOrBody_NotBodyOnly()
    {
        // LK-3: 「Body 必須」から「Title または Body のいずれかがあれば有効」へ最小補正したことを、
        // 実装ソースの判定式で確認する（TransferToWorkspaceTab は private のため直接呼べない。
        // 実際の 0/1/複数件の分岐は LK-4 と同様に実機で確認する）。
        var src = File.ReadAllText(Path.Combine(
            RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.WorkspaceTransfer.cs"));

        Assert.Contains("string.IsNullOrWhiteSpace(content.Title) && string.IsNullOrWhiteSpace(content.Body)", src);
        Assert.DoesNotContain("if (string.IsNullOrWhiteSpace(content.Body)) return WorkspaceTransferResult.InvalidContent;", src);
    }

    // ── 4. TempNestSlotViewModel: TransferToIdeaNestRequested / TransferToIdeaNestCommand ──

    [Fact]
    public void TransferToIdeaNestRequested_DefaultsToNull()
    {
        var slot = new TempNestSlotViewModel();
        Assert.Null(slot.TransferToIdeaNestRequested);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "\r\n\t ")]
    public void TransferToIdeaNestCommand_TitleAndBodyBothEmptyOrWhitespace_IsDisabled(string title, string body)
    {
        var slot = new TempNestSlotViewModel { Title = title, Body = body };
        Assert.False(slot.TransferToIdeaNestCommand.CanExecute(null));
    }

    [Fact]
    public void TransferToIdeaNestCommand_TitleOnly_BodyEmpty_IsEnabled()
    {
        // LK-3 確定事項: IdeaNest の通常カード追加契約（CommitAdd）が Title のみでも有効なため、
        // TempNest でも Title のみでの転送を許容する（決定案 B）。
        var slot = new TempNestSlotViewModel { Title = "タイトルのみ", Body = "" };
        Assert.True(slot.TransferToIdeaNestCommand.CanExecute(null));
    }

    [Fact]
    public void TransferToIdeaNestCommand_BodyOnly_TitleEmpty_IsEnabled()
    {
        var slot = new TempNestSlotViewModel { Title = "", Body = "本文のみ" };
        Assert.True(slot.TransferToIdeaNestCommand.CanExecute(null));
    }

    [Fact]
    public void TransferToIdeaNestCommand_TitleAndBody_IsEnabled()
    {
        var slot = new TempNestSlotViewModel { Title = "タイトル", Body = "本文" };
        Assert.True(slot.TransferToIdeaNestCommand.CanExecute(null));
    }

    [Fact]
    public void TransferToIdeaNestCommand_Execute_InvokesTransferToIdeaNestRequestedWithSelf()
    {
        var slot = new TempNestSlotViewModel { Body = "断片" };
        TempNestSlotViewModel? received = null;
        slot.TransferToIdeaNestRequested = s => { received = s; return false; };

        slot.TransferToIdeaNestCommand.Execute(null);

        Assert.Same(slot, received);
    }

    [Fact]
    public void TransferToIdeaNestCommand_Execute_ResultTrue_ClearsSlot()
    {
        var slot = new TempNestSlotViewModel { Title = "タイトル", Body = "断片" };
        slot.TransferToIdeaNestRequested = _ => true;

        slot.TransferToIdeaNestCommand.Execute(null);

        Assert.Equal("", slot.Title);
        Assert.Equal("", slot.Body);
    }

    [Fact]
    public void TransferToIdeaNestCommand_Execute_ResultFalse_KeepsSlotContent()
    {
        var slot = new TempNestSlotViewModel { Title = "タイトル", Body = "断片" };
        slot.TransferToIdeaNestRequested = _ => false;

        slot.TransferToIdeaNestCommand.Execute(null);

        Assert.Equal("タイトル", slot.Title);
        Assert.Equal("断片", slot.Body);
    }

    [Fact]
    public void TransferToIdeaNestCommand_Execute_ResultNull_KeepsSlotContent()
    {
        // null = 失敗・キャンセル・対象なし。元スロットは一切変更しない。
        var slot = new TempNestSlotViewModel { Title = "タイトル", Body = "断片" };
        slot.TransferToIdeaNestRequested = _ => null;

        slot.TransferToIdeaNestCommand.Execute(null);

        Assert.Equal("タイトル", slot.Title);
        Assert.Equal("断片", slot.Body);
    }

    [Fact]
    public void TransferToIdeaNestCommand_Execute_WhenRequestedIsNull_DoesNothing()
    {
        var slot = new TempNestSlotViewModel { Title = "タイトル", Body = "断片" };

        var ex = Record.Exception(() => slot.TransferToIdeaNestCommand.Execute(null));

        Assert.Null(ex);
        Assert.Equal("タイトル", slot.Title);
        Assert.Equal("断片", slot.Body);
    }

    [Fact]
    public void TransferToIdeaNestCommand_DisabledWhileTransferring()
    {
        var slot = new TempNestSlotViewModel { Body = "断片" };
        var reentrantCanExecute = true;
        slot.TransferToIdeaNestRequested = _ =>
        {
            reentrantCanExecute = slot.TransferToIdeaNestCommand.CanExecute(null);
            return false;
        };

        slot.TransferToIdeaNestCommand.Execute(null);

        Assert.False(reentrantCanExecute);
        Assert.True(slot.TransferToIdeaNestCommand.CanExecute(null));
    }

    [Fact]
    public void TransferToIdeaNestCommand_Execute_DoesNotAffectPromoteToNoteCommand_OrViceVersa()
    {
        // TN-3（PromoteToNoteCommand）とLK-3（TransferToIdeaNestCommand）は独立した別操作であり、
        // 一方の実行中フラグがもう一方の CanExecute に影響しないことを確認する。
        var slot = new TempNestSlotViewModel { Title = "タイトル", Body = "断片" };
        var promoteCanExecuteDuringTransfer = false;
        slot.TransferToIdeaNestRequested = _ =>
        {
            promoteCanExecuteDuringTransfer = slot.PromoteToNoteCommand.CanExecute(null);
            return false;
        };

        slot.TransferToIdeaNestCommand.Execute(null);

        Assert.True(promoteCanExecuteDuringTransfer);
    }

    [Fact]
    public void Dispose_ClearsTransferToIdeaNestRequested()
    {
        var slot = new TempNestSlotViewModel();
        slot.TransferToIdeaNestRequested = _ => null;

        slot.Dispose();

        Assert.Null(slot.TransferToIdeaNestRequested);
    }

    // ── 5. IdeaNest 受入 API 再利用: AddCardFromTransfer（新しい専用APIを作らない） ──

    [Fact]
    public void AddCardFromTransfer_TitleOnly_EmptyBody_AddsCard()
    {
        // 決定案 B の根拠: IdeaNest の通常カード追加契約（CommitAdd）はもとから Title のみで有効。
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace());

        var ok = vm.AddCardFromTransfer("タイトルのみ", "");

        Assert.True(ok);
        var card = Assert.Single(vm.AllCards);
        Assert.Equal("タイトルのみ", card.Title);
        Assert.Equal("", card.Body);
    }

    [Fact]
    public void AddCardFromTransfer_TitleAndBody_PreservesBothAsIs_NoHeaderOrTimestamp()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace());

        var ok = vm.AddCardFromTransfer("スロットのタイトル", "スロットの本文\n2行目");

        Assert.True(ok);
        var card = Assert.Single(vm.AllCards);
        Assert.Equal("スロットのタイトル", card.Title);
        Assert.Equal("スロットの本文\n2行目", card.Body);
        Assert.DoesNotContain("TempNestから転送", card.Body);
        Assert.DoesNotContain("[NOTE]", card.Body);
        Assert.DoesNotContain("[IDEA]", card.Body);
    }

    [Fact]
    public void AddCardFromTransfer_TitleAndBody_MarksIdeaNestDirty_OnSuccess()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace());
        Assert.False(vm.HasChanges);

        var ok = vm.AddCardFromTransfer("タイトル", "本文");

        Assert.True(ok);
        Assert.True(vm.HasChanges);
    }

    [Fact]
    public void AddCardFromTransfer_TitleAndBodyBothEmpty_DoesNotAddCard_AndStaysNotDirty()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.LoadFromWorkspace(new Workspace());

        var ok = vm.AddCardFromTransfer(null, "");

        Assert.False(ok);
        Assert.Empty(vm.AllCards);
        Assert.False(vm.HasChanges);
    }

    // ── 6. LK-3 導線ソース: Title=slot.Title(空ならnull)/Body=slot.Body、余計な付加情報なし ──

    private static string ReadTempNestToIdeaNestSource() =>
        File.ReadAllText(Path.Combine(
            RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.TempNestToIdeaNest.cs"));

    [Fact]
    public void TempNestToIdeaNestSource_MapsTitleAndBodyDirectly_FromSlot()
    {
        var src = ReadTempNestToIdeaNestSource();

        Assert.Contains("IsNullOrWhiteSpace(slot.Title) ? null : slot.Title", src);
        Assert.Contains("Body = slot.Body", src);
    }

    [Fact]
    public void TempNestToIdeaNestSource_DoesNotAddHeaderOrTimestampOrSlotNumber()
    {
        var src = ReadTempNestToIdeaNestSource();

        Assert.DoesNotContain("TempNestから転送", src);
        Assert.DoesNotContain("[NOTE]", src);
        Assert.DoesNotContain("[IDEA]", src);
        Assert.DoesNotContain("DateTime.Now", src);
        Assert.DoesNotContain("DateTimeOffset.Now", src);
    }

    [Fact]
    public void TempNestToIdeaNestSource_DoesNotCallActivateTab_OrSaveApis()
    {
        var src = ReadTempNestToIdeaNestSource();

        Assert.DoesNotContain("ActivateTab", src);
        Assert.DoesNotContain(".Save(", src);
        Assert.DoesNotContain(".SaveAs(", src);
        Assert.DoesNotContain("AtomicFileWriter", src);
        Assert.DoesNotContain("SaveSessionAfterTabChange", src);
    }

    [Fact]
    public void TempNestToIdeaNestSource_ReusesExistingHelpers_NoNewTransferService()
    {
        var src = ReadTempNestToIdeaNestSource();

        Assert.Contains("EnumerateTransferTargets(", src);
        Assert.Contains("TransferToWorkspaceTab<IdeaNestWorkspaceViewModel>(", src);
        Assert.Contains("AddCardFromTransfer(", src);
        Assert.Contains("WorkspaceTransferTargetDialog(", src);

        Assert.DoesNotContain("TempNestToIdeaNestTransferService", src);
        Assert.DoesNotContain("WorkspaceTransferService2", src);
        Assert.DoesNotContain("TransferManager", src);
    }

    [Fact]
    public void TempNestToIdeaNestSource_UsesConfirmWithSafeDefault_ForClearConfirmation()
    {
        // TN-3 と同じ考え方（既定「残す」）を再利用する。TN-3 実装自体は移行しない。
        var src = ReadTempNestToIdeaNestSource();
        Assert.Contains("_dialogs.ConfirmWithSafeDefault(", src);
    }

    // ── 7. TempNest 側 XAML: 各スロットへの入口ボタン ────────────────────────

    private static string ReadTempNestWorkspaceViewXaml() =>
        File.ReadAllText(Path.Combine(
            RepoRoot, "NestSuite", "NestSuite", "TempNest", "TempNestWorkspaceView.xaml"));

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void TempNestWorkspaceView_Slot_HasTransferToIdeaNestButton(int slot)
    {
        var xaml = ReadTempNestWorkspaceViewXaml();

        Assert.Contains($"AutomationProperties.AutomationId=\"TempNest.Slot{slot}.TransferToIdeaNestButton\"", xaml);
        Assert.Contains($"Command=\"{{Binding Slot{slot}.TransferToIdeaNestCommand}}\"", xaml);
        Assert.Contains("Content=\"IdeaNestカードに追加\"", xaml);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void TempNestWorkspaceView_Slot_TransferButton_IsPlacedAfterPromoteButton(int slot)
    {
        var xaml = ReadTempNestWorkspaceViewXaml();

        var promoteIdx = xaml.IndexOf($"AutomationProperties.AutomationId=\"TempNest.Slot{slot}.PromoteButton\"", StringComparison.Ordinal);
        var transferIdx = xaml.IndexOf($"AutomationProperties.AutomationId=\"TempNest.Slot{slot}.TransferToIdeaNestButton\"", StringComparison.Ordinal);

        Assert.True(promoteIdx >= 0 && transferIdx >= 0);
        Assert.True(promoteIdx < transferIdx);
    }

    [Fact]
    public void TempNestWorkspaceView_DoesNotAddNewToolbarOrSettingsScreen()
    {
        // LK-3 は最小限の入口追加に限定し、新しい常設ツールバー・設定画面・専用ダイアログを増やさない。
        var xaml = ReadTempNestWorkspaceViewXaml();

        Assert.DoesNotContain("ToolBar", xaml);
        Assert.DoesNotContain("SettingsWindow", xaml);
    }

    // ── 8. docs: TD-92 → LK-4 → LK-3 の関係・backlog・release notes ─────────

    [Fact]
    public void DesignDoc_RecordsLk3ImplementationResult()
    {
        var doc = File.ReadAllText(Path.Combine(
            RepoRoot, "docs", "planning", "workspace-manual-transfer-helper-design.md"));

        Assert.Contains("v2.22.0", doc);
        Assert.Contains("LK-3", doc);
        Assert.Contains("実装結果（v2.22.0 / LK-3）", doc);
    }

    [Fact]
    public void ReleaseNotes_RecordsV2220Lk3()
    {
        var notes = TestPaths.ReadReleaseNotes();

        Assert.Contains("v2.22.0", notes);
        Assert.Contains("LK-3", notes);
    }

    [Fact]
    public void Backlog_DoesNotContainLk3AsOpenItem()
    {
        var backlog = TestPaths.ReadBacklog();

        Assert.False(backlog.Contains("| LK-3 |", StringComparison.Ordinal),
            "LK-3 は v2.22.0 で実装済みのため、backlog に open item として残っていてはならない");
    }

    [Fact]
    public void Backlog_StillContainsLk2AsOpenItem()
    {
        var backlog = TestPaths.ReadBacklog();

        Assert.True(backlog.Contains("| LK-2 |", StringComparison.Ordinal),
            "LK-2 は今回実装対象外のため backlog に残っている必要がある");
    }

    [Fact]
    public void Backlog_RecordsCommonHelperReusedByTwoTransfers()
    {
        // LK-4 と LK-3 の 2 本で共通ヘルパーが実利用されたことの記録。ただし、これを理由に
        // 新たな汎用化（LK-5 相当）へ自動着手しない方針も維持されていることを確認する。
        var backlog = TestPaths.ReadBacklog();

        Assert.Contains("LK-4 と LK-3", backlog);
        Assert.Contains("実際に再利用された", backlog);
    }

    [Fact]
    public void Backlog_Lk5ReassessmentTrigger_RequiresActualUsage_NotJustTwoImplementations()
    {
        // 旧 LK-5（横断クイック投入）の再評価条件は「転送導線が2本実装された」だけでは成立せず、
        // 「実際に利用された実績」が必要という既存ルールを維持していることを確認する。
        var backlog = TestPaths.ReadBacklog();

        Assert.Contains("実際に利用された実績", backlog);
    }

    // ── helpers ─────────────────────────────────────────────────────────────
}
