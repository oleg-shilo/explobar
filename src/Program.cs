using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shell32;

// using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Explobar
{
    internal class Program
    {
        private static LowLevelKeyboardHook? _keyboardHook;
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

            // Cleanup on exit
            _keyboardHook?.UnhookKeyboard();
        }

        private static void KeyboardHook_OnKeyPressed(Keys key)
        {
            if (key == Keys.LShiftKey && !_isProcessing)
            {
                _isProcessing = true;

                // Execute on a new STA thread to avoid COM issues
                var thread = new Thread(() =>
                {
                    try
                    {
                        CheckUserInputAndPopupToolbar();
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
            (var root, var selection) = UserInputListener.GetExplorerSelection();
            Console.WriteLine("root: " + root);

            if (root != null)
            {
                foreach (var item in selection)
                {
                    Console.WriteLine(item);
                }

                Desktop.GetCursorPos(out Desktop.POINT cursorPos);
                Desktop.ShowSelectionForm(root, selection, cursorPos.X, cursorPos.Y);

                // Wait a bit to avoid showing multiple forms
                // Thread.Sleep(2000);
            }
        }
    }
}