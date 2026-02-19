using System.IO;
using System.Text;

namespace Explobar
{
    static class ConfigConstants
    {
        public const string new_tab = "{new-tab}";
        public const string from_clip = "{from-clipboard}";
        public const string separator = "{separator}";
        public const string new_file = "{new-file}";
        public const string new_folder = "{new-folder}";
        public const string recent = "{recent}";
        public const string props = "{props}";
        public const string favs = "{favorites}";
        public const string icons = "{icons}";
        public const string apps = "{applications}";
        public const string app_config = "{app-config}";
        public const string CurrDir = "%c%";
        public const string SelectedFile = "%f%";

        public static string[] StockButtons = new[]
        {
            new_tab, from_clip, separator, new_file, new_folder, recent, props, favs, apps, app_config
        };
    }

    static class Globals
    {
        public const int WindowStabilizationDelay = 2000;
        public const int ProcessWindowInitTimeout = 100;

        public const string CliArgWait = "-wait";
        public const string CliArgHelp = "-help";
        public const string CliArgConfigHelp = "-confighelp";

        public static string CliHelpText
        {
            get
            {
                var help = new StringBuilder();
                help.AppendLine("Explobar - Keyboard-driven toolbar extension for Windows Explorer");
                help.AppendLine();
                help.AppendLine("Usage: explobar.exe [options]");
                help.AppendLine();
                help.AppendLine("Options:");
                help.AppendLine("  " + Globals.CliArgHelp + "          Show this CLI help text");
                help.AppendLine("  " + Globals.CliArgConfigHelp + "    Show configuration file format documentation");
                help.AppendLine();
                help.AppendLine("When run without options, Explobar starts in system tray and monitors");
                help.AppendLine("Windows Explorer for keyboard shortcuts.");
                help.AppendLine();
                help.AppendLine("Configuration:");
                help.AppendLine($"  Config file: %LocalAppData%\\Explobar\\toolbar-items.yaml");
                help.AppendLine($"  Log files:   %LocalAppData%\\Explobar\\logs\\");
                help.AppendLine();
                help.AppendLine("Default shortcut: Shift+Escape (configurable in toolbar-items.yaml)");
                help.AppendLine();
                help.AppendLine("For more information, visit: https://github.com/oleg-shilo/explobar");

                return help.ToString();
            }
        }

        public static string ConfigFileHeader
        {
            get
            {
                var comments = new StringBuilder();
                comments.AppendLine("# Explobar Toolbar Configuration");
                comments.AppendLine("# You can find out about config file format either by:");
                comments.AppendLine("#  - right-clicking tray icon > Help");
                comments.AppendLine("#  - visiting For more information: https://github.com/oleg-shilo/explobar/config-help.md");
                comments.AppendLine("#  - executing explobar.exe -config-help");
                comments.AppendLine("#");
                comments.AppendLine("#================================");
                comments.AppendLine();

                return comments.ToString();
            }
        }

