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
        public List<ToolbarItem> Items { get; set; } = new List<ToolbarItem>();
    }

    class ToolbarSettings
    {
        public int ButtonSize { get; set; } = 24;
        public int HistorySize { get; set; } = 10;
        // public int ImagePadding { get; set; } = 2;
    }

    static class ToolbarItems
    {
        public static string ConfigPath = SpecialFolder.ApplicationData.Combine("Explobar", "toolbar-items.yaml");

        public static List<ToolbarItem> Items => LoadConfig().Items;
        public static ToolbarSettings Settings => LoadConfig().Settings;

        static DateTime configFileTimestamp = DateTime.MinValue;
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
                    // ImagePadding = 2
                },
                Items = GetDefaultItems()
            };

            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var serializer = new SerializerBuilder()
                    .WithNamingConvention(PascalCaseNamingConvention.Instance)
                    .Build();

                var yaml = serializer.Serialize(result);

                // Add comments at the start of the file
                var comments = new StringBuilder();
                comments.AppendLine("# Explobar Toolbar Configuration");
                comments.AppendLine("# This file defines the toolbar settings and items displayed when pressing Left Shift in Windows Explorer");
                comments.AppendLine("#");
                comments.AppendLine("# Settings:");
                comments.AppendLine("#   ButtonSize: Size of toolbar button icons in pixels (default: 24)");
                comments.AppendLine("#   HistorySize: Maximum number of recently visited locations to remember (default: 20)");
                comments.AppendLine("#");
                comments.AppendLine("# Each toolbar item has the following properties:");
                comments.AppendLine("#   Icon: Path to icon file with optional index (e.g., 'shell32.dll,314' or 'notepad.exe')");
                comments.AppendLine("#   Path: Executable or application to launch");
                comments.AppendLine("#   Arguments: Command line arguments (supports placeholders)");
                comments.AppendLine("#   WorkingDir: Working directory for the application");
                comments.AppendLine("#   Tooltip: Tooltip text shown on hover");
                comments.AppendLine("#");
                comments.AppendLine("# Available placeholders:");
                comments.AppendLine("#   %f% - First selected file (unquoted)");
                comments.AppendLine("#   %c% - Current directory (unquoted)");
                comments.AppendLine("#   %<environment-variable>% - Application data folder");
                comments.AppendLine("#");
                comments.AppendLine("# To add a separator between toolbar items, use:");
                comments.AppendLine("#   Path: '{separator}'");
                comments.AppendLine("#================================");
                comments.AppendLine();

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
                    Arguments = @"%AppData%\Explobar\toolbar-items.yaml",
                    Tooltip = "Open configuration"
                }
            };
            return items;
        }
    }

    public class ToolbarItem
    {
        public string Icon { get; set; } = "";
        internal string IconPath => Icon.ParseIconPath().path.ResolvePath();
        internal int IconIndex => Icon.ParseIconPath().index;
        public string Path { get; set; } = "";
        public string Arguments { get; set; } = "";
        public string WorkingDir { get; set; } = "";
        public string Tooltip { get; set; } = "";
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

                var firstItem = selectedItems.FirstOrDefault() ?? "";

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
                Process.Start(startInfo);
            }
            catch
            {
                // Ignore errors
            }
        }

        public static Image ExtractIcon(this string iconPath, int iconIndex)
        {
            try
            {
                if (iconPath.IsEmpty())
                    return null;

                using (var icon = new IconExtractor(iconPath).GetIcon(iconIndex))
                {
                    if (icon == null)
                        return null;

                    // Convert icon to bitmap and return a copy
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
                GetFolderPath(SpecialFolder.Windows),
                GetFolderPath(SpecialFolder.System),
                GetFolderPath(SpecialFolder.SystemX86),
                GetFolderPath(SpecialFolder.ProgramFiles),
                GetFolderPath(SpecialFolder.ProgramFilesX86),
                Path.Combine(GetFolderPath(SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps")
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