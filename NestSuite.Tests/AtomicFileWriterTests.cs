using System.IO;
using System.Text;
using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;
using System.Reflection;
using System.Linq;

namespace NestSuite.Tests;

/// <summary>
/// v2.9.8: AtomicFileWriter のロジック回帰テスト。
/// ディレクトリ作成・新規作成・上書き・バックアップ・tmp 残留なし を確認する。
/// </summary>
public class AtomicFileWriterTests : IDisposable
{
    private readonly string _tempDir;

    public AtomicFileWriterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "AtomicFileWriterTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── 新規作成 ─────────────────────────────────────────────────────────

    [Fact]
    public void WriteAllText_NewFile_CreatesFile()
    {
        var path = Path.Combine(_tempDir, "new.txt");
        AtomicFileWriter.WriteAllText(path, "hello", Encoding.UTF8);
        Assert.True(File.Exists(path));
        Assert.Equal("hello", File.ReadAllText(path, Encoding.UTF8));
    }

    [Fact]
    public void WriteAllText_NewFile_NoTmpRemaining()
    {
        var path = Path.Combine(_tempDir, "notmp.txt");
        AtomicFileWriter.WriteAllText(path, "content", Encoding.UTF8);
        Assert.False(File.Exists(path + ".tmp"));
    }

    // ── ディレクトリ自動作成 ─────────────────────────────────────────────

    [Fact]
    public void WriteAllText_NestedDirectory_CreatesDirectory()
    {
        var path = Path.Combine(_tempDir, "sub", "deep", "file.txt");
        AtomicFileWriter.WriteAllText(path, "nested", Encoding.UTF8);
        Assert.True(File.Exists(path));
    }

    // ── 上書き ───────────────────────────────────────────────────────────

    [Fact]
    public void WriteAllText_ExistingFile_Overwrites()
    {
        var path = Path.Combine(_tempDir, "overwrite.txt");
        File.WriteAllText(path, "old", Encoding.UTF8);
        AtomicFileWriter.WriteAllText(path, "new", Encoding.UTF8);
        Assert.Equal("new", File.ReadAllText(path, Encoding.UTF8));
    }

    [Fact]
    public void WriteAllText_ExistingFile_NoTmpRemaining()
    {
        var path = Path.Combine(_tempDir, "overwrite_notmp.txt");
        File.WriteAllText(path, "old", Encoding.UTF8);
        AtomicFileWriter.WriteAllText(path, "new", Encoding.UTF8);
        Assert.False(File.Exists(path + ".tmp"));
    }

    // ── バックアップ ─────────────────────────────────────────────────────

    [Fact]
    public void WriteAllText_WithBackupPath_CreatesBackup()
    {
        var path   = Path.Combine(_tempDir, "backup.txt");
        var bakPath = Path.Combine(_tempDir, "backup.txt.bak");
        File.WriteAllText(path, "original", Encoding.UTF8);

        AtomicFileWriter.WriteAllText(path, "updated", Encoding.UTF8, bakPath);

        Assert.True(File.Exists(bakPath));
        Assert.Equal("original", File.ReadAllText(bakPath, Encoding.UTF8));
        Assert.Equal("updated",  File.ReadAllText(path,    Encoding.UTF8));
    }

    // v2.14.5 FM-5: バックアップを作成できない場合は保存自体が失敗し、元ファイルが壊れないことを固定する。
    // （IdeaNest の旧「保存前 File.Copy + silent catch」を廃止し、NoteNest / ChatNest と同じ
    // AtomicFileWriter の File.Replace 統合方式へ寄せたため、この契約がすべての Workspace 共通になった）
    [Fact]
    public void WriteAllText_BackupPathUnwritable_ThrowsAndKeepsOriginal()
    {
        var path = Path.Combine(_tempDir, "unwritable_bak.txt");
        File.WriteAllText(path, "ORIGINAL", Encoding.UTF8);

        // 通常のファイルを親ディレクトリ扱いすることで、バックアップパスの作成を確実に失敗させる。
        var blockingFile = Path.Combine(_tempDir, "block");
        File.WriteAllText(blockingFile, "x", Encoding.UTF8);
        var badBakPath = Path.Combine(blockingFile, "sub", "f.bak");

        Assert.ThrowsAny<Exception>(() => AtomicFileWriter.WriteAllText(path, "NEW", Encoding.UTF8, badBakPath));
        Assert.Equal("ORIGINAL", File.ReadAllText(path, Encoding.UTF8));
    }

