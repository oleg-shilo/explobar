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

            contextMenu.Items.Add(new ToolStripMenuItem("Configuration", null,
                (s, e) => Process.Start("notepad.exe", ConfigManager.ConfigPath)));

            contextMenu.Items.Add(new ToolStripMenuItem("Icon Browser", null,
                (s, e) => IconBrowser.Show()));

            contextMenu.Items.Add(new ToolStripMenuItem("Show Logs", null,
                (s, e) => Process.Start("notepad.exe", Runtime.LogFilePath)));

            contextMenu.Items.Add(new ToolStripMenuItem("Mark in Logs", null,
                (s, e) => Runtime.Log("================")));

            contextMenu.Items.Add(new ToolStripSeparator());

            contextMenu.Items.Add(new ToolStripMenuItem("About", null,
                (s, e) => AboutBox.Show()));

            contextMenu.Items.Add(new ToolStripSeparator());

            contextMenu.Items.Add(new ToolStripMenuItem("Exit", null,
                (s, e) =>
                {
                    ToolbarForm.HideOnClosing = false;
                    Application.Exit();
                }));

            _trayIcon.ContextMenuStrip = contextMenu;

            _trayIcon.DoubleClick += (s, e) => Process.Start("notepad.exe", ConfigManager.ConfigPath);
        }
    }
}