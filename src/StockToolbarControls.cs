using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Explobar
{
    public class ClickArgs
    {
        public ExplorerContext Context { get; set; }
        public bool DoNotHideToolbar { get; set; }
        public ToolbarForm Toolbar { get; set; }
    }

    public interface ICustomButton
    {
        void OnClick(ClickArgs args);

        void OnInit(ToolbarItem item, ExplorerContext context);

        int IconIndex { get; }
        string IconPath { get; }

        string Tooltip { get; }
    }

    class StockToolbarControls
    {
        public static Dictionary<string, Func<Button>> Items = new Dictionary<string, Func<Button>>
        {
            { "{from-clipboard}", () => new NavigateFromClipboard() },
            { "{new-file}", () => new NewFile() },
            { "{new-folder}", () => new NewFolder() },
            { "{new-tab}", () => new NewTab() },
            { "{props}", () => new FileProperties() },
            { "{icons}", () => new BrowseIcons() },
            { "{recent}", () => new RecentLocations() },
            { "{app-config}", () => new AppConfig() },
            { "{favorites}", () => new FavoriteLocations() },
            { "{application}", () => new FavoriteApplications() },
        };
    }

    public class CustomButton : Button, ICustomButton
    {
        // public bool DonotHideToolbarOnClick { get; protected set; }
        public int IconIndex { get; protected set; }

        public string IconPath { get; protected set; }
        public string Tooltip { get; protected set; }
        public bool IsExpandabe { get; protected set; }

        public virtual void OnClick(ClickArgs args)
        {
        }

        public virtual void OnInit(ToolbarItem item, ExplorerContext context)
        {
        }

        public static void PopupMenu(Control button, ClickArgs args, Func<ContextMenuStrip> buildMenu)
        {
            args.DoNotHideToolbar = true;  // keep toolbar open and close it when the menu closes

            var menu = buildMenu();

            // Prevent toolbar from closing while menu is open
            var toolbarForm = button.FindForm() as ToolbarForm;
            if (toolbarForm != null)
            {
                toolbarForm.SuspendMouseCheck();

                menu.Closed += (s, e) =>
                {
                    toolbarForm.ResumeMouseCheck();
                    // toolbarForm.HideToolbar();
                };
            }

            // Position the menu below the button
            var buttonLocation = button.PointToScreen(new System.Drawing.Point(0, button.Height));
            menu.Show(buttonLocation);
        }

        public static void NavigateToPath(ExplorerContext context, string newRoot)
        {
            bool isCtrlPressed = (Desktop.GetAsyncKeyState(Desktop.VK_CONTROL) & 0x8000) != 0;
            if (!isCtrlPressed)
            {
                var latestContext = context.GetFreshCopy();
                Explorer.NavigateToPath(latestContext.Window, newRoot);
            }
            else
            {
                // no need to get the fresh copy as GetTabs() will return the fresh one anyway
                var tabs = Explorer.GetTabs();
                Desktop.SentKeyInput(context.HWND, "^t");
                Thread.Sleep(100);

                var newTab = Explorer.GetTabs().Except(tabs).FirstOrDefault();
                if (newTab != null)
                    Explorer.NavigateToPath(newTab, newRoot);
            }
        }
    }

    class MisconfiguredButton : CustomButton
    {
        string buttonPath;

        public MisconfiguredButton(string buttonPath)
        {
            this.buttonPath = buttonPath;
            IconIndex = 219;
            IconPath = @"%SystemRoot%\System32\shell32.dll";
            Tooltip = "Invalid custom button";
        }

        public override void OnClick(ClickArgs args)
        {
            Runtime.ShowWarning($"This custom button is misconfigured and cannot be used.\nButton path: {this.buttonPath}");
        }
    }

    class AppConfig : CustomButton
    {
        public AppConfig()
        {
            IconIndex = 314;
            IconPath = @"%SystemRoot%\System32\shell32.dll";
            Tooltip = "Application Configuration";
            IsExpandabe = true;
        }

        public override void OnClick(ClickArgs args)
        {
            CustomButton.PopupMenu(this, args, () =>
            {
                var menu = new ContextMenuStrip();

                void addMenuItem(string text, string tooltip, Action onClick)
                {
                    var menuItem = new ToolStripMenuItem(text);
                    menuItem.ToolTipText = tooltip;
                    menuItem.Click += (s, e) => onClick();
                    menu.Items.Add(menuItem);
                }

                addMenuItem("Toolbar Items Configuration", "Configure toolbar buttons appearance and actions.",
                            () => Process.Start("notepad.exe", ToolbarItems.ConfigPath));

                addMenuItem("Preview icons", "Browse all icons from a given file.",
                            IconBrowser.Show);

                addMenuItem("Show/Hide Console", "Toggle visibility of the application console.",
                            ConsoleManager.Toggle);

                addMenuItem("Restart Explorer", "Restart Window Expolorer",
                            () =>
                            {
                                var processes = Process.GetProcessesByName("explorer");
                                foreach (var process in processes)
                                    try { process.Kill(); } catch { }
                                Process.Start("explorer.exe", $"{args.Context.RootPath}");
                            });

                menu.Items.Add(new ToolStripSeparator());

                addMenuItem("About", "",
                            AboutBox.Show);

                return menu;
            });
        }
    }

    class RecentLocations : CustomButton
    {
        public RecentLocations()
        {
            IconIndex = 316;
            IconPath = @"%SystemRoot%\System32\shell32.dll";
            Tooltip = "Recent locations";
            IsExpandabe = true; // indicates that this button shows a dropdown menu
        }

        public override void OnClick(ClickArgs args)
        {
            CustomButton.PopupMenu(this, args, () =>
            {
                var menu = new ContextMenuStrip();

                foreach (string path in ExplorerHistory.GetRecentLocations())
                {
                    var menuItem = new ToolStripMenuItem(Path.GetFileName(path));
                    menuItem.ToolTipText = path;
                    menuItem.Click += (s, e) =>
                    {
                        string newRoot = path.ExpandEnvars();
                        if (Directory.Exists(newRoot))
                            CustomButton.NavigateToPath(args.Context, newRoot);
                        else
                            Runtime.ShowWarning("The selected item path is not a valid folder path.");

                        args.Toolbar.HideToolbar();
                    };
                    menu.Items.Add(menuItem);
                }
                return menu;
            });
        }
    }

    class FavoriteApplications : CustomButton
    {
        public FavoriteApplications()
        {
            IconIndex = 137;
            IconPath = @"%SystemRoot%\System32\shell32.dll";
            Tooltip = "Application Launcher";
            IsExpandabe = true; // indicates that this button shows a dropdown menu
        }

        public override void OnClick(ClickArgs args)
        {
            CustomButton.PopupMenu(this, args, () =>
            {
                var menu = new ContextMenuStrip();

                foreach (string appDef in ToolbarItems.Applications)
                {
                    var (path, arguments, workingDir) = ParseApplicationDefinition(appDef);

                    var menuItem = new ToolStripMenuItem(Path.GetFileName(path));
                    menuItem.ToolTipText = appDef;
                    menuItem.Click += (s, e) =>
                    {
                        try
                        {
                            LaunchApplication(path, arguments, workingDir, args.Context);
                        }
                        catch (Exception ex)
                        {
                            Runtime.ShowWarning("Failed to launch application.\n\n" + ex.Message);
                        }

                        args.Toolbar.HideToolbar();
                    };
                    menu.Items.Add(menuItem);
                }
                return menu;
            });
        }

        (string path, string arguments, string workingDir) ParseApplicationDefinition(string appDef)
        {
            var parts = appDef.Split(new[] { '|' }, 3);

            if (parts.Length == 3)
                return (parts[0].Trim(), parts[1].Trim(), parts[2].Trim());

            if (parts.Length == 2)
                return (parts[0].Trim(), parts[1].Trim(), "");

            return (appDef.Trim(), "", "");
        }

        void LaunchApplication(string path, string arguments, string workingDir, ExplorerContext context)
        {
            // Expand environment variables in path
            path = Environment.ExpandEnvironmentVariables(path);

            // Replace placeholders in arguments
            if (!string.IsNullOrEmpty(arguments))
            {
                var firstItem = context.SelectedItems?.FirstOrDefault() ?? "";
                var currDir = context.RootPath ?? Environment.CurrentDirectory;

                arguments = arguments
                    .Replace("%f%", $"\"{firstItem}\"")
                    .Replace("%c%", $"\"{currDir}\"");

                arguments = Environment.ExpandEnvironmentVariables(arguments);
            }

            // Determine working directory
            string effectiveWorkingDir;
            if (!string.IsNullOrWhiteSpace(workingDir))
            {
                // Use specified working directory with placeholder replacement
                var currDir = context.RootPath ?? Environment.CurrentDirectory;
                effectiveWorkingDir = workingDir
                    .Replace("%c%", currDir);
                effectiveWorkingDir = Environment.ExpandEnvironmentVariables(effectiveWorkingDir);
            }
            else
            {
                // Default to directory where executable is located
                effectiveWorkingDir = Path.GetDirectoryName(path);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = path,
                Arguments = arguments,
                UseShellExecute = true,
                WorkingDirectory = effectiveWorkingDir
            };

            var process = Process.Start(startInfo);

            // Set focus to the process window
            if (process != null)
            {
                Task.Run(() =>
                {
                    try
                    {
                        if (process.WaitForInputIdle(2000))
                        {
                            Thread.Sleep(100);
                            if (process.MainWindowHandle != IntPtr.Zero)
                            {
                                Desktop.SetForegroundWindow(process.MainWindowHandle);
                            }
                        }
                    }
                    catch
                    {
                        // Ignore errors
                    }
                });
            }
        }
    }

    class FavoriteLocations : CustomButton
    {
        public FavoriteLocations()
        {
            IconIndex = 19;
            IconPath = @"%SystemRoot%\System32\ieframe.dll";
            // IconIndex = 43;
            // IconPath = @"%SystemRoot%\System32\shell32.dll";
            Tooltip = "Favorite locations";
            IsExpandabe = true; // indicates that this button shows a dropdown menu
        }

        public override void OnClick(ClickArgs args)
        {
            CustomButton.PopupMenu(this, args, () =>
            {
                var menu = new ContextMenuStrip();

                foreach (string path in ToolbarItems.Favorites)
                {
                    var menuItem = new ToolStripMenuItem(Path.GetFileName(path));
                    menuItem.ToolTipText = path;
                    menuItem.Click += (s, e) =>
                    {
                        string newRoot = path.ExpandEnvars();
                        if (Directory.Exists(newRoot))
                            CustomButton.NavigateToPath(args.Context, newRoot);
                        else
                            Runtime.ShowWarning("The selected item path is not a valid folder path.");

                        args.Toolbar.HideToolbar();
                    };
                    menu.Items.Add(menuItem);
                }

                return menu;
            });
        }
    }

    class BrowseIcons : CustomButton
    {
        public BrowseIcons()
        {
            IconIndex = 96;
            IconPath = @"%SystemRoot%\System32\shell32.dll";
            Tooltip = "Show properties of the selected file/folder";
        }

        public override void OnClick(ClickArgs args)
            => IconBrowser.Show();
    }

    class FileProperties : CustomButton
    {
        public FileProperties()
        {
            IconIndex = 1;//39;
            IconPath = @"%SystemRoot%\System32\sud.dll";
            Tooltip = "Show properties of the selected file/folder";
        }

        public override void OnClick(ClickArgs args)
        {
            string path = args.Context.SelectedItems.FirstOrDefault();

            if (path.HasText())
                Explorer.ShowFileProperties(path);
            else
                Explorer.ShowFileProperties(args.Context.RootPath);
        }
    }

    class NewFile : CustomButton
    {
        public NewFile()
        {
            IconIndex = 0;// 1;
            IconPath = @"%SystemRoot%\System32\shell32.dll";
            Tooltip = "Create new file";
        }

        public override void OnClick(ClickArgs args)
        {
            var path = args.Context.RootPath.NextAvailableName("New Text Document.txt");

            File.WriteAllText(path, "");
            Thread.Sleep(50);
            Desktop.NotifyFileCreated(path);

            // Get a fresh reference to the window to avoid RCW separation issues
            var latestContext = args.Context.GetFreshCopy();

            Explorer.SelectItem(latestContext.Window, path);

            Task.Run(() =>
            {
                Thread.Sleep(500);
                Desktop.SentKeyInput(latestContext.HWND, "{F2}");
            });
        }
    }

    class NewTab : CustomButton
    {
        public NewTab()
        {
            // IconIndex = 110;//209;296;45;209
            IconIndex = 0;
            // IconPath = @"%SystemRoot%\System32\shell32.dll"; // @"%SystemRoot%\System32\wmploc.dll,11"; @"%SystemRoot%\System32\twinui,0"
            IconPath = @"%SystemRoot%\System32\twinui.dll";
            Tooltip = "Create new tab (copy of the current tab)";
        }

        public override void OnClick(ClickArgs args)
        {
            string newRoot = args.Context.RootPath;

            var tabs = Explorer.GetTabs();
            Desktop.SentKeyInput(args.Context.HWND, "^t");
            Thread.Sleep(100);

            var newTab = Explorer.GetTabs().Except(tabs).FirstOrDefault();
            if (newTab != null)
                Explorer.NavigateToPath(newTab, newRoot);
        }
    }

    class NewFolder : CustomButton
    {
        public NewFolder()
        {
            IconIndex = 4;
            IconPath = @"%SystemRoot%\System32\shell32.dll";
            Tooltip = "Create new folder";
        }

        public override void OnClick(ClickArgs args)
        {
            // Possible error: COM object that has been separated from Its underlying RCW cannot be used.

            var path = args.Context.RootPath.NextAvailableName("New Folder");

            Directory.CreateDirectory(path);
            Thread.Sleep(50);
            Desktop.NotifyFileCreated(path);

            // Get a fresh reference to the window to avoid RCW separation issues
            var latestContext = args.Context.GetFreshCopy();

            Explorer.SelectItem(latestContext.Window, path);

            Task.Run(() =>
            {
                Thread.Sleep(500);
                Desktop.SentKeyInput(latestContext.HWND, "{F2}");
            });
        }
    }

    class NavigateFromClipboard : Button, ICustomButton
    {
        public int IconIndex { get; set; } = 260;
        public string IconPath { get; set; } = @"%SystemRoot%\System32\shell32.dll";

        public string Tooltip { get; set; } = "Open new tab from clipboard path";

        public void OnClick(ClickArgs args)
        {
            string newRoot = null;

            var path = Clipboard.GetText()?.Trim()?.Trim('"')?.ExpandEnvars();
            if (path.HasText())
            {
                if (Directory.Exists(path))
                    newRoot = path;
                else if (File.Exists(path))
                    newRoot = Path.GetDirectoryName(path);

                if (newRoot != null)
                    CustomButton.NavigateToPath(args.Context, newRoot);
                else
                    Runtime.ShowWarning("The clipboard does not contain a valid file or folder path.");
            }
        }

        public void OnInit(ToolbarItem item, ExplorerContext context)
        {
        }
    }
}