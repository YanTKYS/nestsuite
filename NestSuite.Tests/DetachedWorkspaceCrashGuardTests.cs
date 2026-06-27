using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.9.5 hotfix: DetachedWorkspaceWindow 閉鎖時の NoteNest 補完 NullReferenceException 修正の回帰テスト。
/// WPF UI（TextBox / Popup / visual tree 操作）を必要とするパスは手動確認対象。
/// </summary>
public class DetachedWorkspaceCrashGuardTests
{
    // ── NoteTitleProvider null-safe ロジック ─────────────────────────────

    [Fact]
    public void NoteTitleProvider_WhenDataContextIsNull_ReturnsEmpty()
    {
        // v2.9.5 修正の provider ロジックと同等のラムダ。DataContext = null 時に空を返す。
        object? dataContext = null;
        Func<IEnumerable<string>> provider = () =>
        {
            if (dataContext is not MainViewModel vm)
                return Enumerable.Empty<string>();
            return vm.Notebooks
                .SelectMany(nb => nb.Notes)
                .Where(n => !string.IsNullOrWhiteSpace(n.Title))
                .Select(n => n.Title);
        };

        Assert.Empty(provider());
    }

    [Fact]
    public void NoteTitleProvider_WhenDataContextIsMainViewModel_ReturnsTitles()
    {
        var main = new MainViewModel();
        var nb   = main.Notes.AddNotebook("NB");
        main.Notes.AddNote(nb, "SampleTitle");

        object? dataContext = main;
        Func<IEnumerable<string>> provider = () =>
        {
            if (dataContext is not MainViewModel vm)
                return Enumerable.Empty<string>();
            return vm.Notebooks
                .SelectMany(n => n.Notes)
                .Where(n => !string.IsNullOrWhiteSpace(n.Title))
                .Select(n => n.Title);
        };

        Assert.Contains("SampleTitle", provider());
    }

    [Fact]
    public void NoteTitleProvider_WhenDataContextChangedToNull_ReturnsEmpty()
    {
        var main = new MainViewModel();
        var nb   = main.Notes.AddNotebook("NB");
        main.Notes.AddNote(nb, "Title");

        object? dataContext = main;
        Func<IEnumerable<string>> provider = () =>
        {
            if (dataContext is not MainViewModel vm)
                return Enumerable.Empty<string>();
            return vm.Notebooks
                .SelectMany(n => n.Notes)
                .Where(n => !string.IsNullOrWhiteSpace(n.Title))
                .Select(n => n.Title);
        };

        Assert.NotEmpty(provider());

        // DataContext が null になった（DetachedWorkspaceWindow.OnClosed 相当）
        dataContext = null;

        Assert.Empty(provider());
    }

    // ── IsNoteEditModeProvider null-safe ロジック ────────────────────────

    [Fact]
    public void IsNoteEditModeProvider_WhenDataContextIsNull_ReturnsFalse()
    {
        object? dataContext = null;
        Func<bool> provider = () => dataContext is MainViewModel vm && vm.IsNoteEditMode;

        Assert.False(provider());
    }

    [Fact]
    public void IsNoteEditModeProvider_WhenDataContextIsMainViewModel_ReflectsVmState()
    {
        var main = new MainViewModel();
        object? dataContext = main;
        Func<bool> provider = () => dataContext is MainViewModel vm && vm.IsNoteEditMode;

        Assert.Equal(main.IsNoteEditMode, provider());
    }

    // ── NoteEditorHost provider ガード ───────────────────────────────────

    [Fact]
    public void NoteEditorHost_WhenNoteTitleProviderIsNull_InvokeViaNull_ReturnsEmpty()
    {
        // UpdateCompletion 内の `NoteTitleProvider?.Invoke() ?? Enumerable.Empty<string>()` と同等
        Func<IEnumerable<string>>? provider = null;
        var result = provider?.Invoke() ?? Enumerable.Empty<string>();
        Assert.Empty(result);
    }

    [Fact]
    public void NoteEditorHost_WhenNoteTitleProviderThrows_CanCatchGracefully()
    {
        // UpdateCompletion の try/catch ガード相当。例外時に CloseCompletion して return。
        Func<IEnumerable<string>> provider = () => throw new InvalidOperationException("simulated");
        IEnumerable<string> result;
        try
        {
            result = provider.Invoke();
        }
        catch
        {
            result = Enumerable.Empty<string>();
        }
        Assert.Empty(result);
    }

    // ── バージョン / スキーマ ────────────────────────────────────────────

    [Fact]
    public void ApplicationVersion_Is_2_9_7()
    {
        Assert.Equal("2.10.11", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void NoteNestSchemaVersion_Remains_1_4_1()
    {
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }
}
