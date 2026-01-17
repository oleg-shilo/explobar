using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Explbar;

// using static System.Windows.Forms.VisualStyles.VisualStyleElement;
// using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using Shell32;

namespace Explobar
{
    public class DoNotShowFormInVS { }

    public class ToolbarForm : Form
    {
        System.Windows.Forms.Timer checkMouseTimer;
        bool enableMouseCheck = false;
        FlowLayoutPanel toolbarPanel;
        dynamic explorer = null;
        IntPtr previousFocusedWindow;

        ToolTip toolTip;

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_SHOWWINDOW = 0x0040;

        public ToolbarForm(List<string> items, dynamic explorerWindow)
        {
            long hWnd = explorerWindow.HWND;
            this.previousFocusedWindow = hWnd != 0 ? new IntPtr(hWnd) : IntPtr.Zero;
            this.explorer = explorerWindow;
            this.Text = "Selected Items";
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.KeyPreview = true;
            this.ShowInTaskbar = false;
            this.BackColor = Color.DarkGray;
            this.Padding = new Padding(1);
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // Initialize tooltip
            toolTip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 500,
                ReshowDelay = 100,
                ShowAlways = true
            };

            // Create toolbar panel
            toolbarPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                // BackColor = Color.AliceBlue,
                BackColor = Color.WhiteSmoke,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            this.Shown += (s, e) =>
            {
                long hWnd = explorerWindow.HWND;
                previousFocusedWindow = hWnd != 0 ? new IntPtr(hWnd) : IntPtr.Zero;

                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                this.Activate();
                Task.Run(() =>
                {
                    Thread.Sleep(1000);
                    this.BeginInvoke((Action)(() =>
                    {
                        enableMouseCheck = true;
                        checkMouseTimer?.Start();
                    }));
                });
            };
            checkMouseTimer = new System.Windows.Forms.Timer { Interval = 100 };
            checkMouseTimer.Tick += CheckMouseTimer_Tick;

            // Ensure topmost when form is shown
            this.FormClosing += (s, e) =>
            {
                checkMouseTimer?.Stop();
                checkMouseTimer?.Dispose();
                toolTip?.Dispose();

                // Restore focus to the previous window
                if (previousFocusedWindow != IntPtr.Zero)
                {
                    SetForegroundWindow(previousFocusedWindow);
                }
            };

            foreach (var item in ToolbarItems.Items)
            {
                AddToolbarButton(item, items);
                this.Controls.Add(toolbarPanel);
            }
        }

        void AddToolbarButton(ToolbarItem info, List<string> selectedItems)
        {
            var originalIcon = info.IconPath.ExtractIcon(info.IconIndex);

            // Resize icon to exactly 24x24
            var resizedIcon = new Bitmap(24, 24);
            using (var graphics = Graphics.FromImage(resizedIcon))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(originalIcon, 0, 0, 24, 24);
            }

            var button = new Button
            {
                Width = 32,
                Height = 32,
                BackgroundImage = resizedIcon,
                BackgroundImageLayout = ImageLayout.Center,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            // Configure border and appearance
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, Color.LightBlue);
            button.FlatAppearance.MouseDownBackColor = Color.Transparent;

            // Set tooltip if available
            if (!string.IsNullOrEmpty(info.Tooltip))
            {
                toolTip.SetToolTip(button, info.Tooltip);
            }

            button.Click += (_, _) =>
            {
                bool test = false;
                if (test)
                {
                    SentCtrlT();
                    while (GetForegroundWindow() == previousFocusedWindow)
                        Thread.Sleep(100);
                    // need to get new explorer window, this.explorer is old
                    // Explorer.NavigateToPath(explorer, @"C:\Windows");
                }
                else
                {
                    info.Execute(selectedItems);
                    checkMouseTimer?.Stop();
                }
                this.Close();
            };
            toolbarPanel.Controls.Add(button);
        }

        void SentCtrlT()
        {
            SetForegroundWindow(previousFocusedWindow);
            SendKeys.Flush();
            Thread.Sleep(10);
            SendKeys.SendWait("^t");
            Thread.Sleep(10);
            SendKeys.Flush();
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

        void CheckMouseTimer_Tick(object? sender, EventArgs e)
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