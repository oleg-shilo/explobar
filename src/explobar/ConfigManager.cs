using System;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Environment;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Explobar
{
    static class ConfigManager
    {
        public static bool IsConfigLoadingInProgress { get; set; } = false;
        public static string ConfigPath = SpecialFolder.LocalApplicationData.Combine("Explobar", "toolbar-items.yaml");
        public static DateTime configFileTimestamp = DateTime.MinValue;

        static ToolbarConfig currentConfig = null;
        public static bool IsConfigUpToDate = false;
        static bool arePluginsUpToDate = false;
        static FileSystemWatcher configWatcher;
        static List<FileSystemWatcher> pluginWatchers = new List<FileSystemWatcher>();
        static bool suppressWatcherEvents = false; // Ignore events triggered by our own save operations

        public static bool ArePluginsUpToDate
        {
            get
            {
                if (currentConfig?.Items == null)
                    return true;
                return arePluginsUpToDate;
            }
        }

        static (string path, int timestamp) lastPluginChangeReport = (null, 0);

        static void ReportNewFileChange(string context, string path, WatcherChangeTypes changeType)
        {
            // To avoid spamming logs with multiple events for the same change, only report if it's a different file or if
            // more than 1 second has passed since the last report
            if (lastPluginChangeReport.path != path || (Environment.TickCount - lastPluginChangeReport.timestamp) > 1000)
            {
                Runtime.Output($"{context ?? "File"} change detected: {path} ({changeType})");
                lastPluginChangeReport = (path, Environment.TickCount);
            }
        }

        public static void Initialize()
        {
            // Ensure config directory exists
            ConfigPath.GetDirName().EnsureDir();

            // Set up file watcher for config file
            configWatcher = ConfigPath.WatchForChanges(onChange: (s, e) =>
                {
                    if (!suppressWatcherEvents)
                    {
                        ReportNewFileChange("Config file", e.FullPath, e.ChangeType);
                        IsConfigUpToDate = false;
                    }
                }
                                                      );
            Runtime.Output($"Config file watcher initialized for: {ConfigPath}");

            InitPluginWatchers();
        }

        static void InitPluginWatchers()
        {
            // Dispose existing watchers
            foreach (var watcher in pluginWatchers)
            {
                watcher.Dispose();
            }

            pluginWatchers.Clear();

            if (currentConfig?.Items == null)
                return;

            var pluginPaths = currentConfig.Items.GetPluginPaths();
            foreach (var pluginPath in pluginPaths)
            {
                try
                {
                    var watcher = pluginPath.WatchForChanges(onChange: (s, e) =>
                            {
                                if (!suppressWatcherEvents)
                                {
                                    ReportNewFileChange("Plugin file", e.FullPath, e.ChangeType);
                                    arePluginsUpToDate = false;
                                }
                            });

                    pluginWatchers.Add(watcher);
                    Runtime.Output($"Plugin file watcher initialized for: {pluginPath}");
                }
                catch (Exception ex)
                {
                    Runtime.Output($"Failed to initialize watcher for plugin {pluginPath}: {ex.Message}");
                }
            }
        }

        public static ToolbarConfig CurrentConfigUnsafe => currentConfig;

        static IDeserializer aggresiveDeserializer = new DeserializerBuilder()
                                 .WithNamingConvention(PascalCaseNamingConvention.Instance)
                                 .Build();

        static IDeserializer forgivingDeserializer = new DeserializerBuilder()
                                 .WithNamingConvention(PascalCaseNamingConvention.Instance)
                                 .IgnoreUnmatchedProperties()
                                 .Build();

        public static ToolbarConfig LoadConfig()
        {
            lock (typeof(ConfigManager))
            {
                if (IsConfigUpToDate && currentConfig != null)
                    return currentConfig;

                IsConfigLoadingInProgress = true; // Block keyboard input
                try
                {
                    try
                    {
                        if (File.Exists(ConfigPath))
                        {
                            var yaml = File.ReadAllText(ConfigPath);

                            try
                            {
                                currentConfig = aggresiveDeserializer.Deserialize<ToolbarConfig>(yaml);
                            }
                            catch (YamlDotNet.Core.YamlException exc)
                            {
                                // If aggressive deserialization fails due to syntax errors, try forgiving deserialization
                                currentConfig = forgivingDeserializer.Deserialize<ToolbarConfig>(yaml);
                                Runtime.ShowError("The configuration file has been loaded but some of the settings will be ignored due to the syntax error:\n\n" +
                                    $"{exc.Message}\n\n" +
                                    "Review the file and address syntax issue(s).");
                                Process.Start(new ProcessStartInfo(ConfigPath) { UseShellExecute = true });
                            }

                            if (currentConfig?.Items == null || !currentConfig.Items.Any())
                                currentConfig = SaveDefaultConfig();
                        }
                        else
                        {
                            currentConfig = SaveDefaultConfig();
                        }
                    }
                    catch (YamlDotNet.Core.YamlException ex)
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

                    // Update plugin dirty after successful load
                    ResetPluginDirtyFlag();

                    currentConfig.Items.Resolve();

                    // Mark config as up to date after successful load
                    IsConfigUpToDate = true;

                    return currentConfig;
                }
                finally
                {
                    if (configWatcher == null)
                        Initialize(); // Ensure watcher is initialized after first load
                    IsConfigLoadingInProgress = false; // Unblock keyboard input
                }
            }
        }

        static string BackupConfigFile()
        {
            if (!File.Exists(ConfigPath))
                return null;

            try
            {
                // Suppress watcher events during backup
                suppressWatcherEvents = true;
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
                finally
                {
                    suppressWatcherEvents = false;
                }
            }
            catch (Exception ex)
            {
                suppressWatcherEvents = false;
                Runtime.Output($"Failed to backup config file: {ex.Message}");
                return null;
            }
        }

        public static void ResetPluginDirtyFlag()
        {
            arePluginsUpToDate = true;
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

        public static ToolbarConfig SaveDefaultConfig(string coutputPath = null)
        {
            var result = ToolbarItems.DefaultConfig;

            try
            {
                var outputPath = coutputPath ?? ConfigPath;

                // Suppress watcher events during our own save
                suppressWatcherEvents = true;
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

                    outputPath.EnsureFileDir();
                    var yamlWithComments = comments + yaml;
                    File.WriteAllText(outputPath, yamlWithComments);

                    Runtime.Output($"Default config created at: {outputPath}");
                }
                finally
                {
                    suppressWatcherEvents = false;
                }
            }
            catch (Exception ex)
            {
                suppressWatcherEvents = false;
                Runtime.Output($"Error saving default config: {ex.Message}");
            }
            return result;
        }
    }
}