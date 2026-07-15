using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// M14: NoteNest 左ペインのノート一覧の表示順切替（作成順・更新日順・タイトル順）。
/// 保存データ（Notebooks/Notes のコレクション順）は変更せず、表示専用の派生状態として実装した。
/// </summary>
public class NoteSortServiceTests
{
    private static NoteViewModel MakeNote(string title, DateTime created, DateTime updated) =>
        new(new Note { Title = title, CreatedAt = created, UpdatedAt = updated });

    private static readonly DateTime Base = new(2026, 1, 1, 0, 0, 0);

    // ── 作成順 ───────────────────────────────────────────────────────────────

    [Fact]
    public void Sort_Created_ReturnsOriginalOrderUnchanged()
    {
        var notes = new[]
        {
            MakeNote("C", Base, Base),
            MakeNote("A", Base, Base),
            MakeNote("B", Base, Base),
        };

        var sorted = NoteSortService.Sort(notes, NoteSortMode.Created);

        Assert.Equal(notes, sorted);
    }

    // ── 更新日順 ─────────────────────────────────────────────────────────────

    [Fact]
    public void Sort_Updated_OrdersByUpdatedAtDescending()
    {
        var oldest = MakeNote("Oldest", Base, Base);
        var middle = MakeNote("Middle", Base, Base.AddHours(1));
        var newest = MakeNote("Newest", Base, Base.AddHours(2));
        var notes = new[] { oldest, middle, newest };

        var sorted = NoteSortService.Sort(notes, NoteSortMode.Updated);

        Assert.Equal(new[] { newest, middle, oldest }, sorted);
    }

    [Fact]
    public void Sort_Updated_SameUpdatedAt_PreservesOriginalOrder()
    {
        var first = MakeNote("First", Base, Base);
        var second = MakeNote("Second", Base, Base);
        var notes = new[] { first, second };

        var sorted = NoteSortService.Sort(notes, NoteSortMode.Updated);

        Assert.Equal(new[] { first, second }, sorted);
    }

    [Fact]
    public void Sort_Updated_DefaultUpdatedAt_FallsBackToCreatedAt()
    {
        var withoutUpdate = MakeNote("NoUpdate", Base.AddDays(1), default);
        var withUpdate = MakeNote("WithUpdate", Base, Base.AddHours(1));
        var notes = new[] { withUpdate, withoutUpdate };

        var sorted = NoteSortService.Sort(notes, NoteSortMode.Updated);

        // withoutUpdate の実効更新日時は CreatedAt=2026-01-02 のため withUpdate(2026-01-01 01:00) より新しい。
        Assert.Equal(new[] { withoutUpdate, withUpdate }, sorted);
    }

    [Fact]
    public void Sort_Updated_BothDatesDefault_FallsBackToOriginalOrderAmongThem()
    {
        var a = MakeNote("A", default, default);
        var b = MakeNote("B", default, default);
        var notes = new[] { a, b };

        var sorted = NoteSortService.Sort(notes, NoteSortMode.Updated);

        Assert.Equal(new[] { a, b }, sorted);
    }

    // ── タイトル順 ───────────────────────────────────────────────────────────

    [Fact]
    public void Sort_Title_OrdersAlphabeticallyAscending()
    {
        var notes = new[] { MakeNote("Charlie", Base, Base), MakeNote("Alpha", Base, Base), MakeNote("Bravo", Base, Base) };

        var sorted = NoteSortService.Sort(notes, NoteSortMode.Title);

        Assert.Equal(new[] { "Alpha", "Bravo", "Charlie" }, sorted.Select(n => n.Title));
    }

    [Fact]
    public void Sort_Title_IsCaseInsensitive()
    {
        var notes = new[] { MakeNote("banana", Base, Base), MakeNote("Apple", Base, Base) };

        var sorted = NoteSortService.Sort(notes, NoteSortMode.Title);

        Assert.Equal(new[] { "Apple", "banana" }, sorted.Select(n => n.Title));
    }

    [Fact]
    public void Sort_Title_HandlesJapaneseTitles()
    {
        var notes = new[] { MakeNote("会議メモ", Base, Base), MakeNote("あいさつ", Base, Base) };

        var sorted = NoteSortService.Sort(notes, NoteSortMode.Title);

        Assert.Equal("あいさつ", sorted[0].Title);
    }

    [Fact]
    public void Sort_Title_HandlesEmptyTitle()
    {
        var notes = new[] { MakeNote("Zebra", Base, Base), MakeNote("", Base, Base) };

        var sorted = NoteSortService.Sort(notes, NoteSortMode.Title);

        Assert.Equal("", sorted[0].Title);
    }

