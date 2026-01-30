using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Explobar;
using Shell32;

namespace Explobar
{
    static class Explorer
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern bool SHObjectProperties(IntPtr hwnd, uint shopObjectType, [MarshalAs(UnmanagedType.LPWStr)] string pszObjectName, [MarshalAs(UnmanagedType.LPWStr)] string pszPropertyPage);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        const uint SHOP_FILEPATH = 0x2;

        public static void ShowFileProperties(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            if (!System.IO.File.Exists(path) && !System.IO.Directory.Exists(path))
                throw new System.IO.FileNotFoundException("The specified file or directory does not exist", path);

            // SHObjectProperties shows the properties dialog for the file/folder
            // Pass IntPtr.Zero for the parent window handle, SHOP_FILEPATH for file/folder paths,
            // the path, and null for default property page
            if (!SHObjectProperties(IntPtr.Zero, SHOP_FILEPATH, path, null))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    $"Failed to show properties for: {path}");
            }
        }

        public static List<dynamic> GetTabs()
        {
            var shell = new Shell();
            try
            {
                var explorersTabs = new List<dynamic>();        // all tabs of all explorers
                foreach (dynamic window in shell.Windows())     // need to use foreach since LINQ does not work with dynamic
                {
                    if (!window.FullName.EndsWith("explorer.exe", StringComparison.OrdinalIgnoreCase))
                        continue;
                    explorersTabs.Add(window);
                }
                return explorersTabs;
            }
            finally
            {
                Marshal.ReleaseComObject(shell);
            }
        }

        public static dynamic GetTab(string path, IntPtr hwnd)
        {
            var shell = new Shell();
            try
            {
                // Normalize the path to handle special folders
                string normalizedPath = path.GetSpecialFolderCLSID();

                foreach (dynamic window in shell.Windows())
                {
                    // Only process explorer.exe windows
                    if (!window.FullName.EndsWith("explorer.exe", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string windowPath = window.Document.Folder.Self.Path?.ToString();
                    if (windowPath == null)
                        continue;

                    // Direct path match
                    if (windowPath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase) &&
                        (IntPtr)(long)window.HWND == hwnd)
                        return window;

                    string windowPathAsName = windowPath.GetSpecialFolderName();
                    string normalizedPathAsName = normalizedPath.GetSpecialFolderName();

                    if (windowPathAsName.Equals(normalizedPathAsName, StringComparison.OrdinalIgnoreCase))
                        return window;
                }
            }
            catch (Exception ex)
            {
                Runtime.Log($"Error getting tab for path {path}: {ex.Message}");
            }
            finally
            {
                Marshal.ReleaseComObject(shell);
            }

            return null;
        }

        public static (string root, List<string> selected, dynamic window) GetSelection()
        {
            dynamic explorerWindow = null;
            var selectedPaths = new List<string>();
            string root = null;

            var shell = new Shell();
            Runtime.Log("GetSelection");
            try
            {
                // Get the window under mouse
                IntPtr windowUnderMouse = Desktop.WindowFromPoint(Cursor.Position);
                IntPtr rootWindowUnderMouse = Desktop.GetAncestor(windowUnderMouse, Desktop.GA_ROOT);

                // Get the foreground window (the one with focus)
                IntPtr foregroundWindow = GetForegroundWindow();

                var explorersTabs = new List<dynamic>();        // all tabs of all explorers
                foreach (dynamic window in shell.Windows())     // need to use foreach since LINQ does not work with dynamic
                    explorersTabs.Add(window);

                foreach (dynamic tabObject in explorersTabs)
                {
                    if (!tabObject.FullName.EndsWith("explorer.exe", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // all tabs of a given explorer window share the same HWND but have different COM objects (tabObject)
                    IntPtr windowHandle = new IntPtr(tabObject.HWND);

                    bool hasMouseOver = windowHandle == rootWindowUnderMouse;

                    // Only process if the window has mouse over
                    if (!hasMouseOver)
                        continue;

                    if (tabObject.Document == null)
                        continue;

                    var thisExplorerTabs = explorersTabs
                            .Where(x => new IntPtr(x.HWND) == windowHandle)
                            .ToList();

                    bool supportWin11Tabs = true;
                    if (thisExplorerTabs.Count > 1 && supportWin11Tabs)
                    {
                        explorerWindow = ResolveActiveTabInMultiTabWindow(tabObject, thisExplorerTabs, explorersTabs);
                        if (explorerWindow == null)
                            break; // Error was shown to user
                    }
                    else
                    {
                        explorerWindow = tabObject;
                    }

                    root = explorerWindow?.Document?.Folder?.Self?.Path?.ToString();

                    if (root != null)
                    {
                        // Track location in history
                        ExplorerHistory.AddLocation(root);

                        foreach (FolderItem item in explorerWindow.Document.SelectedItems())
                            selectedPaths.Add(item.Path);
                    }

                    break;
                }
            }
            catch (Exception ex)
            {
                Runtime.Log("Error getting explorer selection: " + ex.Message);
            }
            finally
            {
                Marshal.ReleaseComObject(shell);
            }

            return (root, selectedPaths, explorerWindow);
        }

        static dynamic ResolveActiveTabInMultiTabWindow(dynamic tabObject, List<dynamic> thisExplorerTabs, List<dynamic> explorersTabs)
        {
            // At this point we have identified the target explorer window.
            // The problem is that in Windows 11, if the explorer window has multiple tabs,
            // the Document.Folder.Self.Path returns the path of the first tab, not the active tab.
            // Thus, we need to use UI Automation to find the active tab and get its path.
            // And then we need to find the tab object that matches that path.

            // Use UI Automation returns tab name like "This PC" but the explorer window object has
            // it as "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}"
            string activeTabPath = AutomationHelper.GetExplorerRoot(tabObject);
            activeTabPath = activeTabPath.GetSpecialFolderCLSID();

            // Controlled by the explorer has 'Display the full path in the title bar' enabled
            bool fullPathInTitleBar = Path.IsPathRooted(activeTabPath);

            List<dynamic> matchingTabs;

            if (fullPathInTitleBar)
                matchingTabs = thisExplorerTabs.Where(x => x.Document.Folder.Self.Path == activeTabPath).ToList();
            else
                matchingTabs = thisExplorerTabs.Where(x => x.Document.Folder.Self.Path.ToString().EndsWith(activeTabPath)).ToList();

            if (matchingTabs.Count > 1)
            {
                var i = 1;

                var errorMessage = $"Warning: Multiple matching tabs found for path '{activeTabPath}':\n\n" +
                    string.Join("\n", matchingTabs.Select(x => $"{i++}: {(x.Document.Folder.Self.Path as string).GetSpecialFolderName()}".Trim())) + "\n\n" +
                    "Due to the Windows Explorer API limitations it's impossible to detect which one is active.\n\n" +
                    "You can minimize the chances of this error by enabling folder options 'Display the full path in the title bar'.\n\n" +
                    "Please close duplicate tabs and try again.";
                Runtime.ShowWarning(errorMessage);

                return null; // Signal error
            }

            dynamic explorerWindow = matchingTabs.FirstOrDefault();

            if (explorerWindow == null) // we could not match (e.g. it was special folder)
            {
                var firstSpecialFolder = explorersTabs
                    .FirstOrDefault(x => x.Document.Folder.Self.Path.ToString().StartsWith("::{"));

                explorerWindow = firstSpecialFolder;
            }

            return explorerWindow;
        }

        public static void NavigateToPath(dynamic explorerWindow, string path)
        {
            try
            {
                explorerWindow.Navigate2(path);
            }
            catch (Exception ex)
            {
                Runtime.Log($"Error navigating to path {path}: {ex.Message}");
            }
        }

        public static void SelectItem(dynamic explorerWindow, string fullPath)
        {
            var folderView = explorerWindow.Document;
            var folder = folderView.Folder;

            var itemName = Path.GetFileName(fullPath);
            var item = folder.ParseName(itemName);
            if (item == null)
                return;

            const int SVSI_SELECT = 0x1;
            const int SVSI_FOCUSED = 0x10;
            const int SVSI_ENSUREVISIBLE = 0x8;
            const int SVSI_DESELECTOTHERS = 0x4;

            folderView.SelectItem(item,
                SVSI_SELECT | SVSI_DESELECTOTHERS | SVSI_FOCUSED);

            var ttt = folderView.SelectItem(item,
                SVSI_SELECT | SVSI_FOCUSED | SVSI_ENSUREVISIBLE);
        }
    }
}

static class AutomationHelper

{
    public static string GetExplorerRoot(dynamic window)
    {
        // AutomationHelper.GetExplorer(explorer)?.FindTabControl()?.GetTabs().FirstOrDefault(t => t.IsActive())?.Current.Name
        // Works well but it is extremely slow. So we just get the window name directly.

        var root = AutomationElement.FromHandle((IntPtr)window.HWND);
        if (root == null) return null;

        var name = root.Current.Name;
        if (name.IsEmpty()) return null;

        var folderName = window.LocationName?.ToString();

        // window.LocationName returns the name of the folder (not the path).
        // root.Current.Name returns he path of the active tab, but with extra text.
        // Path: @"D:\tools\QTTabBar"
        // root.Current.Name: @"D:\tools\QTTabBar and 1 more tab - File Explorer"
        // window.LocationName: "QTTabBar"

        // var index = name.LastIndexOf(folderName) + folderName.Length;
        // if (index <= 0)
        //     return null;
        // else
        //     return name.Substring(0, index);

        return name.Replace(" and ", "|").Split('|').First(); // will break if the name is translated or changed
    }

    public static AutomationElement GetExplorer(IntPtr hwnd)
    {
        var root = AutomationElement.FromHandle(hwnd);
        if (root == null) return null;

        var name = root.Current.Name;
        if (name.IsEmpty()) return null;

        Runtime.Log("Foreground tabObject title: " + name);
        return name.Contains("File Explorer") ? root : null;
    }

    public static AutomationElement FindTabControl(this AutomationElement explorer)
    {
        return explorer.FindFirst(
            TreeScope.Descendants,
            new PropertyCondition(
                AutomationElement.ControlTypeProperty,
                ControlType.Tab));
    }

    public static AutomationElement[] GetTabs(this AutomationElement tabControl)
    {
        var result = new List<AutomationElement>();
        // First, let's see what children the tab control has
        var allChildren = tabControl.FindAll(TreeScope.Children, Condition.TrueCondition);

        // Search descendants instead of just children
        var tabs = tabControl.FindAll(
                   TreeScope.Descendants,
                   new PropertyCondition(
                       AutomationElement.ControlTypeProperty,
                       ControlType.TabItem));

        foreach (AutomationElement tab in tabs)
            result.Add(tab);

        return result.ToArray();
    }

    public static bool IsActive(this AutomationElement tab)
    {
        // Try SelectionItemPattern
        if (tab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object pattern))
        {
            var selectionPattern = (SelectionItemPattern)pattern;
            if (selectionPattern.Current.IsSelected)
                return true;
        }
        return false;
    }
}