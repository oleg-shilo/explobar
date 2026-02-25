# Explobar Customization Guide

Complete reference for configuring Explobar through YAML configuration and .NET plugins.

## Table of Contents

1. [Configuration File Structure](#configuration-file-structure)
2. [Settings](#settings)
3. [Stock Toolbar Buttons](#stock-toolbar-buttons)
4. [Custom Commands](#custom-commands)
5. [Favorites and Applications](#favorites-and-applications)
6. [Keyboard Shortcuts](#keyboard-shortcuts)
7. [Advanced Features](#advanced-features)
8. [Plugin Development](#plugin-development)

---

## Configuration File Structure

Location: `%LocalAppData%\Explobar\toolbar-items.yaml`

```yaml
Settings:
 ButtonSize: 24 
 HistorySize: 10 
 ShortcutKey: 'Shift+Escape' 
 ShowConsoleAtStartup: false
Favorites:
- C:\Projects 
- %UserProfile%\Downloads
Applications:
- Path: notepad.exe
- Name: Calculator
  Path: calc.exe
- Name: Terminal
  Path: wt.exe
  Arguments: '-d %c%'
Items:
- Path: '{new-file}'
- Path: 'notepad.exe' 
  Arguments: '%f%'
```

The configuration file has four main sections:

- **Settings:** Global application settings
- **Favorites:** Quick-access folder list
- **Applications:** Quick-launch application list
- **Items:** Toolbar buttons and their behavior

---

## Settings

### ButtonSize

Size of toolbar button icons in pixels.

```yaml
Settings: ButtonSize: 24    # Default: 24, Practical range: 16-48
```

Larger values make buttons easier to click but take more screen space.

### HistorySize

Maximum number of recently visited folders to remember.

```yaml
Settings: HistorySize: 10    # Default: 10
```

Higher values provide more history but may clutter the menu.
History is accessible via the `{recent}` button.

### ShortcutKey

Keyboard combination to show the toolbar.

```yaml
Settings: ShortcutKey: 'Shift+Escape'   # Default
```

**Format:** `[Modifiers+]Key`
**Modifiers:** `Shift`, `Ctrl`, `Alt` (can be combined)
**Keys:** `F1`-`F12`, `Escape`, `OemTilde` (~), letter keys

**Examples:**

```yaml
ShortcutKey: 'F1'               # Just F1 
ShortcutKey: 'Shift+F1'         # Shift + F1 
ShortcutKey: 'Ctrl+Alt+F12'     # Ctrl + Alt + F12 
ShortcutKey: 'OemTilde'         # ~ key
```

**Note:** Some shortcuts conflict with Windows. Use [PowerToys Keyboard Manager](https://learn.microsoft.com/en-us/windows/powertoys/keyboard-manager) to remap conflicting keys.

### ShowConsoleAtStartup

Show debug console window on application startup.

```yaml
Settings: ShowConsoleAtStartup: false   # Default: false
```

Set to `true` for troubleshooting. Console shows:
- Configuration loading messages
- Button click events
- Plugin loading status
- Path navigation attempts and fallbacks
- Error details

Console can also be toggled at runtime via:
- System tray menu → "Show/Hide Console"
- Toolbar → `{app-config}` → "Show/Hide Console"

**Note:** When running as Windows Application, console output is only visible when the console window is shown.
---

## Stock Toolbar Buttons

Stock buttons are built-in functionality identified by names in curly brackets.

### Available Stock Buttons

| Button | Description | Behavior |
| ------ | ----------- | -------- |
| `{new-tab}` | New Explorer tab | Opens new tab in current window |
| `{new-file}` | New text file | Creates "New Text Document.txt", selects it, enters rename mode |
| `{new-folder}` | New folder | Creates "New Folder", selects it, enters rename mode |
| `{from-clipboard}` | Navigate from clipboard | Navigates to path in clipboard; Ctrl+click opens in new tab |
| `{recent}` | Recent folders | Dropdown menu of recently visited folders |
| `{favorites}` | Favorite folders | Dropdown menu of folders from Favorites list |
| `{application}` | Application launcher | Dropdown menu of applications from Applications list |
| `{props}` | Properties dialog | Opens properties for selected file/folder or current folder |
| `{separator}` | Visual separator | Vertical line to group buttons |
| `{app-config}` | Configuration menu | Access to config file, icon browser, console toggle, about |

### Example: Essential Toolbar

```yaml
Items:
- Path: '{new-tab}'
- Path: '{new-file}'
- Path: '{new-folder}'
- Path: '{separator}'
- Path: '{recent}'
- Path: '{favorites}'
- Path: '{separator}'
- Path: '{app-config}'
```

---

## Custom Commands

Custom commands execute applications with automatic context injection.

### Basic Command

```yaml
Items:
- Path: 'notepad.exe' 
  Arguments: '%f%' 
  Tooltip: 'Open in Notepad'
```

### Item Properties

| Property | Description | Required | Example |
| -------- | ----------- | -------- | ------- |
| `Path` | Executable path or stock button | Yes | `notepad.exe` |
| `Arguments` | Command line arguments | No | `%f%` |
| `WorkingDir` | Working directory | No | `%c%` |
| `Icon` | Icon path with index | No | `shell32.dll,42` |
| `Tooltip` | Hover text | No | `Edit file` |
| `Shortcut` | Keyboard shortcut | No | `Ctrl+N` |
| `Hidden` | Hide from toolbar | No | `true` |
| `SystemWide` | Works without Explorer focus | No | `true` |

### Placeholders

Placeholders are automatically replaced when the command executes:

- **`%f%`** - First selected file (full path, quoted)
- **`%c%`** - Current directory (full path, quoted)
- **`%Variable%`** - Any environment variable (e.g., `%UserProfile%`, `%SystemRoot%`)

**Example: Pass selected file to application**

```yaml
Items:
- Path: 'notepad++.exe' 
  Arguments: '%f%' 
  Tooltip: 'Open in Notepad++'
```

**Example: Open terminal in current folder**

```yaml
- Path: 'wt.exe' 
  Arguments: '-d %c%' 
  Icon: 'cmd.exe' 
  Tooltip: 'Windows Terminal here'
```

**Example: Complex command**

```yaml
Items:
- Path: 'powershell.exe' 
  Arguments: '-NoExit -Command "Set-Location %c%"' 
  WorkingDir: '%c%' 
  Icon: 'powershell.exe' 
  Tooltip: 'PowerShell here'
```

### Icon Specification

Icons can be:

- **Executable:** `notepad.exe` - Uses application's icon
- **DLL with index:** `shell32.dll,42` - Icon at index 42 in shell32.dll
- **Icon file:** `C:\Icons\myicon.ico` - Direct icon file
- **Path with environment variables:** - `%SystemRoot%\System32\shell32.dll,314`

Use the built-in Icon Browser to find icon indices:

- Right-click tray icon → Icon Browser
- Or: Toolbar → `{app-config}` → Preview icons

<img width="785" height="593" alt="image" src="https://github.com/user-attachments/assets/1b024caa-0a26-442d-9976-412c586060c6" />

### Path Resolution

Explobar searches for executables in:
1. Current directory
2. `%SystemRoot%` (C:\Windows)
3. `%SystemRoot%\System32`
4. `%ProgramFiles%`
5. `%ProgramFiles(x86)%`
6. All directories in `%PATH%`

You can use:
- **Simple names:** `notepad.exe`, `calc.exe`
- **Full paths:** `C:\Program Files\App\app.exe`
- **Relative paths:** `..\Tools\tool.exe`
- **Environment variables:** `%ProgramFiles%\App\app.exe`

---

## Favorites and Applications

Quick-access lists for folders and applications.

### Favorites

Folders that appear in the `{favorites}` dropdown menu.

```yaml
Favorites:
- C:\Projects
- %UserProfile%\Downloads
- %UserProfile%\Documents
- D:\Work\Active
```

**Features:**

- Click to navigate
- Ctrl+click to open in new tab
- Tooltip shows full path
- Non-existent paths are skipped
- Environment variables supported

### Applications

Applications that appear in the `{applications}` dropdown menu.

**Object Format:** Each application is defined with named properties:

| Property | Description | Required | Example |
|----------|-------------|----------|----------|
| `Name` | Display name in menu | No | `Terminal` |
| `Path` | Path to executable | Yes | `wt.exe` |
| `Arguments` | Command line arguments | No | `-d %c%` |
| `WorkingDir` | Working directory | No | `%c%` |

**Properties:**
- **Name** - Display name in menu (defaults to executable filename without extension if omitted)
- **Path** - Path to the executable (required)
- **Arguments** - Command line arguments with placeholder support (`%f%`, `%c%`, environment variables)
- **WorkingDir** - Working directory (defaults to executable's directory if omitted)

```yaml
Applications:
# Simple launch - name defaults to "notepad"
- Path: notepad.exe

# With custom name
- Name: Calculator
  Path: calc.exe

# With arguments to open in current directory
- Name: Terminal
  Path: wt.exe
  Arguments: '-d %c%'

# With arguments to open selected file
- Name: Notepad++
  Path: notepad++.exe
  Arguments: '%f%'

# With working directory
- Name: PowerShell
  Path: powershell.exe
  Arguments: '-NoExit'
  WorkingDir: '%c%'

# Full path with environment variable
- Name: Git Bash
  Path: '%ProgramFiles%\Git\git-bash.exe'
  WorkingDir: '%c%'

# Python script with specific working directory
- Name: My Script
  Path: python.exe
  Arguments: 'script.py'
  WorkingDir: 'C:\Scripts'

# VS Code: open current folder
- Name: VS Code
  Path: code.exe
  Arguments: '.'
  WorkingDir: '%c%'
```

**Features:**

- Click to launch
- Tooltip shows command details
- Same path resolution as toolbar items
- Arguments support same placeholders as toolbar items (`%f%`, `%c%`, environment variables)
- Working directory defaults to executable's folder but can be overridden
- Non-existent files are skipped
- Name field is optional and defaults to executable filename

**Placeholder Support:**

- **`%f%`** - First selected file (full path, quoted)
- **`%c%`** - Current directory (full path, quoted)
- **`%Variable%`** - Any environment variable (e.g., `%UserProfile%`, `%ProgramFiles%`)

**Example Configurations:**

```yaml
Applications:
# Simple application launch
- Path: notepad.exe

# Open selected file in editor
- Name: Edit with Notepad++
  Path: notepad++.exe
  Arguments: '%f%'

# Terminal in current directory
- Name: Windows Terminal Here
  Path: wt.exe
  Arguments: '-d %c%'

# PowerShell with no exit, in current folder
- Name: PowerShell Here
  Path: powershell.exe
  Arguments: '-NoExit'
  WorkingDir: '%c%'

# Command prompt in current folder
- Name: CMD Here
  Path: cmd.exe
  Arguments: '/K'
  WorkingDir: '%c%'

# Python script with specific working directory
- Name: Run Backup Script
  Path: python.exe
  Arguments: 'backup.py'
  WorkingDir: 'C:\Scripts'

# Git Bash in current directory
- Name: Git Bash
  Path: '%ProgramFiles%\Git\git-bash.exe'
  WorkingDir: '%c%'

# VS Code in current folder
- Name: Open in VS Code
  Path: code.exe
  Arguments: '.'
  WorkingDir: '%c%'
```


---

## Keyboard Shortcuts

Assign keyboard shortcuts to any toolbar item for instant access without showing the toolbar.

### Basic Shortcut

```yaml
Items:
- Path: 'notepad.exe'
  Arguments: '%f%'
  Shortcut: 'Ctrl+Alt+N'
```

When you press `Ctrl+Alt+N` in Explorer, the command executes immediately.

### Shortcut Format

Same format as `ShortcutKey` setting:

```yaml
Shortcut: 'Ctrl+N'          # Ctrl + N
Shortcut: 'Shift+F1'        # Shift + F1
Shortcut: 'Ctrl+Alt+F12'    # Ctrl + Alt + F12
```

### Hidden Commands Shortcuts

Create keyboard-only commands (no toolbar button):

```yaml
Items:
- Path: 'calc.exe'
  Shortcut: 'Ctrl+Alt+C'
  Hidden: true
```

### System-Wide Shortcuts

Make shortcuts work anywhere in Windows, not just when Explorer has focus:

```yaml
Items:
- Path: 'notepad.exe'
  Shortcut: 'Ctrl+Alt+N' 
  SystemWide: true
```

**Important:** When `SystemWide: true`, placeholders `%f%` and `%c%` will be empty because there's no Explorer context.

### Shortcut Examples

Open terminal (works in Explorer only)

```yaml
- Path: 'wt.exe'
  Arguments: '-d %c%' 
  Shortcut: 'Ctrl+Alt+T'
```

Launch calculator (works anywhere)

```yaml
- Path: 'calc.exe' 
  Shortcut: 'Ctrl+Alt+C' 
  SystemWide: true 
  Hidden: true
```

Screenshot tool (works anywhere)

```yaml
- Path: 'SnippingTool.exe' 
  Shortcut: 'Ctrl+Alt+S' 
  SystemWide: true Hidden: true
```

---

## Advanced Features

### Auto-Focus Launched Applications

When launching applications, Explobar automatically attempts to bring the new window to the foreground after a brief delay. This works for most GUI applications.

### Ctrl+Click Navigation

In `{recent}` and `{favorites}` menus, holding Ctrl while clicking opens the folder in a new tab instead of navigating the current window.

### Live Configuration Reload

Changes to the configuration file are detected automatically:

- Edit `toolbar-items.yaml`
- Save the file
- Changes apply on next toolbar activation (no restart needed)

### Configuration File Validation

Invalid YAML syntax is detected on load:

- Error message shows line and column
- Enable console to see detailed error messages
- Use online YAML validators for complex configs

### History Tracking

Explobar monitors all Explorer windows every 30 seconds and tracks visited folders automatically.

History file: `%LocalAppData%\Explobar\explorer-history.txt`

Access via `{recent}` button.

### Path Navigation Limitations

**Hash (#) Character:**
Explorer's COM API (`Navigate2`) has issues with paths containing the `#` character. Explobar detects this automatically and uses `explorer.exe` as a fallback, which may open the path in a new window instead of navigating the current tab.

**Workaround:**
- Avoid `#` in folder names when possible
- Path navigation will still work but may not preserve current tab and will open in another Explorer window.
- Short path (8.3 format) is attempted first if available

**Other special characters:**
Most other characters work fine with `Navigate2`. If you encounter issues with other characters (`?`, `&`), they will be handled similarly.

---

## Plugin Development

For workflows that need more than launching applications, create custom .NET plugins.

### Two Plugin Formats

Explobar supports two plugin formats:

**1. C# Source Files (`.cs`) - Scripted Plugins**
- Write plugin code directly in C# files
- Auto-compiled at runtime using CS-Script engine
- No Visual Studio or build tools required
- Perfect for simple plugins and quick prototyping
- Changes applied by reloading toolbar (no restart)
- Supports CS-Script directives (`//css_ref`, `//css_winapp`)

**2. Pre-compiled DLLs (`.dll`)**
- Traditional .NET Class Library projects
- Full IDE support with IntelliSense and debugging
- Better for complex workflows
- Multiple plugins per assembly
- Standard build and deployment

Both formats work identically once loaded. Choose based on your workflow complexity and development preferences.

### Plugin Template

A comprehensive template is available in multiple ways:

**Option 1: Generate from Tray Icon**
- Right-click Explobar tray icon
- Select **Development → Create Plugin**
- Choose location and filename
- Template file is created and opened in your default editor

**Option 2: Use Included Template**
- Located in release package: `Plugins\MyCustomButton.cs`
- Copy to your plugins folder and customize

**Template Contents:**
- Complete implementation with detailed comments
- Icon configuration examples
- Context access patterns (current path, selected files)
- Menu creation example
- Best practices and usage patterns
- C# 7.3 language features guide

The template is ready to use - copy it, modify the class name and logic, and reference it in your configuration.

### When to Use Plugins

Use plugins when you need to:

- Process multiple files
- Interact with Explorer programmatically
- Show custom UI elements
- Implement complex business logic
- Access file system or Windows APIs

### Quick Start

1. Create Class Library (.NET Framework 4.7.2)
2. Add reference to `Explobar.exe`
3. Inherit from `CustomButton` base class
4. Override `OnClick` method
5. Build and configure

A complete VS project sample can be found [here](https://github.com/oleg-shilo/explobar/blob/main/CustomPlugins/CustomPlugins.cs).

### Minimal Plugin

```csharp
using Explobar; 
using System.Windows.Forms;

public class HelloButton : CustomButton 
{ 
    public HelloButton() 
    { 
        IconIndex = 1; 
        IconPath = "shell32.dll"; 
        Tooltip = "Say Hello"; 
    }

    public override void OnClick(ClickArgs args)
    {
        MessageBox.Show($"Hello from: {args.Context.RootPath}");
    }
}
```

### Configuration

**Important:** Plugin paths MUST be enclosed in curly brackets `{}`.

**C# Source Files:**

```yaml
# Single class in file
- Path: '{C:\Plugins\MyButton.cs}'
  Tooltip: 'My Custom Button'

# With icon override
- Path: '{C:\Plugins\HelloButton.cs}'
  Icon: 'shell32.dll,43'
  Tooltip: 'Say Hello'

# With keyboard shortcut
- Path: '{C:\Plugins\QuickAction.cs}'
  Shortcut: 'Ctrl+Alt+Q'
```

**Pre-compiled DLLs:**

```yaml
# Load first ICustomButton found
- Path: '{C:\Plugins\MyPlugin.dll}'

# Load specific class
- Path: '{C:\Plugins\MyPlugin.dll,HelloButton}'
  Tooltip: 'Say hello'

# Multiple classes from same DLL
- Path: '{C:\Plugins\Tools.dll,FolderLister}'
- Path: '{C:\Plugins\Tools.dll,FileCounter}'
```

**Format:**
- DLL: `{path\to\assembly.dll}` or `{path\to\assembly.dll,ClassName}`
- C#: `{path\to\script.cs}` (class name detected automatically)

**Path Options:**
- Absolute paths: `{C:\Plugins\MyPlugin.dll}`
- Relative paths: `{..\CustomPlugins\bin\Debug\CustomPlugins.dll}`
- Environment variables: `{%LocalAppData%\Explobar\Plugins\tools.dll}`

**Class Loading:**
- Without class name: Loads first `ICustomButton` implementation found
- With class name: Loads specified class (case-insensitive match on Name or FullName)
- For C# source files: Class name auto-detected from file

If plugin fails to load, a "misconfigured button" placeholder appears with error details.

### C# Script Plugins

C# source files are compiled at runtime using the CS-Script engine.

**Supported Features:**
- C# 7.3 language features
- .NET Framework 4.7.2 APIs
- CS-Script directives for references and options
- Automatic reference to Explobar.exe

**CS-Script Directives:**

```csharp
//css_winapp                                    // Hide console window
//css_ref D:\dev\explobar\src\bin\Debug\explobar.exe  // Reference Explobar
//css_ref System.Windows.Forms.dll              // Additional references

using Explobar;
using System.Windows.Forms;

public class MyButton : CustomButton
{
    // Implementation
}
```

**Common Directives:**
- `//css_ref <assembly>` - Add assembly reference
- `//css_winapp` - Build as Windows application (no console)
- `//css_include <file>` - Include another C# file
- `//css_import <namespace>` - Import namespace

**Compilation:**
- Happens automatically on first load
- Cached in memory for subsequent use
- Compilation errors shown in console/logs
- Recompiled when file timestamp changes

**Debugging C# Scripts:**
- Enable console: `ShowConsoleAtStartup: true`
- Check logs for compilation errors
- Use `Runtime.Output()` for logging
- Compilation errors include line numbers

**Example Template Usage:**

1. Generate template:
   - Tray icon → Development → Create Plugin, OR
   - Copy `Plugins\MyCustomButton.cs` to your plugins folder
2. Rename file and class: `MyAwesomeButton.cs`
3. Modify the `OnClick` implementation
4. Add to configuration:
   ```yaml
   - Path: '{C:\MyPlugins\MyAwesomeButton.cs}'
   ```
5. Reload toolbar to compile and load

**Note:** Plugin paths must be enclosed in curly brackets `{}`.

### CustomButton Base Class

`CustomButton` provides everything you need for most plugins:

```csharp
public class CustomButton : Button, ICustomButton 
{ 
    // Should be set in the constructor 
    public int IconIndex { get; protected set; } 
    public string IconPath { get; protected set; } 
    public string Tooltip { get; protected set; } 
    public bool IsExpandabe { get; protected set; }  // Show dropdown indicator

    // Override these
    public virtual void OnClick(ClickArgs args) { }
    public virtual void OnInit(ToolbarItem item, ExplorerContext context) { }
}
```

### Accessing Explorer Context

The `ClickArgs` parameter provides full access to Explorer state:

```csharp
public override void OnClick(ClickArgs args) 
{ 
    // Current folder 
    string folder = args.Context.RootPath;

    // Selected files
    List<string> files = args.Context.SelectedItems;

    // Keep toolbar visible
    args.DoNotHideToolbar = true;

    // Hide toolbar manually later
    args.Toolbar.HideToolbar();
}

```

### Advanced Plugin Features

```csharp
using Explobar; 
using System.IO; 
using System.Linq; 
using System.Text; 
sing System.Windows.Forms;

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
        try
        {
            var content = new StringBuilder();
            
            // List directories
            foreach (var dir in Directory.GetDirectories(args.Context.RootPath))
            {
                content.AppendLine($"[DIR]  {Path.GetFileName(dir)}");
            }
            
            // List files
            foreach (var file in Directory.GetFiles(args.Context.RootPath))
            {
                var info = new FileInfo(file);
                content.AppendLine($"[FILE] {info.Name} ({info.Length:N0} bytes)");
            }
            
            Clipboard.SetText(content.ToString());
            MessageBox.Show(
                $"Copied {content.ToString().Split('\n').Length} items to clipboard!",
                "Folder Listing",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
```

### Example: File Counter

```csharp
using Explobar; 
using System.IO; 
using System.Linq; 
using System.Windows.Forms;

public class FileCounter : CustomButton 
{ 
    public FileCounter() 
    { 
        IconIndex = 71; 
        IconPath = "shell32.dll"; 
        Tooltip = "Count files and folders"; 
   }

    public override void OnClick(ClickArgs args)
    {
        int fileCount = Directory.GetFiles(args.Context.RootPath).Length;
        int dirCount = Directory.GetDirectories(args.Context.RootPath).Length;
        long totalSize = Directory.GetFiles(args.Context.RootPath)
            .Sum(f => new FileInfo(f).Length);
        
        // equivalent MessageBox.Show(..., MessageBoxIcon.Information) but with a better focus management
        Runtime.ShowInfo(                     
            $"Files: {fileCount}\n" +
            $"Folders: {dirCount}\n" +
            $"Total Size: {totalSize:N0} bytes",
            "Folder Statistics");
    }
}
```

### Example: Dropdown Menu Button

```csharp
using Explobar; 
using System.IO; 
using System.Linq; 
using System.Windows.Forms;

public class ToolsMenu : CustomButton 
{ 
    public ToolsMenu() 
    { 
        IconIndex = 137; 
        IconPath = "shell32.dll"; 
        Tooltip = "Tools"; 
        IsExpandabe = true;  // Shows dropdown indicator 
    }

    public override void OnClick(ClickArgs args)
    {
        CustomButton.PopupMenu(this, args, () =>
        {
            var menu = new ContextMenuStrip();
            
            menu.Items.Add("Count Files", null, (s, e) => CountFiles(args));
            menu.Items.Add("Total Size", null, (s, e) => ShowSize(args));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Copy Paths", null, (s, e) => CopyPaths(args));
            
            return menu;
        });
    }

    void CountFiles(ClickArgs args)
    {
        int count = Directory.GetFiles(args.Context.RootPath).Length;
        MessageBox.Show($"Files: {count}");
        args.Toolbar.HideToolbar();
    }

    void ShowSize(ClickArgs args)
    {
        long size = Directory.GetFiles(args.Context.RootPath)
                             .Sum(f => new FileInfo(f).Length);
        MessageBox.Show($"Total: {size:N0} bytes");
        args.Toolbar.HideToolbar();
    }

    void CopyPaths(ClickArgs args)
    {
        var paths = string.Join("\n", Directory.GetFileSystemEntries(args.Context.RootPath));
        Clipboard.SetText(paths);
        MessageBox.Show("Paths copied to clipboard!");
        args.Toolbar.HideToolbar();
    }
}
```

### Helper Methods

`CustomButton` provides useful helper methods:

```csharp
// Show context menu and keep toolbar open 
CustomButton.PopupMenu(this, args, () => menu);
// Navigate Explorer to folder (Ctrl+click opens in new tab) 
CustomButton.NavigateToPath(args.Context, @"C:\Windows");
// Get fresh Explorer context (avoids COM RCW errors) 
var fresh = args.Context.GetFreshCopy();
```

### Best Practices

**1. Always handle errors**

```csharp
public override void OnClick(ClickArgs args) 
{ 
    try 
    { 
        // Your code 
    } catch (Exception ex) 
    { 
        Runtime.ShowError($"Error: {ex.Message}"); 
    } 
}
```

**2. Validate input**

```csharp
if (string.IsNullOrEmpty(args.Context.RootPath)) 
{ 
    MessageBox.Show("No folder selected"); 
    return; 
}

if (!args.Context.SelectedItems.Any()) 
{ 
    MessageBox.Show("No files selected"); return; 
}
```

**3. Use async for long operations**

```csharp
using System.Threading.Tasks;
. . .
public override void OnClick(ClickArgs args) 
{ 
    args.DoNotHideToolbar = true;

    Task.Run(async () =>
    {
        // Long operation
        await ProcessFilesAsync();
        
        // Update UI on UI thread
        args.Toolbar.BeginInvoke((Action)(() =>
        {
            MessageBox.Show("Done!");
            args.Toolbar.HideToolbar();
        }));
    });
}
```

**4. Use GetFreshCopy() for COM operations**

```csharp
// Avoid "COM object separated from RCW" errors 
var fresh = args.Context.GetFreshCopy(); 
Explorer.SelectItem(fresh.Window, filePath);
```

### Debugging Plugins

**Enable console output:**

Settings: ShowConsoleAtStartup: true

**Add logging:**

```csharp
Console.WriteLine($"Processing: {args.Context.RootPath}"); 
// or
Runtime.Log("Custom log message"); // logs to console
```

**Attach Visual Studio debugger:**

1. Build plugin in Debug mode
2. Start Explobar
3. Visual Studio: Debug → Attach to Process → Explobar.exe
4. Set breakpoints in plugin code
5. Trigger plugin in Explobar

### Plugin Configuration Examples

**Load single class:**

```yaml
Items:
- Path: '{C:\Plugins\MyTools.dll}' 
  Tooltip: 'First button found'
```

**Load specific class:**

```yaml
- Path: '{C:\Plugins\MyTools.dll,HelloButton}' 
  Tooltip: 'Say hello'
```

**Multiple classes from same DLL:**

```yaml
Items:
- Path: '{C:\Plugins\MyTools.dll,FolderLister}'
- Path: '{C:\Plugins\MyTools.dll,FileCounter}'
- Path: '{C:\Plugins\MyTools.dll,ToolsMenu}'
```

**Override icon and tooltip:**

```yaml
Items:
- Path: '{C:\Plugins\MyTools.dll,FolderLister}' 
  Icon: 'shell32.dll,100' 
  Tooltip: 'Custom tooltip'
```

**With keyboard shortcut:**

```yaml
Items:
- Path: '{C:\Plugins\MyTools.dll,QuickAction}' 
  Shortcut: 'Ctrl+Alt+Q' 
  Hidden: true
```

**Explobar Configuration Example**

```yaml
Settings: 
  ButtonSize: 24 
  HistorySize: 15 
  ShortcutKey: 'Shift+Escape' 
  ShowConsoleAtStartup: false
Favorites:
- C:\Projects
- %UserProfile%\Downloads
- %UserProfile%\Documents
- D:\Work
Applications:
- Path: notepad.exe
- Name: Calculator
  Path: calc.exe
- Name: Notepad++
  Path: 'C:\Program Files\Notepad++\notepad++.exe'
  Arguments: '%f%'
- Name: Terminal Here
  Path: wt.exe
  Arguments: '-d %c%'
- Name: PowerShell Here
  Path: powershell.exe
  Arguments: '-NoExit'
  WorkingDir: '%c%'
Items:
# Stock buttons
- Path: '{new-tab}'
- Path: '{new-file}'
- Path: '{new-folder}'
- Path: '{separator}'
# Quick access
- Path: '{recent}'
- Path: '{favorites}'
- Path: '{application}'
- Path: '{separator}'
# Custom commands
- Path: 'wt.exe' 
  Arguments: '-d %c%' 
  Icon: 'cmd.exe' 
  Tooltip: 'Windows Terminal' 
  Shortcut: 'Ctrl+Alt+T'
- Path: 'notepad.exe' 
  Arguments: '%f%' 
  Tooltip: 'Edit in Notepad' 
  Shortcut: 'Ctrl+Alt+N'
- Path: 'explorer.exe' 
  Arguments: '/select,%f%' 
  Tooltip: 'Show in Explorer'
- Path: '{separator}'
# Plugins
- Path: '{C:\Plugins\ExplobarTools.dll,FolderLister}' 
  Tooltip: 'Copy folder listing'
- Path: '{C:\Plugins\ExplobarTools.dll,FileCounter}' 
  Shortcut: 'Ctrl+Alt+I'
- Path: '{separator}'
# System-wide shortcuts (hidden)
- Path: 'calc.exe' 
  Shortcut: 'Ctrl+Alt+C' 
  SystemWide: true 
  Hidden: true
- Path: 'SnippingTool.exe' 
  Shortcut: 'Ctrl+Alt+S' 
  SystemWide: true 
  Hidden: true
# Config and tools
- Path: '{separator}'
- Path: '{props}'
- Path: '{app-config}'

---

**For more help:**
- GitHub: https://github.com/oleg-shilo/explobar
- Issues: https://github.com/oleg-shilo/explobar/issues
