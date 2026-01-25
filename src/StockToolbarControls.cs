using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public interface ICustomButton
    {
        void OnClick(ExplorerContext context);

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
        };
    }

    class CustomButton : Button, ICustomButton
    {
        public int IconIndex { get; protected set; }
        public string IconPath { get; protected set; }
        public string Tooltip { get; protected set; }

        public virtual void OnClick(ExplorerContext context)
        {
        }

        public virtual void OnInit(ToolbarItem item, ExplorerContext context)
        {
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

        public override void OnClick(ExplorerContext context)
        {
            var path = context.RootPath.NextAvailableName("New Text Document.txt");

            File.WriteAllText(path, "");
            Thread.Sleep(50);
            Desktop.NotifyFileCreated(path);

            // Get a fresh reference to the window to avoid RCW separation issues
            var latestContext = context.GetFreshCopy();

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
            IconIndex = 110;//209;296;45;209
            IconPath = @"%SystemRoot%\System32\shell32.dll"; // @"%SystemRoot%\System32\wmploc.dll,11"; @"%SystemRoot%\System32\twinui,0"
            Tooltip = "Create new tab (copy of the current tab)";
        }

        public override void OnClick(ExplorerContext context)
        {
            string newRoot = context.RootPath;

            var tabs = Explorer.GetTabs();
            Desktop.SentKeyInput(context.HWND, "^t");
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

        public override void OnClick(ExplorerContext context)
        {
            // Possible error: COM object that has been separated from Its underlying RCW cannot be used.

            var path = context.RootPath.NextAvailableName("New Folder");

            Directory.CreateDirectory(path);
            Thread.Sleep(50);
            Desktop.NotifyFileCreated(path);

            // Get a fresh reference to the window to avoid RCW separation issues
            var latestContext = context.GetFreshCopy();

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

        public void OnClick(ExplorerContext context)
        {
            string newRoot = null;

            var path = Clipboard.GetText()?.Trim()?.Trim('"');
            if (path.HasText())
            {
                if (Directory.Exists(path))
                    newRoot = path;
                else if (File.Exists(path))
                    newRoot = Path.GetDirectoryName(path);

                if (newRoot.HasText())
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
        }

        public void OnInit(ToolbarItem item, ExplorerContext context)
        {
            // this.Text = "CB";
        }
    }
}