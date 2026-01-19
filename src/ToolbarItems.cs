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
    static class ToolbarItems
    {
        static string ConfigPath = SpecialFolder.ApplicationData.Combine("Explobar", "toolbar-items.yaml");

        public static List<ToolbarItem> Items => LoadItems();

        static List<ToolbarItem> LoadItems()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var yaml = File.ReadAllText(ConfigPath);
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(PascalCaseNamingConvention.Instance)
                        .Build();

                    var items = deserializer.Deserialize<List<ToolbarItem>>(yaml);
                    if (items != null && items.Count > 0)
                    {
                        // Resolve paths after loading
                        foreach (var item in items)
                        {
                            item.Path = item.Path.ResolvePath();
                            item.Arguments = ExpandEnvironmentVariables(item.Arguments);
                        }
                        return items.Resolve();
                    }
                }
                else
                {
                    // Create default config file if it doesn't exist
                    SaveDefaultConfig();
                }
            }
            catch (YamlDotNet.Core.SyntaxErrorException ex)
            {
                Console.WriteLine($"Error loading toolbar items: {ex.Message}; start: {ex.Start}, end: {ex.End}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading toolbar items: {ex.Message}");
            }

            // Return default items if file doesn't exist or loading fails
            return GetDefaultItems().Resolve();
        }

        static void SaveDefaultConfig()
        {
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

                var yaml = serializer.Serialize(GetDefaultItems());
                File.WriteAllText(ConfigPath, yaml);

                Console.WriteLine($"Default config created at: {ConfigPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving default config: {ex.Message}");
            }
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

    // class ExplorerContext
    // {
    //     public List<string> SelectedItems { get; set; } = new List<string>();
    //     public dynamic ExplorerObject;
    // }

    class ToolbarItem
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
        public static void Execute(this ToolbarItem info, List<string> selectedItems, string currDir)
        {
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

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = info.Path,
                    Arguments = args,
                    WorkingDirectory = workDir
                };
                System.Diagnostics.Process.Start(startInfo);
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