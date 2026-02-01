# Explobar

A keyboard-driven toolbar extension for Windows Explorer that eliminates the friction between file navigation and productivity tools.

## The Problem

Windows Explorer offers virtually no customization beyond appearance. When you need to open a terminal, launch an editor, or run a script on files you're viewing, you face constant friction: copying paths, switching windows, typing commands, or navigating through nested context menus. Every action requires multiple intermediate steps that break your flow.

While brilliant tools like QTTabBar have extended Windows functionality dramatically, they have become increasingly fragile due to being tightly coupled with Explorer internals. Microsoft's dramatic changes to these internals in Windows 11 have rendered QTTabBar (and similar tools) unreliable. Thus in some cases QTTabBar is unable to even start.

## The Solution

Explobar provides immediate access to your productivity tools right where you work - in Explorer. Press a keyboard shortcut, and a toolbar appears at your cursor with instant access to any command, application, or custom action. No copying paths. No switching windows. No mouse hunting. Just navigation → selection → action.

## Key Principles

**Zero Friction UX**

- Tools appear instantly where your focus is
- Selected files and current folder are automatically available
- Single keypress or click executes any action
- No intermediate steps between thought and execution

**Customization for Everyone**

- **Simple:** Edit a YAML file to add buttons and shortcuts
- **Powerful:** Write .NET plugins for complex workflows
- Both approaches work together seamlessly

**Safe & Reliable**

- Runs in its own process - Explorer crashes won't affect it, and it won't crash Explorer
- No external dependencies except Explorer itself
- No services, no elevated privileges required
- Works entirely through standard Windows automation

**Zero-Friction Deployment**

- Just an executable and `YamlDotNet.dll`
- Run it once, configure once, forget it
- WinGet and Chocolatey packages coming soon

## Features

- 🎯 **Keyboard-Triggered Toolbar** - Appears on-demand at cursor position
- ⚡ **Stock Actions** - Built-in operations for common tasks
- 🔧 **Custom Commands** - Execute any application with automatic path injection
- ⌨️ **Global Shortcuts** - Assign hotkeys to any toolbar action
- 🔌 **Plugin System** - Extend with .NET assemblies for complex workflows
- 📂 **Favorites & Quick Access** - Folders and applications at your fingertips
- 📜 **Explorer History** - Automatically tracked, always available
- 🎨 **Icon Browser** - Preview and extract icons from any file
- 🔄 **Live Configuration** - Changes apply immediately, no restart

## Installation

