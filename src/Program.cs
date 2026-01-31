using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

// using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

// TODO
//     Allow scripted buttons
// settings:
// ✅ configure shortcut
// ✅ support shortcuts
//
// buttons:
// ✅ create new file
// ✅ create new folder
// ✅ create new tab
// ✅ show selected file properties
// ✅ navigate from clipboard content
// ✅ button separator
// ✅ favorites
//     apps
// ✅ recent folders
// ✅ config button should pop up the menu for
//    ✅ edit config
//    ✅ explore icons
//    ✅ about box
//
// misc:
// ✅  Keep history of Icon explorer navigation
// ✅  Tray Icon support
// ✅  App Singleton
// ✅  Button default icon
// ✅  Profiler
// ✅  Shortcut in tooltip
// ✅  app icon
// ✅  taskbar icon for Icon Browser
// ✅  recent for Icon Browser

namespace Explobar
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // ConsoleHelper.AttachConsole();

            // Console.WriteLine("Hello from WinForms 👋");

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

        static bool _isProcessing = false;

        static void OnShortcutPressed(Keys key)
        {
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