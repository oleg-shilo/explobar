using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Explobar
{
    static class PluginLoader
    {
        public static ICustomButton LoadCustomButton(string assemblyPath, string className = null)
        {
            try
            {
                // Expand environment variables and resolve path
                assemblyPath = assemblyPath.ExpandEnvars().ResolvePath();

                if (!File.Exists(assemblyPath))
                {
                    Runtime.Log($"Plugin assembly not found: {assemblyPath}");
                    return null;
                }

                // Check if it's a .NET assembly
                if (!assemblyPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                Runtime.Log($"Loading plugin from: {assemblyPath}" +
                    (className != null ? $", class: {className}" : ""));

                // Load the assembly
                var assembly = Assembly.LoadFrom(assemblyPath);

                // Find all types implementing ICustomButton
                var buttonTypes = assembly.GetTypes()
                    .Where(t => typeof(ICustomButton).IsAssignableFrom(t)
                           && !t.IsInterface
                              && !t.IsAbstract
                              && typeof(Button).IsAssignableFrom(t))
                    .ToList();

                if (!buttonTypes.Any())
                {
                    Runtime.Log($"No ICustomButton implementation found in: {assemblyPath}");
                    return null;
                }

                Type buttonType;

                // If class name is specified, find that specific type
                if (!string.IsNullOrWhiteSpace(className))
                {
                    buttonType = buttonTypes.FirstOrDefault(t =>
                        t.Name.Equals(className, StringComparison.OrdinalIgnoreCase) ||
                        t.FullName.Equals(className, StringComparison.OrdinalIgnoreCase));

                    if (buttonType == null)
                    {
                        Runtime.Log($"Class '{className}' not found in {assemblyPath}. Available types: {string.Join(", ", buttonTypes.Select(t => t.Name))}");
                        return null;
                    }
                }
                else
                {
                    // Use first type found
                    buttonType = buttonTypes[0];

                    if (buttonTypes.Count > 1)
                    {
                        Runtime.Log($"Warning: Multiple ICustomButton implementations found in {assemblyPath}, using first one: {buttonType.FullName}. Available: {string.Join(", ", buttonTypes.Select(t => t.Name))}");
                    }
                }

                // Instantiate the type
                var instance = Activator.CreateInstance(buttonType);

                Runtime.Log($"Successfully loaded plugin: {buttonType.FullName}");

                return instance as ICustomButton;
            }
            catch (Exception ex)
            {
                Runtime.Log($"Error loading plugin from {assemblyPath}: {ex.Message}");
                return null;
            }
        }

        public static (string assemblyPath, string className) ParsePluginPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return (null, null);

            // Remove curly brackets if present
            path = path.Trim();
            if (path.StartsWith("{") && path.EndsWith("}"))
            {
                path = path.Substring(1, path.Length - 2).Trim();
            }

            // Check if path contains comma-separated class name
            var parts = path.Split(new[] { ',' }, 2);

            if (parts.Length == 2)
            {
                return (parts[0].Trim(), parts[1].Trim());
            }

            return (path.Trim(), null);
        }

        public static bool IsPluginAssembly(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // Must be enclosed in curly brackets
            path = path.Trim();
            if (!path.StartsWith("{") || !path.EndsWith("}"))
                return false;

            // Parse to get assembly path (before comma)
            var (assemblyPath, _) = ParsePluginPath(path);

            if (string.IsNullOrWhiteSpace(assemblyPath))
                return false;

            var expanded = assemblyPath.ExpandEnvars().ResolvePath();
            return expanded.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                && File.Exists(expanded);
        }

        public static Button LoadCustomButtonFromAssembly(string path)
        {
            var (assemblyPath, className) = ParsePluginPath(path);
            var customButton = LoadCustomButton(assemblyPath, className);
            return customButton as Button;
        }
    }
}