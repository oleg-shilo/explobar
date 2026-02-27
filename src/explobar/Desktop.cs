using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Shell32;
using YamlDotNet.Core.Tokens;

namespace Explobar
{
    static class Win32
    {
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }

    static class Desktop
    {
        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(Point point);

        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        public static bool SetForegroundWindow(IntPtr hWnd)
        {
            Runtime.Output("SetForegroundWindow");
            return Win32.SetForegroundWindow(hWnd);
        }

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

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

            // Calculate offset based on IndexOfButtonUnderMouse setting
            int buttonIndex = ToolbarItems.Settings.IndexOfButtonUnderMouse;
            int offsetX;

            if (buttonIndex == 0)
            {
                // Center-based offset calculation
                offsetX = form.Width / 2;
            }
            else
            {
                // Position specific button under cursor (1-based, supports negative for right-side)
                offsetX = form.CalculateButtonOffset(buttonIndex);
            }

            int offsetY = form.Height / 2;

            // Get screen bounds to ensure form is visible
            var cursorPos = Cursor.Position;

            var screen = Screen.FromPoint(cursorPos);
            int formX = Math.Min(cursorPos.X - offsetX, screen.WorkingArea.Right - form.Width);
            int formY = Math.Min(cursorPos.Y - offsetY, screen.WorkingArea.Bottom - form.Height);

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
        static int explorerButtonXOffset => ToolbarItems.Settings.ExplorerButtonXPosition;
        const int explorerButtonYOffset = -1;

        public static void NotifyFileCreated(string path)
        {
            SHChangeNotify(SHCNE_CREATE, SHCNF_PATHW,
            Marshal.StringToHGlobalUni(path),
            IntPtr.Zero);
        }

        // Explorer button monitoring - managing multiple explorer instances
        static Dictionary<IntPtr, ExplorerButtonInfo> explorerButtons = new Dictionary<IntPtr, ExplorerButtonInfo>();

        static System.Windows.Forms.Timer explorerMonitorTimer = null;

        class ExplorerButtonInfo
        {
            public IntPtr ExplorerHandle { get; set; }
            public IntPtr DetailsViewHandle { get; set; }
            public Control Button { get; set; }
            public System.Windows.Forms.Timer NavigationMonitor { get; set; }
            public string LastActiveTabPath { get; set; } // Track which tab was last active
        }

