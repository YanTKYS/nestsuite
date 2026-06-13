using System.Reflection;
using NoteNest.Models;
using NoteNest.Services;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// AppShell / Workspace 境界テスト。
/// v1.5.1: シグネチャ（フィールド・プロパティ・コンストラクタ・メソッドパラメータ）ベースの依存確認
/// v1.5.2: Model型追加・Window継承チェック追加・ソースファイル文字列チェック追加
/// v1.5.5: Views/NoteNestWorkspaceView コードビハインドをソースチェック対象に追加
/// </summary>
public class ArchitectureBoundaryTests
{
    private static readonly Type[] WorkspaceViewModelTypes =
    [
        typeof(NoteWorkspaceViewModel),
        typeof(TaskBoardViewModel),
        typeof(MarkerPanelViewModel),
        typeof(EditorStateViewModel),
        typeof(ProjectSessionViewModel),
    ];

    private static readonly Type[] WorkspaceCoordinatorAndServiceTypes =
    [
        typeof(WorkspaceChangeCoordinator),
        typeof(NoteChangeCoordinator),
        typeof(EditorChangeCoordinator),
        typeof(ProjectFileService),
        typeof(ProjectDocumentService),
        typeof(ProjectLifecycleService),
        typeof(MarkerExtractorService),
        typeof(NoteLinkService),
        typeof(ExportService),
    ];

    private static readonly Type[] WorkspaceModelTypes =
    [
        typeof(Project),
        typeof(Notebook),
        typeof(Note),
        typeof(NoteTask),
        typeof(TaskCollection),
        typeof(AppSettings),
        typeof(ExportOptions),
    ];

    // FullName 部分一致で確認するため、ネストした generic 型も検出できる
    private static readonly string[] ForbiddenTypeFullNames =
    [
        "System.Windows.Window",
        "System.Windows.MessageBox",
        "Microsoft.Win32.OpenFileDialog",
        "Microsoft.Win32.SaveFileDialog",
        "System.Windows.Forms.FolderBrowserDialog",
    ];

    // ソースファイル内で禁止するコールサイトパターン（v1.5.2 追加）
    private static readonly string[] ForbiddenCallSitePatterns =
    [
        "MessageBox.Show",
        "new OpenFileDialog",
        "new SaveFileDialog",
        "new FolderBrowserDialog",
        "Application.Current",
        "new StartDialog",
        "new ExportDialog",
        "new ProjectInfoDialog",
        "new TutorialWindow",
        "typeof(MainWindow)",
        "new MainWindow",
    ];

    // AppShell 側として除外する Services ファイル名（ソースチェック対象外）
    // DialogService    : AppShell UI サービス
    // DragDropState    : MainWindow ドラッグ状態
    // ThemeService     : Application.Current を使う AppShell テーマ管理
    // UiSettingsService: ウィンドウ設定の永続化（AppShell 責務）
    private static readonly HashSet<string> ExcludedServiceFiles =
    [
        "DialogService.cs",
        "DragDropState.cs",
        "ThemeService.cs",
        "UiSettingsService.cs",
    ];

    // ======= リフレクションベース =======

