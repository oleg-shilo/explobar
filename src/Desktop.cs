using Shell32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using YamlDotNet.Core.Tokens;

namespace Explobar
{
    static class Desktop
    {
        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(Point point);

        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool DestroyWindow(IntPtr hWnd);

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_SHOWWINDOW = 0x0040;

        public const uint GA_ROOT = 2;

        public static void ShowToolbarForm(string root, List<string> items, dynamic window, bool createNew)
        {
            if (!ConfigManager.IsConfigUpToDate || !ConfigManager.ArePluginsUpToDate)
                ToolbarForm.ResetInstance();

            var form = createNew ?
                ToolbarForm.Create() :
                ToolbarForm.GetInstance() ?? ToolbarForm.Create();

            form.ExplorerContext.RootPath = root;
            form.ExplorerContext.SelectedItems = items;
            form.ExplorerContext.Window = window;

            form.StartPosition = FormStartPosition.Manual;

            int offsetX = 0 - form.Width / 2;
            int offsetY = 0 - form.Height / 2;

            // Get screen bounds to ensure form is visible
            var cursorPos = Cursor.Position;

            var screen = Screen.FromPoint(cursorPos);
            int formX = Math.Min(cursorPos.X + offsetX, screen.WorkingArea.Right - form.Width);
            int formY = Math.Min(cursorPos.Y + offsetY, screen.WorkingArea.Bottom - form.Height);

            // Ensure it's not off the left or top edge
            formX = Math.Max(formX, screen.WorkingArea.Left);
            formY = Math.Max(formY, screen.WorkingArea.Top);

            form.Location = new Point(formX, formY);

            // Show the form and bring it to front
            form.Show();

            if (createNew)
                Application.Run(form);
        }

        public static void SendCtrlT()
        {
            const byte VK_CONTROL = 0x11;
            const byte VK_T = 0x54;

            // Simulate Ctrl + T
            SendKeyDown(VK_CONTROL);
            SendKeyDown(VK_T);
            SendKeyUp(VK_T);
            SendKeyUp(VK_CONTROL);
        }

        static void SendKeyDown(byte vk)
        {
            SendInput(vk, 0);
        }

        static void SendKeyUp(byte vk)
        {
            SendInput(vk, 2);
        }

        static void SendInput(byte vk, uint flags)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = 1; // Input type 1 is for keyboard input
            inputs[0].ki.wVk = vk;

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        public static void SentCtrlT(IntPtr hWnd)
        {
            SetForegroundWindow(hWnd);
            SendKeys.Flush();
            Thread.Sleep(10);
            SendKeys.SendWait("^t");
            Thread.Sleep(10);
            SendKeys.Flush();
        }

        // "^t" for Ctrl+T

        /// <summary>
        /// Sends the key input.
        ///
        /// <code language="txt">
        /// SendKeys Notation for Common Keys:
        /// Key         SendKeys Notation
        /// F2	        "{F2}"
        /// F5	        "{F5}"
        /// Enter	    "{ENTER}" or "~"
        /// Escape	    "{ESC}"
        /// Tab	        "{TAB}"
        /// Backspace	"{BACKSPACE}" or "{BS}"
        /// Delete	    "{DELETE}" or "{DEL}"
        /// Ctrl+C	    "^c"
        /// Ctrl+V	    "^v"
        /// Alt+F4	    "%{F4}"
        /// </code>
        /// </summary>
        /// <param name="hWnd">The h WND.</param>
        /// <param name="input">The input.</param>
        public static void SentKeyInput(IntPtr hWnd, string input)
        {
            SetForegroundWindow(hWnd);
            SendKeys.Flush();
            Thread.Sleep(10);
            SendKeys.SendWait(input);
            Thread.Sleep(10);
            SendKeys.Flush();
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public int type;
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public const int VK_LSHIFT = 0xA0;
        public const int VK_SHIFT = 0x10;
        public const int VK_CONTROL = 0x11;
        public const int VK_MENU = 0x12; // Alt key

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("shell32.dll")]
        static extern void SHChangeNotify(
            int wEventId,
            int uFlags,
            IntPtr dwItem1,
            IntPtr dwItem2);

        const int SHCNE_CREATE = 0x00000002;
        const int SHCNF_PATHW = 0x0005;
        private const int explorerButtonXOffset = 200;
        private const int explorerButtonYOffset = -1;

