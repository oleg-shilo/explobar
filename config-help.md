## Explobar Toolbar Configuration
The config file defines the toolbar settings and items displayed when pressing the configured shortcut in Windows Explorer

## Settings:
- **ButtonSize**: Size of toolbar button icons in pixels (default: 24)
- **HistorySize**: Maximum number of recently visited locations to remember (default: 10)
- **ShortcutKey**: Keyboard key combination to trigger the toolbar (default: Shift+Escape).<br>
   Valid values: `Escape, F1-F12, OemTilde, Shift+Escape, Ctrl+F1, Alt+F2`, etc.<br>
   Supported modifiers: `Shift, Ctrl, Alt` (can be combined with +)<br>
   Examples: `'F1', 'Shift+F1', 'Ctrl+Alt+F12', 'OemTilde' (~)`
- **ShowConsoleAtStartup**: Show debug console window on application startup (default: false).<br>
   Console can be toggled later via tray icon or toolbar menu.

## Favorites:
List of favorite folder paths that appear in the Favorites menu.<br>
You can add any valid folder path (supports environment variables like `%UserProfile%`).<br>
Example:
```yaml
Favorites:
- C:\Projects
- %UserProfile%\Downloads
```

## Applications:
List of applications that appear in the Applications menu.<br>
Each application can have the following properties:
- **Name**: Display name in the menu (optional - defaults to filename without extension)
- **Path**: Path to executable (required)
- **Arguments**: Command line arguments (optional)
- **Icon**: Custom icon path with optional index (optional - defaults to executable icon)
 
Arguments and Icon support placeholders: %f% (selected file), %c% (current directory).
Path resolution follows same rules as toolbar items (searches system paths)
 
Examples:
```yaml
- Name: "Notepad"
  Path: "notepad.exe"
- Name: "Calculator"
  Path: "calc.exe"

- Name: "Windows Terminal"
  Path: "wt.exe"
  Arguments: "-d %c%"
  Icon: "cmd.exe"

- Name: "PowerShell Here"
  Path: "powershell.exe"
  Arguments: "-NoExit -Command Set-Location %c%"

- Path: "notepad.exe"           # Name optional - will show 'notepad'

- Name: "Edit in VS Code"
  Path: "%LocalAppData%\Programs\Microsoft VS Code\Code.exe"
  Arguments: "%f%"
  Icon: "%LocalAppData%\Programs\Microsoft VS Code\Code.exe"
```

## Stock Toolbar Buttons (built-in functionality):
- `{new-tab}`: Opens a new Explorer tab
- `{new-file}`: Creates a new text file in the current directory
- `{new-folder}`: Creates a new folder in the current directory
- `{from-clipboard}`: Navigates to path from clipboard (Ctrl-press opens in new tab)
- `{recent}`: Shows dropdown menu of recently visited folders
- `{favorites}`: Shows dropdown menu of favorite folders (defined above)
- `{applications}`: Shows dropdown menu of applications (defined above)
- `{props}`: Opens properties dialog for selected file/folder
- `{separator}`: Adds a visual separator between toolbar items
- `{app-config}`: Shows configuration menu (Edit Config, Icon Explorer, About)

## Custom Toolbar Items:
Each custom toolbar item has the following properties:
- **Icon**: Path to icon file with optional index (e.g., 'shell32.dll,314' or 'notepad.exe')
- **Path**: Executable or application to launch
- **Arguments**: Command line arguments (supports placeholders)
- **WorkingDir**: Working directory for the application
- **Tooltip**: Tooltip text shown on hover
- **Shortcut**: Keyboard shortcut to trigger this item (e.g., 'Ctrl+N', 'Shift+F1')
             Uses same format as ShortcutKey setting
             Use PowerToys if you need to resolve conflicts with the Windows system shortcuts
- **Hidden** : Set to true to hide button from toolbar (useful for shortcut-only items). Default: **false**
- **SystemWide** : Set to true to make shortcut work system-wide (doesn't require Explorer focus).<br>
  When true, %c% and %f% placeholders will be empty.<br>
  Useful for launching applications from anywhere.

**Available placeholders:**
- `%f%` - First selected file (quoted)
- `%c%` - Current directory (quoted)
- `%<environment-variable>%` - Any environment variable (e.g., `%UserProfile%`, `%SystemRoot%`)

Example custom toolbar item:
```yaml
- Icon: '%SystemRoot%\System32\shell32.dll,314'
  Path: 'notepad.exe'
  Arguments: '%f%'
  Tooltip: 'Open in Notepad'
  Shortcut: 'Ctrl+N'
```
 Example shortcut-only item (no toolbar button):
```yaml
- Path: 'calc.exe'
  Shortcut: 'Ctrl+Alt+C'
  Hidden: true
```

## Plugin Buttons (custom .NET assemblies):
Path must be enclosed in curly brackets and point to a .dll or C# file (script) containing a class that:
- Implements ICustomButton interface
- Inherits from System.Windows.Forms.Button
   
Format:
- assembly: `{path\to\CustomButtons.dll}` or `{path\to\CustomButtons.dll,ClassName}`
- script: `{path\to\CustomButtons.cs}` or `{path\to\CustomButtons.cs,ClassName}`
   
If class name is not specified, the first matching type is loaded<br>
If class name is specified, that specific class is loaded
   
Examples:
```yaml
- Path: '{C:\Plugins\MyCustomButtons.dll}'
- Path: '{C:\Plugins\MyCustomButtons.dll,FolderContentButton}'
  Icon: 'shell32.dll,43'
  Tooltip: 'Specific button from assembly'
```

================================

