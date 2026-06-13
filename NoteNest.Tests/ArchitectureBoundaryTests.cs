using System.Reflection;
using NoteNest.Services;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.5.1 AppShell / Workspace 境界テスト。
/// Workspace 再利用候補が AppShell 型（Window・ダイアログ類）を
/// フィールド・プロパティ・コンストラクタ・メソッドシグネチャで参照していないことを確認します。
/// メソッド本体内の呼び出しは IL 解析が必要なため本テストの対象外です。
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

    // FullName 部分一致で確認するため、ネストした generic 型も検出できる
    private static readonly string[] ForbiddenTypeFullNames =
    [
        "System.Windows.Window",
        "System.Windows.MessageBox",
        "Microsoft.Win32.OpenFileDialog",
        "Microsoft.Win32.SaveFileDialog",
        "System.Windows.Forms.FolderBrowserDialog",
    ];

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

    private static List<string> FindViolations(IEnumerable<Type> types)
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

    [Fact]
    public void WorkspaceViewModels_DoNotExposeAppShellTypesInSignatures()
    {
        Assert.Empty(FindViolations(WorkspaceViewModelTypes));
    }

    [Fact]
    public void WorkspaceCoordinatorsAndServices_DoNotExposeAppShellTypesInSignatures()
    {
        Assert.Empty(FindViolations(WorkspaceCoordinatorAndServiceTypes));
    }

    [Fact]
    public void WorkspaceViewModels_CanBeInstantiatedWithoutWindowInfrastructure()
    {
        // Workspace ViewModel が UI スレッドやウィンドウなしで生成できることを確認する。
        // テスト環境で生成できること自体が AppShell 非依存の証拠になる。
        _ = new NoteWorkspaceViewModel();
        _ = new TaskBoardViewModel();
        _ = new MarkerPanelViewModel(new MarkerExtractorService());
        _ = new EditorStateViewModel();
        _ = new ProjectSessionViewModel();
    }
}
