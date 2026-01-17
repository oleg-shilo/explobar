using Explobar;
using Shell32;

namespace Explbar
{
    class Explorer
    {
        public static (string root, List<string> selected, dynamic window) GetExplorerSelection()
        {
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

                if (!hasFocus)
                {
                    Console.WriteLine("Window does not have focus, skipping.");
                    continue;
                }

                if (!hasMouseOver)
                {
                    Console.WriteLine("Window is not under mouse, skipping.");
                    continue;
                }

                if (!window.FullName.EndsWith("explorer.exe", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Window is not an Explorer window, skipping.");
                    continue;
                }

                dynamic folder = window.Document;
                if (folder == null)
                {
                    Console.WriteLine("Window document is null, skipping.");
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

        static public void NavigateToPath(dynamic explorerWindow, string path, bool newTab = false)
        {
            try
            {
                if (newTab)
                {
                    // Open a new tab and navigate to the path
                    // 1. Activate Explorer
                    // SetForegroundWindow((IntPtr)w.HWND);

                    // 2. Open new tab
                    Desktop.SendCtrlT();

                    explorerWindow.Navigate2(path); // 2048 is the flag for opening in a new tab
                }
                else
                    explorerWindow.Navigate2(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error navigating to path {path}: {ex.Message}");
            }
        }
    }
}