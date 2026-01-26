using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Explobar;
using Shell32;

namespace Explobar
{
    public class ClickArgs
    {
        public ExplorerContext Context { get; set; }
        public bool DoNotHideToolbar { get; set; }
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
            { "{navigate-from-clipboard}", () => new NavigateFromClipboard() },
            { "{new-file}", () => new NewFile() },
            { "{new-folder}", () => new NewFolder() },
            { "{new-tab}", () => new NewTab() },
            { "{props}", () => new FileProperties() },
            { "{icons}", () => new BrowseIcons() },
            { "{recent}", () => new RecentLocations() },
            { "{app-config}", () => new AppConfig() },
        };
    }

    public class CustomButton : Button, ICustomButton
    {
        // public bool DonotHideToolbarOnClick { get; protected set; }
        public int IconIndex { get; protected set; }

        public string IconPath { get; protected set; }
        public string Tooltip { get; protected set; }

        public virtual void OnClick(ClickArgs args)
        {
        }

        public virtual void OnInit(ToolbarItem item, ExplorerContext context)
        {
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

    class AppConfig : CustomButton
    {
        public AppConfig()
        {
            IconIndex = 314;
            IconPath = @"%SystemRoot%\System32\shell32.dll";
            Tooltip = "Application Configuration";
        }

        public override void OnClick(ClickArgs args)
        {
            // Prevent toolbar from closing while menu is open
            args.DoNotHideToolbar = true;

            var menu = new ContextMenuStrip();

            // Add 3 sample menu items
            menu.Items.Add("Toolbar Items Configuration", null, (s, e)
                => Process.Start("notepad.exe", ToolbarItems.ConfigPath));

            menu.Items.Add("Preview icons", null, (s, e)
                => IconBrowser.Show());

            menu.Items.Add("About", null, (s, e)
                => AboutBox.Show());

            var toolbarForm = this.FindForm() as ToolbarForm;
            if (toolbarForm != null)
            {
                toolbarForm.SuspendMouseCheck();
                menu.Closed += (s, e) =>
                {
                    toolbarForm.ResumeMouseCheck();
                    toolbarForm.HideToolbar();
                };
            }

            // Position the menu below the button
            var buttonLocation = this.PointToScreen(new Point(0, this.Height));
            menu.Show(buttonLocation);
        }
    }

    class RecentLocations : CustomButton
    {
        public RecentLocations()
        {
            IconIndex = 316;
            IconPath = @"%SystemRoot%\System32\shell32.dll";
            Tooltip = "Recent locations";
        }

        public override void OnClick(ClickArgs args)
        {
            args.DoNotHideToolbar = true;  // keep toolbar open and close it when the menu closes

            var menu = new ContextMenuStrip();

            foreach (string path in ExplorerHistory.GetRecentLocations())
            {
                menu.Items.Add(Path.GetFileName(path), null, (s, e) =>
                {
                    string newRoot = path;
                    CustomButton.NavigateToPath(args.Context, newRoot);
                });
            }

            // Prevent toolbar from closing while menu is open
            var toolbarForm = this.FindForm() as ToolbarForm;
            if (toolbarForm != null)
            {
                toolbarForm.SuspendMouseCheck();
                menu.Closed += (s, e) =>
                {
                    toolbarForm.ResumeMouseCheck();
                    toolbarForm.HideToolbar();
                };
            }

            // Position the menu below the button
            var buttonLocation = this.PointToScreen(new System.Drawing.Point(0, this.Height));
            menu.Show(buttonLocation);
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
            IconIndex = 1;
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

            var path = Clipboard.GetText()?.Trim()?.Trim('"');
            if (path.HasText())
            {
                if (Directory.Exists(path))
                    newRoot = path;
                else if (File.Exists(path))
                    newRoot = Path.GetDirectoryName(path);

                CustomButton.NavigateToPath(args.Context, newRoot);
            }
        }

        public void OnInit(ToolbarItem item, ExplorerContext context)
        {
        }
    }
}