using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Automation;

namespace NestSuite.UiSmoke;

class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: NestSuite.UiSmoke <path-to-NestSuite.exe>");
            return 2;
        }

        var exePath = args[0];
        if (!File.Exists(exePath))
        {
            Console.Error.WriteLine($"FAIL: Executable not found: {exePath}");
            return 1;
        }

        var smokeAppData = Path.Combine(Path.GetTempPath(), $"NestSuite-Smoke-{Guid.NewGuid():N}");
        Directory.CreateDirectory(smokeAppData);
        Console.WriteLine($"Smoke AppData: {smokeAppData}");

        var psi = new ProcessStartInfo(exePath) { UseShellExecute = false };
        psi.EnvironmentVariables["APPDATA"] = smokeAppData;

        Process? proc = null;
        try
        {
            proc = Process.Start(psi);
            if (proc == null)
            {
                Console.Error.WriteLine("FAIL: Could not start NestSuite.exe");
                return 1;
            }

            Console.WriteLine($"Launched NestSuite.exe (PID {proc.Id})");
            return RunChecks(proc);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL: Exception during smoke test: {ex.Message}");
            return 1;
        }
        finally
        {
            try
            {
                if (proc != null && !proc.HasExited)
                {
                    proc.Kill(entireProcessTree: true);
                    proc.WaitForExit(5000);
                    Console.WriteLine("Process terminated.");
                }
            }
            catch { }

            try { Directory.Delete(smokeAppData, recursive: true); } catch { }
        }
    }

    static int RunChecks(Process proc)
    {
        var timeout = TimeSpan.FromSeconds(60);
        var deadline = DateTime.Now + timeout;

        // 1. Find main window
        Console.WriteLine("Waiting for main window...");
        AutomationElement? mainWindow = null;
        while (DateTime.Now < deadline)
        {
            if (proc.HasExited)
            {
                Console.Error.WriteLine($"FAIL: Process exited unexpectedly (code {proc.ExitCode})");
                return 1;
            }

            try
            {
                mainWindow = AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.ProcessIdProperty, proc.Id));
                if (mainWindow != null) break;
            }
            catch { }

            Thread.Sleep(500);
        }

        if (mainWindow == null)
        {
            Console.Error.WriteLine("FAIL: Main window not found within 60 seconds");
            return 1;
        }
        Console.WriteLine("PASS: Main window found");

        // 2. Check required AutomationId elements
        // Shell.TabStrip / Shell.TempTab / Shell.StatusBar: always present in the shell
        // TempNest.Slot1.BodyBox: main editing area visible on startup (TempNest is default active workspace)
        string[] requiredIds =
        [
            "Shell.TabStrip",
            "Shell.TempTab",
            "Shell.StatusBar",
            "TempNest.Slot1.BodyBox",
        ];
        foreach (var id in requiredIds)
        {
            Console.WriteLine($"Checking element '{id}'...");
            AutomationElement? el = null;
            while (DateTime.Now < deadline)
            {
                if (proc.HasExited)
                {
                    Console.Error.WriteLine($"FAIL: Process exited while waiting for '{id}'");
                    return 1;
                }

                try
                {
                    el = mainWindow.FindFirst(
                        TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.AutomationIdProperty, id));
                    if (el != null) break;
                }
                catch { }

                Thread.Sleep(200);
            }

            if (el == null)
            {
                Console.Error.WriteLine($"FAIL: Element '{id}' not found within timeout");
                return 1;
            }
            Console.WriteLine($"PASS: Element '{id}' found");
        }

        Console.WriteLine("PASS: All smoke test checks passed");
        return 0;
    }
}
