using Shell32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
// using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ConsoleApp28
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Console.WriteLine("Press...");
            // Console.ReadLine();
            while (true)
            {
                Thread.Sleep(200);
                Console.WriteLine("------------");
                var selection = GetExplorerSelection();

                if (selection.Any())
                {
                    foreach (var item in selection)
                    {
                        Console.WriteLine(item);
                    }

                    GetCursorPos(out POINT cursorPos);
                    ShowSelectionForm(selection, cursorPos.X, cursorPos.Y);

                    // Wait a bit to avoid showing multiple forms
                    Thread.Sleep(2000);
                }
            }
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_SHOWWINDOW = 0x0040;

        const uint GA_ROOT = 2;
        const int VK_LSHIFT = 0xA0;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        static void ShowSelectionForm(List<string> items, int x, int y)
        {
            var form = new SelectionForm(items);
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

        static List<string> GetExplorerSelection()
        {
            var shell = new Shell();
            IntPtr foregroundWindow = GetForegroundWindow();

            GetCursorPos(out POINT cursorPos);
            IntPtr windowUnderMouse = WindowFromPoint(cursorPos);
            IntPtr rootWindowUnderMouse = GetAncestor(windowUnderMouse, GA_ROOT);

            bool isLeftShiftPressed = (GetAsyncKeyState(VK_LSHIFT) & 0x8000) != 0;

            var selectedPaths = new List<string>();

            foreach (dynamic window in shell.Windows())
            {
                IntPtr windowHandle = new IntPtr(window.HWND);
                bool hasFocus = windowHandle == foregroundWindow;
                bool hasMouseOver = windowHandle == rootWindowUnderMouse;


                if (!hasMouseOver)
                    continue;

                if (!isLeftShiftPressed)
                    continue;

                if (!window.FullName.EndsWith("explorer.exe", StringComparison.OrdinalIgnoreCase))
                    continue;

                dynamic folder = window.Document;
                if (folder == null)
                    continue;

                Console.WriteLine("focused");
                foreach (FolderItem item in folder.SelectedItems())
                {
                    selectedPaths.Add(item.Path);

                }

                if (selectedPaths.Any())
                    return selectedPaths;
            }
            return selectedPaths;
        }
    }

    public class SelectionForm : Form
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

        public SelectionForm(List<string> items)
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

    class ToolbarItem
    {
        public string IconPath;
        public string Path;
        public string Arguments;
        public string WorkingDir;
    }

    static class ToolbarItems
    {
        public static List<ToolbarItem> Items =
            [
                new()
            {
                IconPath = @"D:\tools\SublimeText\sublime_text.exe",
                Path = @"D:\tools\SublimeText\sublime_text.exe",
                Arguments = "%f%",
                WorkingDir = "%c%"
            },
            new()
            {
                IconPath = @"C:\Program Files\Everything\Everything.exe",
                Path = @"C:\Program Files\Everything\Everything.exe",
                Arguments = @"-path %c%",
            },
            new()
            {
                IconPath = @"C:\Windows\System32\cmd.exe",
                Path = @"C:\Users\Oleg.Shilo\AppData\Local\Microsoft\WindowsApps\wt.exe",
                // Path = @"C:\Windows\System32\cmd.exe",
                Arguments = @"-d %c% -p ""Command Prompt""; -d %c% -p ""Windows PowerShell""",
            },
        ];
    }

    static class ToolbarExtesnions
    {
        public static void Execute(this ToolbarItem info, List<string> selectedItems)
        {
            try
            {
                if (string.IsNullOrEmpty(info.Path) || !System.IO.File.Exists(info.Path))
                    return;

                var firstItem = selectedItems.FirstOrDefault() ?? "";
                var currDir = Path.GetDirectoryName(firstItem) ?? "";

                var args = info.Arguments?
                    .Replace("%f%", $"\"{firstItem}\"")
                    .Replace("%c%", $"\"{currDir}\"")
                    ?? "";
                var workDir = info.WorkingDir?
                    .Replace("%c%", currDir)
                    ?? "";

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = info.Path,
                    Arguments = args,
                    WorkingDirectory = workDir
                };
                System.Diagnostics.Process.Start(startInfo);
            }
            catch
            {
                // Ignore errors
            }
        }
        public static Image? ExtractIcon(this string exePath)
        {
            try
            {
                if (string.IsNullOrEmpty(exePath) || !System.IO.File.Exists(exePath))
                    return null;

                using (var icon = Icon.ExtractAssociatedIcon(exePath))
                {
                    if (icon == null)
                        return null;

                    // Convert icon to bitmap and return a copy
                    return new Bitmap(icon.ToBitmap());
                }
            }
            catch
            {
                return null;
            }
        }
    }
}