    [Fact]
    public void Sort_Title_SameTitle_PreservesOriginalOrder()
    {
        var first = MakeNote("Same", Base, Base);
        var second = MakeNote("Same", Base, Base);
        var notes = new[] { first, second };

        var sorted = NoteSortService.Sort(notes, NoteSortMode.Title);

        Assert.Equal(new[] { first, second }, sorted);
    }

    [Fact]
    public void Sort_DoesNotMutateInputCollection()
    {
        var notes = new[] { MakeNote("B", Base, Base), MakeNote("A", Base, Base) };

        NoteSortService.Sort(notes, NoteSortMode.Title);

        Assert.Equal("B", notes[0].Title);
        Assert.Equal("A", notes[1].Title);
    }
}

/// <summary>M14: NotebookViewModel.DisplayNotes / RefreshDisplayOrder の派生表示の再構築確認。</summary>
public class NotebookViewModelDisplayOrderTests
{
    private static NotebookViewModel MakeNotebook(params (string Title, DateTime Created, DateTime Updated)[] notes)
    {
        var model = new Notebook { Title = "NB" };
        foreach (var (title, created, updated) in notes)
            model.Notes.Add(new Note { Title = title, CreatedAt = created, UpdatedAt = updated });
        return new NotebookViewModel(model);
    }

    private static readonly DateTime Base = new(2026, 1, 1);

    [Fact]
    public void NewNotebook_DisplayNotesMatchesNotesOrder()
    {
        var nb = MakeNotebook(("A", Base, Base), ("B", Base, Base));

        Assert.Equal(nb.Notes, nb.DisplayNotes);
    }

    [Fact]
    public void RefreshDisplayOrder_Updated_ReordersDisplayNotesOnly_NotesUnchanged()
    {
        var nb = MakeNotebook(("Old", Base, Base), ("New", Base, Base.AddHours(1)));
        var originalNotesOrder = nb.Notes.ToList();

        nb.RefreshDisplayOrder(NoteSortMode.Updated);

        Assert.Equal(new[] { "New", "Old" }, nb.DisplayNotes.Select(n => n.Title));
        Assert.Equal(originalNotesOrder, nb.Notes);
    }

    [Fact]
    public void RefreshDisplayOrder_BackToCreated_RestoresNotesOrder()
    {
        var nb = MakeNotebook(("Old", Base, Base), ("New", Base, Base.AddHours(1)));
        nb.RefreshDisplayOrder(NoteSortMode.Updated);

        nb.RefreshDisplayOrder(NoteSortMode.Created);

        Assert.Equal(nb.Notes, nb.DisplayNotes);
    }

    [Fact]
    public void RefreshDisplayOrder_UsesMove_NotReset_SameInstancesRemain()
    {
        var nb = MakeNotebook(("Old", Base, Base), ("New", Base, Base.AddHours(1)));
        var oldNote = nb.Notes[0];
        var newNote = nb.Notes[1];
        var resets = 0;
        nb.DisplayNotes.CollectionChanged += (_, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset) resets++;
        };

        nb.RefreshDisplayOrder(NoteSortMode.Updated);

        Assert.Equal(0, resets);
        Assert.Same(oldNote, nb.DisplayNotes.Single(n => n.Title == "Old"));
        Assert.Same(newNote, nb.DisplayNotes.Single(n => n.Title == "New"));
    }

    [Fact]
    public void RefreshDisplayOrder_AfterNoteAddedToNotes_SyncsIntoDisplayNotes()
    {
        var nb = MakeNotebook(("A", Base, Base));
        var added = new NoteViewModel(new Note { Title = "B", CreatedAt = Base, UpdatedAt = Base });
        nb.Notes.Add(added);

        nb.RefreshDisplayOrder(NoteSortMode.Created);

        Assert.Contains(added, nb.DisplayNotes);
        Assert.Equal(2, nb.DisplayNotes.Count);
    }

    [Fact]
    public void RefreshDisplayOrder_AfterNoteRemovedFromNotes_SyncsOutOfDisplayNotes()
    {
        var nb = MakeNotebook(("A", Base, Base), ("B", Base, Base));
        var removed = nb.Notes[0];
        nb.Notes.Remove(removed);

        nb.RefreshDisplayOrder(NoteSortMode.Created);

        Assert.DoesNotContain(removed, nb.DisplayNotes);
        Assert.Single(nb.DisplayNotes);
    }
}

/// <summary>M14: NoteWorkspaceViewModel.SortMode / RefreshDisplayOrder の統合確認（トリガーの網羅）。</summary>
public class NoteWorkspaceViewModelSortModeTests
{
    [Fact]
    public void DefaultSortMode_IsCreated()
    {
        var workspace = new NoteWorkspaceViewModel();
        Assert.Equal(NoteSortMode.Created, workspace.SortMode);
    }

