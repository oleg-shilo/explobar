using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shell32;
using TsudaKageyu;

namespace Explobar
{
    static class ToolbarItems
    {
        public static List<ToolbarItem> Items =
            [
                new()
            {
                IconPath = @"C:\Program Files\Sublime Text\sublime_text.exe",
                Path = @"C:\Program Files\Sublime Text\sublime_text.exe",
                Arguments = "%f%",
                WorkingDir = "%c%"
            },
            new()
            {
                IconPath = @"C:\Program Files\Everything\Everything.exe",
                Path = @"C:\Program Files\Everything\Everything.exe",
                Arguments = @"-path %c%",
            },
            new()
            {
                IconPath = @"C:\Windows\System32\cmd.exe",
                Path = @"C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.19.11213.0_x64__8wekyb3d8bbwe\wt.exe",
                // Path = @"C:\Windows\System32\cmd.exe",
                Arguments = @"-d %c% -p ""Command Prompt""; -d %c% -p ""Windows PowerShell""",
            },
        ];
    }

    class ToolbarItem
    {
        public string IconPath;
        public string Path;
        public string Arguments;
        public string WorkingDir;
    }

    static class ToolbarExtesnions
    {
        public static void Execute(this ToolbarItem info, List<string> selectedItems)
        {
            try
            {
                if (string.IsNullOrEmpty(info.Path) || !System.IO.File.Exists(info.Path))
                    return;

                var firstItem = selectedItems.FirstOrDefault() ?? "";
                var currDir = Path.GetDirectoryName(firstItem) ?? "";

                var args = info.Arguments?
                    .Replace("%f%", $"\"{firstItem}\"")
                    .Replace("%c%", $"\"{currDir}\"")
                    ?? "";
                var workDir = info.WorkingDir?
                    .Replace("%c%", currDir)
                    ?? "";

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = info.Path,
                    Arguments = args,
                    WorkingDirectory = workDir
                };
                System.Diagnostics.Process.Start(startInfo);
            }
            catch
            {
                // Ignore errors
            }
        }

        public static Image? ExtractIcon(this string exePath)
        {
            try
            {
                if (string.IsNullOrEmpty(exePath) || !System.IO.File.Exists(exePath))
                    return null;

                using (var icon = new IconExtractor(exePath).GetIcon(0))
                // using (var icon = Icon.ExtractAssociatedIcon(exePath))
                {
                    if (icon == null)
                        return null;

                    // Convert icon to bitmap and return a copy
                    return new Bitmap(icon.ToBitmap());
                }
            }
            catch
            {
                return null;
            }
        }
    }
}