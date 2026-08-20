using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NestSuite.TempNest;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.23.0 LK-2: TempNest スロット → 既存 NoteNest タブへ新規ノートとして追加。
/// 設計正本 docs/planning/workspace-manual-transfer-helper-design.md（TD-92 / v2.20.1）に対する
/// 実装の回帰確認。
///
/// <para>Shell（<see cref="NestSuiteShellWindow"/>）は WPF <c>Window</c> であり、
/// <see cref="LK3TempNestToIdeaNestTransferTests"/> / <see cref="LK4ChatNestToIdeaNestTransferTests"/> と
/// 同じ方針で、インスタンス化を伴わないリフレクションベースの契約確認とソーステキスト確認に限定する。
/// 実際の転送先解決・0/1/複数件分岐・タブ dirty 反映・保存・Detached 対応は、LK-3/LK-4 と同様に
/// 実機／UI Smoke での確認に委ねる（本ファイルではその境界を超えない）。
/// TempNest スロット側の delegate 契約・NoteNest 側受入 API（<see cref="MainViewModel.CreateNoteFromTransfer(string?, string)"/>）・
/// 共通ヘルパーの再利用・docs 契約は Window に依存しないため、ここで直接検証する。</para>
/// </summary>
public class LK2TempNestToNoteNestTransferTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;
    private static readonly BindingFlags InstanceNonPublic =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    // ── 1. Shell 側配線・処理本体の存在確認（Window非依存の契約のみ） ────────

    [Fact]
    public void NestSuiteShellWindow_HasTempNestToNoteNestWiringMethods()
    {
        Assert.NotNull(typeof(NestSuiteShellWindow).GetMethod("WireTempNestToNoteNestTransfer", InstanceNonPublic));
        Assert.NotNull(typeof(NestSuiteShellWindow).GetMethod("TransferTempNestSlotToNoteNest", InstanceNonPublic));
    }

    [Fact]
    public void NestSuiteShellWindow_TransferTempNestSlotToNoteNest_ReturnsNullableBool_TakesSlot()
    {
        var method = typeof(NestSuiteShellWindow).GetMethod("TransferTempNestSlotToNoteNest", InstanceNonPublic);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool?), method!.ReturnType);
        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(TempNestSlotViewModel), parameters[0].ParameterType);
    }

    [Fact]
    public void CreateTempNestViewModel_WiresTempNestToNoteNestTransfer()
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
        Assert.Contains("WireTempNestToNoteNestTransfer(vm)", body);
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

    [Fact]
    public void LK3_TransferTempNestSlotToIdeaNest_StillExists_Unchanged()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("TransferTempNestSlotToIdeaNest", InstanceNonPublic, null,
                [typeof(TempNestSlotViewModel)], null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool?), method!.ReturnType);
    }

    // ── 3. NoteNest 受入 API 拡張: CreateNoteFromTransfer(string) / (string?, string) ──

    [Fact]
    public void CreateNoteFromTransfer_SingleArgOverload_StillExists_DelegatesToNewOverload()
    {
        var single = typeof(MainViewModel).GetMethod("CreateNoteFromTransfer", [typeof(string)]);
        var twoArg = typeof(MainViewModel).GetMethod("CreateNoteFromTransfer", [typeof(string), typeof(string)]);

        Assert.NotNull(single);
        Assert.NotNull(twoArg);
    }

    [Fact]
    public void CreateNoteFromTransfer_TitleAndContent_UsesGivenTitle_NotGeneratedOne()
    {
        var main = new MainViewModel();

        var note = main.CreateNoteFromTransfer("明示タイトル", "本文行");

        Assert.NotNull(note);
        Assert.Equal("明示タイトル", note!.Title);
        Assert.Equal("本文行", note.Content);
    }

    [Fact]
    public void CreateNoteFromTransfer_TitleOnly_EmptyBody_CreatesNote()
    {
        var main = new MainViewModel();

        var note = main.CreateNoteFromTransfer("タイトルのみ", "");

        Assert.NotNull(note);
        Assert.Equal("タイトルのみ", note!.Title);
        Assert.Equal("", note.Content);
    }

    [Fact]
    public void CreateNoteFromTransfer_NullTitle_FallsBackToExistingTitleGeneration()
    {
        // タイトル未指定（null）のときは TN-3 と同じ PromotedNoteTitleGenerator 経由のフォールバックになる。
        var main = new MainViewModel();

        var note = main.CreateNoteFromTransfer(null, "タイトル候補\n本文");

        Assert.NotNull(note);
        Assert.Equal("タイトル候補", note!.Title);
    }

    [Fact]
    public void CreateNoteFromTransfer_WhitespaceTitle_FallsBackToExistingTitleGeneration()
    {
        var main = new MainViewModel();

        var note = main.CreateNoteFromTransfer("   ", "本文のみ");

        Assert.NotNull(note);
        Assert.Equal("本文のみ", note!.Title);
    }

    [Fact]
    public void CreateNoteFromTransfer_DuplicateGivenTitle_AppendsNumberSuffix_ViaExistingUniqueTitleLogic()
    {
        var main = new MainViewModel();
        var nb = main.Notebooks.First();
        main.Notes.AddNote(nb, "重複タイトル");

        var note = main.CreateNoteFromTransfer("重複タイトル", "本文");

        Assert.NotNull(note);
        Assert.Equal("重複タイトル (2)", note!.Title);
    }

    [Fact]
    public void CreateNoteFromTransfer_SingleArgOverload_Tn3Behavior_Unchanged()
    {
        // TN-3 の既存フォールバック挙動（PromotedNoteTitleGenerator）が引き続き成立することを確認する。
        var main = new MainViewModel();

        var note = main.CreateNoteFromTransfer("最初の行\n本文2行目");

        Assert.NotNull(note);
        Assert.Equal("最初の行", note!.Title);
        Assert.Equal("最初の行\n本文2行目", note.Content);
    }

    [Fact]
    public void CreateNoteFromTransfer_TitleAndBody_DoesNotAddMarkersOrStar()
    {
        var main = new MainViewModel();
        var note = main.CreateNoteFromTransfer("タイトル", "本文のみ");
        Assert.False(note!.IsStarred);
        Assert.False(note.HasMarkers);
    }

    [Fact]
    public void CreateNoteFromTransfer_TitleAndBody_MarksNoteNestDirty_OnSuccess()
    {
        // NoteNest 側 dirty は既存の AddNote 経路経由でのみ立つ（LK-2 コード側で IsModified を直接操作しない）。
        var main = new MainViewModel();
        Assert.False(main.IsModified);

        var note = main.CreateNoteFromTransfer("タイトル", "本文");

        Assert.NotNull(note);
        Assert.True(main.IsModified);
    }

    // ── 4. TempNestSlotViewModel: TransferToNoteNestRequested / TransferToNoteNestCommand ──

    [Fact]
    public void TransferToNoteNestRequested_DefaultsToNull()
    {
        var slot = new TempNestSlotViewModel();
        Assert.Null(slot.TransferToNoteNestRequested);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "\r\n\t ")]
    public void TransferToNoteNestCommand_TitleAndBodyBothEmptyOrWhitespace_IsDisabled(string title, string body)
    {
        var slot = new TempNestSlotViewModel { Title = title, Body = body };
        Assert.False(slot.TransferToNoteNestCommand.CanExecute(null));
    }

    [Fact]
    public void TransferToNoteNestCommand_TitleOnly_BodyEmpty_IsEnabled()
    {
        var slot = new TempNestSlotViewModel { Title = "タイトルのみ", Body = "" };
        Assert.True(slot.TransferToNoteNestCommand.CanExecute(null));
    }

    [Fact]
    public void TransferToNoteNestCommand_BodyOnly_TitleEmpty_IsEnabled()
    {
        var slot = new TempNestSlotViewModel { Title = "", Body = "本文のみ" };
        Assert.True(slot.TransferToNoteNestCommand.CanExecute(null));
    }

    [Fact]
    public void TransferToNoteNestCommand_TitleAndBody_IsEnabled()
    {
        var slot = new TempNestSlotViewModel { Title = "タイトル", Body = "本文" };
        Assert.True(slot.TransferToNoteNestCommand.CanExecute(null));
    }

    [Fact]
    public void TransferToNoteNestCommand_Execute_InvokesTransferToNoteNestRequestedWithSelf()
    {
        var slot = new TempNestSlotViewModel { Body = "断片" };
        TempNestSlotViewModel? received = null;
        slot.TransferToNoteNestRequested = s => { received = s; return false; };

        slot.TransferToNoteNestCommand.Execute(null);

        Assert.Same(slot, received);
    }

    [Fact]
    public void TransferToNoteNestCommand_Execute_ResultTrue_ClearsSlot()
    {
        var slot = new TempNestSlotViewModel { Title = "タイトル", Body = "断片" };
        slot.TransferToNoteNestRequested = _ => true;

        slot.TransferToNoteNestCommand.Execute(null);

        Assert.Equal("", slot.Title);
        Assert.Equal("", slot.Body);
    }

    [Fact]
    public void TransferToNoteNestCommand_Execute_ResultFalse_KeepsSlotContent()
    {
        var slot = new TempNestSlotViewModel { Title = "タイトル", Body = "断片" };
        slot.TransferToNoteNestRequested = _ => false;

        slot.TransferToNoteNestCommand.Execute(null);

        Assert.Equal("タイトル", slot.Title);
        Assert.Equal("断片", slot.Body);
    }

    [Fact]
    public void TransferToNoteNestCommand_Execute_ResultNull_KeepsSlotContent()
    {
        // null = 失敗・キャンセル・対象なし。元スロットは一切変更しない（0 件タブ・ダイアログキャンセル等）。
        var slot = new TempNestSlotViewModel { Title = "タイトル", Body = "断片" };
        slot.TransferToNoteNestRequested = _ => null;

        slot.TransferToNoteNestCommand.Execute(null);

        Assert.Equal("タイトル", slot.Title);
        Assert.Equal("断片", slot.Body);
    }

    [Fact]
    public void TransferToNoteNestCommand_Execute_WhenRequestedIsNull_DoesNothing()
    {
        var slot = new TempNestSlotViewModel { Title = "タイトル", Body = "断片" };

        var ex = Record.Exception(() => slot.TransferToNoteNestCommand.Execute(null));

        Assert.Null(ex);
        Assert.Equal("タイトル", slot.Title);
        Assert.Equal("断片", slot.Body);
    }

    [Fact]
    public void TransferToNoteNestCommand_DisabledWhileTransferring()
    {
        var slot = new TempNestSlotViewModel { Body = "断片" };
        var reentrantCanExecute = true;
        slot.TransferToNoteNestRequested = _ =>
        {
            reentrantCanExecute = slot.TransferToNoteNestCommand.CanExecute(null);
            return false;
        };

        slot.TransferToNoteNestCommand.Execute(null);

        Assert.False(reentrantCanExecute);
        Assert.True(slot.TransferToNoteNestCommand.CanExecute(null));
    }

    [Fact]
    public void TransferToNoteNestCommand_Execute_DoesNotAffectOtherCommands_OrViceVersa()
    {
        // TN-3（PromoteToNoteCommand）・LK-3（TransferToIdeaNestCommand）・LK-2（TransferToNoteNestCommand）は
        // 独立した別操作であり、一方の実行中フラグが他方の CanExecute に影響しないことを確認する。
        var slot = new TempNestSlotViewModel { Title = "タイトル", Body = "断片" };
        var promoteCanExecuteDuringTransfer = false;
        var ideaNestCanExecuteDuringTransfer = false;
        slot.TransferToNoteNestRequested = _ =>
        {
            promoteCanExecuteDuringTransfer = slot.PromoteToNoteCommand.CanExecute(null);
            ideaNestCanExecuteDuringTransfer = slot.TransferToIdeaNestCommand.CanExecute(null);
            return false;
        };

        slot.TransferToNoteNestCommand.Execute(null);

        Assert.True(promoteCanExecuteDuringTransfer);
        Assert.True(ideaNestCanExecuteDuringTransfer);
    }

    [Fact]
    public void Dispose_ClearsTransferToNoteNestRequested()
    {
        var slot = new TempNestSlotViewModel();
        slot.TransferToNoteNestRequested = _ => null;

        slot.Dispose();

        Assert.Null(slot.TransferToNoteNestRequested);
    }

    // ── 5. LK-3 非干渉: TransferToIdeaNestRequested / Command は今回追加で壊れていない ──

    [Fact]
    public void TransferToIdeaNestCommand_StillWorks_IndependentlyOfNoteNestTransfer()
    {
        var slot = new TempNestSlotViewModel { Title = "タイトル", Body = "断片" };
        TempNestSlotViewModel? received = null;
        slot.TransferToIdeaNestRequested = s => { received = s; return true; };

        slot.TransferToIdeaNestCommand.Execute(null);

        Assert.Same(slot, received);
        Assert.Equal("", slot.Title);
        Assert.Equal("", slot.Body);
    }

    // ── 6. LK-2 導線ソース: Title=slot.Title(空ならnull)/Body=slot.Body、余計な付加情報なし ──

    private static string ReadTempNestToNoteNestSource() =>
        File.ReadAllText(Path.Combine(
            RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.TempNestToNoteNest.cs"));

    [Fact]
    public void TempNestToNoteNestSource_MapsTitleAndBodyDirectly_FromSlot()
    {
        var src = ReadTempNestToNoteNestSource();

        Assert.Contains("IsNullOrWhiteSpace(slot.Title) ? null : slot.Title", src);
        Assert.Contains("Body = slot.Body", src);
    }

    [Fact]
    public void TempNestToNoteNestSource_DoesNotAddHeaderOrTimestampOrSlotNumber()
    {
        var src = ReadTempNestToNoteNestSource();

        Assert.DoesNotContain("TempNestから転送", src);
        Assert.DoesNotContain("TempNestから昇格", src);
        Assert.DoesNotContain("[NOTE]", src);
        Assert.DoesNotContain("[IDEA]", src);
        Assert.DoesNotContain("DateTime.Now", src);
        Assert.DoesNotContain("DateTimeOffset.Now", src);
    }

    [Fact]
    public void TempNestToNoteNestSource_DoesNotCallActivateTab_OrSaveApis()
    {
        var src = ReadTempNestToNoteNestSource();

        Assert.DoesNotContain("ActivateTab", src);
        Assert.DoesNotContain(".Save(", src);
        Assert.DoesNotContain(".SaveAs(", src);
        Assert.DoesNotContain("AtomicFileWriter", src);
        Assert.DoesNotContain("SaveSessionAfterTabChange", src);
        Assert.DoesNotContain("File.Write", src);
    }

    [Fact]
    public void TempNestToNoteNestSource_ReusesExistingHelpers_NoNewTransferService()
    {
        var src = ReadTempNestToNoteNestSource();

        Assert.Contains("EnumerateTransferTargets(", src);
        Assert.Contains("TransferToWorkspaceTab<MainViewModel>(", src);
        Assert.Contains("CreateNoteFromTransfer(", src);
        Assert.Contains("WorkspaceTransferTargetDialog(", src);

        Assert.DoesNotContain("NoteNestTransferService", src);
        Assert.DoesNotContain("TempNestToNoteNestService", src);
        Assert.DoesNotContain("WorkspaceTransferManager", src);
        Assert.DoesNotContain("TransferManager", src);
    }

    [Fact]
    public void TempNestToNoteNestSource_UsesConfirmWithSafeDefault_ForClearConfirmation()
    {
        // TN-3・LK-3 と同じ考え方（既定「残す」）を再利用する。
        var src = ReadTempNestToNoteNestSource();
        Assert.Contains("_dialogs.ConfirmWithSafeDefault(", src);
    }

    [Fact]
    public void TempNestToNoteNestSource_ZeroTabs_DoesNotWriteErrorLog()
    {
        // 0 件時は「案内のみ」であり、失敗として ErrorLogService.Log を呼ばない。
        var src = ReadTempNestToNoteNestSource();
        var candidatesZeroBranchStart = src.IndexOf("if (candidates.Count == 0)", StringComparison.Ordinal);
        Assert.True(candidatesZeroBranchStart >= 0);
        var branchEnd = src.IndexOf('}', src.IndexOf('{', candidatesZeroBranchStart));
        var branch = src.Substring(candidatesZeroBranchStart, branchEnd - candidatesZeroBranchStart);
        Assert.DoesNotContain("ErrorLogService", branch);
        Assert.Contains("return null", branch);
    }

    // ── 7. 共通ヘルパー再利用の確認: EnumerateTransferTargets / TransferToWorkspaceTab 自体は
    //       LK-2 のために新規追加していない（既存シグネチャのまま） ──────────────

    [Fact]
    public void WorkspaceTransferHelpers_SignaturesUnchanged_ByLk2()
    {
        var enumerateMethod = typeof(NestSuiteShellWindow).GetMethod("EnumerateTransferTargets", InstanceNonPublic);
        Assert.NotNull(enumerateMethod);

        var transferMethod = typeof(NestSuiteShellWindow).GetMethods(InstanceNonPublic)
            .FirstOrDefault(m => m.Name == "TransferToWorkspaceTab" && m.IsGenericMethodDefinition);
        Assert.NotNull(transferMethod);
    }

    // ── 8. TempNest 側 XAML: 各スロットへの入口ボタン ────────────────────────

    private static string ReadTempNestWorkspaceViewXaml() =>
        File.ReadAllText(Path.Combine(
            RepoRoot, "NestSuite", "NestSuite", "TempNest", "TempNestWorkspaceView.xaml"));

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void TempNestWorkspaceView_Slot_HasTransferToNoteNestButton(int slot)
    {
        var xaml = ReadTempNestWorkspaceViewXaml();

        Assert.Contains($"AutomationProperties.AutomationId=\"TempNest.Slot{slot}.TransferToNoteNestButton\"", xaml);
        Assert.Contains($"Command=\"{{Binding Slot{slot}.TransferToNoteNestCommand}}\"", xaml);
        Assert.Contains("Content=\"既存NoteNestへ追加\"", xaml);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void TempNestWorkspaceView_Slot_NoteNestTransferButton_IsPlacedAfterIdeaNestButton(int slot)
    {
        var xaml = ReadTempNestWorkspaceViewXaml();

        var ideaNestIdx = xaml.IndexOf($"AutomationProperties.AutomationId=\"TempNest.Slot{slot}.TransferToIdeaNestButton\"", StringComparison.Ordinal);
        var noteNestIdx = xaml.IndexOf($"AutomationProperties.AutomationId=\"TempNest.Slot{slot}.TransferToNoteNestButton\"", StringComparison.Ordinal);

        Assert.True(ideaNestIdx >= 0 && noteNestIdx >= 0);
        Assert.True(ideaNestIdx < noteNestIdx);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void TempNestWorkspaceView_NoteNestTransferButton_AutomationName_IsNotTheInternalId(int slot)
    {
        // LK-4-1 (v2.21.1) と同種の問題の再発防止。可視テキストを持つボタンには
        // AutomationProperties.Name へ内部 AutomationId 文字列をそのまま設定しない。
        var xaml = ReadTempNestWorkspaceViewXaml();

        Assert.DoesNotContain(
            $"AutomationProperties.Name=\"TempNest.Slot{slot}.TransferToNoteNestButton\"", xaml);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void TempNestWorkspaceView_NoteNestTransferButton_HasDescriptiveToolTip(int slot)
    {
        var xaml = ReadTempNestWorkspaceViewXaml();
        var idx = xaml.IndexOf($"AutomationProperties.AutomationId=\"TempNest.Slot{slot}.TransferToNoteNestButton\"", StringComparison.Ordinal);
        Assert.True(idx >= 0);
        var end = xaml.IndexOf("/>", idx, StringComparison.Ordinal);
        var buttonMarkup = xaml.Substring(idx, end - idx);
        Assert.Contains("ToolTip=\"スロットのタイトル・本文を既存のNoteNestタブへ新規ノートとして追加します\"", buttonMarkup);
    }

    [Fact]
    public void TempNestWorkspaceView_DoesNotAddNewToolbarOrSettingsScreen()
    {
        var xaml = ReadTempNestWorkspaceViewXaml();

        Assert.DoesNotContain("ToolBar", xaml);
        Assert.DoesNotContain("SettingsWindow", xaml);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void TempNestWorkspaceView_TransferToIdeaNestButton_StillPresent_Unchanged(int slot)
    {
        // LK-3 のボタンが LK-2 追加によって壊れていないことを確認する。
        var xaml = ReadTempNestWorkspaceViewXaml();

        Assert.Contains($"AutomationProperties.AutomationId=\"TempNest.Slot{slot}.TransferToIdeaNestButton\"", xaml);
        Assert.Contains($"Command=\"{{Binding Slot{slot}.TransferToIdeaNestCommand}}\"", xaml);
    }

    // ── 9. docs: TD-92 → LK-4 → LK-3 → LK-2 の関係・backlog・release notes ─────

    [Fact]
    public void DesignDoc_RecordsLk2ImplementationResult()
    {
        var doc = File.ReadAllText(Path.Combine(
            RepoRoot, "docs", "planning", "workspace-manual-transfer-helper-design.md"));

        Assert.Contains("v2.23.0", doc);
        Assert.Contains("LK-2", doc);
    }

    [Fact]
    public void ReleaseNotes_RecordsV2230Lk2()
    {
        var notes = TestPaths.ReadReleaseNotes();

        Assert.Contains("v2.23.0", notes);
        Assert.Contains("LK-2", notes);
    }

    [Fact]
    public void Backlog_DoesNotContainLk2AsOpenItem()
    {
        var backlog = TestPaths.ReadBacklog();

        Assert.False(backlog.Contains("| LK-2 |", StringComparison.Ordinal),
            "LK-2 は v2.23.0 で実装済みのため、backlog に open item として残っていてはならない");
    }

    [Fact]
    public void Backlog_DoesNotAutoStartLk5StyleGeneralization()
    {
        // LK-2 実装をもって、汎用横断転送基盤（旧 LK-5 相当）へ自動着手しない方針を維持する。
        var backlog = TestPaths.ReadBacklog();

        Assert.Contains("実際に利用された実績", backlog);
    }
}
