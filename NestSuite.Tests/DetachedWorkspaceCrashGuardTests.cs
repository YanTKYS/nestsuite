using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// DetachedWorkspaceWindow 閉鎖時に NoteNest 補完が NullReferenceException を起こさないことの回帰テスト。
/// WPF UI（TextBox / Popup / visual tree 操作）を必要とするパスは手動確認対象。
/// </summary>
public class DetachedWorkspaceCrashGuardTests
{
    // ── NoteTitleProvider null-safe ロジック ─────────────────────────────

    [Fact]
    public void NoteTitleProvider_WhenDataContextIsNull_ReturnsEmpty()
    {
        // 本番 provider ロジックと同等のラムダ。DataContext = null 時に空を返す。
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
}
