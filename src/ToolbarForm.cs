using System;
using System.Collections.Generic;
using static System.Collections.Specialized.BitVector32;
using System.Diagnostics;
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
        public ExplorerContext()
        {
        }

        public ExplorerContext(string root, List<string> selection, dynamic window)
        {
            RootPath = root;
            SelectedItems = selection;
            Window = window;
        }

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
        public List<string> SelectedItems { get; set; } = new List<string>();
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
            if (Instance != null && Instance.lastLoadedConfiguration == ConfigManager.configFileTimestamp)
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
            if (Instance == null || Instance.lastLoadedConfiguration != ConfigManager.configFileTimestamp)
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
            var items = ConfigManager.LoadConfig().Items;

            this.Text = "Selected Items";
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.KeyPreview = true;
            this.ShowInTaskbar = false;
            this.BackColor = Color.DarkGray;
            this.Padding = new Padding(1);
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // Handle Escape key to hide toolbar
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    HideToolbar();
                    e.Handled = true;
                }

                // Move focus through the buttons in the toolbar for easier keyboard navigation
                if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Left)
                {
                    toolbarPanel
                        .NetxInFocusChain(forward: e.KeyCode == Keys.Right)
                        ?.Focus();
                }
            };

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
                BackColor = Color.WhiteSmoke,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            this.Controls.Add(toolbarPanel);

            foreach (var item in items)
            {
                if (item.Hidden)
                    continue; // Skip hidden items - they're only accessible via shortcuts

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

            this.Load += (s, e) =>
            {
                enableMouseCheck = true;
                checkMouseTimer?.Start();
            };

            this.VisibleChanged += (s, e) =>
            {
                if (this.Visible)
                    OnVisible();
            };

            this.FormClosing += (s, e) =>
            {
                Profiler.Log($"Form is closing: {this.Handle}");
                this.Invoke((Action)(() =>
                {
                    if (HideOnClosing)
                    {
                        Runtime.Output("Hiding toolbar instead of closing");
                        e.Cancel = true;
                        this.Hide();
                    }
                    else
                    {
                        Runtime.Output("Disposing toolbar");
                        checkMouseTimer?.Stop();
                        checkMouseTimer?.Dispose();
                        toolTip?.Dispose();
                        Instance = null;
                    }
                }));
            };

            this.lastLoadedConfiguration = ConfigManager.configFileTimestamp;

            Profiler.Log($"Form is initiated: {this.Handle}");
            ConfigManager.ResetPluginDirtyFlag();
            Instance = this;
            return this;
        }

        public ToolbarForm OnVisible()
        {
            // first, bring explorer to the full view
            SetForegroundWindow(this.ExplorerContext.HWND);

            Instance = this;
            this.BringToFront();
            this.Activate();

            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            SetForegroundWindow(this.Handle);
            this.toolbarPanel.Focus(); // to allow tab navigation

            if (this.IsHandleCreated)
                Task.Run(() =>
                {
                    for (int i = 0; i < 10 && !this.IsHandleCreated; i++)
                        Thread.Sleep(700);

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
                        // Ignore errors - we may fail if the for is disposed by other means
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

        static Bitmap _defaultIcon;
        public static Bitmap DefaultIcon => _defaultIcon ?? (_defaultIcon = (Bitmap)@"%SystemRoot%\System32\imageres.dll".ExpandEnvars().ExtractIcon(231));

        void AddToolbarButton(ToolbarItem info)
        {
            Button button;
            Bitmap resizedIcon;
            ICustomButton customButton = null;

            string iconPath;
            int iconIndex;

            bool isStockButton = info.Path.StartsWith("{") && info.Path.EndsWith("}") && StockToolbarControls.Items.ContainsKey(info.Path);
            bool isPluginButton = !isStockButton && PluginLoader.IsPluginAssembly(info.Path);
            bool isScriptedButton = isPluginButton && info.Path.EndsWith(".cs}");

            if (isStockButton || isPluginButton) // both implement ICustomButton
            {
                button = isStockButton ?
                    StockToolbarControls.Items[info.Path]() :
                    PluginLoader.LoadCustomButtonFromAssembly(info.Path) ?? new MisconfiguredButton(info.Path);

                customButton = (button as ICustomButton);

                button.Name = info.Path;

                // add context menu to this plugin button
                if (isScriptedButton)
                {
                    var script = info.Path.Trim('{', '}');
                    var menu = new ContextMenuStrip();
                    menu.Items.Add("Edit Source", null, (s, ev) => Process.Start("notepad.exe", $"\"{script}\""));
                    menu.Items.Add("Open Location", null, (s, ev) => Process.Start("explorer.exe", $"\"{Path.GetDirectoryName(script)}\""));
                    button.MouseUp += (s, ev) =>
                    {
                        if (ev.Button == MouseButtons.Right)
                            menu.Show(button, ev.Location);
                    };
                }

                iconPath = info.IconPath.IfEmpty(customButton.IconPath.ExpandEnvars());
                iconIndex = info.IconPath.IsEmpty() ? customButton.IconIndex : info.IconIndex;
            }
            else
            {
                button = new Button();
                button.Name = info.Path;
                iconPath = info.IconPath.IfEmpty(info.Path);
                iconIndex = info.IconIndex;
            }
            button.Tag = this;
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

            // Add dropdown indicator for expandable buttons
            if (customButton is CustomButton cb && cb.IsExpandabe)
            {
                button.Paint += (s, e) =>
                {
                    // Draw ComboBox-style dropdown indicator in bottom-right corner
                    int indicatorSize = 7;
                    int margin = 0;

                    Rectangle indicatorRect = new Rectangle(
                        button.Width - margin - indicatorSize,
                        button.Height - margin - indicatorSize,
                        indicatorSize,
                        indicatorSize
                                                           );
                    DrawExpandIndicator(e.Graphics, indicatorRect);
                };
            }

            toolTip.SetToolTip(button, $"Button: \"{info.Path}\"");
            try
            {
                // Build tooltip text with shortcut if available
                string tooltipText = info.Tooltip.IfEmpty(customButton?.Tooltip ?? info.Path.GetFileName());
                if (info.Shortcut.HasText())
                    tooltipText += Environment.NewLine + "Shortcut: " + info.Shortcut;
                toolTip.SetToolTip(button, tooltipText);

                customButton?.OnInit(info, this.ExplorerContext);

                button.Click += (x, y) =>
                {
                    try
                    {
                        var clickArgs = new ClickArgs { Context = this.ExplorerContext, Toolbar = this };

                        if (customButton != null)
                            customButton.OnClick(clickArgs);
                        else
                            info.Execute(this.ExplorerContext);

                        if (!clickArgs.DoNotHideToolbar)
                        {
                            HideToolbar();
                            SetForegroundWindow((IntPtr)ExplorerContext.HWND);
                        }
                    }
                    catch (Exception e)
                    {
                        Runtime.ShowError(e.Message);
                    }
                };
            }
            catch
            {
                // Ignore errors in tooltip or click handler setup
            }
            toolbarPanel.Controls.Add(button);
        }

        // Cache for loaded images to avoid reloading and resizing the same icons multiple times
        // Cache is not cleared in the current implementation - it can grow indefinitely if
        // there are many different icons, but in typical usage it should be limited and it
        // significantly improves performance when the same icons are used multiple times (e.g. for stock buttons)
        // or when the toolbar is shown multiple times with the same configuration.
        static Dictionary<string, Image> imageCache = new Dictionary<string, Image>();

        Bitmap CreateButtonImage(string iconPath, int iconIndex)
        {
            var imageId = $"{iconPath}|{iconIndex}";

            Image originalIcon = null;
            if (imageCache.ContainsKey(imageId))
            {
                originalIcon = imageCache[imageId];
            }
            else
            {
                try
                {
                    var ext = Path.GetExtension(iconPath).ToLowerInvariant();

                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".ico" || ext == ".gif")
                        originalIcon = Image.FromFile(iconPath);
                    else
                        originalIcon = iconPath.ExtractIcon(iconIndex);
                }
                catch
                {
                    // Ignore errors and use default icon
                }
            }

            if (originalIcon == null)
                originalIcon = DefaultIcon;
            else
                imageCache[imageId] = originalIcon;

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

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                HideToolbar();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public void HideToolbar()
        {
            // Runtime.Output("Stopping mouse timer");
            checkMouseTimer?.Stop();
            if (HideOnClosing)
            {
                // Runtime.Output("Hiding toolbar");
                this.Hide();
            }
            else
            {
                // Runtime.Output("Closing toolbar");
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
                    // Ignore errors - we may fail if the for is disposed by other means
                }
            }
        }

        void DrawExpandIndicator(Graphics graphics, Rectangle rect)
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw a small downward arrow
            int centerX = rect.X + rect.Width / 2;
            int centerY = rect.Y + rect.Height / 2;
            int arrowSize = 3;

            Point[] arrow = new Point[]
            {
                new Point(centerX - arrowSize, centerY - 1),
                new Point(centerX + arrowSize, centerY - 1),
                new Point(centerX, centerY + arrowSize - 1)
            };
            using (var pen = new Pen(Color.FromArgb(96, 96, 96), 1.5f))
            {
                graphics.DrawLine(pen, arrow[0], arrow[2]);
                graphics.DrawLine(pen, arrow[1], arrow[2]);
            }
        }

        public int CalculateButtonOffset(int buttonIndex)
        {
            if (this == null)
                return 0;

            // Find the toolbar panel (FlowLayoutPanel)
            FlowLayoutPanel toolbarPanel = null;
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is FlowLayoutPanel panel)
                {
                    toolbarPanel = panel;
                    break;
                }
            }

            if (toolbarPanel == null)
                return this.Width / 2; // Fallback to center

            // Build list of non-separator buttons
            var buttons = new List<Control>();

            foreach (Control control in toolbarPanel.Controls)
            {
                // Only count non-separator controls as buttons
                if (control.Tag is ToolbarForm)
                    buttons.Add(control);
            }

            if (buttons.Count == 0)
                return this.Width / 2; // Fallback to center

            // Convert 1-based index to 0-based
            int targetIndex;
            if (buttonIndex > 0)
            {
                // Positive: 1-based from left (1 = first button, 2 = second, etc.)
                targetIndex = buttonIndex - 1;
            }
            else // buttonIndex < 0
            {
                // Negative: 1-based from right (-1 = last button, -2 = second-to-last, etc.)
                targetIndex = buttons.Count + buttonIndex;
            }

            // Clamp to valid range
            if (targetIndex < 0 || targetIndex >= buttons.Count)
                return this.Width / 2; // Fallback to center if out of range

            // Get the target button and calculate offset to its center
            var targetButton = buttons[targetIndex];

            return this.Padding.Left + targetButton.Left + targetButton.Width / 2;
        }
    }
}