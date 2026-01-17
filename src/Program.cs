using Shell32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
// using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Explobar
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Console.WriteLine("Press...");
            // Console.ReadLine();
            while (true)
            {
                Thread.Sleep(200);
                Console.WriteLine("------------");
                var selection = GetExplorerSelection();

                if (selection.Any())
                {
                    foreach (var item in selection)
                    {
                        Console.WriteLine(item);
                    }

                    Desktop.GetCursorPos(out Desktop.POINT cursorPos);
                    Desktop.ShowSelectionForm(selection, cursorPos.X, cursorPos.Y);

                    // Wait a bit to avoid showing multiple forms
                    Thread.Sleep(2000);
                }
            }
        }



        static List<string> GetExplorerSelection()
        {
            var shell = new Shell();
            IntPtr foregroundWindow = Desktop.GetForegroundWindow();

            Desktop.GetCursorPos(out Desktop.POINT cursorPos);
            IntPtr windowUnderMouse = Desktop.WindowFromPoint(cursorPos);
            IntPtr rootWindowUnderMouse = Desktop.GetAncestor(windowUnderMouse, Desktop.GA_ROOT);

            bool isLeftShiftPressed = (Desktop.GetAsyncKeyState(Desktop.VK_LSHIFT) & 0x8000) != 0;

            var selectedPaths = new List<string>();

            foreach (dynamic window in shell.Windows())
            {
                IntPtr windowHandle = new IntPtr(window.HWND);
                bool hasFocus = windowHandle == foregroundWindow;
                bool hasMouseOver = windowHandle == rootWindowUnderMouse;

                if (!hasMouseOver)
                    continue;

                if (!isLeftShiftPressed)
                    continue;

                if (!window.FullName.EndsWith("explorer.exe", StringComparison.OrdinalIgnoreCase))
                    continue;

                dynamic folder = window.Document;
                if (folder == null)
                    continue;

                dynamic root = window.Document.Folder.Self.Path;
                Console.WriteLine($"root: {root}");

                Console.WriteLine("focused");
                foreach (FolderItem item in folder.SelectedItems())
                {
                    selectedPaths.Add(item.Path);

                }

                if (selectedPaths.Any())
                    return selectedPaths;
            }
            return selectedPaths;
        }
    }
}

