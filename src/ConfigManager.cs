using System;
using System.Collections.Generic;
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
        public static ToolbarConfig LoadConfig()
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
                var pluginPaths = currentConfig.Items.GetPluginPaths();

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

        public static void UpdatePluginTimestamps()
        {
            pluginTimestamps.Clear();

            if (currentConfig?.Items == null)
                return;

            var pluginPaths = currentConfig.Items.GetPluginPaths();

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

        static ToolbarConfig SaveDefaultConfig()
        {
            var result = ToolbarItems.DefaultConfig;

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

        public static bool IsConfigLoadingInProgress { get; private set; } = false;
        public static string ConfigPath = SpecialFolder.LocalApplicationData.Combine("Explobar", "toolbar-items.yaml");
        static Dictionary<string, DateTime> pluginTimestamps = new Dictionary<string, DateTime>();
        public static DateTime configFileTimestamp = DateTime.MinValue;
        static ToolbarConfig currentConfig = null;
    }
}