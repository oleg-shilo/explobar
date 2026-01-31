# Explobar Plugin Development Guide

Create custom toolbar buttons using .NET Framework assemblies.

## Requirements
- .NET Framework 4.7.2+
- C# 7.3+
- Reference to Explobar.exe
- Windows Forms knowledge

## Quick Start

1. Create Class Library (.NET Framework 4.7.2)
2. Add reference to Explobar.exe
3. Implement CustomButton class
4. Build and configure

## Example Plugin

using System.Windows.Forms;
using Explobar;

public class MyButton : CustomButton
{
    public MyButton()
    {
        IconIndex = 42;
        IconPath = "shell32.dll";
        Tooltip = "My Custom Action";
    }

    public override void OnClick(ClickArgs args)
    {
        MessageBox.Show($"Current folder: {args.Context.RootPath}");
    }
}

## Configuration

Items:
  - Path: '{C:\Plugins\MyPlugin.dll}'
    Tooltip: 'My custom button'
  
  - Path: '{C:\Plugins\MyPlugin.dll,SpecificClassName}'
    Tooltip: 'Specific class from DLL'

## ICustomButton Interface

Methods:
- OnClick(ClickArgs args) - Called when button clicked
- OnInit(ToolbarItem item, ExplorerContext context) - Called on initialization

Properties:
- IconIndex - Icon index in file (0-based)
- IconPath - Path to icon file (DLL/EXE/ICO)
- Tooltip - Hover text

## ClickArgs

args.Context.RootPath - Current folder
args.Context.SelectedItems - Selected files list
args.Context.Window - COM Explorer window
args.Context.HWND - Window handle
args.Toolbar - Toolbar form reference
args.DoNotHideToolbar - Set true to keep toolbar open

## CustomButton Base Class

public class CustomButton : Button, ICustomButton
{
    public int IconIndex { get; protected set; }
    public string IconPath { get; protected set; }
    public string Tooltip { get; protected set; }
    public bool IsExpandabe { get; protected set; }
}

Set IsExpandabe = true to show dropdown indicator.

## Helper Methods

CustomButton.PopupMenu(this, args, () => menu) - Show context menu
CustomButton.NavigateToPath(context, path) - Navigate Explorer
context.GetFreshCopy() - Get updated context (avoid COM errors)

## Example: Folder Content Lister

using System.IO;
using System.Text;
using System.Windows.Forms;
using Explobar;

public class FolderContentButton : CustomButton
{
    public FolderContentButton()
    {
        IconIndex = 4;
        IconPath = "shell32.dll";
        Tooltip = "Copy folder content";
    }

    public override void OnClick(ClickArgs args)
    {
        var content = new StringBuilder();
        foreach (var dir in Directory.GetDirectories(args.Context.RootPath))
            content.AppendLine($"[DIR]  {Path.GetFileName(dir)}");
        foreach (var file in Directory.GetFiles(args.Context.RootPath))
            content.AppendLine($"[FILE] {Path.GetFileName(file)}");
        
        Clipboard.SetText(content.ToString());
        MessageBox.Show("Copied to clipboard!");
    }
}

## Example: Dropdown Menu

public class MenuButton : CustomButton
{
    public MenuButton()
    {
        IconIndex = 137;
        IconPath = "shell32.dll";
        Tooltip = "Tools";
        IsExpandabe = true;
    }

    public override void OnClick(ClickArgs args)
    {
        CustomButton.PopupMenu(this, args, () =>
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Count Files", null, (s, e) => CountFiles(args));
            menu.Items.Add("Total Size", null, (s, e) => ShowSize(args));
            return menu;
        });
    }

    void CountFiles(ClickArgs args)
    {
        int count = Directory.GetFiles(args.Context.RootPath).Length;
        MessageBox.Show($"Files: {count}");
    }

    void ShowSize(ClickArgs args)
    {
        long size = Directory.GetFiles(args.Context.RootPath)
            .Sum(f => new FileInfo(f).Length);
        MessageBox.Show($"Total: {size:N0} bytes");
    }
}

## Best Practices

1. Error Handling - Always wrap in try-catch
2. Validate Input - Check for null/empty values
3. Long Operations - Use Task.Run for async work
4. Resource Cleanup - Dispose resources properly
5. Thread Safety - Use GetFreshCopy() for COM operations

## Debugging

Enable console in settings:
Settings:
  ShowConsoleAtStartup: true

Add logging:
Console.WriteLine("Debug message");
Runtime.Log("Log message");

Attach debugger:
1. Build plugin in Debug mode
2. Start Explobar
3. Visual Studio: Debug → Attach to Process → Explobar.exe
4. Set breakpoints

## Common Issues

Plugin Not Loading:
- Use curly brackets: {path\to\plugin.dll}
- Implement ICustomButton interface
- Inherit from Button or CustomButton
- Check .NET Framework 4.7.2
- Enable console to see errors

COM RCW Separation:
- Use context.GetFreshCopy() before COM calls

Icon Not Showing:
- Verify file exists
- Check icon index is valid
- Use Icon Browser to find index

## Configuration Examples

Single class from DLL:
- Path: '{C:\Plugins\MyButtons.dll}'

Specific class:
- Path: '{C:\Plugins\MyButtons.dll,FolderContentButton}'
  Icon: 'shell32.dll,43'
  Tooltip: 'Copy folder content'

Multiple classes from same DLL:
- Path: '{C:\Plugins\MyButtons.dll,Button1}'
- Path: '{C:\Plugins\MyButtons.dll,Button2}'
- Path: '{C:\Plugins\MyButtons.dll,Button3}'

Override default icon:
- Path: '{C:\Plugins\MyPlugin.dll}'
  Icon: 'shell32.dll,100'
  Tooltip: 'Custom tooltip'

## Advanced Topics

Navigate Explorer:
CustomButton.NavigateToPath(args.Context, @"C:\Windows");

Select file:
var fresh = args.Context.GetFreshCopy();
Explorer.SelectItem(fresh.Window, filePath);

File properties:
Explorer.ShowFileProperties(filePath);

Create and select file:
var path = Path.Combine(args.Context.RootPath, "New File.txt");
File.WriteAllText(path, "Content");
Desktop.NotifyFileCreated(path);
var fresh = args.Context.GetFreshCopy();
Explorer.SelectItem(fresh.Window, path);

## Distribution

1. Build in Release mode
2. Include DLL and dependencies
3. Provide installation instructions
4. Document configuration

## Support

GitHub: https://github.com/oleg-shilo/explobar
Issues: https://github.com/oleg-shilo/explobar/issues
Sample Plugins: See CustomPlugins folder in source