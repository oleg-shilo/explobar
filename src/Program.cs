using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

// TODO
// settings:
//     configure shortcut
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
        private static LowLevelKeyboardHook _keyboardHook;
        private static bool _isProcessing = false;

        [STAThread]
        static void Main(string[] args)
        {
            // Start monitoring Explorer windows for history
            ExplorerHistory.StartMonitoring();

            // Set up keyboard hook
            _keyboardHook = new LowLevelKeyboardHook();
            _keyboardHook.OnKeyPressed += KeyboardHook_OnKeyPressed;
            _keyboardHook.HookKeyboard();

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
                _keyboardHook?.UnhookKeyboard();
            };

            // Keep the application running
            Application.Run();
        }

        private static void KeyboardHook_OnKeyPressed(Keys key)
        {
            // if (key == Keys.LShiftKey && !_isProcessing)
            // if (key == Keys.Oemtilde && !_isProcessing)
            if (key == Keys.Escape && !_isProcessing)
            {
                _isProcessing = true;

                // Execute on a new STA thread to avoid COM issues
                var thread = new Thread(() =>
                {
                    try
                    {
                        (var root, var selection, var window) = Explorer.GetSelection();

                        if (root != null)
                        {
                            // foreach (var item in selection) Runtime.Log(item);

                            bool isToolbarHidden = (ToolbarForm.HideOnClosing && ToolbarForm.Instance != null);
                            Runtime.Log($"root ({(isToolbarHidden ? "unhide" : "show")}): {root}");

                            if (isToolbarHidden)
                            {
                                // ShowToolbarForm will not block
                                Action unhide = () => Desktop.ShowToolbarForm(root, selection, window, startMessagePump: false);
                                ToolbarForm.Instance.Invoke(unhide);
                            }
                            else
                            {
                                // ShowToolbarForm will block until the form is closed
                                Desktop.ShowToolbarForm(root, selection, window, startMessagePump: true);
                            }
                        }
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
}