    private static IEnumerable<string> GetShallowDependencyNames(Type type)
    {
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Static
                               | BindingFlags.Public | BindingFlags.NonPublic
                               | BindingFlags.DeclaredOnly;

        foreach (var f in type.GetFields(All))
            yield return f.FieldType.FullName ?? "";

        foreach (var p in type.GetProperties(All))
            yield return p.PropertyType.FullName ?? "";

        foreach (var ctor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            foreach (var param in ctor.GetParameters())
                yield return param.ParameterType.FullName ?? "";

        foreach (var method in type.GetMethods(All))
        {
            yield return method.ReturnType.FullName ?? "";
            foreach (var param in method.GetParameters())
                yield return param.ParameterType.FullName ?? "";
        }
    }

    private static List<string> FindSignatureViolations(IEnumerable<Type> types)
    {
        var violations = new List<string>();
        foreach (var type in types)
        {
            foreach (var dep in GetShallowDependencyNames(type))
            {
                foreach (var forbidden in ForbiddenTypeFullNames)
                {
                    if (dep.Contains(forbidden))
                        violations.Add($"{type.Name} → {forbidden}");
                }
            }
        }
        return violations;
    }

    private static bool HasWindowInHierarchy(Type t)
    {
        var baseType = t.BaseType;
        while (baseType != null)
        {
            if (baseType.FullName == "System.Windows.Window") return true;
            baseType = baseType.BaseType;
        }
        return false;
    }

    [Fact]
    public void WorkspaceViewModels_DoNotExposeAppShellTypesInSignatures()
    {
        Assert.Empty(FindSignatureViolations(WorkspaceViewModelTypes));
    }

    [Fact]
    public void WorkspaceCoordinatorsAndServices_DoNotExposeAppShellTypesInSignatures()
    {
        Assert.Empty(FindSignatureViolations(WorkspaceCoordinatorAndServiceTypes));
    }

    // v1.5.2 追加：Model 型も確認対象へ
    [Fact]
    public void WorkspaceModels_DoNotExposeAppShellTypesInSignatures()
    {
        Assert.Empty(FindSignatureViolations(WorkspaceModelTypes));
    }

    // v1.5.2 追加：Window 継承確認
    [Fact]
    public void WorkspaceTypes_DoNotInheritFromWindow()
    {
        var allTypes = WorkspaceViewModelTypes
            .Concat(WorkspaceCoordinatorAndServiceTypes)
            .Concat(WorkspaceModelTypes);

        var violations = allTypes
            .Where(HasWindowInHierarchy)
            .Select(t => t.Name)
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void WorkspaceViewModels_CanBeInstantiatedWithoutWindowInfrastructure()
    {
        _ = new NoteWorkspaceViewModel();
        _ = new TaskBoardViewModel();
        _ = new MarkerPanelViewModel(new MarkerExtractorService());
        _ = new EditorStateViewModel();
        _ = new ProjectSessionViewModel();
    }

    // ======= ソースファイル文字列チェック（v1.5.2 追加）=======
    // メソッド本体内のコールサイトをテキストレベルで検出する。
    // 本格的な IL 解析は行わず、パターン文字列の有無を確認する軽量チェック。

    private static string FindSolutionRoot()
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        while (!string.IsNullOrEmpty(dir) && !File.Exists(Path.Combine(dir, "NoteNest.sln")))
            dir = Path.GetDirectoryName(dir)!;
        return !string.IsNullOrEmpty(dir)
            ? dir
            : throw new InvalidOperationException("NoteNest.sln が見つかりません");
    }

    private static IEnumerable<string> GetWorkspaceSourceFiles()
    {
        var src = Path.Combine(FindSolutionRoot(), "NoteNest");

        // ViewModels: MainViewModel*.cs は AppShell/Workspace 境界ファサードのため除外
        foreach (var f in Directory.GetFiles(Path.Combine(src, "ViewModels"), "*.cs"))
        {
            if (!Path.GetFileName(f).StartsWith("MainViewModel", StringComparison.Ordinal))
                yield return f;
        }

        // Services: AppShell 側サービスを除外
        foreach (var f in Directory.GetFiles(Path.Combine(src, "Services"), "*.cs"))
        {
            if (!ExcludedServiceFiles.Contains(Path.GetFileName(f)))
                yield return f;
        }

        // Models: すべて対象
        foreach (var f in Directory.GetFiles(Path.Combine(src, "Models"), "*.cs"))
            yield return f;

        // Views: NoteNestWorkspaceView コードビハインドのみ（.g.cs は除外）
        var viewsDir = Path.Combine(src, "Views");
        if (Directory.Exists(viewsDir))
        {
            foreach (var f in Directory.GetFiles(viewsDir, "*.cs"))
            {
                var name = Path.GetFileName(f);
                if (!name.EndsWith(".g.cs", StringComparison.Ordinal) &&
                    !name.EndsWith(".g.i.cs", StringComparison.Ordinal))
                    yield return f;
            }
        }
    }

    [Fact]
    public void WorkspaceSourceFiles_DoNotContainAppShellCallSites()
    {
        var violations = new List<string>();

        foreach (var file in GetWorkspaceSourceFiles())
        {
            var content = File.ReadAllText(file);
            var fileName = Path.GetFileName(file);

            foreach (var pattern in ForbiddenCallSitePatterns)
            {
                if (content.Contains(pattern, StringComparison.Ordinal))
                    violations.Add($"{fileName}: '{pattern}'");
            }
        }

        Assert.Empty(violations);
    }
}
