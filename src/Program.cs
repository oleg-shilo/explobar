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
//      create new file
//      create new folder
//      favorites
//      recent folders
//      navigate from clipboard content
//      show selected file properties
//      duplicate explorer tab in a new window
//      button separator

namespace Explobar
{
    internal class Program
    {
        private static LowLevelKeyboardHook _keyboardHook;
        private static bool _isProcessing = false;

        [STAThread]
        static void Main(string[] args)
        {
            Explorer.ShowWarning = (msg) =>
                MessageBox.Show(msg, "Explobar", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            // var ttt = AutomationHelper.GetExplorer(h)?.FindTabControl()?.GetTabs().FirstOrDefault(t => t.IsActive())?.Current.Name;
            // return;
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

            // Cleanup on exit
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
                        Console.WriteLine(">>>");
                        CheckUserInputAndPopupToolbar();
                        Console.WriteLine("<<<");
                    }
                    finally
                    {
                        _isProcessing = false;
                    }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }
        }

        static void CheckUserInputAndPopupToolbar()
        {
            Console.WriteLine("------------");
            (var root, var selection, var window) = Explorer.GetSelection();
            Console.WriteLine("root: " + root);

            if (root != null)
            {
                foreach (var item in selection)
                {
                    Console.WriteLine(item);
                }

                Desktop.GetCursorPos(out Desktop.POINT cursorPos);
                Desktop.ShowToolbarForm(root, selection, cursorPos.X, cursorPos.Y, window);

                // Wait a bit to avoid showing multiple forms
                // Thread.Sleep(2000);
            }
        }
    }
}