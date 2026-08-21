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
}