    [Fact]
    public void ChangingSortMode_RefreshesAllNotebooksDisplayOrder()
    {
        var workspace = new NoteWorkspaceViewModel();
        var nb1 = workspace.AddNotebook("NB1");
        var a = workspace.AddNote(nb1, "Bravo")!;
        var b = workspace.AddNote(nb1, "Alpha")!;

        workspace.SortMode = NoteSortMode.Title;

        Assert.Equal(new[] { "Alpha", "Bravo" }, nb1.DisplayNotes.Select(n => n.Title));
    }

    [Fact]
    public void Load_AppliesCurrentSortMode()
    {
        var workspace = new NoteWorkspaceViewModel { SortMode = NoteSortMode.Title };

        workspace.Load(new[]
        {
            new Notebook
            {
                Title = "NB",
                Notes = { new Note { Title = "Zebra" }, new Note { Title = "Apple" } },
            },
        });

        var nb = workspace.Notebooks.Single();
        Assert.Equal(new[] { "Apple", "Zebra" }, nb.DisplayNotes.Select(n => n.Title));
    }

    [Fact]
    public void AddNote_ReflectsInDisplayNotesUnderCurrentSortMode()
    {
        var workspace = new NoteWorkspaceViewModel { SortMode = NoteSortMode.Title };
        var nb = workspace.AddNotebook("NB");
        workspace.AddNote(nb, "Zebra");

        workspace.AddNote(nb, "Apple");

        Assert.Equal(new[] { "Apple", "Zebra" }, nb.DisplayNotes.Select(n => n.Title));
    }

    [Fact]
    public void DeleteNote_RemovesFromDisplayNotes()
    {
        var workspace = new NoteWorkspaceViewModel();
        var nb = workspace.AddNotebook("NB");
        var note = workspace.AddNote(nb, "Note")!;

        workspace.DeleteNote(note);

        Assert.Empty(nb.DisplayNotes);
    }

    [Fact]
    public void DuplicateNote_ReflectsInDisplayNotes()
    {
        var workspace = new NoteWorkspaceViewModel();
        var nb = workspace.AddNotebook("NB");
        var note = workspace.AddNote(nb, "Note")!;

        var copy = workspace.DuplicateNote(note);

        Assert.Contains(copy, nb.DisplayNotes);
    }

    [Fact]
    public void MoveNoteToNotebook_UpdatesSourceAndTargetDisplayNotes()
    {
        var workspace = new NoteWorkspaceViewModel();
        var source = workspace.AddNotebook("Source");
        var target = workspace.AddNotebook("Target");
        var note = workspace.AddNote(source, "Note")!;

        workspace.MoveNoteToNotebook(note, target);

        Assert.DoesNotContain(note, source.DisplayNotes);
        Assert.Contains(note, target.DisplayNotes);
    }

    [Fact]
    public void MoveNoteUpDown_ReflectsInDisplayNotes_WhenCreatedMode()
    {
        var workspace = new NoteWorkspaceViewModel();
        var nb = workspace.AddNotebook("NB");
        var a = workspace.AddNote(nb, "A")!;
        var b = workspace.AddNote(nb, "B")!;

        workspace.MoveNoteUp(b);

        Assert.Equal(new[] { b, a }, nb.DisplayNotes);
    }

    [Fact]
    public void RenameNote_ExplicitCommit_RefreshesDisplayOrderUnderTitleMode()
    {
        var workspace = new NoteWorkspaceViewModel { SortMode = NoteSortMode.Title };
        var nb = workspace.AddNotebook("NB");
        var a = workspace.AddNote(nb, "Bravo")!;
        var b = workspace.AddNote(nb, "Charlie")!;

        workspace.RenameNote(b, "Alpha");

        Assert.Equal(new[] { "Alpha", "Bravo" }, nb.DisplayNotes.Select(n => n.Title));
    }

    [Fact]
    public void UpdateContent_DoesNotResortImmediately_UnderTitleModeOrUpdatedMode()
    {
        // 本文の1文字入力ごとの再ソートを避ける: UpdateContent 単体では表示順を変えない。
        var workspace = new NoteWorkspaceViewModel { SortMode = NoteSortMode.Updated };
        var nb = workspace.AddNotebook("NB");
        var a = workspace.AddNote(nb, "First")!;
        var b = workspace.AddNote(nb, "Second")!;
        var orderBefore = nb.DisplayNotes.ToList();

        workspace.UpdateContent(a, "本文を編集中...");
        workspace.UpdateContent(a, "本文を編集中もう少し...");

        Assert.Equal(orderBefore, nb.DisplayNotes);
    }
}

