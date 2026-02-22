using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

// using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

// TODO
//    visibility of explorer button should be configurable (e.g. only show if the current folder is not the same as the one in the toolbar)
//    theme for explorer button (e.g. dark / light / system)
//    drugging explorer button 
//    CLI -kill to kill any running instance
//    print default config file
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
                PrintGenericHelp(args);
            }
            else if (args.Any(a => a.SameAsEither(Globals.CliArgConfigHelp, "-config-help", "--config-help")))
            {
                PrintConfigHelp(args);
            }
            else
            {
                try
                {
                    var otherInstanceToWaitFor = args.FirstOrDefault(x => x.StartsWith($"{Globals.CliArgWait}:"))?.Substring(6);

                    otherInstanceToWaitFor?.GetProcess()?.WaitForExit();

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

                    UserInputMonitor.StartMonitor(OnShortcutPressed);
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

        static void PrintGenericHelp(string[] args)
        {
            var outFile = args.Skip(1).FirstOrDefault() ?? "help.txt";
            var helpText = Globals.CliHelpText;
            File.WriteAllText(outFile, helpText);
            Process.Start(new ProcessStartInfo(outFile) { UseShellExecute = true });
        }

        static void PrintConfigHelp(string[] args)
        {
            var outFile = args.Skip(1).FirstOrDefault() ?? "config-help.txt";
            var helpText = Globals.ConfigFileHelp;
            if (!outFile.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                // clear MD markdown formatting for better readability in plain text
                helpText = helpText.ClearMdMarkup();
            }
            File.WriteAllText(outFile, helpText);
            Process.Start(new ProcessStartInfo(outFile) { UseShellExecute = true });
        }

        static bool _isProcessing = false;

        public static void OnShortcutPressed(Keys key)
        {
            // Ignore keystrokes while config is loading or error dialog is shown
            if (ConfigManager.IsConfigLoadingInProgress)
            {
                Runtime.Output("Keystroke ignored - config loading in progress");
                return;
            }

            if (_isProcessing)
                return;

            _isProcessing = true;

            Profiler.Start();

            ApartmentState.STA.Run(() => // Execute on a new STA thread to avoid COM issues
            {
                try
                {
                    (var root, var selection, var window) = Explorer.GetSelection();
                    Profiler.Log();

                    if (root != null)
                    {
                        bool isToolbarHidden = (ToolbarForm.Instance?.IsInitializedButHidden() == true);

                        if (isToolbarHidden)
                        {
                            // ShowToolbarForm will not block because we're unhiding an existing form (createNew: false)
                            Action unhide = () => Desktop.ShowToolbarForm(root, selection, window, createNew: false);
                            ToolbarForm.Instance.Invoke(unhide);
                        }
                        else
                        {
                            // ShowToolbarForm will block until the form is closed
                            Desktop.ShowToolbarForm(root, selection, window, createNew: true);
                        }
                    }
                    else
                        Profiler.Reset();
                }
                finally
                {
                    _isProcessing = false;
                }
            });

            if (ToolbarForm.HideOnClosing)
                _isProcessing = false;
        }
    }
}