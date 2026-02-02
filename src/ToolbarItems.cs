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
        public List<string> Applications { get; set; } = new List<string>();
        public List<ToolbarItem> Items { get; set; } = new List<ToolbarItem>();
    }

    class ToolbarSettings
    {
        public int ButtonSize { get; set; } = 24;
        public int HistorySize { get; set; } = 10;
        public string ShortcutKey { get; set; } = "Shift+Escape";
        public bool ShowConsoleAtStartup { get; set; } = false;
    }

    static class ToolbarItems
    {
        public static string ConfigPath = SpecialFolder.LocalApplicationData.Combine("Explobar", "toolbar-items.yaml");

        public static List<ToolbarItem> Items => LoadConfig().Items;
        public static ToolbarSettings Settings => LoadConfig().Settings;
        public static List<string> Favorites => LoadConfig().Favorites;
        public static List<string> Applications => LoadConfig().Applications;

        public static DateTime configFileTimestamp = DateTime.MinValue;
        static ToolbarConfig currentConfig = null;

        public static bool IsConfigUpToDate
        {
            get
            {
                if (!File.Exists(ConfigPath))
                    return false;
                if (currentConfig == null)
                    return false;

                var lastWriteTime = File.GetLastWriteTime(ConfigPath);
                return lastWriteTime == configFileTimestamp;
            }
        }

        static ToolbarConfig LoadConfig()
        {
            if (IsConfigUpToDate)
                return currentConfig;

            try
            {
                if (File.Exists(ConfigPath))
                {
                    var yaml = File.ReadAllText(ConfigPath);
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(PascalCaseNamingConvention.Instance)
                        .Build();

                    currentConfig = deserializer.Deserialize<ToolbarConfig>(yaml);
                    if (currentConfig == null || currentConfig.Items == null || !currentConfig.Items.Any())
                        currentConfig = SaveDefaultConfig();
                }
                else
                {
                    currentConfig = SaveDefaultConfig();
                }
            }
            catch (YamlDotNet.Core.SyntaxErrorException ex)
            {
                var message = $"Error loading toolbar items: {ex.Message}; start: {ex.Start}, end: {ex.End}";
                Runtime.ShowWarning(message);
                Runtime.Log(message);
            }
            catch (Exception ex)
            {
                Runtime.Log($"Error loading toolbar items: {ex.Message}");
            }

            if (currentConfig == null)
                currentConfig = SaveDefaultConfig();

            if (File.Exists(ConfigPath))
                configFileTimestamp = File.GetLastWriteTime(ConfigPath);
            else
                configFileTimestamp = DateTime.MinValue;

            currentConfig.Items.Resolve();
            return currentConfig;
        }

        static ToolbarConfig SaveDefaultConfig()
        {
            var result = new ToolbarConfig
            {
                Settings = new ToolbarSettings
                {
                    ButtonSize = 24,
                    HistorySize = 10,
                    ShortcutKey = "Shift+Escape"
                },
                Favorites = new List<string>
                {
                    SpecialFolder.Desktop.GetPath(),
                    SpecialFolder.MyDocuments.GetPath(),
                    SpecialFolder.UserProfile.GetPath()
                },
                Applications = new List<string>
                {
                    "notepad.exe",
                    "calc.exe",
                    "wt.exe|-d %c%",  // Terminal in current directory
                    "powershell.exe|-NoExit|%c%"  // PowerShell with working dir set to current
                },
                Items = GetDefaultItems()
            };

            try
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(PascalCaseNamingConvention.Instance)
                    .Build();

                var yaml = serializer.Serialize(result);
                yaml = yaml
                    .Replace($"  Arguments: ''{NewLine}", "")
                    .Replace($"  WorkingDir: ''{NewLine}", "")
                    .Replace($"  Icon: ''{NewLine}", "")
                    .Replace($"  Shortcut: ''{NewLine}", "")
                    .Replace($"  Hidden: false{NewLine}", "")
                    .Replace($"  SystemWide: false{NewLine}", "")
                    .Replace($"  Tooltip: ''{NewLine}", "");

                // Add comments at the start of the file
                var comments = new StringBuilder();
                comments.AppendLine("# Explobar Toolbar Configuration");
                comments.AppendLine("# This file defines the toolbar settings and items displayed when pressing the configured shortcut in Windows Explorer");
                comments.AppendLine("#");
                comments.AppendLine("# Settings:");
                comments.AppendLine("#   ButtonSize: Size of toolbar button icons in pixels (default: 24)");
                comments.AppendLine("#   HistorySize: Maximum number of recently visited locations to remember (default: 10)");
                comments.AppendLine("#   ShortcutKey: Keyboard key combination to trigger the toolbar (default: Shift+Escape)");
                comments.AppendLine("#                Valid values: Escape, F1-F12, OemTilde, Shift+Escape, Ctrl+F1, Alt+F2, etc.");
                comments.AppendLine("#                Supported modifiers: Shift, Ctrl, Alt (can be combined with +)");
                comments.AppendLine("#                Examples: 'F1', 'Shift+F1', 'Ctrl+Alt+F12', 'OemTilde' (~)");
                comments.AppendLine("#   ShowConsoleAtStartup: Show debug console window on application startup (default: false)");
                comments.AppendLine("#                         Console can be toggled later via tray icon or toolbar menu");
                comments.AppendLine("#");
                comments.AppendLine("# Favorites:");
                comments.AppendLine("#   List of favorite folder paths that appear in the Favorites menu");
                comments.AppendLine("#   You can add any valid folder path (supports environment variables like %UserProfile%)");
                comments.AppendLine("#   Example:");
                comments.AppendLine("#     - C:\\Projects");
                comments.AppendLine("#     - %UserProfile%\\Downloads");
                comments.AppendLine("#");
                comments.AppendLine("# Applications:");
                comments.AppendLine("#   List of application paths that appear in the Applications menu");
                comments.AppendLine("#   Format: 'path' or 'path|arguments' or 'path|arguments|workingdir'");
                comments.AppendLine("#   Arguments support placeholders: %f% (selected file), %c% (current directory)");
                comments.AppendLine("#   WorkingDir defaults to executable's directory if not specified");
                comments.AppendLine("#   WorkingDir supports placeholder: %c% (current directory in Explorer)");
                comments.AppendLine("#   Example:");
                comments.AppendLine("#     - notepad.exe");
                comments.AppendLine("#     - notepad.exe|%f%");
                comments.AppendLine("#     - wt.exe|-d %c%");
                comments.AppendLine("#     - powershell.exe|-NoExit||%c%");
                comments.AppendLine("#     - C:\\Program Files\\Notepad++\\notepad++.exe|%f%");
                comments.AppendLine("#     - python.exe|script.py|C:\\Scripts");
                comments.AppendLine("#     - %ProgramFiles%\\Git\\git-bash.exe||%c%");
                comments.AppendLine("#");
                comments.AppendLine("# Stock Toolbar Buttons (built-in functionality):");
                comments.AppendLine("#   {new-tab}         - Opens a new Explorer tab");
                comments.AppendLine("#   {new-file}        - Creates a new text file in the current directory");
                comments.AppendLine("#   {new-folder}      - Creates a new folder in the current directory");
                comments.AppendLine("#   {from-clipboard}  - Navigates to path from clipboard (Ctrl+click opens in new tab)");
                comments.AppendLine("#   {recent}          - Shows dropdown menu of recently visited folders");
                comments.AppendLine("#   {favorites}       - Shows dropdown menu of favorite folders (defined above)");
                comments.AppendLine("#   {application}     - Shows dropdown menu of applications (defined above)");
                comments.AppendLine("#   {props}           - Opens properties dialog for selected file/folder");
                comments.AppendLine("#   {separator}       - Adds a visual separator between toolbar items");
                comments.AppendLine("#   {app-config}      - Shows configuration menu (Edit Config, Icon Explorer, About)");
                comments.AppendLine("#");
                comments.AppendLine("# Custom Toolbar Items:");
                comments.AppendLine("#   Each custom toolbar item has the following properties:");
                comments.AppendLine("#   Icon: Path to icon file with optional index (e.g., 'shell32.dll,314' or 'notepad.exe')");
                comments.AppendLine("#   Path: Executable or application to launch");
                comments.AppendLine("#   Arguments: Command line arguments (supports placeholders)");
                comments.AppendLine("#   WorkingDir: Working directory for the application");
                comments.AppendLine("#   Tooltip: Tooltip text shown on hover");
                comments.AppendLine("#   Shortcut: Keyboard shortcut to trigger this item (e.g., 'Ctrl+N', 'Shift+F1')");
                comments.AppendLine("#             Uses same format as ShortcutKey setting");
                comments.AppendLine("#             Uses PowerToys if you need to solve conflicts with the Windows system shortcuts");
                comments.AppendLine("#   Hidden: Set to true to hide button from toolbar (useful for shortcut-only items)");
                comments.AppendLine("#           Default: false");
                comments.AppendLine("#   SystemWide: Set to true to make shortcut work system-wide (doesn't require Explorer focus)");
                comments.AppendLine("#               When true, %c% and %f% placeholders will be empty");
                comments.AppendLine("#               Useful for launching applications from anywhere");
                comments.AppendLine("#               Default: false");
                comments.AppendLine("#");
                comments.AppendLine("# Available placeholders:");
                comments.AppendLine("#   %f% - First selected file (quoted)");
                comments.AppendLine("#   %c% - Current directory (quoted)");
                comments.AppendLine("#   %<environment-variable>% - Any environment variable (e.g., %UserProfile%, %SystemRoot%)");
                comments.AppendLine("#");
                comments.AppendLine("# Example custom toolbar item:");
                comments.AppendLine("#   - Icon: '%SystemRoot%\\System32\\shell32.dll,314'");
                comments.AppendLine("#     Path: 'notepad.exe'");
                comments.AppendLine("#     Arguments: '%f%'");
                comments.AppendLine("#     Tooltip: 'Open in Notepad'");
                comments.AppendLine("#     Shortcut: 'Ctrl+N'");
                comments.AppendLine("#");
                comments.AppendLine("# Example shortcut-only item (no toolbar button):");
                comments.AppendLine("#   - Path: 'calc.exe'");
                comments.AppendLine("#     Shortcut: 'Ctrl+Alt+C'");
                comments.AppendLine("#     Hidden: true");
                comments.AppendLine("#");
                comments.AppendLine("# Plugin Buttons (custom .NET assemblies):");
                comments.AppendLine("#   Path must be enclosed in curly brackets and point to a .dll file containing a class that:");
                comments.AppendLine("#   - Implements ICustomButton interface");
                comments.AppendLine("#   - Inherits from System.Windows.Forms.Button");
                comments.AppendLine("#   ");
                comments.AppendLine("#   Format: '{path\\to\\assembly.dll}' or '{path\\to\\assembly.dll,ClassName}'");
                comments.AppendLine("#   ");
                comments.AppendLine("#   If class name is not specified, the first matching type is loaded");
                comments.AppendLine("#   If class name is specified, that specific class is loaded");
                comments.AppendLine("#   ");
                comments.AppendLine("#   Examples:");
                comments.AppendLine("#     - Path: '{C:\\Plugins\\MyCustomButtons.dll}'");
                comments.AppendLine("#     ");
                comments.AppendLine("#     - Path: '{C:\\Plugins\\MyCustomButtons.dll,FolderContentButton}'");
                comments.AppendLine("#       Icon: 'shell32.dll,43'");
                comments.AppendLine("#       Tooltip: 'Specific button from assembly'");
                comments.AppendLine("#     ");
                comments.AppendLine("#");
                comments.AppendLine("#================================");
                comments.AppendLine();

                ConfigPath.EnsureFileDir();
                var yamlWithComments = comments.ToString() + yaml;
                File.WriteAllText(ConfigPath, yamlWithComments);

                Runtime.Log($"Default config created at: {ConfigPath}");
            }
            catch (Exception ex)
            {
                Runtime.Log($"Error saving default config: {ex.Message}");
            }
            return result;
        }

        static List<ToolbarItem> GetDefaultItems()
        {
            var items = new List<ToolbarItem>
            {
                new ToolbarItem() { Path = "{new-tab}" },
                new ToolbarItem() { Path = "{from-clipboard}" },
                new ToolbarItem() { Path = "{separator}" },
                new ToolbarItem() { Path = "{new-file}" },
                new ToolbarItem() { Path = "{new-folder}" },
                new ToolbarItem() { Path = "{separator}" },
                new ToolbarItem() { Path = "{recent}" },
                new ToolbarItem() { Path = "{props}" },
                new ToolbarItem() { Path = "{favorites}" },
                new ToolbarItem() { Path = "{application}" },
                new ToolbarItem() { Path = "{separator}" },
                new ToolbarItem()
                {
                    Icon = @"%SystemRoot%\System32\cmd.exe",
                    Path = "wt.exe",
                    Arguments = @"-d %c% -p ""Command Prompt""; -d %c% -p ""Windows PowerShell""",
                    Tooltip = "Open Windows Terminal"
                },
                new ToolbarItem()
                {
                    Icon = @"%SystemRoot%\System32\shell32.dll,314",
                    Path = "notepad.exe",
                    Arguments = "%f%",
                    Tooltip = "Open in notepad",
                    Shortcut = "Ctrl+Alt+N"
                },
                new ToolbarItem() { Path = "{separator}" },
                new ToolbarItem() { Path = "{app-config}" },
            };
            return items;
        }
    }

    public class ToolbarItem
    {
        public string Path { get; set; } = "";
        public string Arguments { get; set; } = "";
        public string WorkingDir { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Tooltip { get; set; } = "";
        public string Shortcut { get; set; } = "";
        public bool Hidden { get; set; } = false;
        public bool SystemWide { get; set; } = false;

        internal string IconPath => Icon.ParseIconPath().path.ResolvePath();
        internal int IconIndex => Icon.ParseIconPath().index;
    }

    static class ToolbarExtesnions
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

                if (info.Arguments.Contains("%f%") && firstItem.IsEmpty())
                {
                    Runtime.ShowWarning("Please select the item in the explorer view to be passed to the command.");
                    return;
                }

                var args = info.Arguments?
                    .Replace("%f%", $"\"{firstItem}\"")
                    .Replace("%c%", $"\"{currDir}\"")
                    ?? "";
                var workDir = info.WorkingDir?
                    .Replace("%c%", currDir)
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
                    Task.Run(() =>
                    {
                        try
                        {
                            // Wait for the process to create its main window (up to 2 seconds)
                            if (process.WaitForInputIdle(2000))
                            {
                                // Give the window a moment to fully initialize
                                Thread.Sleep(100);

                                if (process.MainWindowHandle != IntPtr.Zero)
                                {
                                    Desktop.SetForegroundWindow(process.MainWindowHandle);
                                }
                            }
                        }
                        catch
                        {
                            // Ignore errors - some processes don't have a UI or can't be accessed
                        }
                    });
                }
            }
            catch
            {
                // Ignore errors
            }
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