        public static void NotifyFileCreated(string path)
        {
            SHChangeNotify(SHCNE_CREATE, SHCNF_PATHW,
            Marshal.StringToHGlobalUni(path),
            IntPtr.Zero);
        }

        // Explorer button monitoring - managing multiple explorer instances
        private static Dictionary<IntPtr, ExplorerButtonInfo> explorerButtons = new Dictionary<IntPtr, ExplorerButtonInfo>();
        private static System.Windows.Forms.Timer explorerMonitorTimer = null;

        private class ExplorerButtonInfo
        {
            public IntPtr ExplorerHandle { get; set; }
            public IntPtr DetailsViewHandle { get; set; }
            public Control Button { get; set; }
            public System.Windows.Forms.Timer NavigationMonitor { get; set; }
        }

        public static void StartMonitoringAllExplorerWindows()
        {
            // Initial setup - add buttons to all current explorer windows
            UpdateExplorerButtons();

            // Start monitoring for new/closed explorer windows
            if (explorerMonitorTimer == null)
            {
                explorerMonitorTimer = new System.Windows.Forms.Timer();
                explorerMonitorTimer.Interval = 4000; // Check every 4 seconds for new/closed explorer windows
                explorerMonitorTimer.Tick += (s, e) => UpdateExplorerButtons();
            }

            explorerMonitorTimer.Start();
        }

        public static void StopMonitoringAllExplorerWindows()
        {
            if (explorerMonitorTimer != null)
            {
                explorerMonitorTimer.Stop();
                explorerMonitorTimer.Dispose();
                explorerMonitorTimer = null;
            }

            CleanupAllButtons();
        }

        private static void UpdateExplorerButtons()
        {
            var shell = new Shell();
            var currentExplorerHandles = new HashSet<IntPtr>();

            try
            {
                // Find all current explorer windows
                foreach (dynamic window in shell.Windows())
                {
                    IntPtr explorerHandle = new IntPtr(window.HWND);
                    currentExplorerHandles.Add(explorerHandle);

                    // Add button if this is a new explorer instance
                    if (!explorerButtons.ContainsKey(explorerHandle))
                    {
                        IntPtr detailsViewHandle = FindDetailsView(explorerHandle);
                        if (detailsViewHandle != IntPtr.Zero)
                        {
                            AddButtonToExplorer(explorerHandle, detailsViewHandle);
                        }
                    }
                }
            }
            finally
            {
                Marshal.ReleaseComObject(shell);
            }

            // Remove buttons from closed explorer instances
            var closedExplorers = explorerButtons.Keys.Where(h => !currentExplorerHandles.Contains(h)).ToList();
            foreach (var closedExplorer in closedExplorers)
            {
                RemoveButtonFromExplorer(closedExplorer);
            }
        }

        private static void AddButtonToExplorer(IntPtr explorerHandle, IntPtr detailsViewHandle)
        {
            try
            {
                var button = CreateButtonForWindow(detailsViewHandle, explorerButtonXOffset, explorerButtonYOffset);

                // Create navigation monitor for this specific explorer instance
                var navigationMonitor = new System.Windows.Forms.Timer();
                navigationMonitor.Interval = 2000; // Check every 2 seconds for navigation
                navigationMonitor.Tag = explorerHandle; // Store explorer handle for reference

                navigationMonitor.Tick += (s, e) =>
                {
                    var timer = s as System.Windows.Forms.Timer;
                    IntPtr explorer = (IntPtr)timer.Tag;

                    if (explorerButtons.ContainsKey(explorer))
                    {
                        var info = explorerButtons[explorer];

                        // Check if details view still exists (navigation destroys it)
                        if (!IsWindow(info.DetailsViewHandle))
                        {
                            // Navigation occurred, find new details view and recreate button
                            IntPtr newDetailsView = FindDetailsView(explorer);
                            if (newDetailsView != IntPtr.Zero)
                            {
                                // Dispose old button
                                if (info.Button != null && !info.Button.IsDisposed)
                                {
                                    info.Button.Dispose();
                                }

                                // Create new button on new details view
                                info.Button = CreateButtonForWindow(newDetailsView, explorerButtonXOffset, explorerButtonYOffset);
                                info.DetailsViewHandle = newDetailsView;
                            }
                        }
                    }
                };

                navigationMonitor.Start();

                var buttonInfo = new ExplorerButtonInfo
                {
                    ExplorerHandle = explorerHandle,
                    DetailsViewHandle = detailsViewHandle,
                    Button = button,
                    NavigationMonitor = navigationMonitor
                };

                explorerButtons[explorerHandle] = buttonInfo;
                Runtime.Log($"Added button to explorer instance: 0x{explorerHandle.ToInt64():X}");
            }
            catch (Exception ex)
            {
                Runtime.Log($"Error adding button to explorer: {ex.Message}");
            }
        }

