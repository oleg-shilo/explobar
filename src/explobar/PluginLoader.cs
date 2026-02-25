using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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

                Runtime.Output($"Loading plugin from: {assemblyPath}" +
                    (className != null ? $", class: {className}" : ""));

                // Load the assembly

                Assembly assembly;

                if (assemblyPath.EndsWithEither(".cs.dll"))
                    assembly = Assembly.Load(File.ReadAllBytes(assemblyPath));
                else
                    assembly = Assembly.LoadFrom(assemblyPath);

                // Find all types implementing ICustomButton
                var buttonTypes = assembly.GetTypes()
                    .Where(t => typeof(ICustomButton).IsAssignableFrom(t)
                           && !t.IsInterface
                              && !t.IsAbstract
                              && typeof(Button).IsAssignableFrom(t))
                    .ToList();

                if (!buttonTypes.Any())
                {
                    Runtime.Output($"No ICustomButton implementation found in: {assemblyPath}");
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
                        Runtime.Output($"Class '{className}' not found in {assemblyPath}. Available types: {string.Join(", ", buttonTypes.Select(t => t.Name))}");
                        return null;
                    }
                }
                else
                {
                    // Use first type found
                    buttonType = buttonTypes[0];

                    if (buttonTypes.Count > 1)
                    {
                        Runtime.Output($"Warning: Multiple ICustomButton implementations found in {assemblyPath}, using first one: {buttonType.FullName}. Available: {string.Join(", ", buttonTypes.Select(t => t.Name))}");
                    }
                }

                // Instantiate the type
                var instance = Activator.CreateInstance(buttonType);

                Runtime.Output($"Successfully loaded plugin: {buttonType.FullName}");

                return instance as ICustomButton;
            }
            catch (Exception ex)
            {
                Runtime.Output($"Error loading plugin from {assemblyPath}: {ex.Message}");
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
            return expanded.EndsWithEither(".dll", ".exe", ".cs");
            // && File.Exists(expanded);
        }

        public static Button LoadCustomButtonFromAssembly(string path)
        {
            var (assemblyPath, className) = ParsePluginPath(path);

            if (assemblyPath.EndsWithEither(".cs"))
            {
                var scriptFile = assemblyPath;
                var expectedAssembly = assemblyPath + ".dll";

                if (File.Exists(expectedAssembly) && File.GetLastWriteTimeUtc(scriptFile) == File.GetLastWriteTimeUtc(expectedAssembly))
                {
                    assemblyPath = expectedAssembly;
                }
                else
                {
                    var result = CompileScriptedPlugin(scriptFile, expectedAssembly);

                    if (result.sucecss)
                    {
                        assemblyPath = expectedAssembly;
                    }
                    else
                    {
                        Runtime.Log($"Plugin source file {scriptFile} contains some errors: {result.error}");
                        return new MisconfiguredButton(scriptFile, result.error);
                    }
                }
            }

            var customButton = LoadCustomButton(assemblyPath, className);
            return customButton as Button;
        }

        public static (bool sucecss, string error) CompileScriptedPlugin(string csFile, string outPath)
        {
            Runtime.Output($"Compiling scripted plugin: {csFile}");

            try
            {
                var source = File.ReadAllText(csFile);

                using (var codeProvider = new Microsoft.CSharp.CSharpCodeProvider())
                {
                    var parameters = new System.CodeDom.Compiler.CompilerParameters
                    {
                        GenerateExecutable = false,
                        GenerateInMemory = false, // Output to file
                        OutputAssembly = outPath,
                        IncludeDebugInformation = false,
                        TreatWarningsAsErrors = false
                    };

                    // Add references to required assemblies
                    parameters.ReferencedAssemblies.Add("System.dll");
                    parameters.ReferencedAssemblies.Add("System.Core.dll");
                    parameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");
                    parameters.ReferencedAssemblies.Add("System.Drawing.dll");

                    // Add reference to the current assembly (Explobar.exe) to access ICustomButton, etc.
                    parameters.ReferencedAssemblies.Add(System.Reflection.Assembly.GetExecutingAssembly().Location);

                    // Add any other referenced assemblies that might be needed
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                            {
                                var name = assembly.GetName().Name;
                                if (name == "Shell32" || name == "YamlDotNet" || name == "TsudaKageyu")
                                {
                                    parameters.ReferencedAssemblies.Add(assembly.Location);
                                }
                            }
                        }
                        catch
                        {
                            // Ignore assemblies we can't reference
                        }
                    }

                    // Compile the code
                    var results = codeProvider.CompileAssemblyFromSource(parameters, source);

                    // Check for compilation errors
                    if (results.Errors.HasErrors)
                    {
                        var errors = new StringBuilder();
                        errors.AppendLine($"Compilation failed for {Path.GetFileName(csFile)}:");

                        foreach (System.CodeDom.Compiler.CompilerError error in results.Errors)
                        {
                            errors.AppendLine($"  Line {error.Line}: {error.ErrorText}");
                        }

                        throw new Exception(errors.ToString());
                    }

                    // Set the output DLL timestamp to match the source file

                    // This helps with change detection
                    if (File.Exists(outPath))
                    {
                        File.SetLastWriteTime(outPath, File.GetLastWriteTime(csFile));
                    }

                    Runtime.Output($"Successfully compiled: {Path.GetFileName(csFile)} -> {Path.GetFileName(outPath)}");
                    return (true, null);
                }
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }
    }
}