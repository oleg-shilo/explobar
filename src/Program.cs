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
// settings:
//     ✅ configure shortcut
//     support shortcuts
//
// buttons:
// ✅ create new file
// ✅ create new folder`
// ✅ create new tab
// ✅ show selected file properties
// ✅ navigate from clipboard content
// ✅ button separator
// ✅ favorites
// ✅ applications
// ✅ recent folders
// ✅ config button should pop up the menu for
//    ✅ edit config
//    ✅ explore icons
//    ✅ about box
//
// misc:
// ✅ Keep history of Icon explorer navigation
//    Tray Icon support
//    App Singleton
//    Button default icon

namespace Explobar
{
    internal class Program
    {
        private static UserInputMonitor _inputMonitor;
        private static bool _isProcessing = false;

        [STAThread]
        static void Main(string[] args)
        {
            // Start monitoring Explorer windows for history
            ExplorerHistory.StartMonitoring();

            // Set up input monitoring
            _inputMonitor = new UserInputMonitor();
            _inputMonitor.OnShortcutPressed += InputMonitor_OnShortcutPressed;
            _inputMonitor.Start();

            Task.Run(() =>
            {
                Console.ReadLine();
                Application.Exit();
            });

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Handle application exit
            Application.ApplicationExit += (s, e) =>
            {
                ExplorerHistory.StopMonitoring();
                _inputMonitor?.Stop();
            };

            ToolbarForm.Preheat();

            // Keep the application running
            Application.Run();
        }

        private static void InputMonitor_OnShortcutPressed(Keys key)
        {
            if (_isProcessing)
                return;

            _isProcessing = true;

            Profiler.Start();
            // Execute on a new STA thread to avoid COM issues
            var thread = new Thread(() =>
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
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            if (ToolbarForm.HideOnClosing)
                _isProcessing = false;
        }
    }
}