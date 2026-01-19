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

        public static void ShowToolbarForm(string root, List<string> items, dynamic window)
        {
            var form = ToolbarForm.Create();

            form.ExplorerContext.RootPath = root;
            form.ExplorerContext.SelectedItems = items;
            form.ExplorerContext.Window = window;

            form.StartPosition = FormStartPosition.Manual;

            int offsetX = 0 - form.Width / 2;
            int offsetY = 0 - form.Height / 2;

            // Get screen bounds to ensure form is visible
            Desktop.GetCursorPos(out Desktop.POINT cursorPos);

            var screen = Screen.FromPoint(new Point(cursorPos.X, cursorPos.Y));
            int formX = Math.Min(cursorPos.X + offsetX, screen.WorkingArea.Right - form.Width);
            int formY = Math.Min(cursorPos.Y + offsetY, screen.WorkingArea.Bottom - form.Height);

            // Ensure it's not off the left or top edge
            formX = Math.Max(formX, screen.WorkingArea.Left);
            formY = Math.Max(formY, screen.WorkingArea.Top);

            form.Location = new Point(formX, formY);

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