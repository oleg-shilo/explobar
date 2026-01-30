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
// ✅ applications
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
        static UserInputMonitor _inputMonitor;
        static bool _isProcessing = false;
        static Mutex _singleInstanceMutex;

        [STAThread]
        static void Main(string[] args)
        {
            bool createdNew;
            _singleInstanceMutex = new Mutex(true, "Global\\Explobar_SingleInstance", out createdNew);

            if (!createdNew)
            {
                MessageBox.Show("Explobar is already running.", "Explobar",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                ExplorerHistory.StartMonitoring(); // for history

                _inputMonitor = new UserInputMonitor();
                _inputMonitor.OnShortcutPressed += InputMonitor_OnShortcutPressed;
                _inputMonitor.Start();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                AppNotify.Setup();

                Application.ApplicationExit += (s, e) =>
                {
                    ExplorerHistory.StopMonitoring();
                    _inputMonitor?.Stop();
                    AppNotify.Dispose();

                    try
                    {
                        _singleInstanceMutex?.ReleaseMutex();
                    }
                    catch { }
                    _singleInstanceMutex?.Dispose();
                };

                ToolbarForm.Preheat();

                Application.Run();
            }
            finally
            {
                try
                {
                    _singleInstanceMutex?.ReleaseMutex();
                }
                catch { }
                _singleInstanceMutex?.Dispose();
            }
        }

        static void InputMonitor_OnShortcutPressed(Keys key)
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