    [Fact]
    public void WriteAllText_WithoutBackupPath_NoBackupFile()
    {
        var path    = Path.Combine(_tempDir, "nobackup.txt");
        var bakPath = path + ".bak";
        File.WriteAllText(path, "original", Encoding.UTF8);

        AtomicFileWriter.WriteAllText(path, "updated", Encoding.UTF8, backupPath: null);

        Assert.False(File.Exists(bakPath));
    }

    [Fact]
    public void WriteAllText_NewFile_WithBackupPath_NoBackupCreated()
    {
        // 既存ファイルなし → File.Move パス → バックアップは作成されない
        var path    = Path.Combine(_tempDir, "new_with_bak.txt");
        var bakPath = path + ".bak";

        AtomicFileWriter.WriteAllText(path, "content", Encoding.UTF8, bakPath);

        Assert.True(File.Exists(path));
        Assert.False(File.Exists(bakPath));
    }

    // v2.14.10 TD-60: UiSettingsService.Save / TempNestStoreService.Save が使う
    // 「backupPath: null（.bak 世代管理なし）+ UTF8Encoding(false)（BOM なし）」の組み合わせを
    // helper 単位で固定する。DataPath が private static readonly で固定のため、両サービス自体の
    // Save() は直接テストできない（本タスクのスコープ外の production 変更が必要）。
    [Fact]
    public void WriteAllText_NoBackupPath_ExistingFile_OverwritesContent_NoBakCreated()
    {
        var path    = Path.Combine(_tempDir, "no_backup_no_bom.txt");
        var bakPath = path + ".bak";

        AtomicFileWriter.WriteAllText(path, "A", new UTF8Encoding(false));
        AtomicFileWriter.WriteAllText(path, "B", new UTF8Encoding(false), backupPath: null);

        Assert.Equal("B", File.ReadAllText(path, new UTF8Encoding(false)));
        Assert.False(File.Exists(bakPath));
        Assert.False(File.Exists(path + ".tmp"));
    }

    // ── エンコーディング — NoteNest/ChatNest(BOM) vs IdeaNest(no BOM) ────

    [Fact]
    public void WriteAllText_Utf8WithBom_HasBomBytes()
    {
        var path = Path.Combine(_tempDir, "bom.txt");
        AtomicFileWriter.WriteAllText(path, "テスト", Encoding.UTF8);
        var bytes = File.ReadAllBytes(path);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
    }

    [Fact]
    public void WriteAllText_Utf8NoBom_HasNoBomBytes()
    {
        var path = Path.Combine(_tempDir, "nobom.txt");
        AtomicFileWriter.WriteAllText(path, "テスト", new UTF8Encoding(false));
        var bytes = File.ReadAllBytes(path);
        Assert.NotEqual(0xEF, bytes[0]);
    }

    // ── IdeaNest バックアップ + AtomicFileWriter の組み合わせ ──────────────

    [Fact]
    public void IdeaNestWorkspaceService_Save_CreatesBackup()
    {
        // v2.14.5 FM-5: IdeaNestWorkspaceService は保存前 File.Copy（silent catch）を廃止し、
        // AtomicFileWriter の File.Replace 統合 .bak 方式（NoteNest / ChatNest と同方針）へ移行した。
        var path = Path.Combine(_tempDir, "test.ideanest");
        var workspace = new NestSuite.IdeaNest.Models.Workspace
        {
            Ideas = [new NestSuite.IdeaNest.Models.Idea { Id = "first", Title = "First" }]
        };
        NestSuite.IdeaNest.Services.IdeaNestWorkspaceService.Save(path, workspace);

        workspace.Ideas[0].Title = "Updated";
        NestSuite.IdeaNest.Services.IdeaNestWorkspaceService.Save(path, workspace);

        Assert.True(File.Exists(path + ".bak"), ".bak が作成されていること");
        Assert.False(File.Exists(path + ".tmp"), ".tmp が残っていないこと");
    }

    [Fact]
    public void IdeaNestWorkspaceService_Save_NoTmpRemaining()
    {
        var path = Path.Combine(_tempDir, "notmp.ideanest");
        NestSuite.IdeaNest.Services.IdeaNestWorkspaceService.Save(path, new NestSuite.IdeaNest.Models.Workspace());
        Assert.False(File.Exists(path + ".tmp"));
    }