        public static string ConfigFileHelp
        {
            get
            {
                var comments = new StringBuilder();
                comments.AppendLine("## Explobar Toolbar Configuration");
                comments.AppendLine("The config file defines the toolbar settings and items displayed when pressing the configured shortcut in Windows Explorer");
                comments.AppendLine();
                comments.AppendLine("### Settings:");
                comments.AppendLine("- **ButtonSize**: Size of toolbar button icons in pixels (default: 24)");
                comments.AppendLine("- **HistorySize**: Maximum number of recently visited locations to remember (default: 10)");
                comments.AppendLine("- **ShortcutKey**: Keyboard key combination to trigger the toolbar (default: Shift+Escape).<br>");
                comments.AppendLine("   Valid values: `Escape, F1-F12, OemTilde, Shift+Escape, Ctrl+F1, Alt+F2`, etc.<br>");
                comments.AppendLine("   Supported modifiers: `Shift, Ctrl, Alt` (can be combined with +)<br>");
                comments.AppendLine("   _Examples:_ `'F1', 'Shift+F1', 'Ctrl+Alt+F12', 'OemTilde' (~)`");
                comments.AppendLine("- **ShowConsoleAtStartup**: Show debug console window on application startup (default: false).<br>");
                comments.AppendLine("   Console can be toggled later via tray icon or toolbar menu.");
                comments.AppendLine();
                comments.AppendLine("### Favorites:");
                comments.AppendLine("List of favorite folder paths that appear in the Favorites menu.<br>");
                comments.AppendLine("You can add any valid folder path (supports environment variables like `%UserProfile%`).<br>");
                comments.AppendLine("Example:");
                comments.AppendLine("```yaml");
                comments.AppendLine("Favorites:");
                comments.AppendLine("- C:\\Projects");
                comments.AppendLine("- %UserProfile%\\Downloads");
                comments.AppendLine("```");
                comments.AppendLine("");
                comments.AppendLine("### Applications:");
                comments.AppendLine("List of applications that appear in the Applications menu.<br>");
                comments.AppendLine("Each application can have the following properties:");
                comments.AppendLine("- **Name**: Display name in the menu (optional - defaults to filename without extension)");
                comments.AppendLine("- **Path**: Path to executable (required)");
                comments.AppendLine("- **Arguments**: Command line arguments (optional)");
                comments.AppendLine("- **Icon**: Custom icon path with optional index (optional - defaults to executable icon)");
                comments.AppendLine(" ");
                comments.AppendLine("Arguments and Icon support placeholders: `%f%` (selected file), `%c%` (current directory).");
                comments.AppendLine("Path resolution follows same rules as toolbar items (searches system paths)");
                comments.AppendLine(" ");
                comments.AppendLine("_Examples:_");
                comments.AppendLine("```yaml");
                comments.AppendLine("- Name: Notepad");
                comments.AppendLine("  Path: notepad.exe");
                comments.AppendLine("- Name: Calculator");
                comments.AppendLine("  Path: calc.exe");
                comments.AppendLine("");
                comments.AppendLine("- Name: Windows Terminal");
                comments.AppendLine("  Path: wt.exe");
                comments.AppendLine("  Arguments: '-d %c%'");
                comments.AppendLine("  Icon: cmd.exe");
                comments.AppendLine("");
                comments.AppendLine("- Name: PowerShell Here");
                comments.AppendLine("  Path: powershell.exe");
                comments.AppendLine("  Arguments: '-NoExit -Command Set-Location %c%'");
                comments.AppendLine("");
                comments.AppendLine("- Path: notepad.exe           # Name optional - will show 'notepad'");
                comments.AppendLine("");
                comments.AppendLine("- Name: Edit in VS Code");
                comments.AppendLine("  Path: '%LocalAppData%\\Programs\\Microsoft VS Code\\Code.exe'");
                comments.AppendLine("  Arguments: %f%");
                comments.AppendLine("  Icon: '%LocalAppData%\\Programs\\Microsoft VS Code\\Code.exe'");
                comments.AppendLine("```");
                comments.AppendLine("");
                comments.AppendLine("### Stock Toolbar Buttons (built-in functionality):");
                comments.AppendLine($"- `{ConfigConstants.new_tab}`: Opens a new Explorer tab");
                comments.AppendLine($"- `{ConfigConstants.new_file}`: Creates a new text file in the current directory");
                comments.AppendLine($"- `{ConfigConstants.new_folder}`: Creates a new folder in the current directory");
                comments.AppendLine($"- `{ConfigConstants.from_clip}`: Navigates to path from clipboard (Ctrl-press opens in new tab)");
                comments.AppendLine($"- `{ConfigConstants.recent}`: Shows dropdown menu of recently visited folders");
                comments.AppendLine($"- `{ConfigConstants.favs}`: Shows dropdown menu of favorite folders (defined above)");
                comments.AppendLine($"- `{ConfigConstants.apps}`: Shows dropdown menu of applications (defined above)");
                comments.AppendLine($"- `{ConfigConstants.props}`: Opens properties dialog for selected file/folder");
                comments.AppendLine($"- `{ConfigConstants.separator}`: Adds a visual separator between toolbar items");
                comments.AppendLine($"- `{ConfigConstants.app_config}`: Shows configuration menu (Edit Config, Icon Explorer, About)");
                comments.AppendLine("");
                comments.AppendLine("### Custom Toolbar Items:");
                comments.AppendLine("Each custom toolbar item has the following properties:");
                comments.AppendLine("- **Icon**: Path to icon file with optional index (e.g., 'shell32.dll,314' or 'notepad.exe')");
                comments.AppendLine("- **Path**: Executable or application to launch");
                comments.AppendLine("- **Arguments**: Command line arguments (supports placeholders)");
                comments.AppendLine("- **WorkingDir**: Working directory for the application");
                comments.AppendLine("- **Tooltip**: Tooltip text shown on hover");
                comments.AppendLine("- **Shortcut**: Keyboard shortcut to trigger this item (e.g., 'Ctrl+N', 'Shift+F1')");
                comments.AppendLine("             Uses same format as ShortcutKey setting");
                comments.AppendLine("             Use PowerToys if you need to resolve conflicts with the Windows system shortcuts");
                comments.AppendLine("- **Hidden** : Set to true to hide button from toolbar (useful for shortcut-only items). Default: **false**");
                comments.AppendLine("- **SystemWide** : Set to true to make shortcut work system-wide (doesn't require Explorer focus).<br>");
                comments.AppendLine("  When true, %c% and %f% placeholders will be empty.<br>");
                comments.AppendLine("  Useful for launching applications from anywhere.");
                comments.AppendLine("");
                comments.AppendLine("**Available placeholders:**");
                comments.AppendLine("- `%f%` - First selected file (quoted)");
                comments.AppendLine("- `%c%` - Current directory (quoted)");
                comments.AppendLine("- `%<environment-variable>%` - Any environment variable (e.g., `%UserProfile%`, `%SystemRoot%`)");
                comments.AppendLine("");
                comments.AppendLine("Example custom toolbar item:");
                comments.AppendLine("```yaml");
                comments.AppendLine("- Icon: '%SystemRoot%\\System32\\shell32.dll,314'");
                comments.AppendLine("  Path: 'notepad.exe'");
                comments.AppendLine("  Arguments: '%f%'");
                comments.AppendLine("  Tooltip: 'Open in Notepad'");
                comments.AppendLine("  Shortcut: 'Ctrl+N'");
                comments.AppendLine("```");
                comments.AppendLine(" Example shortcut-only item (no toolbar button):");
                comments.AppendLine("```yaml");
                comments.AppendLine("- Path: 'calc.exe'");
                comments.AppendLine("  Shortcut: 'Ctrl+Alt+C'");
                comments.AppendLine("  Hidden: true");
                comments.AppendLine("```");
                comments.AppendLine("");
                comments.AppendLine("### Plugin Buttons (custom .NET assemblies):");
                comments.AppendLine("Path must be enclosed in curly brackets and point to a .dll or C# file (script) containing a class that:");
                comments.AppendLine("- Implements ICustomButton interface");
                comments.AppendLine("- Inherits from System.Windows.Forms.Button");
                comments.AppendLine("   ");
                comments.AppendLine("Format:");
                comments.AppendLine("- assembly: `{path\\to\\CustomButtons.dll}` or `{path\\to\\CustomButtons.dll,ClassName}`");
                comments.AppendLine("- script: `{path\\to\\CustomButtons.cs}` or `{path\\to\\CustomButtons.cs,ClassName}`");
                comments.AppendLine("   ");
                comments.AppendLine("If class name is not specified, the first matching type is loaded<br>");
                comments.AppendLine("If class name is specified, that specific class is loaded");
                comments.AppendLine("   ");
                comments.AppendLine("Examples:");
                comments.AppendLine("```yaml");
                comments.AppendLine("- Path: '{C:\\Plugins\\MyCustomButtons.dll}'");
                comments.AppendLine("- Path: '{C:\\Plugins\\MyCustomButtons.dll,FolderContentButton}'");
                comments.AppendLine("  Icon: 'shell32.dll,43'");
                comments.AppendLine("  Tooltip: 'Specific button from assembly'");
                comments.AppendLine("```");
                comments.AppendLine("");
                comments.AppendLine("#================================");
                comments.AppendLine();

                return comments.ToString();
            }
        }
    }
}