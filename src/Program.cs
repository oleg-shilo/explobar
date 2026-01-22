using System;
using System.Collections.Generic;
using System.Linq;

// using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

// TODO
// buttons:
//      create new file (shell32.dll,2)
//      create new folder (shell32.dll,4)
//      favorites (shell32.dll,44)
//      recent folders (shell32.dll,7+22*11)
// ✅ navigate from clipboard content
//      show selected file properties (shell32.dll,73)
//      duplicate explorer tab in a new window
// ✅ button separator

namespace Explobar
{
    internal class Program
    {
        private static LowLevelKeyboardHook _keyboardHook;
        private static bool _isProcessing = false;

        [STAThread]
        static void Main(string[] args)
        {
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
            // Keep the application running
            Application.Run();

            _keyboardHook?.UnhookKeyboard();
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