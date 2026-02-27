using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using static System.Environment;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shell32;
using TsudaKageyu;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Explobar
{
    class ToolbarConfig
    {
        public ToolbarSettings Settings { get; set; } = new ToolbarSettings();
        public List<string> Favorites { get; set; } = new List<string>();
        public List<ApplicationItem> Applications { get; set; } = new List<ApplicationItem>();
        public List<ToolbarItem> Items { get; set; } = new List<ToolbarItem>();
    }

    class ToolbarSettings
    {
        public bool DisableExplorerLaunchButton { get; set; } = false;
        public int ButtonSize { get; set; } = 24;
        public int HistorySize { get; set; } = 10;
        public string ShortcutKey { get; set; } = "Shift+Escape";
        public bool ShowConsoleAtStartup { get; set; } = false;
        public bool DarkTheme { get; set; } = false;

        /// <summary>
        /// Controls which button appears under the mouse cursor when the toolbar pops up.
        /// 0 = center the toolbar under cursor
        /// Positive values = 1-based index from the left (1 = first button, 2 = second, etc.)
        /// Negative values = 1-based index from the right (-1 = last button, -2 = second-to-last, etc.)
        /// If the index is out of range, the toolbar will be centered.
        /// </summary>
        public int IndexOfButtonUnderMouse { get; set; } = 0;

        /// <summary>
        /// Horizontal position of the explorer launch button from the left edge (in pixels).
        /// Default: 200
        /// </summary>
        public int ExplorerButtonXPosition { get; set; } = 200;
    }

    static class ToolbarItems
    {
        public static List<ToolbarItem> Items => ConfigManager.LoadConfig().Items;
        public static ToolbarSettings Settings => ConfigManager.LoadConfig().Settings;
        public static List<string> Favorites => ConfigManager.LoadConfig().Favorites;
        public static List<ApplicationItem> Applications => ConfigManager.LoadConfig().Applications;

        // Flag to indicate config loading is in progress (user is viewing error dialog)

        static ToolbarConfig _defaultConfig;

        public static ToolbarConfig DefaultConfig => _defaultConfig ?? (_defaultConfig =
            new ToolbarConfig
            {
                Settings = new ToolbarSettings
                {
                    ButtonSize = 24,
                    HistorySize = 10,
                    ShortcutKey = "Shift+Escape",
                    IndexOfButtonUnderMouse = 0  // Add this line to the DefaultConfig
                },
                Favorites = new List<string>
                            {
                                SpecialFolder.Desktop.GetPath(),
                                SpecialFolder.MyDocuments.GetPath(),
                                SpecialFolder.UserProfile.GetPath()
                            },
                Applications = new List<ApplicationItem>
                    {
                        new ApplicationItem { Path = "notepad.exe" },
                        new ApplicationItem { Name = "Calculator", Path = "calc.exe" },
                        new ApplicationItem
                        {
                            Name = "Terminal",
                            Path = "wt.exe",
                            Arguments = "-d %c%"
                        },
                        new ApplicationItem
                        {
                            Name = "PowerShell",
                            Path = "powershell.exe",
                            Arguments = "-NoExit %c%"
                        }
                    },
                Items = new List<ToolbarItem>
                    {
                        // new ToolbarItem() { Path = ConfigConstants.new_tab }, // may not be reliable across different explorer versions
                        new ToolbarItem() { Path = ConfigConstants.from_clip },
                        new ToolbarItem() { Path = ConfigConstants.separator },
                        new ToolbarItem() { Path = ConfigConstants.new_file },
                        new ToolbarItem() { Path = ConfigConstants.new_folder },
                        new ToolbarItem() { Path = ConfigConstants.separator },
                        new ToolbarItem() { Path = ConfigConstants.recent },
                        new ToolbarItem() { Path = ConfigConstants.props },
                        new ToolbarItem() { Path = ConfigConstants.favs },
                        new ToolbarItem() { Path = ConfigConstants.apps },
                        new ToolbarItem() { Path = ConfigConstants.separator },
                        new ToolbarItem()
                        {
                            Icon = @"%SystemRoot%\System32\cmd.exe",
                            Path = "wt.exe",
                            Arguments = $@"-d {ConfigConstants.CurrDir} -p ""Command Prompt""; -d {ConfigConstants.CurrDir} -p ""Windows PowerShell""",
                            Tooltip = "Open Windows Terminal"
                        },
                        new ToolbarItem()
                        {
                            Icon = @"%SystemRoot%\System32\shell32.dll,314",
                            Path = "notepad.exe",
                            Arguments = ConfigConstants.SelectedFile,
                            Tooltip = "Open in notepad",
                            Shortcut = "Ctrl+Alt+N"
                        },
                        new ToolbarItem() { Path = ConfigConstants.separator },
                        new ToolbarItem() { Path = ConfigConstants.app_config },
                    }
            }
                                                                       );
    }

    public class ToolbarItem
    {
        public string Path { get; set; } = "";
        public string Arguments { get; set; } = "";
        public string WorkingDir { get; set; } = "";

        // public bool GrabFocus { get; set; } = true;
        public string Icon { get; set; } = "";

        public string Tooltip { get; set; } = "";
        public string Shortcut { get; set; } = "";
        public bool Hidden { get; set; } = false;
        public bool SystemWide { get; set; } = false;

        internal string IconPath => Icon.ParseIconPath().path.ResolvePath();
        internal int IconIndex => Icon.ParseIconPath().index;
    }

    public class ApplicationItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string Arguments { get; set; } = "";
        public string WorkingDir { get; set; } = "";
    }

    static class ToolbarExtensions
    {
        public static void Execute(this ToolbarItem info, ExplorerContext context)
        {
            List<string> selectedItems = context.SelectedItems;
            string currDir = context.RootPath ?? CurrentDirectory;
            try
            {
                if (info.Path.IsEmpty() || !File.Exists(info.Path))
                    return;

                var firstItem = selectedItems?.FirstOrDefault() ?? "";

                if (info.Arguments.Contains(ConfigConstants.SelectedFile) && firstItem.IsEmpty())
                {
                    Runtime.ShowWarning("Please select the item in the explorer view to be passed to the command.");
                    return;
                }

                var args = info.Arguments?
                    .Replace(ConfigConstants.SelectedFile, firstItem.EnquoteAsPath())
                    .Replace(ConfigConstants.CurrDir, currDir.EnquoteAsPath())
                    ?? "";

                var workDir = info.WorkingDir?
                    .Replace(ConfigConstants.CurrDir, currDir)
                    ?? "";

                var startInfo = new ProcessStartInfo
                {
                    FileName = info.Path,
                    Arguments = args,
                    WorkingDirectory = workDir
                };

                var process = Process.Start(startInfo);

                // Set focus to the process window
                if (process != null)
                {
                    TrySetProcessFocus(process);
                }
            }
            catch (Exception ex)
            {
                // Avoid interfering with the UX via message boxes or other popups.
                Runtime.Output($"Failed to execute toolbar item '{info.Path}': {ex.Message}");
            }
        }

        static void TrySetProcessFocus(Process process)
        {
            Task.Run(() =>
            {
                try
                {
                    if (process.WaitForInputIdle(Globals.WindowStabilizationDelay))
                    {
                        Thread.Sleep(100);
                        if (!process.HasExited && process.MainWindowHandle != IntPtr.Zero)
                            Desktop.SetForegroundWindow(process.MainWindowHandle);
                    }
                }
                catch { /* Non-critical background operation */ }
            });
        }

        public static Image ExtractIcon(this string iconPath, int iconIndex, int iconSize = 0)
        {
            try
            {
                if (iconPath.IsEmpty())
                    return null;

                var extractor = new IconExtractor(iconPath);

                if (iconIndex >= extractor.Count)
                    return null;

                var icon = extractor.GetIcon(iconIndex);
                if (icon == null)
                    return null;

                // If a specific size is requested, try to get that size
                if (iconSize > 0)
                {
                    try
                    {
                        using (icon)
                        using (var sizedIcon = new Icon(icon, iconSize, iconSize))
                        {
                            return new Bitmap(sizedIcon.ToBitmap());
                        }
                    }
                    catch
                    {
                        // If the requested size is not available, fall through to default
                    }
                }

                // Return the icon at its default size
                using (icon)
                {
                    return new Bitmap(icon.ToBitmap());
                }
            }
            catch
            {
                return null;
            }
        }

        public static List<string> GetPluginPaths(this List<ToolbarItem> items)
        {
            var paths = new List<string>();

            foreach (var item in items)
            {
                if (item.Path.IsEmpty())
                    continue;

                // Check if path is a plugin reference (enclosed in curly brackets)
                if (item.Path.StartsWith("{") && item.Path.EndsWith("}"))
                {
                    // Skip built-in stock controls
                    if (ConfigConstants.StockButtons.Contains(item.Path))
                        continue;

                    var pathContent = item.Path.Trim('{', '}');

                    // Extract DLL path (format: path or path,ClassName)
                    var dllPath = pathContent.Split(',')[0].Trim();

                    // Resolve path (expand environment variables)
                    dllPath = dllPath.ExpandEnvars();

                    // Check if it's a valid DLL file
                    if (dllPath.EndsWithEither(".dll", ".exe", ".cs") && !paths.Contains(dllPath))
                    {
                        paths.Add(dllPath);
                    }
                }
            }

            return paths;
        }

        public static List<ToolbarItem> Resolve(this List<ToolbarItem> items)
        {
            foreach (var item in items)
            {
                item.Path = item.Path.ResolvePath();
                item.Arguments = ExpandEnvironmentVariables(item.Arguments);
            }

            return items;
        }

        public static (string path, int index) ParseIconPath(this string iconPath)
        {
            if (iconPath.IsEmpty())
                return (iconPath, 0);

            var parts = iconPath.Split(',');
            if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out int index))
            {
                return (parts[0].Trim(), index);
            }

            return (iconPath, 0);
        }

        public static string ResolvePath(this string path)
        {
            if (path.IsEmpty())
                return path;

            path = ExpandEnvironmentVariables(path);

            if (File.Exists(path))
                return path;

            // If path contains directory separator, it's a full/relative path
            if (path.Contains(Path.DirectorySeparatorChar) || path.Contains(Path.AltDirectorySeparatorChar))
                return path;

            // Search in well-known locations
            var searchLocations = new[]
            {
                CurrentDirectory,
                SpecialFolder.Windows.GetPath(),
                SpecialFolder.System.GetPath(),
                SpecialFolder.SystemX86.GetPath(),
                SpecialFolder.ProgramFiles.GetPath(),
                SpecialFolder.ProgramFilesX86.GetPath(),
                SpecialFolder.LocalApplicationData.Combine("Microsoft", "WindowsApps")
            };

            // Search in standard locations
            foreach (var location in searchLocations)
            {
                if (location.IsEmpty())
                    continue;

                var fullPath = Path.Combine(location, path);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            // Search in PATH environment variable
            var pathEnv = GetEnvironmentVariable("PATH") ?? "";
            foreach (var pathDir in pathEnv.Split(Path.PathSeparator))
            {
                if (pathDir.IsEmpty())
                    continue;

                try
                {
                    var fullPath = Path.Combine(pathDir, path);
                    if (File.Exists(fullPath))
                        return fullPath;
                }
                catch
                {
                    // Ignore invalid paths
                }
            }

            // Return original path if not found
            return path;
        }
    }
}