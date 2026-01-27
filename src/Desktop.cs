using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using YamlDotNet.Core.Tokens;

namespace Explobar
{
    static class Desktop
    {
        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(Point point);

        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_SHOWWINDOW = 0x0040;

        public const uint GA_ROOT = 2;

        public static void ShowToolbarForm(string root, List<string> items, dynamic window, bool createNew)
        {
            if (!ToolbarItems.IsConfigUpToDate)
                ToolbarForm.ResetInstance();

            var form = createNew ?
                ToolbarForm.Create() :
                ToolbarForm.Instance ?? ToolbarForm.Create();

            form.ExplorerContext.RootPath = root;
            form.ExplorerContext.SelectedItems = items;
            form.ExplorerContext.Window = window;

            form.StartPosition = FormStartPosition.Manual;

            int offsetX = 0 - form.Width / 2;
            int offsetY = 0 - form.Height / 2;

            // Get screen bounds to ensure form is visible
            var cursorPos = Cursor.Position;

            var screen = Screen.FromPoint(cursorPos);
            int formX = Math.Min(cursorPos.X + offsetX, screen.WorkingArea.Right - form.Width);
            int formY = Math.Min(cursorPos.Y + offsetY, screen.WorkingArea.Bottom - form.Height);

            // Ensure it's not off the left or top edge
            formX = Math.Max(formX, screen.WorkingArea.Left);
            formY = Math.Max(formY, screen.WorkingArea.Top);

            form.Location = new Point(formX, formY);

            // Show the form and bring it to front
            form.Show();

            if (createNew)
                Application.Run(form);
        }

        public static void SendCtrlT()
        {
            const byte VK_CONTROL = 0x11;
            const byte VK_T = 0x54;

            // Simulate Ctrl + T
            SendKeyDown(VK_CONTROL);
            SendKeyDown(VK_T);
            SendKeyUp(VK_T);
            SendKeyUp(VK_CONTROL);
        }

        static void SendKeyDown(byte vk)
        {
            SendInput(vk, 0);
        }

        static void SendKeyUp(byte vk)
        {
            SendInput(vk, 2);
        }

        static void SendInput(byte vk, uint flags)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = 1; // Input type 1 is for keyboard input
            inputs[0].ki.wVk = vk;

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        public static void SentCtrlT(IntPtr hWnd)
        {
            SetForegroundWindow(hWnd);
            SendKeys.Flush();
            Thread.Sleep(10);
            SendKeys.SendWait("^t");
            Thread.Sleep(10);
            SendKeys.Flush();
        }

        // "^t" for Ctrl+T

        /// <summary>
        /// Sends the key input.
        ///
        /// <code language="txt">
        /// SendKeys Notation for Common Keys:
        /// Key         SendKeys Notation
        /// F2	        "{F2}"
        /// F5	        "{F5}"
        /// Enter	    "{ENTER}" or "~"
        /// Escape	    "{ESC}"
        /// Tab	        "{TAB}"
        /// Backspace	"{BACKSPACE}" or "{BS}"
        /// Delete	    "{DELETE}" or "{DEL}"
        /// Ctrl+C	    "^c"
        /// Ctrl+V	    "^v"
        /// Alt+F4	    "%{F4}"
        /// </code>
        /// </summary>
        /// <param name="hWnd">The h WND.</param>
        /// <param name="input">The input.</param>
        public static void SentKeyInput(IntPtr hWnd, string input)
        {
            SetForegroundWindow(hWnd);
            SendKeys.Flush();
            Thread.Sleep(10);
            SendKeys.SendWait(input);
            Thread.Sleep(10);
            SendKeys.Flush();
        }

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public int type;
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public const int VK_LSHIFT = 0xA0;
        public const int VK_CONTROL = 0x11;

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("shell32.dll")]
        static extern void SHChangeNotify(
    int wEventId,
    int uFlags,
    IntPtr dwItem1,
    IntPtr dwItem2);

        const int SHCNE_CREATE = 0x00000002;
        const int SHCNF_PATHW = 0x0005;

        public static void NotifyFileCreated(string path)
        {
            SHChangeNotify(SHCNE_CREATE, SHCNF_PATHW,
            Marshal.StringToHGlobalUni(path),
            IntPtr.Zero);
        }
    }
}