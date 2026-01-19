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
using static Explobar.Desktop;

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
        static ToolbarForm nextInstance = null;

        public static void ClearCache() => nextInstance = null;

        public static ToolbarForm Create()
        {
            // To avoid flickering, we create the next instance in advance and reuse it

            var result = nextInstance ?? new ToolbarForm().Init();

            nextInstance = new ToolbarForm();
            nextInstance.Init();

            return result;
        }

        System.Windows.Forms.Timer checkMouseTimer;
        bool enableMouseCheck = false;
        FlowLayoutPanel toolbarPanel;

        public readonly ExplorerContext ExplorerContext = new ExplorerContext();

        ToolTip toolTip;

        public ToolbarForm Init()
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

            toolTip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 500,
                ReshowDelay = 100,
                ShowAlways = true
            };

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

            return this;
        }

        int buttonSize = 24;
        int imagePadding => (int)(buttonSize * 0.1); // 10% padding

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

                // Resize icon to exactly imageSize x imageSize
                var resizedIcon = new Bitmap(imageSize, imageSize);

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
            this.Close();
        }

        void CheckMouseTimer_Tick(object sender, EventArgs e)
        {
            if (enableMouseCheck)
            {
                try
                {
                    var cursorPos = Cursor.Position;
                    var formBounds = new Rectangle(this.Location, this.Size);
                    formBounds.Inflate(15, 15); // add some tolerance

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
}