/// <summary>M14: UiSettings.NoteSortMode の既定値・正規化・保存/復元確認。</summary>
public class NoteSortModeUiSettingsTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "NoteSortModeUiSettingsTests_" + Guid.NewGuid().ToString("N"));

    public NoteSortModeUiSettingsTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void UiSettings_DefaultNoteSortMode_IsCreated()
    {
        Assert.Equal(NoteSortMode.Created, new UiSettings().NoteSortMode);
    }

    [Fact]
    public void NormalizeNoteSortMode_ValidValue_Unchanged()
    {
        Assert.Equal(NoteSortMode.Updated, UiSettingsService.NormalizeNoteSortMode(NoteSortMode.Updated));
    }

    [Fact]
    public void NormalizeNoteSortMode_InvalidValue_FallsBackToCreated()
    {
        Assert.Equal(NoteSortMode.Created, UiSettingsService.NormalizeNoteSortMode((NoteSortMode)999));
    }

    [Fact]
    public void SaveThenLoad_RoundTripsNoteSortMode()
    {
        var path = Path.Combine(_dir, "ui-settings.json");
        var service = new UiSettingsService(path);
        service.Save(new UiSettings { NoteSortMode = NoteSortMode.Title });

        var reloaded = service.Load();

        Assert.Equal(NoteSortMode.Title, reloaded.NoteSortMode);
    }

    [Fact]
    public void SaveThenLoad_DoesNotAffectOtherExistingSettings()
    {
        var path = Path.Combine(_dir, "ui-settings.json");
        var service = new UiSettingsService(path);
        service.Save(new UiSettings { NoteSortMode = NoteSortMode.Updated, LastSearchText = "既存の検索語" });

        var reloaded = service.Load();

        Assert.Equal(NoteSortMode.Updated, reloaded.NoteSortMode);
        Assert.Equal("既存の検索語", reloaded.LastSearchText);
    }

    [Fact]
    public void Load_InvalidJson_ReturnsDefaultNoteSortMode()
    {
        var path = Path.Combine(_dir, "ui-settings.json");
        File.WriteAllText(path, "{ not valid json");
        var service = new UiSettingsService(path);

        var settings = service.Load();

        Assert.Equal(NoteSortMode.Created, settings.NoteSortMode);
    }
}

/// <summary>M14: MainViewModel.NoteSortMode ファサードの確認。</summary>
public class MainViewModelNoteSortModeTests
{
    [Fact]
    public void DefaultNoteSortMode_IsCreated()
    {
        var main = new MainViewModel();
        Assert.Equal(NoteSortMode.Created, main.NoteSortMode);
    }

    [Fact]
    public void SettingNoteSortMode_DoesNotMarkModified()
    {
        var main = new MainViewModel();
        Assert.False(main.IsModified);

        main.NoteSortMode = NoteSortMode.Title;

        Assert.False(main.IsModified);
    }

    [Fact]
    public void SettingNoteSortMode_UpdatesUnderlyingNotesWorkspaceSortMode_AndReordersDisplay()
    {
        var main = new MainViewModel();
        main.AddNotebookWithTitle("NB2");
        var nb = main.Notebooks.First(n => n.Title == "NB2");
        main.AddNoteToNotebook(nb, "Zebra");
        main.AddNoteToNotebook(nb, "Apple");

        main.NoteSortMode = NoteSortMode.Title;

        Assert.Equal(new[] { "Apple", "Zebra" }, nb.DisplayNotes.Select(n => n.Title));
    }

    [Fact]
    public void SelectingNote_RefreshesDisplayOrder_ReflectingLatestEdit()
    {
        var main = new MainViewModel();
        var nb = main.Notebooks.First();
        foreach (var existing in nb.Notes.ToList()) main.DeleteNote(existing);
        main.AddNoteToNotebook(nb, "Old");
        main.AddNoteToNotebook(nb, "Older");
        var oldNote = nb.Notes.Single(n => n.Title == "Old");
        var olderNote = nb.Notes.Single(n => n.Title == "Older");
        // 明示的に過去日時へ固定する（実時計依存の不安定さを避けるため）。
        oldNote.Model.CreatedAt = oldNote.Model.UpdatedAt = new DateTime(2026, 1, 1);
        olderNote.Model.CreatedAt = olderNote.Model.UpdatedAt = new DateTime(2020, 1, 1);
        main.NoteSortMode = NoteSortMode.Updated;
        Assert.Equal(oldNote, nb.DisplayNotes[0]);

        // olderNote を選択して本文編集（=UpdatedAt が現在時刻へ更新される）した後、
        // 別ノートを選択することで、その明示的な区切りで表示順へ反映される。
        main.SelectNote(olderNote);
        main.EditorContent = "編集済み";
        main.SelectNote(oldNote);

        Assert.Equal(olderNote, nb.DisplayNotes[0]);
    }
}
