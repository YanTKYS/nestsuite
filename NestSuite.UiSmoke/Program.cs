using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;

namespace NestSuite.UiSmoke;

class Program
{
    // ── Win32 helpers for click simulation ───────────────────────────────────
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);
    const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    const uint MOUSEEVENTF_LEFTUP   = 0x0004;

    // ── Required element groups ───────────────────────────────────────────────
    static readonly string[] ShellElements =
    [
        "Shell.TabStrip",
        "Shell.TempTab",
        "Shell.StatusBar",
    ];

    static readonly string[] TempNestElements =
    [
        "TempNest.Slot1.BodyBox",
        "TempNest.Slot1.TitleBox",
        "TempNest.Slot1.CopyButton",
        "TempNest.Slot1.ClearButton",
    ];

    static readonly string[] NoteNestElements =
    [
        "NoteNest.WorkspaceRoot",
        "NoteNest.NotebookTree",
        "NoteNest.EditorHost",
        "NoteNest.RightPaneTabControl",
    ];

    static readonly string[] IdeaNestElements =
    [
        "IdeaNest.WorkspaceRoot",
        "IdeaNest.CardArea",
        "IdeaNest.AddIdeaButton",
    ];

    static readonly string[] ChatNestElements =
    [
        "ChatNest.WorkspaceRoot",
        "ChatNest.MessageList",
        "ChatNest.InputBox",
        "ChatNest.PostButton",
    ];

    // ── Entry point ───────────────────────────────────────────────────────────

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

    // ── Test orchestration ────────────────────────────────────────────────────

    static int RunChecks(Process proc)
    {
        // 1. Wait for main window (60 s)
        Console.WriteLine("Waiting for main window...");
        var mainWindow = WaitForMainWindow(proc, TimeSpan.FromSeconds(60));
        if (mainWindow == null)
        {
            Console.Error.WriteLine("FAIL: Main window not found within 60 seconds");
            return 1;
        }
        Console.WriteLine("PASS: Main window found");

        // 2. Shell chrome (30 s — allows slow first-launch)
        if (!CheckRequiredElements(mainWindow, proc, TimeSpan.FromSeconds(30), "Shell", ShellElements))
            return 1;

        // 3. TempNest (default active workspace)
        if (!CheckRequiredElements(mainWindow, proc, TimeSpan.FromSeconds(15), "TempNest", TempNestElements))
            return 1;

        // 4. NoteNest — click launcher, then check workspace elements
        Console.WriteLine("Navigating to NoteNest...");
        if (!ClickElementByPoint(mainWindow, "Shell.NoteNestLaunchButton"))
        {
            Console.Error.WriteLine("FAIL: Could not click Shell.NoteNestLaunchButton");
            return 1;
        }
        Thread.Sleep(800);
        if (!CheckRequiredElements(mainWindow, proc, TimeSpan.FromSeconds(15), "NoteNest", NoteNestElements))
            return 1;

        // 5. IdeaNest
        Console.WriteLine("Navigating to IdeaNest...");
        if (!ClickElementByPoint(mainWindow, "Shell.IdeaNestLaunchButton"))
        {
            Console.Error.WriteLine("FAIL: Could not click Shell.IdeaNestLaunchButton");
            return 1;
        }
        Thread.Sleep(800);
        if (!CheckRequiredElements(mainWindow, proc, TimeSpan.FromSeconds(15), "IdeaNest", IdeaNestElements))
            return 1;

        // 6. ChatNest
        Console.WriteLine("Navigating to ChatNest...");
        if (!ClickElementByPoint(mainWindow, "Shell.ChatNestLaunchButton"))
        {
            Console.Error.WriteLine("FAIL: Could not click Shell.ChatNestLaunchButton");
            return 1;
        }
        Thread.Sleep(800);
        if (!CheckRequiredElements(mainWindow, proc, TimeSpan.FromSeconds(15), "ChatNest", ChatNestElements))
            return 1;

        Console.WriteLine("PASS: All smoke test checks passed");
        return 0;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static AutomationElement? WaitForMainWindow(Process proc, TimeSpan timeout)
    {
        var deadline = DateTime.Now + timeout;
        while (DateTime.Now < deadline)
        {
            if (proc.HasExited)
            {
                Console.Error.WriteLine($"FAIL: Process exited unexpectedly (code {proc.ExitCode})");
                return null;
            }
            try
            {
                var win = AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.ProcessIdProperty, proc.Id));
                if (win != null) return win;
            }
            catch { }
            Thread.Sleep(500);
        }
        return null;
    }

    static AutomationElement? WaitForElementByAutomationId(
        AutomationElement root, Process proc, string id, DateTime deadline)
    {
        while (DateTime.Now < deadline)
        {
            if (proc.HasExited)
            {
                Console.Error.WriteLine($"FAIL: Process exited while waiting for '{id}'");
                return null;
            }
            try
            {
                var el = root.FindFirst(
                    TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, id));
                if (el != null) return el;
            }
            catch { }
            Thread.Sleep(200);
        }
        return null;
    }

    static bool CheckRequiredElements(
        AutomationElement root, Process proc, TimeSpan timeout, string group, string[] ids)
    {
        Console.WriteLine($"Checking {group} elements...");
        var deadline = DateTime.Now + timeout;
        foreach (var id in ids)
        {
            var el = WaitForElementByAutomationId(root, proc, id, deadline);
            if (el == null)
            {
                Console.Error.WriteLine($"FAIL [{group}]: Element '{id}' not found within timeout");
                return false;
            }
            Console.WriteLine($"PASS [{group}]: '{id}' found");
        }
        return true;
    }

    static bool ClickElementByPoint(AutomationElement root, string automationId)
    {
        try
        {
            var el = root.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));
            if (el == null)
            {
                Console.Error.WriteLine($"  [click] Element '{automationId}' not found");
                return false;
            }

            var hwnd = new IntPtr(root.Current.NativeWindowHandle);
            SetForegroundWindow(hwnd);
            Thread.Sleep(100);

            System.Windows.Point pt;
            if (!el.TryGetClickablePoint(out pt))
            {
                var rect = el.Current.BoundingRectangle;
                pt = new System.Windows.Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
            }

            SetCursorPos((int)pt.X, (int)pt.Y);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            Console.WriteLine($"  [click] Clicked '{automationId}' at ({pt.X:F0},{pt.Y:F0})");
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  [click] Exception clicking '{automationId}': {ex.Message}");
            return false;
        }
    }
}
