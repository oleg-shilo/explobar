using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Explobar
{
    public class ExplorerContext
    {
        public dynamic Window { get; set; }
        public string RootPath { get; set; }
        public List<string> SelectedItems { get; set; }
    }

    public class ToolbarForm : Form
    {
        static public Form ActiveInstance()
        {
            if (currentInstance != null && reuseInstance)
                return currentInstance;
            else
                return null;
        }

        public static bool reuseInstance = true;

        public static ToolbarForm currentInstance = null;

        public static (ToolbarForm, bool existing) Create()
        {
            if (currentInstance == null || !reuseInstance)
            {
                currentInstance = new ToolbarForm();
                currentInstance.Init();
                return (currentInstance, false);
            }
            return (currentInstance, true);
        }

        System.Windows.Forms.Timer checkMouseTimer;
        bool enableMouseCheck = false;
        FlowLayoutPanel toolbarPanel;

        public readonly ExplorerContext ExplorerContext = new ExplorerContext();

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

        public void PlaceUnderCursor()
        {
        }

        // public void Init(List<string> items, dynamic explorerWindow)
        public void Init()
        {
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

            if (checkMouseTimer == null)
            {
                checkMouseTimer = new System.Windows.Forms.Timer { Interval = 100 };
                checkMouseTimer.Tick += CheckMouseTimer_Tick;
            }

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
                if (item.Path == "{separator}")
                    AddToolbarGroupSeparator();
                else
                    AddToolbarButton(item);
            }
        }

        int buttonSize = 24;
        int imagePadding = 2;

        void AddToolbarGroupSeparator()
        {
            var separator = new Panel
            {
                Width = 1,
                Height = buttonSize,
                BackColor = Color.Gray,
                Margin = new Padding(imagePadding, imagePadding, imagePadding, imagePadding)
            };
            toolbarPanel.Controls.Add(separator);
        }

        void AddToolbarButton(ToolbarItem info)
        {
            var iconIndex = info.IconIndex;
            using (var originalIcon = info.IconPath.IfEmpty(info.Path).ExtractIcon(info.IconIndex))
            {
                int imageSize = buttonSize - imagePadding - imagePadding;
                var resizedIcon = new Bitmap(imageSize, imageSize);

                // Resize icon to exactly 24x24
                if (originalIcon != null)
                    using (var graphics = Graphics.FromImage(resizedIcon))
                    {
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(originalIcon, 0, 0, imageSize, imageSize);
                    }

                var button = new Button
                {
                    Width = buttonSize,
                    Height = buttonSize,
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

                toolTip.SetToolTip(button, info.Tooltip.IfEmpty(info.Path.GetFileName()));

                button.Click += (x, y) =>
                {
                    HideToolbar();
                    SetForegroundWindow((IntPtr)ExplorerContext.Window.HWND);

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
                        var explorerDir = (string)ExplorerContext.Window?.Document?.Folder?.Self?.Path?.ToString();
                        info.Execute(this.ExplorerContext);
                    }
                };
                toolbarPanel.Controls.Add(button);
            }
        }

        void SentCtrlT()
        {
            SetForegroundWindow((IntPtr)ExplorerContext.Window.HWND);
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
                HideToolbar();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        void HideToolbar()
        {
            checkMouseTimer?.Stop();
            if (reuseInstance)
                this.Hide();
            else
                this.Close();
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
                    HideToolbar();
                }
            }
            catch
            {
                // Ignore errors
            }
        }
    }
}