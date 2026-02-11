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
        static Dictionary<string, DateTime> pluginTimestamps = new Dictionary<string, DateTime>();

        // Flag to indicate config loading is in progress (user is viewing error dialog)
        public static bool IsConfigLoadingInProgress { get; private set; } = false;

        public static bool IsConfigUpToDate
        {
            get
            {
                Runtime.Output("Checking if config is up to date...");
                if (!File.Exists(ConfigPath))
                    return false;
                if (currentConfig == null)
                    return false;

                // Check YAML config file timestamp
                var lastWriteTime = File.GetLastWriteTime(ConfigPath);
                if (lastWriteTime != configFileTimestamp)
                    return false;

                return true;
            }
        }

        public static bool ArePluginsUpToDate()
        {
            if (currentConfig?.Items == null)
                return true;

            try
            {
                // Get all plugin DLL paths from config
                var pluginPaths = GetPluginPaths(currentConfig.Items);

                foreach (var pluginPath in pluginPaths)
                {
                    if (!File.Exists(pluginPath))
                    {
                        // Plugin file was deleted - need to reload
                        Runtime.Output($"Plugin file no longer exists: {pluginPath}");
                        return false;
                    }

                    var lastWriteTime = File.GetLastWriteTime(pluginPath);

                    // Check if this is a new plugin or if it was modified
                    if (!pluginTimestamps.ContainsKey(pluginPath))
                    {
                        // New plugin detected
                        return false;
                    }

                    if (pluginTimestamps[pluginPath] < lastWriteTime)
                    {
                        // Plugin was modified
                        Runtime.Output($"Plugin file changed: {pluginPath}");
                        return false;
                    }
                }

                // Check if any plugins were removed from config
                if (pluginTimestamps.Count > pluginPaths.Count)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Runtime.Output($"Error checking plugin timestamps: {ex.Message}");
                return false; // Assume outdated on error
            }
        }

        static List<string> GetPluginPaths(List<ToolbarItem> items)
        {
            var paths = new List<string>();

            foreach (var item in items)
            {
                if (item.Path.IsEmpty())
                    continue;

                // Check if path is a plugin reference (enclosed in curly brackets)
                if (item.Path.StartsWith("{") && item.Path.EndsWith("}"))
                {
                    var pathContent = item.Path.Trim('{', '}');

                    // Skip built-in stock controls
                    if (pathContent.StartsWith("new-") ||
                        pathContent.StartsWith("from-") ||
                        pathContent == "recent" ||
                        pathContent == "favorites" ||
                        pathContent == "application" ||
                        pathContent == "props" ||
                        pathContent == "separator" ||
                        pathContent == "app-config")
                    {
                        continue;
                    }

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

        public static void UpdatePluginTimestamps()
        {
            pluginTimestamps.Clear();

            if (currentConfig?.Items == null)
                return;

            var pluginPaths = GetPluginPaths(currentConfig.Items);

            foreach (var pluginPath in pluginPaths)
            {
                try
                {
                    if (File.Exists(pluginPath))
                    {
                        pluginTimestamps[pluginPath] = File.GetLastWriteTime(pluginPath);
                        Runtime.Output($"Tracked plugin: {pluginPath}");
                    }
                }
                catch (Exception ex)
                {
                    Runtime.Output($"Failed to track plugin {pluginPath}: {ex.Message}");
                }
            }
        }

        internal static ToolbarConfig LoadConfig()
        {
            lock (typeof(ToolbarItems))
            {
                if (IsConfigUpToDate)
                    return currentConfig;

                IsConfigLoadingInProgress = true; // Block keyboard input
                try
                {
                    try
                    {
                        if (File.Exists(ConfigPath))
                        {
                            var yaml = File.ReadAllText(ConfigPath);
                            var deserializer = new DeserializerBuilder()
                                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                                .Build();

                            currentConfig = deserializer.Deserialize<ToolbarConfig>(yaml);

                            if (currentConfig?.Items == null || !currentConfig.Items.Any())
                                currentConfig = SaveDefaultConfig();
                        }
                        else
                        {
                            currentConfig = SaveDefaultConfig();
                        }
                    }
                    catch (YamlDotNet.Core.SyntaxErrorException ex)
                    {
                        if (!HandleConfigLoadError(ex, isYamlError: true))
                        {
                            // User chose to cancel - exit application
                            Application.Exit();
                            return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!HandleConfigLoadError(ex, isYamlError: false))
                        {
                            // User chose to cancel - exit application
                            Application.Exit();
                            return null;
                        }
                    }

                    if (currentConfig == null)
                        currentConfig = SaveDefaultConfig();

                    if (File.Exists(ConfigPath))
                        configFileTimestamp = File.GetLastWriteTime(ConfigPath);
                    else
                        configFileTimestamp = DateTime.MinValue;

                    // Update plugin timestamps after successful load
                    UpdatePluginTimestamps();

                    currentConfig.Items.Resolve();
                    return currentConfig;
                }
                finally
                {
                    IsConfigLoadingInProgress = false; // Unblock keyboard input
                }
            }
        }

        static bool HandleConfigLoadError(Exception ex, bool isYamlError)
        {
            var errorDetails = isYamlError && ex is YamlDotNet.Core.SyntaxErrorException yamlEx
                ? $"{ex.Message}\nStart: {yamlEx.Start}, End: {yamlEx.End}"
                : ex.Message;

            var message = $"Error loading configuration file:\n\n" +
                         $"{errorDetails}\n\n" +
                         $"Location: {ConfigPath}\n\n" +
                         $"Click OK to:\n" +
                         $"  • Backup your current config (with timestamp)\n" +
                         $"  • Create a default configuration\n" +
                         $"  • Continue running Explobar\n\n" +
                         $"Click Cancel to:\n" +
                         $"  • Exit Explobar\n" +
                         $"  • Fix the configuration manually\n" +
                         $"  • Restart when ready";

            // Note: IsConfigLoadingInProgress is true while this dialog is showing
            bool userChoseOK = Runtime.UserDecision(message);

            if (userChoseOK)
            {
                // User chose to backup and create defaults
                var backupPath = BackupConfigFile();

                var successMessage = backupPath != null
                    ? $"Your configuration has been backed up to:\n{backupPath}\n\nA default configuration has been created."
                    : "A default configuration has been created.";

                Runtime.ShowInfo(successMessage);
                Runtime.Output($"Config error handled: backup created, defaults loaded. Error was: {ex.Message}");

                return true; // Continue with defaults
            }
            else
            {
                // User chose to cancel and fix manually
                Runtime.Output($"Config load cancelled by user. Error was: {ex.Message}");
                return false; // Exit application
            }
        }

        static string BackupConfigFile()
        {
            if (!File.Exists(ConfigPath))
                return null;

            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                var directory = Path.GetDirectoryName(ConfigPath);
                var fileName = Path.GetFileNameWithoutExtension(ConfigPath);
                var extension = Path.GetExtension(ConfigPath);

                var backupPath = Path.Combine(directory, $"{fileName}.backup_{timestamp}{extension}");

                File.Copy(ConfigPath, backupPath, overwrite: false);

                Runtime.Output($"Config file backed up to: {backupPath}");
                return backupPath;
            }
            catch (Exception ex)
            {
                Runtime.Output($"Failed to backup config file: {ex.Message}");
                return null;
            }
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
                var comments = Globals.ConfigFileHeader;

                ConfigPath.EnsureFileDir();
                var yamlWithComments = comments + yaml;
                File.WriteAllText(ConfigPath, yamlWithComments);

                Runtime.Output($"Default config created at: {ConfigPath}");
            }
            catch (Exception ex)
            {
                Runtime.Output($"Error saving default config: {ex.Message}");
            }
            return result;
        }

        static List<ToolbarItem> GetDefaultItems()
        {
            var items = new List<ToolbarItem>
            {
                new ToolbarItem() { Path = ConfigConstants.new_tab },
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
                    .Replace(ConfigConstants.SelectedFile, $"\"{firstItem}\"")
                    .Replace(ConfigConstants.CurrDir, $"\"{currDir}\"")
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
                // Ignore errors - user will see no execution anyway and follow it in logs.
                // Avoid interfering with the UX via message boxes or other popups.
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