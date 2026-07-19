using System.Reflection;
using NestSuite;
using NestSuite.PlainText;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.19.0 SH-43: PlainTextWorkspace（.txt）の Workspace 起動導線（開く・保存・起動時読込・
/// タブ閉じ確認・PropertyChanged ハンドラ・別ウィンドウ・種別判定）に関する
/// リフレクションベースの静的存在確認テスト。WPF ウィンドウは起動しない
/// （NestSuiteShellChatNestLaunchTests / NestSuiteShellNoteNestLaunchTests と同じ方針）。
/// </summary>
public class NestSuiteShellPlainTextLaunchTests
{
    [Fact]
    public void NestSuiteShellWindow_HasLoadInitialTextFileMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadInitialTextFile",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(WorkspaceFileOpenContext)],
                null);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasLoadTextFileAtMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadTextFileAt",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(WorkspaceFileOpenContext)],
                null);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasOpenTextFileMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("OpenTextFile", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasNewTextSessionMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("NewTextSession", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_TrySaveTextToPath_TakesSessionParameter()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("TrySaveTextToPath",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteWorkspaceSession), typeof(string)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasUpdateTextTabPathMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("UpdateTextTabPath",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteWorkspaceSession), typeof(string)],
                null);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasSaveTextFileAsMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SaveTextFileAs", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasTrySaveTextForSaveAllMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("TrySaveTextForSaveAll", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasTrySaveTextForCloseMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("TrySaveTextForClose",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteWorkspaceSession)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasConfirmAndResetTextMethod()
    {
        // 未保存タブ閉鎖確認は NoteNest と同じ Save/Discard/Cancel の3択（専用ダイアログは新設しない）。
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("ConfirmAndResetText",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteDocumentTab)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasOnPlainTextPropertyChangedMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("OnPlainTextPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasCreatePlainTextViewModelMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("CreatePlainTextViewModel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Equal(typeof(PlainTextWorkspaceViewModel), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasDetachTextTabMethod()
    {
        // v2.19.0 SH-43: 別ウィンドウ化にも対応する（NoteNest/IdeaNest/ChatNest と対称）。
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("DetachTextTab",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(NestSuiteDocumentTab)],
                null);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasReAttachTextTabMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("ReAttachTextTab",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                [typeof(string)],
                null);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_HasSaveTextForTabIdMethod()
    {
        // v2.19.0 SH-43: 別ウィンドウ内の Ctrl+S 用（DetachedWorkspaceWindow から呼ばれる）。
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SaveTextForTabId", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
    }

    [Fact]
    public void DetachedWorkspaceWindow_HasSelectPlainTextSavePathMethod()
    {
        var method = typeof(DetachedWorkspaceWindow)
            .GetMethod("SelectPlainTextSavePath",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                [typeof(string)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(string), method!.ReturnType);
    }

    [Fact]
    public void DialogService_HasSelectPlainTextOpenAndSavePathMethods()
    {
        Assert.NotNull(typeof(DialogService).GetMethod("SelectPlainTextOpenPath", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(DialogService).GetMethod("SelectPlainTextSavePath", BindingFlags.Instance | BindingFlags.Public));
    }
}
