using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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

            contextMenu.Items.Add(new ToolStripSeparator());

            // Development menu
            var developmentMenu = new ToolStripMenuItem("Development");

            developmentMenu.DropDownItems.Add(new ToolStripMenuItem("Open Logs", null,
                (s, e) => Process.Start("notepad.exe", Runtime.LogFilePath)));

            developmentMenu.DropDownItems.Add(new ToolStripMenuItem("Mark in Logs", null,
                (s, e) => Runtime.Log("================")));

            developmentMenu.DropDownItems.Add(new ToolStripSeparator());

            developmentMenu.DropDownItems.Add(new ToolStripMenuItem("Create Plugin", null,
                (s, e) => CreatePluginTemplate()));

            developmentMenu.DropDownItems.Add(new ToolStripMenuItem("Show/Hide Console", null,
                (s, e) => ConsoleManager.Toggle()));

            developmentMenu.DropDownItems.Add(new ToolStripSeparator());

            developmentMenu.DropDownItems.Add(new ToolStripMenuItem("Open App Folder", null,
                (s, e) => Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory)));

            developmentMenu.DropDownItems.Add(new ToolStripMenuItem("Open Startup Folder", null,
                (s, e) => Process.Start("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.Startup))));

            developmentMenu.DropDownItems.Add(new ToolStripSeparator());

            developmentMenu.DropDownItems.Add(new ToolStripMenuItem("Restart Explorer", null,
                (s, e) => RestartExplorer()));

            developmentMenu.DropDownItems.Add(new ToolStripMenuItem("Restart App", null,
                (s, e) => RestartApp()));

            contextMenu.Items.Add(developmentMenu);

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

        private static void CreatePluginTemplate()
        {
            try
            {
                var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
                if (!Directory.Exists(pluginDir))
                    Directory.CreateDirectory(pluginDir);

                var templateFile = Path.Combine(pluginDir, "MyCustomButton.cs");

                if (File.Exists(templateFile))
                {
                    var result = MessageBox.Show(
                        $"Plugin template already exists at:\n{templateFile}\n\nDo you want to open it?",
                        "Create Plugin",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                        Process.Start("notepad.exe", templateFile);

                    return;
                }

                var template = @"//css_winapp
//css_ref " + Assembly.GetExecutingAssembly().Location + @"
using System;
using System.Windows.Forms;
using Explobar;

// ============================================================================
// Explobar Custom Button Plugin Template
// ============================================================================
// C# Language Version: 7.3
// Target Framework: .NET Framework 4.7.2
// ============================================================================
//
// To use this plugin:
// 1. Save this file to a location of your choice (e.g., C:\Explobar\Plugins\)
// 2. Add to your toolbar-items.yaml:
//    - Path: '{C:\Explobar\Plugins\MyCustomButton.cs}'
//      Icon: 'shell32.dll,43'
//      Tooltip: 'My Custom Button'
//
// The plugin will be automatically compiled when the toolbar loads.
// Check logs (tray icon > Development > Open Logs) for compilation errors.
// ============================================================================

namespace MyPlugins
{
    public class MyCustomButton : CustomButton
    {
        public MyCustomButton()
        {
            // Set icon properties
            IconIndex = 43;
            IconPath = @""%SystemRoot%\System32\shell32.dll"";
            Tooltip = ""My Custom Button"";

            // Set to true if this button shows a dropdown menu
            IsExpandabe = false;
        }

        public override void OnInit(ToolbarItem item, ExplorerContext context)
        {
            // Called once when button is initialized
            // You can access item configuration and initial context here

            string rootPath = context.RootPath;
            Runtime.Output(""MyCustomButton initialized for path: "" + rootPath);
        }

        public override void OnClick(ClickArgs args)
        {
            // Called when button is clicked
            // Access current Explorer context through args.Context

            string currentPath = args.Context.RootPath;
            var selectedFiles = args.Context.SelectedItems;
            int fileCount = selectedFiles.Count;

            MessageBox.Show(
                ""Current Path: "" + currentPath + ""\nSelected Files: "" + fileCount,
                ""Custom Button Clicked"",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            // To keep toolbar open after click (useful for menu buttons):
            // args.DoNotHideToolbar = true;

            // Example: Show a dropdown menu
            // CustomButton.PopupMenu(this, args, BuildMenu);
        }

        // Example: Menu builder method (C# 7.3 local function would work inside OnClick too)
        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();

            menu.Items.Add(""Action 1"", null, (s, e) =>
            {
                MessageBox.Show(""Action 1 executed"");
            });

            menu.Items.Add(""Action 2"", null, (s, e) =>
            {
                MessageBox.Show(""Action 2 executed"");
            });

            return menu;
        }
    }
}

";

                File.WriteAllText(templateFile, template);

                var message = $"Plugin template created at:\n{templateFile}\n\nOpening in Notepad...";
                MessageBox.Show(message, "Create Plugin", MessageBoxButtons.OK, MessageBoxIcon.Information);

                Process.Start("notepad.exe", templateFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create plugin template:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void RestartExplorer()
        {
            try
            {
                var result = MessageBox.Show(
                    "This will restart Windows Explorer.\nAll Explorer windows will be closed and reopened.\n\nContinue?",
                    "Restart Explorer",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    var processes = Process.GetProcessesByName("explorer");
                    foreach (var process in processes)
                    {
                        try
                        {
                            process.Kill();
                            process.WaitForExit(200);
                        }
                        catch { /* ignored */ }
                    }
                    Process.Start("explorer.exe");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to restart Explorer:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void RestartApp()
        {
            try
            {
                var result = MessageBox.Show(
                    "This will restart Explobar.\n\nContinue?",
                    "Restart App",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    ToolbarForm.HideOnClosing = false;
                    Process.Start(Application.ExecutablePath, $"{Globals.CliArgWait}:{Process.GetCurrentProcess().Id}");
                    Application.Exit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to restart application:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}