    // ── バージョン / スキーマ ────────────────────────────────────────────

    private static readonly string RepoRoot = TestPaths.RepoRoot;


    // ── バージョン ────────────────────────────────────────────────────────

    // ── AtomicFileWriter: tmp cleanup ─────────────────────────────────────

    [Fact]
    public void AtomicFileWriter_NoTmpRemaining_AfterSuccessfulWrite()
    {
        var path = Path.Combine(_tempDir, "test.notenest");
        AtomicFileWriter.WriteAllText(path, "content", Encoding.UTF8);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void AtomicFileWriter_NoTmpRemaining_AfterOverwrite()
    {
        var path = Path.Combine(_tempDir, "overwrite.notenest");
        File.WriteAllText(path, "original", Encoding.UTF8);
        AtomicFileWriter.WriteAllText(path, "updated", Encoding.UTF8);
        Assert.False(File.Exists(path + ".tmp"));
        Assert.Equal("updated", File.ReadAllText(path, Encoding.UTF8));
    }

    [Fact]
    public void AtomicFileWriter_CreatesBakFile_WhenBackupPathProvided()
    {
        var path    = Path.Combine(_tempDir, "withbak.notenest");
        var bakPath = path + ".bak";
        File.WriteAllText(path, "original", Encoding.UTF8);

        AtomicFileWriter.WriteAllText(path, "updated", Encoding.UTF8, bakPath);

        Assert.True(File.Exists(bakPath));
        Assert.Equal("original", File.ReadAllText(bakPath, Encoding.UTF8));
        Assert.Equal("updated",  File.ReadAllText(path,    Encoding.UTF8));
    }

    [Fact]
    public void AtomicFileWriter_CreatesDirectory_IfNotExist()
    {
        var path = Path.Combine(_tempDir, "sub", "deep", "file.notenest");
        AtomicFileWriter.WriteAllText(path, "nested", Encoding.UTF8);
        Assert.True(File.Exists(path));
    }

    // ── WriteAllTextWithRandomTemp (v2.14.8) ────────────────────────────────

    [Fact]
    public void WriteAllTextWithRandomTemp_NewFile_CreatesFile()
    {
        var path = Path.Combine(_tempDir, "randomtemp_new.txt");
        AtomicFileWriter.WriteAllTextWithRandomTemp(path, "hello");
        Assert.True(File.Exists(path));
        Assert.Equal("hello", File.ReadAllText(path));
    }

    [Fact]
    public void WriteAllTextWithRandomTemp_ExistingFile_Overwrites()
    {
        var path = Path.Combine(_tempDir, "randomtemp_overwrite.txt");
        File.WriteAllText(path, "old");
        AtomicFileWriter.WriteAllTextWithRandomTemp(path, "new");
        Assert.Equal("new", File.ReadAllText(path));
    }

    [Fact]
    public void WriteAllTextWithRandomTemp_NoLeftoverTmpFiles()
    {
        var path = Path.Combine(_tempDir, "randomtemp_notmp.txt");
        File.WriteAllText(path, "old");
        AtomicFileWriter.WriteAllTextWithRandomTemp(path, "new");

        var leftoverTmp = Directory.GetFiles(_tempDir, "*.tmp");
        Assert.Empty(leftoverTmp);
    }

    // ── WriteAllTextWithBackup (v2.14.8) ────────────────────────────────────

    [Fact]
    public void WriteAllTextWithBackup_ExistingFile_CreatesBakFile()
    {
        var path    = Path.Combine(_tempDir, "withbackup.txt");
        var bakPath = path + ".bak";
        File.WriteAllText(path, "original", Encoding.UTF8);

        AtomicFileWriter.WriteAllTextWithBackup(path, "updated", Encoding.UTF8);

        Assert.True(File.Exists(bakPath));
        Assert.Equal("original", File.ReadAllText(bakPath, Encoding.UTF8));
        Assert.Equal("updated",  File.ReadAllText(path,    Encoding.UTF8));
    }

    [Fact]
    public void WriteAllTextWithBackup_NewFile_NoBackupCreated()
    {
        var path    = Path.Combine(_tempDir, "withbackup_new.txt");
        var bakPath = path + ".bak";

        AtomicFileWriter.WriteAllTextWithBackup(path, "content", Encoding.UTF8);

        Assert.True(File.Exists(path));
        Assert.False(File.Exists(bakPath));
    }

    // ── v2.16.6 TD-64: backupPath なし書き込みは既存 .bak に触れない ──────────

    [Fact]
    public void WriteAllText_NoBackupPath_DoesNotTouchExistingBakFile()
    {
        // 自動保存経路の基礎となる挙動: 呼び出し側が backupPath を渡さなければ、
        // 同じ場所に既存の .bak ファイルがあってもそれを更新・削除しない。
        var path    = Path.Combine(_tempDir, "preserve.txt");
        var bakPath = path + ".bak";
        File.WriteAllText(path, "original", Encoding.UTF8);
        AtomicFileWriter.WriteAllTextWithBackup(path, "manual-save", Encoding.UTF8); // creates .bak = "original"
        Assert.True(File.Exists(bakPath));
        var bakContentBefore = File.ReadAllText(bakPath, Encoding.UTF8);

        AtomicFileWriter.WriteAllText(path, "autosave-content", Encoding.UTF8); // no backupPath

        Assert.Equal(bakContentBefore, File.ReadAllText(bakPath, Encoding.UTF8));
        Assert.Equal("original", bakContentBefore);
        Assert.Equal("autosave-content", File.ReadAllText(path, Encoding.UTF8));
    }

    // ── CloseConfirmationService: Save / Discard / Cancel ─────────────────

    [Fact]
    public void CloseConfirmation_WhenNotDirty_ReturnsNoActionNeeded()
    {
        var result = CloseConfirmationService.EvaluateSingle(
            hasUnsavedChanges: false,
            requestDecision:   () => UnsavedChangeDecision.Save,
            save:              () => true);

        Assert.Equal(UnsavedChangeDecision.NoActionNeeded, result);
    }

    [Fact]
    public void CloseConfirmation_SaveSuccess_CanClose()
    {
        var result = CloseConfirmationService.EvaluateSingle(
            hasUnsavedChanges: true,
            requestDecision:   () => UnsavedChangeDecision.Save,
            save:              () => true);

        Assert.Equal(UnsavedChangeDecision.Save, result);
        Assert.True(CloseConfirmationService.CanCloseSingle(
            true, () => UnsavedChangeDecision.Save, () => true));
    }

    [Fact]
    public void CloseConfirmation_SaveFailure_PreventClose()
    {
        // 保存失敗時はクローズを阻止する（dirty 維持の方針）
        var result = CloseConfirmationService.EvaluateSingle(
            hasUnsavedChanges: true,
            requestDecision:   () => UnsavedChangeDecision.Save,
            save:              () => false);

        Assert.Equal(UnsavedChangeDecision.Cancel, result);
        Assert.False(CloseConfirmationService.CanCloseSingle(
            true, () => UnsavedChangeDecision.Save, () => false));
    }

    [Fact]
    public void CloseConfirmation_Discard_AllowsClose()
    {
        var canClose = CloseConfirmationService.CanCloseSingle(
            true, () => UnsavedChangeDecision.Discard, null);
        Assert.True(canClose);
    }

    [Fact]
    public void CloseConfirmation_Cancel_PreventClose()
    {
        var canClose = CloseConfirmationService.CanCloseSingle(
            true, () => UnsavedChangeDecision.Cancel, null);
        Assert.False(canClose);
    }

    [Fact]
    public void CloseConfirmation_TempTab_SkippedInEvaluateMany()
    {
        // CanClose=false タブ（TempNest）は EvaluateMany で除外される
        var targets = new[]
        {
            new CloseConfirmationTarget("note",  CanClose: true,  HasUnsavedChanges: false),
            new CloseConfirmationTarget("temp",  CanClose: false, HasUnsavedChanges: false),
        };

        var result = CloseConfirmationService.EvaluateMany(
            targets,
            _ => UnsavedChangeDecision.Cancel,
            null);

        Assert.True(result.CanContinue);
    }

    // ── FileErrorMessages: 例外種別 → メッセージ ──────────────────────────

    [Fact]
    public void FileErrorMessages_ForLoad_FileNotFound_ReturnsJapaneseMessage()
    {
        var msg = FileErrorMessages.ForLoad(new FileNotFoundException("test"));
        Assert.False(string.IsNullOrWhiteSpace(msg));
        Assert.DoesNotContain("test", msg); // ex.Message をそのまま出さない
    }

    [Fact]
    public void FileErrorMessages_ForLoad_UnauthorizedAccess_ReturnsDifferentMessage()
    {
        var fileNotFound = FileErrorMessages.ForLoad(new FileNotFoundException("x"));
        var unauthorized = FileErrorMessages.ForLoad(new UnauthorizedAccessException("x"));
        Assert.NotEqual(fileNotFound, unauthorized);
    }

    [Fact]
    public void FileErrorMessages_ForSave_IOException_ReturnsJapaneseMessage()
    {
        var msg = FileErrorMessages.ForSave(new IOException("disk error"));
        Assert.False(string.IsNullOrWhiteSpace(msg));
        Assert.DoesNotContain("disk error", msg); // ex.Message をそのまま出さない
    }

    [Fact]
    public void FileErrorMessages_ForSave_UnauthorizedAccess_ReturnsDifferentMessage()
    {
        var io           = FileErrorMessages.ForSave(new IOException("x"));
        var unauthorized = FileErrorMessages.ForSave(new UnauthorizedAccessException("x"));
        Assert.NotEqual(io, unauthorized);
    }

    // ── ErrorLogService: Error のみ、Info / Warning なし ──────────────────

    [Fact]
    public void ErrorLogService_HasNoLogInfoMethod()
    {
        var type = typeof(MainViewModel).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == "ErrorLogService");

        Assert.NotNull(type);
        var infoMethod = type.GetMethod("LogInfo",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.Null(infoMethod);
    }

    [Fact]
    public void ErrorLogService_HasNoLogWarningMethod()
    {
        var type = typeof(MainViewModel).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == "ErrorLogService");

        Assert.NotNull(type);
        var warnMethod = type.GetMethod("LogWarning",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.Null(warnMethod);
    }

    [Fact]
    public void ErrorLogService_HasLogMethod_WithOperationAndException()
    {
        var type = typeof(MainViewModel).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == "ErrorLogService");

        Assert.NotNull(type);
        var logMethod = type.GetMethod("Log",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(logMethod);

        var parameters = logMethod!.GetParameters();
        Assert.True(parameters.Length >= 2);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(Exception), parameters[1].ParameterType);
    }

