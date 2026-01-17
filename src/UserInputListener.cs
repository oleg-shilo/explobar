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

namespace Explobar
{
    class UserInputListener
    {
        public static (string root, List<string> selected) GetExplorerSelection()
        {
            var shell = new Shell();
            IntPtr foregroundWindow = Desktop.GetForegroundWindow();

            Desktop.GetCursorPos(out Desktop.POINT cursorPos);
            IntPtr windowUnderMouse = Desktop.WindowFromPoint(cursorPos);
            IntPtr rootWindowUnderMouse = Desktop.GetAncestor(windowUnderMouse, Desktop.GA_ROOT);

            bool isLeftShiftPressed = (Desktop.GetAsyncKeyState(Desktop.VK_LSHIFT) & 0x8000) != 0;

            var selectedPaths = new List<string>();
            string root = null;

            foreach (dynamic window in shell.Windows())
            {
                IntPtr windowHandle = new IntPtr(window.HWND);
                bool hasFocus = windowHandle == foregroundWindow;
                bool hasMouseOver = windowHandle == rootWindowUnderMouse;

                if (!hasFocus)
                {
                    Console.WriteLine("Window does not have focus, skipping.");
                    continue;
                }

                if (!hasMouseOver)
                {
                    Console.WriteLine("Window is not under mouse, skipping.");
                    continue;
                }

                if (!window.FullName.EndsWith("explorer.exe", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Window is not an Explorer window, skipping.");
                    continue;
                }

                dynamic folder = window.Document;
                if (folder == null)
                {
                    Console.WriteLine("Window document is null, skipping.");
                    continue;
                }

                root = window.Document.Folder.Self.Path.ToString();

                foreach (FolderItem item in folder.SelectedItems())
                {
                    selectedPaths.Add(item.Path);
                }

                break;
            }
            return (root, selectedPaths);
        }
    }

    // Low-level keyboard hook class
    public class LowLevelKeyboardHook
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public delegate void KeyPressedEventHandler(Keys key);

        public event KeyPressedEventHandler? OnKeyPressed;

        public LowLevelKeyboardHook()
        {
            _proc = HookCallback;
        }

        public void HookKeyboard()
        {
            _hookID = SetHook(_proc);
        }

        public void UnhookKeyboard()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule?.ModuleName ?? ""), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;
                OnKeyPressed?.Invoke(key);
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}