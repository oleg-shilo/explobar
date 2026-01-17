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

    public class ToolbarForm : Form
    {

        private System.Windows.Forms.Timer checkMouseTimer;
        private bool enableMouseCheck = false;
        private FlowLayoutPanel toolbarPanel;

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_SHOWWINDOW = 0x0040;

        public ToolbarForm(List<string> items)
        {
            this.Text = "Selected Items";
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.KeyPreview = true;
            this.ShowInTaskbar = false;
            this.BackColor = System.Drawing.Color.DarkGray;
            this.Padding = new Padding(1);
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // Create toolbar panel
            toolbarPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.White,
                Padding = new Padding(1),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            foreach (var item in ToolbarItems.Items)
            {
                AddToolbarButton(item, items);
                // Add sample buttons (will be added dynamically in the future)
                // AddToolbarButton("1", (s, e) => MessageBox.Show("Button 1 clicked"));
                // AddToolbarButton("2", (s, e) => MessageBox.Show("Button 2 clicked"));
                // AddToolbarButton("3", (s, e) => MessageBox.Show("Button 3 clicked"));

                this.Controls.Add(toolbarPanel);

                // Ensure topmost when form is shown
                this.Shown += (s, e) =>
                {
                    SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                    this.Activate();
                };

                // Wait 2 seconds before starting mouse check
                var delayTimer = new System.Windows.Forms.Timer { Interval = 2000 };
                delayTimer.Tick += (s, e) =>
                {
                    delayTimer.Stop();
                    enableMouseCheck = true;
                    checkMouseTimer.Start();
                };
                delayTimer.Start();

                // Check mouse position every 100ms
                checkMouseTimer = new System.Windows.Forms.Timer { Interval = 100 };
                checkMouseTimer.Tick += CheckMouseTimer_Tick;
            }
        }
        private void AddToolbarButton(ToolbarItem info, List<string> selectedItems)
        {
            var button = new Button
            {
                Width = 30,
                Height = 30,
                Image = info.IconPath.ExtractIcon(),
                FlatStyle = FlatStyle.Flat,
                BackColor = System.Drawing.Color.White
            };
            button.Click += (_, _) =>
            {
                info.Execute(selectedItems);
                checkMouseTimer?.Stop();
                this.Close();
            };
            toolbarPanel.Controls.Add(button);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                checkMouseTimer?.Stop();
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void CheckMouseTimer_Tick(object? sender, EventArgs e)
        {
            if (!enableMouseCheck)
                return;
            try
            {
                var cursorPos = Cursor.Position;
                var formBounds = new System.Drawing.Rectangle(this.Location, this.Size);

                if (!formBounds.Contains(cursorPos))
                {
                    checkMouseTimer.Stop();
                    this.Close();
                }
            }
            catch
            {
                // Ignore errors
            }
        }
    }
}