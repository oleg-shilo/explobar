using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Shell32;

namespace Explobar
{
    static class Desktop
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_SHOWWINDOW = 0x0040;

        public const uint GA_ROOT = 2;
        public const int VK_LSHIFT = 0xA0;

        const uint GW_HWNDPREV = 3;
        const int GWL_EXSTYLE = -20;
        const int WS_EX_TRANSPARENT = 0x00000020;
        const int WS_EX_LAYERED = 0x00080000;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;

            public bool IntersectsWith(RECT other)
            {
                return !(other.Left >= Right ||
                         other.Right <= Left ||
                         other.Top >= Bottom ||
                         other.Bottom <= Top);
            }
        }

        public static uint GetWindowProcess(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return 0;

            GetWindowThreadProcessId(hWnd, out uint processId);
            return processId;
        }

        public static void SendCtrlT()
        {
            SendKeyDown(VK_CONTROL);
            SendKeyDown((byte)'T');
            SendKeyUp((byte)'T');
            SendKeyUp(VK_CONTROL);
        }

        const byte VK_CONTROL = 0x11;

        static void SendKeyDown(byte vk) =>
            SendInput(vk, 0);

        static void SendKeyUp(byte vk) =>
            SendInput(vk, 2);

        static void SendInput(byte vk, uint flags)
        {
            var input = new INPUT
            {
                type = 1,
                ki = new KEYBDINPUT { wVk = vk, dwFlags = flags }
            };
            SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
        }

        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        struct INPUT
        {
            public uint type;
            public KEYBDINPUT ki;
        }

        struct KEYBDINPUT
        {
            public byte wVk;
            public uint dwFlags;
        }

        public static void ShowToolbarForm(string root, List<string> items, int x, int y, dynamic window)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = new ToolbarForm(items, window);
            form.StartPosition = FormStartPosition.Manual;

            // Offset from cursor to avoid it being under the cursor initially
            int offsetX = 0;
            int offsetY = 0;

            // Get screen bounds to ensure form is visible
            var screen = Screen.FromPoint(new Point(x, y));
            int formX = Math.Min(x + offsetX, screen.WorkingArea.Right - form.Width);
            int formY = Math.Min(y + offsetY, screen.WorkingArea.Bottom - form.Height);

            // Ensure it's not off the left or top edge
            formX = Math.Max(formX, screen.WorkingArea.Left);
            formY = Math.Max(formY, screen.WorkingArea.Top);

            form.Location = new System.Drawing.Point(formX, formY);

            // Show the form and bring it to front
            form.Show();
            form.BringToFront();
            form.Activate();
            SetForegroundWindow(form.Handle);
            SetWindowPos(form.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

            Application.Run(form);
        }
    }
}