    // ── GuardNest 方針文書の確認 ─────────────────────────────────────────

    [Fact]
    public void PolicyDocument_DescribesAtomicFileWriter()
    {
        Assert.Contains("AtomicFileWriter", ReadPolicyDocument());
    }

    [Fact]
    public void PolicyDocument_DescribesCloseConfirmationService()
    {
        Assert.Contains("CloseConfirmationService", ReadPolicyDocument());
    }

    [Fact]
    public void PolicyDocument_DescribesErrorLogService()
    {
        Assert.Contains("ErrorLogService", ReadPolicyDocument());
    }

    [Fact]
    public void PolicyDocument_StatesErrorOnlyPolicy()
    {
        var text = ReadPolicyDocument();
        Assert.Contains("Error", text);
        Assert.Contains("Info", text); // "Info / Warning 不可" という形で含まれる
    }

    [Fact]
    public void PolicyDocument_StatesPersonalDataNotLogged()
    {
        var text = ReadPolicyDocument();
        Assert.Contains("本文", text);
        Assert.Contains("個人情報", text);
    }

    // ── backlog / release-notes ───────────────────────────────────────────

    // TD-33: 完了済み項目は release-notes.md で管理
    [Fact]
    public void Backlog_TD26_IsMarkedComplete()
    {
        var path = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(path), $"release-notes.md not found: {path}");
        Assert.Contains("TD-26", File.ReadAllText(path));
    }

    [Fact]
    public void ReleaseNotes_Contains_V2_10_13()
    {
        var path = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(path));
        Assert.Contains("v2.10.13", File.ReadAllText(path));
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private string ReadPolicyDocument()
    {
        var path = Path.Combine(RepoRoot, "docs", "architecture", "sessionnest-guardnest-policy.md");
        Assert.True(File.Exists(path), $"Policy document not found: {path}");
        return File.ReadAllText(path);
    }

}
