using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Explobar
{
    static class PluginLoader
    {
        public static Button LoadCustomButtonFromAssembly(string buttonFilePath)
        {
            var (assemblyPath, className) = PluginLoader.ParsePluginPath(buttonFilePath);
            var result = (Button)PluginLoader.LoadCustomButton(assemblyPath, className);

            if (result == null)
                Runtime.Log($"Failed to load button: {buttonFilePath}");

            return result;
        }

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

                // Check if it's a .NET assembly file name
                if (!assemblyPath.EndsWithEither(".dll", ".exe"))
                {
                    return null;
                }

                Runtime.Log($"Loading plugin from: {assemblyPath}" + (className != null ? $", class: {className}" : ""));

                // Load the assembly
                var assembly = Assembly.LoadFrom(assemblyPath);

                // Find all types implementing ICustomButton
                var buttonClasses = assembly.GetTypes()
                    .Where(t => typeof(ICustomButton).IsAssignableFrom(t)
                                && !t.IsInterface
                                && !t.IsAbstract
                                && typeof(Button).IsAssignableFrom(t))
                    .ToList();

                if (!buttonClasses.Any())
                {
                    Runtime.Log($"No ICustomButton implementation found in: {assemblyPath}");
                    return null;
                }

                Type buttonClass;

                // If class name is specified, find that specific type
                if (className.HasText())
                {
                    buttonClass = buttonClasses.FirstOrDefault(t =>
                        t.Name.Equals(className, StringComparison.OrdinalIgnoreCase) ||
                        t.FullName.Equals(className, StringComparison.OrdinalIgnoreCase));

                    if (buttonClass == null)
                    {
                        Runtime.Log($"Class '{className}' not found in {assemblyPath}. Available types: {string.Join(", ", buttonClasses.Select(t => t.Name))}");
                        return null;
                    }
                }
                else
                {
                    // Use first type found
                    buttonClass = buttonClasses[0];

                    if (buttonClasses.Count > 1)
                    {
                        Runtime.Log($"Warning: Multiple ICustomButton implementations found in {assemblyPath}, using first one: {buttonClass.FullName}. Available: {string.Join(", ", buttonClasses.Select(t => t.Name))}");
                    }
                }

                // Instantiate the type
                var instance = Activator.CreateInstance(buttonClass);

                if (instance == null)
                {
                    Runtime.Log($"Failed to load plugin button from: {assemblyPath}");
                    return null;
                }

                if (!(instance is Button))
                {
                    Runtime.Log($"Plugin does not inherit from Button: {assemblyPath}");
                    return null;
                }

                Runtime.Log($"Successfully loaded plugin: {buttonClass.FullName}");

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

            // Parse to get assembly path (before comma)
            var (assemblyPath, _) = ParsePluginPath(path);

            var expanded = assemblyPath.ExpandEnvars().ResolvePath();
            return expanded.EndsWithEither(".dll", ".exe")
                && File.Exists(expanded);
        }
    }
}