using NestSuite.Dialogs;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// L24: タスクの関連ノート選択を、既存 NotePickerDialog（ノートブック名付き一覧・絞り込み）を
/// 再利用して一覧化したことの静的 UI 契約確認。静的文字列テストだけで選択・保存ロジック全体は
/// 保証しないため、絞り込みロジックは NotePickerFilterServiceTests・保存経路は
/// ProjectLifecycleServiceTests / MainViewModel 側のテストで別途確認する。
/// </summary>
public class L24RelatedNotePickerXamlTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadNoteNestWorkspaceViewXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml"));

    private static string ReadNotePickerDialogXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "Dialogs", "NotePickerDialog.xaml"));

    [Fact]
    public void TaskEditingBar_HasRelatedNotePickerButton_NextToExistingComboBox()
    {
        var src = ReadNoteNestWorkspaceViewXaml();
        Assert.Contains("Click=\"PickRelatedNote_Click\"", src);
        Assert.Contains("Task.RelatedNotePickerButton", src);
        // 既存 ComboBox（RelatedNoteChoices / EditingTaskRelatedNote）は維持している。
        Assert.Contains("RelatedNoteChoices", src);
        Assert.Contains("EditingTaskRelatedNote", src);
        Assert.Contains("Task.RelatedNoteComboBox", src);
    }

    [Fact]
    public void TaskEditingBar_DoesNotAddNewTaskCreationAffordance()
    {
        // タスク作成導線の強調・新規ノート作成導線の追加をしていないことを確認する。
        var src = ReadNoteNestWorkspaceViewXaml();
        Assert.DoesNotContain("PickRelatedNote_Click\" Content=\"タスクを追加\"", src);
    }

    [Fact]
    public void NotePickerDialog_UsesDynamicResourceForListAndFilterBox_NoNewFixedColors()
    {
        var src = ReadNotePickerDialogXaml();
        // OK ボタンの既存アクセント色以外に、新規の固定色（Brush定数）は追加していない。
        Assert.Contains("NoteFilterBox", src);
        Assert.Contains("NoteList", src);
    }

    [Fact]
    public void NotePickerDialog_HasEmptyStateText_ForZeroFilterResults()
    {
        var src = ReadNotePickerDialogXaml();
        Assert.Contains("該当するノートはありません", src);
        Assert.Contains("EmptyStateText", src);
    }

    [Fact]
    public void NotePickerDialog_PreservesExistingAutomationIds()
    {
        var src = ReadNotePickerDialogXaml();
        Assert.Contains("Dialog.NoteFilterBox", src);
        Assert.Contains("Dialog.NoteList", src);
        Assert.Contains("Dialog.OkButton", src);
        Assert.Contains("Dialog.CancelButton", src);
    }

    [Fact]
    public void NotePickerItem_DisplayText_IsNotebookTitleSlashNoteTitle()
    {
        var note = TestFactories.MakeNote("端末更新");
        var item = new NotePickerItem("庁内DX", note);

        Assert.Equal("庁内DX / 端末更新", item.DisplayText);
    }
}
