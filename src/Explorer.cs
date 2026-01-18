using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Explbar;
using Explobar;
using Shell32;

namespace Explbar
{
    static class Explorer
    {
        public static List<dynamic> GetTabs()
        {
            var shell = new Shell();
            var explorersTabs = new List<dynamic>();        // all tabs of all explorers
            foreach (dynamic window in shell.Windows())     // need to use foreach since LINQ does not work with dynamic
                explorersTabs.Add(window);
            return explorersTabs;
        }

        public static (string root, List<string> selected, dynamic window) GetSelection()
        {
            dynamic explorerWindow = null;
            var selectedPaths = new List<string>();
            string root = null;

            try
            {
                var shell = new Shell();

                Desktop.GetCursorPos(out Desktop.POINT cursorPos);
                IntPtr windowUnderMouse = Desktop.WindowFromPoint(cursorPos);
                IntPtr rootWindowUnderMouse = Desktop.GetAncestor(windowUnderMouse, Desktop.GA_ROOT);

                bool isLeftShiftPressed = (Desktop.GetAsyncKeyState(Desktop.VK_LSHIFT) & 0x8000) != 0;

                var explorersTabs = new List<dynamic>();        // all tabs of all explorers
                foreach (dynamic window in shell.Windows())     // need to use foreach since LINQ does not work with dynamic
                    explorersTabs.Add(window);

                foreach (dynamic tabObject in explorersTabs)
                {
                    // all tabs of a given explorer window share the same HWND but have different COM objects (tabObject)
                    IntPtr windowHandle = new IntPtr(tabObject.HWND);

                    bool hasMouseOver = windowHandle == rootWindowUnderMouse;

                    if (!hasMouseOver)
                        continue;

                    if (!tabObject.FullName.EndsWith("explorer.exe", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (tabObject.Document == null)
                        continue;

                    bool supportWin11Tabs = true;
                    if (supportWin11Tabs)
                    {
                        // At this point we have identified the target explorer tabObject.
                        // The problem is that in Windows 11, if the explorer tabObject has multiple tabs,
                        // the Document.Folder.Self.Path returns the path of the first tab, not the active tab.
                        // Thus, we need to use UI Automation to find the active tab and get its path.
                        // And then we need to find the tabObject that matches that path.

                        var activeTabPath = AutomationHelper.GetExplorerRoot(tabObject);

                        if (activeTabPath == "This PC")
                            activeTabPath = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer); // ::{20D04FE0-3AEA-1069-A2D8-08002B30309D}

                        var thisExplorerTabs = explorersTabs
                            .Where(x => new IntPtr(x.HWND) == windowHandle)
                            .ToList();

                        explorerWindow = thisExplorerTabs.FirstOrDefault(x => x.Document.Folder.Self.Path == activeTabPath);

                        if (explorerWindow == null) // we could not match (e.g. it was special folder)
                        {
                            var firstSpecialFolder = explorersTabs
                                .FirstOrDefault(x => x.Document.Folder.Self.Path.ToString().StartsWith("::{"));

                            explorerWindow = firstSpecialFolder;
                        }
                    }
                    else
                    {
                        explorerWindow = tabObject;
                    }

                    root = explorerWindow?.Document?.Folder?.Self?.Path?.ToString();

                    foreach (FolderItem item in explorerWindow.Document.SelectedItems())
                        selectedPaths.Add(item.Path);

                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting explorer selection: " + ex.Message);
            }

            return (root, selectedPaths, explorerWindow);
        }

        public static void NavigateToPath(dynamic explorerWindow, string path)
        {
            try
            {
                explorerWindow.Navigate2(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error navigating to path {path}: {ex.Message}");
            }
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
        // root.Current.Name: @"D:\tools\QTTabBar and 1 more tab -File Explorer"
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

        // Console.WriteLine("Foreground tabObject title: " + name);
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