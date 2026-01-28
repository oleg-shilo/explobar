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

    static class ExplorerContextExtensions
    {
        public static ExplorerContext GetFreshCopy(this ExplorerContext context)
        {
            // Fallback: try using the cached window
            dynamic window = Explorer.GetTab(context.RootPath, context.HWND) ?? context.Window;
            return
                new ExplorerContext
                {
                    Window = window,
                    _HWND = context.HWND,
                    RootPath = context.RootPath,
                    SelectedItems = context.SelectedItems
                };
        }
    }

    public class ToolbarForm : Form
    {
        // In order to reduce flickering when showing the toolbar, there are three modes of operation:
        //
        // 1. Normal mode: create a new instance each time the toolbar is shown
        // 2. Hidden mode: keep the form hidden when not in use
        //
        // #2 gives the best user experience and requires #2 to be disabled.

        public static bool HideOnClosing = true;
        public static ToolbarForm Instance = null;

        public static ToolbarForm GetInstance()
        {
            if (Instance != null && Instance.lastLoadedConfiguration == ToolbarItems.configFileTimestamp)
                return Instance;
            else
                return null;
        }
        public static void ResetInstance()
        {
            Instance?.Close();
            Instance?.Dispose();
            Instance = null;
        }
        public static ToolbarForm Preheat()
        {
            // To avoid flickering, we create the next instance in advance and reuse it
            Profiler.Log();
            if (Instance == null || Instance.lastLoadedConfiguration != ToolbarItems.configFileTimestamp)
                Instance = new ToolbarForm().Init();
            return Instance;
        }
        public bool IsInitializedButHidden()
        {
            return ToolbarForm.HideOnClosing && !this.Visible && this.IsHandleCreated;
        }


        public static ToolbarForm Create()
        {
            // To avoid flickering, we create the next instance in advance and reuse it
            Profiler.Log();

            ToolbarForm result = null;
            if (Instance != null && !Instance.IsHandleCreated)
            {
                Profiler.Log("using preheated");
                result = GetInstance(); // there is a preheated instance that was not shown yet
            }
            return result ?? new ToolbarForm().Init();
        }

        System.Windows.Forms.Timer checkMouseTimer;
        bool enableMouseCheck = false;
        FlowLayoutPanel toolbarPanel;

        public readonly ExplorerContext ExplorerContext = new ExplorerContext();

        public void SuspendMouseCheck()
        {
            enableMouseCheck = false;
        }

        public void ResumeMouseCheck()
        {
            enableMouseCheck = true;
        }

        /// <summary>
        ///
        /// </summary>
        int imagePadding => (int)(buttonSize * 0.1); // 10% padding

        ToolTip toolTip;
        public DateTime lastLoadedConfiguration = DateTime.MinValue;

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

            this.VisibleChanged += (s, e) =>
            {
                if (this.Visible)
                    OnVisible();
            };

            this.FormClosing += (s, e) =>
            {
                if (HideOnClosing)
                {
                    Runtime.Log("Hiding toolbar instead of closing");
                    e.Cancel = true;
                    this.Hide();
                }
                else
                {
                    Runtime.Log("Disposing toolbar");
                    checkMouseTimer?.Stop();
                    checkMouseTimer?.Dispose();
                    toolTip?.Dispose();
                    Instance = null;
                }
            };

            this.lastLoadedConfiguration = ToolbarItems.configFileTimestamp;
            Profiler.Log();
            return this;
        }

        public ToolbarForm OnVisible()
        {
            SetForegroundWindow(this.ExplorerContext.HWND);

            Instance = this;
            this.BringToFront();
            this.Activate();

            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            SetForegroundWindow(this.Handle);

            Task.Run(() =>
            {
                Thread.Sleep(700);
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

            Profiler.Log();
            return this;
        }
        int buttonSize => ToolbarItems.Settings.ButtonSize;

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
                iconPath = info.IconPath.IfEmpty(customButton.IconPath.ExpandEnvars());
                iconIndex = info.IconIndex == 0 ? customButton.IconIndex : 0;
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

            toolTip.SetToolTip(button, info.Tooltip.IfEmpty(customButton?.Tooltip ?? info.Path.GetFileName()));

            customButton?.OnInit(info, this.ExplorerContext);

            button.Click += (x, y) =>
            {
                try
                {
                    var args = new ClickArgs { Context = this.ExplorerContext };

                    if (customButton != null)
                        customButton.OnClick(args);
                    else
                        info.Execute(this.ExplorerContext);

                    if (!args.DoNotHideToolbar)
                        HideToolbar();
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
            Image originalIcon = null;

            var ext = Path.GetExtension(iconPath).ToLowerInvariant();

            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".ico" || ext == ".gif")
                originalIcon = Image.FromFile(iconPath);
            else
                originalIcon = iconPath.ExtractIcon(iconIndex);

            using (originalIcon)
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

        internal void HideToolbar()
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
            SetForegroundWindow((IntPtr)ExplorerContext.HWND);
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