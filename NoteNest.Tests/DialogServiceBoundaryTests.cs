using System.Reflection;
using NoteNest.Services;
using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

public class DialogServiceBoundaryTests
{
    [Fact]
    public void MainWindowAndMainViewModelDoNotOwnConcreteDialogTypes()
    {
        // v1.19.3: MainWindow 削除により NestSuiteShellWindow で確認
        var mainWindowFields = typeof(NoteNest.NestSuite.NestSuiteShellWindow).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
        var mainViewModelMembers = typeof(MainViewModel).GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.DoesNotContain(mainWindowFields, field => field.FieldType.Namespace == "NoteNest.Dialogs");
        Assert.DoesNotContain(mainViewModelMembers, member => GetMemberType(member)?.Namespace == "Microsoft.Win32");
    }

    [Fact]
    public void MainViewModelUsesDialogCallbacksForProjectPathSelection()
    {
        var main = new MainViewModel();
        var directory = Path.Combine(Path.GetTempPath(), $"notenest-dialog-boundary-{Guid.NewGuid()}");
        var path = Path.Combine(directory, "selected.notenest");
        Directory.CreateDirectory(directory);

        try
        {
            main.SelectSaveProjectPath = _ => path;
            main.SaveAsProjectCommand.Execute(null);

            var reopened = new MainViewModel
            {
                SelectOpenProjectPath = () => path
            };
            reopened.OpenProjectCommand.Execute(null);

            Assert.True(File.Exists(path));
            Assert.Equal(path, reopened.CurrentFilePath);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void DialogServiceExposesUnifiedPathSelectionEntryPoints()
    {
        var methodNames = typeof(DialogService).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(method => method.Name)
            .ToHashSet();

        Assert.Contains(nameof(DialogService.SelectProjectOpenPath), methodNames);
        Assert.Contains(nameof(DialogService.SelectProjectSavePath), methodNames);
        Assert.Contains(nameof(DialogService.SelectExportOutputPath), methodNames);
        Assert.Contains(nameof(DialogService.SelectProjectTextExportPath), methodNames);
        Assert.Contains(nameof(DialogService.SelectNotebookExportFolder), methodNames);
    }

    private static Type? GetMemberType(MemberInfo member) => member switch
    {
        FieldInfo field => field.FieldType,
        PropertyInfo property => property.PropertyType,
        MethodInfo method => method.ReturnType,
        _ => null,
    };
}
