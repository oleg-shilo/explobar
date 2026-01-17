using Shell32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_SHOWWINDOW = 0x0040;

        public const uint GA_ROOT = 2;
        public const int VK_LSHIFT = 0xA0;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }
        public static void ShowSelectionForm(List<string> items, int x, int y)
        {
            var form = new ToolbarForm(items);
            form.StartPosition = FormStartPosition.Manual;

            // Offset from cursor to avoid it being under the cursor initially
            int offsetX = 0;
            int offsetY = 0;

            // Get screen bounds to ensure form is visible
            var screen = Screen.FromPoint(new System.Drawing.Point(x, y));
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