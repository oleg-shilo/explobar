using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Explobar
{
    static class App
    {
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

        public static void PrintGenericHelp(string[] args)
        {
            var outFile = args.Skip(1).FirstOrDefault() ?? "help.txt";
            var helpText = Globals.CliHelpText;
            File.WriteAllText(outFile, helpText);
            Process.Start(new ProcessStartInfo(outFile) { UseShellExecute = true });
        }

        public static void PrintConfigHelp(string[] args)
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

        public static void KillRunningInstances()
        {
            ConsoleManager.AllocateVisible();

            var currentProcess = Process.GetCurrentProcess();
            var processName = currentProcess.ProcessName;
            var currentPid = currentProcess.Id;

            var runningInstances = Process.GetProcessesByName(processName)
                .Where(p => p.Id != currentPid)
                .ToList();

            if (runningInstances.Count == 0)
            {
                Console.WriteLine("No running instances of Explobar found.");
            }
            else
            {
                Console.WriteLine($"Found {runningInstances.Count} running instance(s) of Explobar:");

                foreach (var process in runningInstances)
                {
                    try
                    {
                        Console.WriteLine($"  - Killing process {process.Id} (started: {process.StartTime})");
                        process.Kill();
                        process.WaitForExit(2000); // Wait up to 2 seconds for graceful exit
                        Console.WriteLine($"    Successfully terminated process {process.Id}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"    Failed to kill process {process.Id}: {ex.Message}");
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }

                Console.WriteLine();
                Console.WriteLine("Done.");
            }
        }

        public static void ShowDefaultConfig()
        {
            try
            {
                // Get the config folder path
                string defaultConfigFile = ConfigManager.ConfigPath.ChangeFileName("default-config.yaml");

                ConfigManager.SaveDefaultConfig(defaultConfigFile);

                Console.WriteLine($"Default configuration saved to: {defaultConfigFile}");

                // Open with default text viewer
                Process.Start(new ProcessStartInfo(defaultConfigFile) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error printing default config: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}