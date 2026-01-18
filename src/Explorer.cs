using Explobar;
using Shell32;

namespace Explbar
{
    static class Explorer
    {
        public static (string root, List<string> selected, dynamic window) GetSelection()
        {
            GetTabs();
            var shell = new Shell();
            IntPtr foregroundWindow = Desktop.GetForegroundWindow();
            object exporerWindow = null;
            Desktop.GetCursorPos(out Desktop.POINT cursorPos);
            IntPtr windowUnderMouse = Desktop.WindowFromPoint(cursorPos);
            IntPtr rootWindowUnderMouse = Desktop.GetAncestor(windowUnderMouse, Desktop.GA_ROOT);

            bool isLeftShiftPressed = (Desktop.GetAsyncKeyState(Desktop.VK_LSHIFT) & 0x8000) != 0;

            var selectedPaths = new List<string>();
            string root = null;

            foreach (dynamic window in shell.Windows())
            {
                IntPtr windowHandle = new IntPtr(window.HWND);
                bool hasFocus = windowHandle == foregroundWindow;
                bool hasMouseOver = windowHandle == rootWindowUnderMouse;

                // if (!hasFocus) // more aggressive approach than IsObstructedByOtherWindows
                // {
                //     Console.WriteLine("Window does not have focus, skipping.");
                //     continue;
                // }

                if (Desktop.IsObstructedByOtherWindows(windowHandle))
                {
                    // Window is not fully visible. Note, even if the window does not have user input/focus,
                    // but fully visible, we should popup the toolbar
                    Console.WriteLine($"Window is not fully visible, skipping. 0x{window.HWND:X}");
                }

                if (!hasMouseOver)
                {
                    Console.WriteLine($"Window is not under mouse, skipping. 0x{window.HWND:X}");
                    continue;
                }

                if (!window.FullName.EndsWith("explorer.exe", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Window is not an Explorer window, skipping. 0x{window.HWND:X}");
                    continue;
                }

                dynamic folder = window.Document;
                if (folder == null)
                {
                    Console.WriteLine($"Window document is null, skipping. 0x{window.HWND:X}");
                    continue;
                }

                exporerWindow = window;
                root = window.Document.Folder.Self.Path.ToString();

                // window.Navigate2(@"c:\Windows");
                // Desktop.SendCtrlT();
                // NavigateToPath(window, @"c:\Windows", true);
                // root = null;
                // break;

                foreach (FolderItem item in folder.SelectedItems())
                {
                    selectedPaths.Add(item.Path);
                }

                break;
            }
            return (root, selectedPaths, exporerWindow);
        }

        public static void NavigateToPath(dynamic explorerWindow, string path, bool newTab = false)
        {
            try
            {
                if (newTab)
                {
                    var shell = new Shell();

                    // Get the process ID and HWND of the current explorer window
                    IntPtr currentHwnd = new IntPtr((long)explorerWindow.HWND);
                    uint currentProcessId = Desktop.GetWindowProcess(currentHwnd);

                    // Store existing window HWNDs
                    var existingWindows = new HashSet<long>();

                    foreach (dynamic window in shell.Windows())
                    {
                        existingWindows.Add(window.HWND);
                    }

                    // 3. Wait for the new tab/window to appear
                    dynamic newTabExplorerWindow = null;
                    int maxAttempts = 50; // Try for up to 5 seconds

                    for (int i = 0; i < maxAttempts; i++)
                    {
                        System.Threading.Thread.Sleep(100);

                        foreach (dynamic window in shell.Windows())
                        {
                            long hwnd = window.HWND;

                            // Look for a new window that:
                            // 1. Wasn't in the original list
                            // 2. Belongs to the same process
                            if (!existingWindows.Contains(hwnd))
                            {
                                uint windowProcessId = Desktop.GetWindowProcess(new IntPtr(hwnd));

                                if (windowProcessId == currentProcessId)
                                {
                                    newTabExplorerWindow = window;
                                    break;
                                }
                            }
                        }

                        if (newTabExplorerWindow != null)
                            break;
                    }

                    // 4. Navigate to the path
                    if (newTabExplorerWindow != null)
                    {
                        newTabExplorerWindow.Navigate2(path);
                    }
                    else
                    {
                        Console.WriteLine("Failed to get new tab Explorer window, using original window");
                        explorerWindow.Navigate2(path);
                    }
                }
                else
                {
                    explorerWindow.Navigate2(path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error navigating to path {path}: {ex.Message}");
            }
        }

        public static List<dynamic> GetTabs()
        {
            var shell = new Shell();

            IntPtr foreground = Desktop.GetForegroundWindow();

            var result = new List<dynamic>();

            foreach (dynamic window in shell.Windows())
            {
                IntPtr windowHandle = new IntPtr(window.HWND);

                dynamic doc = window.Document;
                if (doc == null)
                {
                    Console.WriteLine($"Window document is null, skipping. 0x{window.HWND:X}");
                    continue;
                }

                result.Add(window);

                bool isActiveTab = windowHandle == foreground;

                var path = doc.Folder?.Self?.Path;
                Console.WriteLine($"{(isActiveTab ? "[ACTIVE]" : "        ")} {path}");
            }
            return result;
        }
    }
}