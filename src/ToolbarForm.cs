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
using Explobar;
using static Explobar.Desktop;
using Shell32;

namespace Explobar
{
    public class ExplorerContext
    {
        dynamic window;

        public dynamic Window
        {
            get => window;

            set
            {
                window = value;
                _HWND = (IntPtr)(window?.HWND ?? 0);
            }
        }

        public IntPtr HWND => _HWND;
        public IntPtr _HWND;
        public string RootPath { get; set; }
        public List<string> SelectedItems { get; set; }
    }

    public class ToolbarForm : Form
    {
        // In order to reduce flickering when showing the toolbar, there are three modes of operation:
        //
        // 1. Normal mode: create a new instance each time the toolbar is shown
        // 2. Hot loading mode: prepare the next instance in advance and reuse it to reduce flickering
        // 3. Hidden mode: keep the form hidden when not in use
        //
        // #3 gives the best user experience and requires #2 to be disabled.

        static bool useHotLoading = false;
        public static bool HideOnClosing = true;

        static ToolbarForm nextInstance = null;

        public static ToolbarForm Instance = null;

        public static void ClearCache() => nextInstance = null;

        public static ToolbarForm Create()
        {
            // To avoid flickering, we create the next instance in advance and reuse it
            if (useHotLoading)
            {
                var result = nextInstance ?? new ToolbarForm().Init();

                // Despite the next instance initialization being blocking (called in the same thread),
                // the performance benefit is still visible. And we also avoid cross-thread issues of using another thread.
                nextInstance = new ToolbarForm();
                nextInstance.Init();

                return result;
            }
            else
            {
                return new ToolbarForm().Init();
            }
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

            this.Controls.Add(toolbarPanel);

            foreach (var item in ToolbarItems.Items)
            {
                if (item.Path == "{separator}")
                    AddToolbarGroupSeparator();
                else
                    AddToolbarButton(item);
            }

            if (checkMouseTimer == null)
            {
                checkMouseTimer = new System.Windows.Forms.Timer { Interval = 100 };
                checkMouseTimer.Tick += CheckMouseTimer_Tick;
            }

            this.Shown += (s, e) =>
            {
                Instance = this;
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
                            // Runtime.Log("Started mouse check timer");
                        }));
                    }
                    catch
                    {
                        // Ignore errors
                    }
                });
            };

            this.FormClosing += (s, e) =>
            {
                if (HideOnClosing)
                {
                    // Runtime.Log("Hiding toolbar instead of closing");
                    e.Cancel = true;
                    this.Hide();
                }
                else
                {
                    // Runtime.Log("Disposing toolbar");
                    checkMouseTimer?.Stop();
                    checkMouseTimer?.Dispose();
                    toolTip?.Dispose();
                    Instance = null;
                }
            };

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
            Button button;
            Bitmap resizedIcon;
            ICustomButton customButton = null;

            string iconPath;
            int iconIndex;

            bool isStockButton = info.Path.StartsWith("{") && StockToolbarControls.Items.ContainsKey(info.Path);

            if (isStockButton)
            {
                button = StockToolbarControls.Items[info.Path]();
                customButton = (button as ICustomButton);
                iconPath = customButton.IconPath.ExpandEnvars().IfEmpty(info.IconPath);
                iconIndex = customButton.IconIndex;
            }
            else
            {
                button = new Button();
                iconPath = info.IconPath.IfEmpty(info.Path);
                iconIndex = info.IconIndex;
            }

            resizedIcon = CreateButtonImage(iconPath, iconIndex);

            button.Width = buttonSize;
            button.Height = buttonSize;
            button.BackgroundImage = resizedIcon;
            button.BackgroundImageLayout = ImageLayout.Center;
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Color.Transparent;
            button.Cursor = Cursors.Hand;

            // Configure border and appearance
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, Color.LightBlue);
            button.FlatAppearance.MouseDownBackColor = Color.Transparent;

            toolTip.SetToolTip(button, info.Tooltip.IfEmpty(info.Path.GetFileName()));

            customButton?.OnInit(info, this.ExplorerContext);

            button.Click += (x, y) =>
            {
                try
                {
                    HideToolbar();
                    SetForegroundWindow((IntPtr)ExplorerContext.HWND);

                    if (customButton != null)
                        customButton.OnClick(this.ExplorerContext);
                    else
                        info.Execute(this.ExplorerContext);
                }
                catch (Exception e)
                {
                    Runtime.ShowError(e.Message);
                }
            };

            toolbarPanel.Controls.Add(button);
        }

        Bitmap CreateButtonImage(string iconPath, int iconIndex)
        {
            using (var originalIcon = iconPath.ExtractIcon(iconIndex))
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
                return resizedIcon;
            }
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
            // Runtime.Log("Stopping mouse timer");
            checkMouseTimer?.Stop();
            if (HideOnClosing)
            {
                // Runtime.Log("Hiding toolbar");
                this.Hide();
            }
            else
            {
                // Runtime.Log("Closing toolbar");
                this.Close();
            }
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