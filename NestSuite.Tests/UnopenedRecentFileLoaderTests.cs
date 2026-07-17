using NestSuite.ChatNest;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.Services;
using NestSuite.Models;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// SH-41 (AT-2 フェーズ1): 未オープンrecent filesの読込（<see cref="UnopenedRecentFileLoader"/>）。
/// Workspace ViewModelを生成せず、既存の TryPrepareOpen → *FileService.LoadPrepared を
/// 再利用して保存モデルまでだけを読み込むことを確認する。
/// </summary>
public class UnopenedRecentFileLoaderTests
{
    // ── .nestsuite（envelope）読込 ───────────────────────────────────────

    [Fact]
    public void Load_NoteNestNestSuiteFile_Succeeds_AndDoesNotCreateViewModel()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.nestsuite");
        try
        {
            var project = new Project { ProjectName = "会議メモ" };
            var nb = new Notebook { Title = "ノートブック" };
            nb.Notes.Add(new Note { Title = "議事録", Content = "決定事項について" });
            project.Notebooks.Add(nb);
            new ProjectFileService().Save(path, project);

            var results = UnopenedRecentFileLoader.Load([path], CancellationToken.None);

            Assert.Single(results);
            Assert.True(results[0].Succeeded);
            Assert.Equal(NestSuiteWorkspaceKind.NoteNest, results[0].Document!.WorkspaceKind);
            Assert.IsType<Project>(results[0].Document!.SavedModel);
            Assert.Equal(Path.GetFileName(path), results[0].Document!.FileName);
        }
        finally { File.Delete(path); File.Delete(path + ".bak"); File.Delete(path + ".tmp"); }
    }

    [Fact]
    public void Load_IdeaNestNestSuiteFile_Succeeds_ReturnsWorkspaceModel()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.nestsuite");
        try
        {
            IdeaNestFileService.Save(path, new Workspace
            {
                Ideas = [new Idea { Title = "企画メモ", Body = "調達方法" }],
            });

            var results = UnopenedRecentFileLoader.Load([path], CancellationToken.None);

            Assert.True(results[0].Succeeded);
            Assert.Equal(NestSuiteWorkspaceKind.IdeaNest, results[0].Document!.WorkspaceKind);
            Assert.IsType<Workspace>(results[0].Document!.SavedModel);
        }
        finally { File.Delete(path); File.Delete(path + ".bak"); File.Delete(path + ".tmp"); }
    }

    [Fact]
    public void Load_ChatNestNestSuiteFile_Succeeds_ReturnsMessageList()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.nestsuite");
        try
        {
            ChatNestFileService.Save(path, [new Message { Speaker = Speaker.自分, Text = "打合せログ" }]);

            var results = UnopenedRecentFileLoader.Load([path], CancellationToken.None);

            Assert.True(results[0].Succeeded);
            Assert.Equal(NestSuiteWorkspaceKind.ChatNest, results[0].Document!.WorkspaceKind);
            Assert.IsType<List<Message>>(results[0].Document!.SavedModel);
        }
        finally { File.Delete(path); File.Delete(path + ".bak"); File.Delete(path + ".tmp"); }
    }

    // ── legacy 拡張子 ────────────────────────────────────────────────────

    [Fact]
    public void Load_LegacyIdeaNestExtension_Succeeds()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.ideanest");
        try
        {
            IdeaNestFileService.Save(path, new Workspace { Ideas = [new Idea { Title = "レガシー" }] });

            var results = UnopenedRecentFileLoader.Load([path], CancellationToken.None);

            Assert.True(results[0].Succeeded);
            Assert.Equal(NestSuiteWorkspaceKind.IdeaNest, results[0].Document!.WorkspaceKind);
        }
        finally { File.Delete(path); File.Delete(path + ".bak"); File.Delete(path + ".tmp"); }
    }

    [Fact]
    public void Load_LegacyNoteNestExtension_Succeeds()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.notenest");
        try
        {
            new ProjectFileService().Save(path, new Project { ProjectName = "レガシーメモ" });

            var results = UnopenedRecentFileLoader.Load([path], CancellationToken.None);

            Assert.True(results[0].Succeeded);
            Assert.Equal(NestSuiteWorkspaceKind.NoteNest, results[0].Document!.WorkspaceKind);
        }
        finally { File.Delete(path); File.Delete(path + ".bak"); File.Delete(path + ".tmp"); }
    }

    [Fact]
    public void Load_LegacyChatNestExtension_Succeeds()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.chatnest");
        try
        {
            ChatNestFileService.Save(path, [new Message { Speaker = Speaker.自分, Text = "レガシー発言" }]);

            var results = UnopenedRecentFileLoader.Load([path], CancellationToken.None);

            Assert.True(results[0].Succeeded);
            Assert.Equal(NestSuiteWorkspaceKind.ChatNest, results[0].Document!.WorkspaceKind);
        }
        finally { File.Delete(path); File.Delete(path + ".bak"); File.Delete(path + ".tmp"); }
    }

    // ── 失敗系: スキップして継続 ─────────────────────────────────────────

    [Fact]
    public void Load_MissingFile_ReturnsUnsucceeded_ForThatFileOnly()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.nestsuite");
        var okPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.nestsuite");
        try
        {
            IdeaNestFileService.Save(okPath, new Workspace { Ideas = [] });

            var results = UnopenedRecentFileLoader.Load([missing, okPath], CancellationToken.None);

            Assert.Equal(2, results.Count);
            Assert.False(results[0].Succeeded);
            Assert.True(results[1].Succeeded);
        }
        finally { File.Delete(okPath); File.Delete(okPath + ".bak"); File.Delete(okPath + ".tmp"); }
    }

    [Fact]
    public void Load_UnsupportedExtension_IsSkipped()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
        var results = UnopenedRecentFileLoader.Load([path], CancellationToken.None,
            fileExists: _ => true, readAllText: _ => "plain text");

        Assert.False(results[0].Succeeded);
    }

    [Fact]
    public void Load_CorruptEnvelopeJson_IsSkipped_ViaInjectedReadAllText()
    {
        var path = "C:\\fake\\corrupt.nestsuite";
        var results = UnopenedRecentFileLoader.Load([path], CancellationToken.None,
            fileExists: _ => true, readAllText: _ => "{ not valid json");

        Assert.False(results[0].Succeeded);
    }

    // ── 実ファイルI/Oに依存しない: 注入関数の呼出回数で確認 ────────────────

    [Fact]
    public void Load_UsesInjectedReadFunctions_NotRealFileIO()
    {
        var path = "C:\\fake\\recent.nestsuite";
        var readCount = 0;
        var workspace = new Workspace { Ideas = [new Idea { Title = "注入テスト" }] };
        var wrapped = IdeaNestFileService.SerializeWrapped(workspace);

        var results = UnopenedRecentFileLoader.Load([path], CancellationToken.None,
            fileExists: _ => true,
            readAllText: _ => { readCount++; return wrapped; });

        Assert.True(results[0].Succeeded);
        Assert.Equal(1, readCount);
    }

    [Fact]
    public void Load_Cancellation_StopsBeforeRemainingFiles()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            UnopenedRecentFileLoader.Load(["a.nestsuite", "b.nestsuite"], cts.Token,
                fileExists: _ => true, readAllText: _ => "{}"));
    }

    [Fact]
    public void Load_OneFileFails_DoesNotStopOtherFiles()
    {
        var okPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.nestsuite");
        try
        {
            IdeaNestFileService.Save(okPath, new Workspace { Ideas = [] });
            var badPath = "C:\\fake\\bad.nestsuite";

            var results = UnopenedRecentFileLoader.Load([badPath, okPath], CancellationToken.None,
                fileExists: path => path == okPath || File.Exists(path),
                readAllText: path => path == badPath ? "{ broken" : File.ReadAllText(path));

            Assert.False(results[0].Succeeded);
            Assert.True(results[1].Succeeded);
        }
        finally { File.Delete(okPath); File.Delete(okPath + ".bak"); File.Delete(okPath + ".tmp"); }
    }
}
