using System;
using System.Linq;
using System.Windows.Forms;

// TODO
//    drugging explorer button
//    theme for explorer button (e.g. dark / light / system)
// ✅ print default config file
// ✅ App -kill to kill any running instance
// ✅ open config folder
// ✅ visibility of explorer button should be configurable (e.g. only show if the current folder is not the same as the one in the toolbar)
// ✅ Add explorer button for popping toolbar up
namespace Explobar
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Any(a => a.SameAsEither(Globals.CliArgHelp, "-h", "--help", "/?", "?", "-?")))
            {
                App.PrintGenericHelp(args);
            }
            else if (args.Any(a => a.SameAsEither(Globals.CliArgConfigHelp, "-config-help", "--config-help")))
            {
                App.PrintConfigHelp(args);
            }
            else if (args.Any(a => a.SameAsEither(Globals.CliArgKill, "--kill")))
            {
                App.KillRunningInstances();
            }
            else
            {
                try
                {
                    string otherInstancePidToWaitFor = args.FirstOrDefault(x => x.StartsWith($"{Globals.CliArgWait}:"))?.Substring(6);
                    otherInstancePidToWaitFor?.GetProcess()?.WaitForExit();

                    if (SingleInstanceApp.AnotherInstanceDetected())
                    {
                        Runtime.ShowError("Explobar is already running.");
                        return;
                    }

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    ConsoleManager.AllocateHidden();

                    if (ToolbarItems.Settings.ShowConsoleAtStartup)
                        ConsoleManager.Show();

                    UserInputMonitor.StartMonitor(App.OnShortcutPressed);
                    ExplorerHistory.StartMonitor();
                    Desktop.StartMonitoringAllExplorerWindows();

                    AppNotify.Setup();

                    Application.ApplicationExit += (s, e) =>
                    {
                        ExplorerHistory.StopMonitor();
                        UserInputMonitor.StopMonitor();
                        AppNotify.Dispose();
                        ConsoleManager.Hide();
                        SingleInstanceApp.Clear();
                    };

                    ToolbarForm.Preheat();
                    Profiler.Reset();

                    Application.Run();

                    SingleInstanceApp.Clear();
                }
                catch (Exception ex)
                {
                    Runtime.Log("An unexpected error occurred: " + ex.Message);
                }
            }
        }
    }
}