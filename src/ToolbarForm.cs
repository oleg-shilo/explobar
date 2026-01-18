using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Explobar
{
    public class DoNotShowInVSDesigner { }

    public class ToolbarForm : Form
    {
        System.Windows.Forms.Timer checkMouseTimer;
        bool enableMouseCheck = false;
        FlowLayoutPanel toolbarPanel;
        dynamic explorer = null;

        ToolTip toolTip;

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        static bool SetForegroundWindow(ulong hWnd) => SetForegroundWindow(hWnd == 0 ? IntPtr.Zero : new IntPtr((int)hWnd));

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_SHOWWINDOW = 0x0040;

        public ToolbarForm(List<string> items, dynamic explorerWindow)
        {
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
                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                this.Activate();
                Task.Run(() =>
                {
                    Thread.Sleep(1000);
                    try
                    {
                        this.BeginInvoke((Action)(() =>
                        {
                            enableMouseCheck = true;
                            checkMouseTimer?.Start();
                        }));
                    }
                    catch
                    {
                        // Ignore errors
                    }
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
            };

            this.Controls.Add(toolbarPanel);
            foreach (var item in ToolbarItems.Items)
            {
                AddToolbarButton(item, items, explorerWindow);
            }
        }

        void AddToolbarButton(ToolbarItem info, List<string> selectedItems, dynamic explorerWindow)
        {
            using (var originalIcon = info.IconPath.ExtractIcon(info.IconIndex))
            {
                var resizedIcon = new Bitmap(24, 24);
                // Resize icon to exactly 24x24
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

                if (info.Tooltip.HasText())
                    toolTip.SetToolTip(button, info.Tooltip);

                button.Click += (x, y) =>
                {
                    this.Close();
                    checkMouseTimer?.Stop();
                    SetForegroundWindow((IntPtr)explorer.HWND);

                    bool test = false;
                    if (test)
                    {
                        // opening new tab and navigating to C:\Windows

                        var tabs = Explorer.GetTabs();
                        SentCtrlT();
                        Thread.Sleep(100);

                        var newTab = Explorer.GetTabs().Except(tabs).FirstOrDefault();
                        if (newTab != null)
                        {
                            Explorer.NavigateToPath(newTab, @"C:\Windows");
                        }
                    }
                    else
                    {
                        var explorerDir = (string)explorer?.Document?.Folder?.Self?.Path?.ToString();
                        info.Execute(selectedItems, explorerDir);
                    }
                };
                toolbarPanel.Controls.Add(button);
            }
        }

        void SentCtrlT()
        {
            SetForegroundWindow((IntPtr)explorer.HWND);
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

        void CheckMouseTimer_Tick(object sender, EventArgs e)
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