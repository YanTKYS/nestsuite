using System.Reflection;
using NoteNest.Services;
using Xunit;

namespace NoteNest.Tests;

public class ProjectLifecycleBoundaryTests
{
    [Fact]
    public void LifecycleDoesNotOwnExportResponsibility()
    {
        var fields = typeof(ProjectLifecycleService).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
        var methods = typeof(ProjectLifecycleService).GetMethods(BindingFlags.Instance | BindingFlags.Public);
        var constructorParameters = typeof(ProjectLifecycleService).GetConstructors()
            .SelectMany(constructor => constructor.GetParameters());

        Assert.DoesNotContain(fields, field => field.FieldType == typeof(ExportService));
        Assert.DoesNotContain(constructorParameters, parameter => parameter.ParameterType == typeof(ExportService));
        Assert.DoesNotContain(methods, method => method.Name.StartsWith("Export", StringComparison.Ordinal));
    }

    [Fact]
    public void LifecycleExposesSnapshotWithoutOwningFileFormatConversion()
    {
        var methods = typeof(ProjectLifecycleService).GetMethods(BindingFlags.Instance | BindingFlags.Public);

        Assert.Contains(methods, method => method.Name == nameof(ProjectLifecycleService.CreateSnapshot));
        Assert.DoesNotContain(methods, method => method.Name == "Build");
    }
}
