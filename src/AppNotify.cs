using System.Diagnostics;

// using System.Reflection.Metadata;
using System.Windows.Forms;

namespace Explobar
{
    class AppNotify
    {
        static NotifyIcon _trayIcon;

        public static void Dispose()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }

        public static void Setup()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Icon = Runtime.AppIcon;

            _trayIcon.Text = "Explobar - Windows Explorer Toolbar";
            _trayIcon.Visible = true;

            // Create context menu
            var contextMenu = new ContextMenuStrip();

            var configItem = new ToolStripMenuItem("Configuration");
            configItem.Click += (s, e) => Process.Start("notepad.exe", ToolbarItems.ConfigPath);
            contextMenu.Items.Add(configItem);

            var iconBrowserItem = new ToolStripMenuItem("Icon Browser");
            iconBrowserItem.Click += (s, e) => IconBrowser.Show();
            contextMenu.Items.Add(iconBrowserItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += (s, e) => AboutBox.Show();
            contextMenu.Items.Add(aboutItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Application.Exit();
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = contextMenu;

            _trayIcon.DoubleClick += (s, e) => Process.Start("notepad.exe", ToolbarItems.ConfigPath);
        }
    }
}