1. Download from [GitHub Releases](https://github.com/oleg-shilo/explobar/releases)
2. Extract `Explobar.exe` and `YamlDotNet.dll`
3. Run `Explobar.exe` (runs in system tray)

**Requirements:** Windows 10/11, .NET Framework 4.7.2+

## Quick Start

1. **Launch:** Run Explobar.exe (minimizes to system tray)
2. **Navigate:** Open any folder in Windows Explorer
3. **Activate:** Press `Shift+Escape` (configurable)
4. **Execute:** Click any button or press `Escape` to dismiss

The toolbar appears where your cursor is, with full context of your current folder and selected files.

## Basic Configuration

Configuration file: `%LocalAppData%\Explobar\toolbar-items.yaml`

### Minimal Example

```yaml
Settings: 
  ButtonSize: 24 
  ShortcutKey: Shift+Escape
Items:
 - Path: '{new-file}'           # Built-in: create text file
 - Path: '{new-folder}'         # Built-in: create folder
 - Path: '{separator}'          # Visual separator
 - Path: 'notepad.exe'          # Custom: launch notepad 
   Arguments: '%f%'             # Pass selected file 
   Tooltip: 'Edit in Notepad'
```

### Configuration Elements

**Settings:**

- `ButtonSize` - Icon size in pixels
- `ShortcutKey` - Keyboard shortcut to show toolbar
- `HistorySize` - Number of recent folders to remember
- `ShowConsoleAtStartup` - Show debug console (for troubleshooting)

**Items:** Toolbar buttons (stock or custom)

**Favorites:** Quick-access folder list

**Applications:** Quick-launch application list

### Placeholders

- `%f%` - First selected file (quoted)
- `%c%` - Current directory (quoted)
- `%Variable%` - Any environment variable

### Stock Buttons

- `{new-tab}` - New Explorer tab
- `{new-file}` - New text file in current folder
- `{new-folder}` - New folder in current folder
- `{from-clipboard}` - Navigate to path from clipboard
- `{recent}` - Recently visited folders menu
- `{favorites}` - Favorite folders menu
- `{application}` - Favorite applications menu
- `{props}` - File/folder properties dialog
- `{separator}` - Visual separator
- `{app-config}` - Configuration and tools menu

### Example: Terminal Integration

```yaml
Items:
- Path: 'wt.exe' 
  Arguments: '-d %c%' 
  Icon: 'cmd.exe' 
  Tooltip: 'Windows Terminal' 
  Shortcut: 'Ctrl+Alt+T'
```

### Example: Hidden, Shortcut-Only Command

```yaml
Items:
- Path: 'calc.exe' 
  Shortcut: 'Ctrl+Alt+C' 
  SystemWide: true      # Works even when Explorer isn't focused 
  Hidden: true          # No toolbar button, keyboard-only
```

For complete configuration reference, see [customization.md](customization.md).

## Plugin Development

Extend Explobar with custom .NET plugins for workflows that need more than launching applications.

**Example:** Copy folder contents to clipboard

```c#
using Explobar; 
using System.IO; 
using System.Text; 
using System.Windows.Forms;

public class FolderLister : CustomButton 
{ 
    public FolderLister() 
    { 
        IconIndex = 4; 
        IconPath = "shell32.dll"; 
        Tooltip = "Copy folder listing"; 
    }

    public override void OnClick(ClickArgs args)
    {
        var content = new StringBuilder();
        foreach (var item in Directory.GetFileSystemEntries(args.Context.RootPath))
            content.AppendLine(Path.GetFileName(item));
    
        Clipboard.SetText(content.ToString());
        MessageBox.Show("Folder listing copied!");
    }
}
```

**Configuration:**

```yaml
Items:
 - Path: '{C:\Plugins\MyPlugin.dll,FolderLister}' 
   Tooltip: 'List folder contents' # if you want to override
```

For complete plugin development guide, see [customization.md](customization.md).

## Troubleshooting

**Toolbar doesn't appear**

- Ensure Explorer window has focus
- Check shortcut key isn't conflicting with other applications
- Verify mouse cursor is over Explorer window

**Configuration changes not applying**

- Check YAML syntax (use online validator)
- Review console for errors: Set `ShowConsoleAtStartup: true`
- Restart Explobar if necessary

**Plugin not loading**

- Verify path uses curly brackets: `{path\to\plugin.dll}`
- Check plugin targets .NET Framework 4.7.2
- Enable console to see detailed error messages

## Architecture Highlights

- **Separate Process:** Explobar runs independently - no risk to Explorer stability
- **COM Automation:** Safe, documented API for Explorer interaction
- **Low-Level Keyboard Hook:** System-wide shortcut monitoring
- **Dynamic Plugin Loading:** Reflection-based assembly loading at runtime
- **Zero Services:** No background services, no system modifications

## System Tray

Right-click tray icon for quick access:

- Icon Browser
- Edit Configuration
- Toggle Console
- About
- Exit

## Links

- **GitHub:** https://github.com/oleg-shilo/explobar
- **Issues:** https://github.com/oleg-shilo/explobar/issues
- **Documentation:** [customization.md](customization.md)

## Credits

- **Author:** Oleg Shilo
- **YAML Parser:** [YamlDotNet](https://github.com/aaubry/YamlDotNet)
- **Icon Extraction:** [IconExtractor](https://github.com/TsudaKageyu/IconExtractor)

---

**Explobar** - Friction-free productivity for Windows Explorer