        private static void RemoveButtonFromExplorer(IntPtr explorerHandle)
        {
            if (explorerButtons.ContainsKey(explorerHandle))
            {
                var info = explorerButtons[explorerHandle];

                try
                {
                    // Stop navigation monitoring
                    if (info.NavigationMonitor != null)
                    {
                        info.NavigationMonitor.Stop();
                        info.NavigationMonitor.Dispose();
                    }

                    // Dispose button
                    if (info.Button != null && !info.Button.IsDisposed)
                    {
                        info.Button.Dispose();
                    }
                }
                catch { }

                explorerButtons.Remove(explorerHandle);
                Runtime.Log($"Removed button from explorer instance: 0x{explorerHandle.ToInt64():X}");
            }
        }

        private static void CleanupAllButtons()
        {
            var allExplorers = explorerButtons.Keys.ToList();
            foreach (var explorer in allExplorers)
            {
                RemoveButtonFromExplorer(explorer);
            }
            explorerButtons.Clear();
        }

        private static IntPtr FindDetailsView(IntPtr explorerHandle)
        {
            // Explorer window hierarchy:
            // CabinetWClass / ExplorerWClass (main window)
            //   -> ShellTabWindowClass
            //     -> DUIViewWndClassName
            //       -> DirectUIHWND
            //         -> Multiple CtrlNotifySink windows
            //           -> One has SHELLDLL_DefView
            //             -> DirectUIHWND (this is what we need)

            IntPtr shellTabWindow = FindWindowEx(explorerHandle, IntPtr.Zero, "ShellTabWindowClass", null);
            if (shellTabWindow == IntPtr.Zero)
                return IntPtr.Zero;

            IntPtr duiView = FindWindowEx(shellTabWindow, IntPtr.Zero, "DUIViewWndClassName", null);
            if (duiView == IntPtr.Zero)
                return IntPtr.Zero;

            IntPtr directUI = FindWindowEx(duiView, IntPtr.Zero, "DirectUIHWND", null);
            if (directUI == IntPtr.Zero)
                return IntPtr.Zero;

            // Enumerate through all CtrlNotifySink children
            IntPtr ctrlNotifySink = IntPtr.Zero;
            while ((ctrlNotifySink = FindWindowEx(directUI, ctrlNotifySink, "CtrlNotifySink", null)) != IntPtr.Zero)
            {
                // Look for SHELLDLL_DefView child in this CtrlNotifySink
                IntPtr shellDefView = FindWindowEx(ctrlNotifySink, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellDefView != IntPtr.Zero)
                {
                    // Found it! Now get its DirectUIHWND child
                    IntPtr targetDirectUI = FindWindowEx(shellDefView, IntPtr.Zero, "DirectUIHWND", null);
                    if (targetDirectUI != IntPtr.Zero)
                    {
                        return targetDirectUI;
                    }
                }
            }

            return IntPtr.Zero;
        }

        public static void PlaceButtonOnWindowTest(IntPtr targetWindow, int x, int y)
        {
            // This method is kept for internal use by the monitoring system
            var button = CreateButtonForWindow(targetWindow, x, y);
        }

        private static Control CreateButtonForWindow(IntPtr targetWindow, int x, int y)
        {
            // Create a custom-drawn button form
            var buttonHost = new CustomButtonForm
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                TopMost = false,
                Size = new Size(80, 20),
            };

            buttonHost.Click += (s, e) =>
            {
                Runtime.Log("Button clicked, sending shortcut to open context menu");
                Program.OnShortcutPressed(Keys.None);
            };

            if (!buttonHost.IsHandleCreated)
                buttonHost.CreateControl();

            buttonHost.Show();

            buttonHost.Width = 35;
            buttonHost.Height = 12;

            // Place the button on the target window
            CustomButtonForm.PlaceButtonOnWindow(buttonHost.Handle, targetWindow, x, y);

            return buttonHost;
        }

    }
}