using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shell32;

namespace Explobar
{
    public class DoNotShowFormInVS { }

    public class ToolbarForm : Form
    {
        System.Windows.Forms.Timer checkMouseTimer;
        bool enableMouseCheck = false;
        FlowLayoutPanel toolbarPanel;
        IntPtr previousFocusedWindow = IntPtr.Zero;

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
                // Padding = new Padding(1),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            // Ensure topmost when form is shown
            this.FormClosing += (s, e) =>
            {
                checkMouseTimer?.Stop();
                checkMouseTimer?.Dispose();

                // Restore focus to the previous window
                if (previousFocusedWindow != IntPtr.Zero)
                {
                    SetForegroundWindow(previousFocusedWindow);
                }
            };

            this.Shown += (s, e) =>
            {
                // Capture the current focused window before showing the toolbar
                previousFocusedWindow = GetForegroundWindow();

                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                this.Activate();
                Task.Run(() =>
                {
                    Thread.Sleep(1000);
                    this.BeginInvoke((Action)(() =>
                    {
                        Console.WriteLine("Enabling mouse check");
                        enableMouseCheck = true;
                        checkMouseTimer?.Start();
                    }));
                });
            };

            foreach (var item in ToolbarItems.Items)
            {
                AddToolbarButton(item, items);
                this.Controls.Add(toolbarPanel);
                // Wait 2 seconds before starting mouse check
                // Task.Run(() =>
                // {
                //     Thread.Sleep(2000);
                // });
                // var delayTimer = new System.Windows.Forms.Timer { Interval = 1000 };
                // delayTimer.Tick += (s, e) =>
                // {
                //     delayTimer.Stop();
                //     Console.WriteLine("Enabling mouse check");

                //     enableMouseCheck = true;
                //     checkMouseTimer.Start();
                // };
                // delayTimer.Start();

                // Check mouse position every 100ms
                checkMouseTimer = new System.Windows.Forms.Timer { Interval = 100 };
                checkMouseTimer.Tick += CheckMouseTimer_Tick;
            }
        }

        void AddToolbarButton(ToolbarItem info, List<string> selectedItems)
        {
            var originalIcon = info.IconPath.ExtractIcon();

            // Resize icon to exactly 24x24
            var resizedIcon = new System.Drawing.Bitmap(24, 24);
            using (var graphics = System.Drawing.Graphics.FromImage(resizedIcon))
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
                BackColor = System.Drawing.Color.Transparent,
                Cursor = Cursors.Hand
            };

            // Configure border and appearance
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(64, System.Drawing.Color.LightBlue);
            button.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;

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