        public static void StartMonitoringAllExplorerWindows()
        {
            // Initial setup - add buttons to all current explorer windows
            UpdateExplorerButtons();

            // Start monitoring for new/closed explorer windows
            if (explorerMonitorTimer == null)
            {
                explorerMonitorTimer = new System.Windows.Forms.Timer();
                explorerMonitorTimer.Interval = 2000; // Check every 2 seconds (increased frequency for better tab switching detection)
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

        static void UpdateExplorerButtons()
        {
            // If config has been reloaded, remove all existing buttons so they get recreated with new settings
            if (!ConfigManager.IsConfigUpToDate)
            {
                CleanupAllButtons();
                Runtime.Log("Config reloaded - removed all explorer buttons, will recreate with new settings");
            }

            if (ConfigManager.CurrentConfigUnsafe?.Settings?.DisableExplorerLaunchButton == true)
            {
                Thread.Sleep(5000);
                return;
            }

            var shell = new Shell();
            var currentExplorerHandles = new HashSet<IntPtr>();

            try
            {
                // Group tabs by their HWND to find the active tab for each Explorer window
                var tabsByWindow = new Dictionary<IntPtr, List<dynamic>>();

                foreach (dynamic window in shell.Windows())
                {
                    if (!window.FullName.EndsWith("explorer.exe", StringComparison.OrdinalIgnoreCase))
                        continue;

                    IntPtr explorerHandle = new IntPtr(window.HWND);
                    currentExplorerHandles.Add(explorerHandle);

                    if (!tabsByWindow.ContainsKey(explorerHandle))
                        tabsByWindow[explorerHandle] = new List<dynamic>();

                    tabsByWindow[explorerHandle].Add(window);
                }

                // Process each Explorer window
                foreach (var kvp in tabsByWindow)
                {
                    IntPtr explorerHandle = kvp.Key;
                    List<dynamic> tabs = kvp.Value;

                    // Find the details view - this will be for the currently active tab
                    IntPtr detailsViewHandle = FindDetailsView(explorerHandle);
                    if (detailsViewHandle == IntPtr.Zero)
                        continue;

                    // Get the path of the active tab (the one whose details view we found)
                    string activeTabPath = null;
                    try
                    {
                        // The active tab is the one whose LocationURL matches the details view
                        // For simplicity, we'll use the first tab's path if we can't determine the active one
                        // In Windows 11 multi-tab, we need to detect which tab is actually active
                        activeTabPath = GetActiveTabPath(explorerHandle, tabs);
                    }
                    catch { }

                    if (activeTabPath == null && tabs.Count > 0)
                        activeTabPath = tabs[0].Document?.Folder?.Self?.Path?.ToString() ?? "";

                    // Check if we need to update the button for this Explorer window
                    bool needsNewButton = false;
                    bool needsButtonMove = false;

                    if (!explorerButtons.ContainsKey(explorerHandle))
                    {
                        needsNewButton = true;
                    }
                    else
                    {
                        var info = explorerButtons[explorerHandle];

                        // Check if details view still exists
                        if (!IsWindow(info.DetailsViewHandle))
                        {
                            needsButtonMove = true;
                        }
                        // Check if the active tab changed (user switched tabs)
                        else if (info.LastActiveTabPath != activeTabPath)
                        {
                            needsButtonMove = true;
                        }
                    }

                    if (needsNewButton)
                    {
                        AddButtonToExplorer(explorerHandle, detailsViewHandle, activeTabPath);
                    }
                    else if (needsButtonMove)
                    {
                        UpdateButtonForExplorer(explorerHandle, detailsViewHandle, activeTabPath);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // ignore all COM exception. Win Explorer is full of them :)
            }
            catch (Exception ex)
            {
                Runtime.ShowError($"Failed to open link: {ex.Message}");
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

        static string GetActiveTabPath(IntPtr explorerHandle, List<dynamic> tabs)
        {
            // Try to use UI Automation to get the active tab's name
            try
            {
                if (tabs.Count > 1)
                {
                    // Use the same logic as ResolveActiveTabInMultiTabWindow
                    string activeTabPath = AutomationHelper.GetExplorerRoot(tabs[0]);
                    return activeTabPath?.GetSpecialFolderCLSID();
                }
                else if (tabs.Count == 1)
                {
                    return tabs[0].Document?.Folder?.Self?.Path?.ToString();
                }
            }
            catch { }

            return null;
        }

        static void AddButtonToExplorer(IntPtr explorerHandle, IntPtr detailsViewHandle, string activeTabPath)
        {
            try
            {
                var button = CreateButtonForWindow(detailsViewHandle, explorerButtonXOffset, explorerButtonYOffset);

                var buttonInfo = new ExplorerButtonInfo
                {
                    ExplorerHandle = explorerHandle,
                    DetailsViewHandle = detailsViewHandle,
                    Button = button,
                    LastActiveTabPath = activeTabPath
                };

                explorerButtons[explorerHandle] = buttonInfo;
                Runtime.Log($"Added button to explorer window: 0x{explorerHandle.ToInt64():X}, tab: {activeTabPath}");
            }
            catch (Exception ex)
            {
                Runtime.Log($"Error adding button to explorer: {ex.Message}");
            }
        }

        static void UpdateButtonForExplorer(IntPtr explorerHandle, IntPtr newDetailsViewHandle, string newActiveTabPath)
        {
            if (!explorerButtons.ContainsKey(explorerHandle))
                return;

            var info = explorerButtons[explorerHandle];

            try
            {
                // Dispose old button
                if (info.Button != null && !info.Button.IsDisposed)
                {
                    info.Button.Dispose();
                }

                // Create new button on new details view
                info.Button = CreateButtonForWindow(newDetailsViewHandle, explorerButtonXOffset, explorerButtonYOffset);
                info.DetailsViewHandle = newDetailsViewHandle;
                info.LastActiveTabPath = newActiveTabPath;

                Runtime.Log($"Updated button for explorer window: 0x{explorerHandle.ToInt64():X}, new tab: {newActiveTabPath}");
            }
            catch (Exception ex)
            {
                Runtime.Log($"Error updating button: {ex.Message}");
            }
        }

        static void RemoveButtonFromExplorer(IntPtr explorerHandle)
        {
            if (explorerButtons.ContainsKey(explorerHandle))
            {
                var info = explorerButtons[explorerHandle];

                try
                {
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

        static void CleanupAllButtons()
        {
            var allExplorers = explorerButtons.Keys.ToList();
            foreach (var explorer in allExplorers)
            {
                RemoveButtonFromExplorer(explorer);
            }
            explorerButtons.Clear();
        }

        static IntPtr FindDetailsView(IntPtr explorerHandle)
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

        static Control CreateButtonForWindow(IntPtr targetWindow, int x, int y)
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
                App.OnShortcutPressed(Keys.None);
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