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

namespace Explobar
{
    class UserInputListener
    {
        public static (string root, List<string> selected) GetExplorerSelection()
        {
            var shell = new Shell();
            IntPtr foregroundWindow = Desktop.GetForegroundWindow();

            Desktop.GetCursorPos(out Desktop.POINT cursorPos);
            IntPtr windowUnderMouse = Desktop.WindowFromPoint(cursorPos);
            IntPtr rootWindowUnderMouse = Desktop.GetAncestor(windowUnderMouse, Desktop.GA_ROOT);

            bool isLeftShiftPressed = (Desktop.GetAsyncKeyState(Desktop.VK_LSHIFT) & 0x8000) != 0;

            var selectedPaths = new List<string>();
            string root = null;

            foreach (dynamic window in shell.Windows())
            {
                IntPtr windowHandle = new IntPtr(window.HWND);
                bool hasFocus = windowHandle == foregroundWindow;
                bool hasMouseOver = windowHandle == rootWindowUnderMouse;

                if (!hasFocus)
                    continue;

                if (!hasMouseOver)
                    continue;

                if (!isLeftShiftPressed)
                    continue;

                if (!window.FullName.EndsWith("explorer.exe", StringComparison.OrdinalIgnoreCase))
                    continue;

                dynamic folder = window.Document;
                if (folder == null)
                    continue;

                root = window.Document.Folder.Self.Path.ToString();

                foreach (FolderItem item in folder.SelectedItems())
                {
                    selectedPaths.Add(item.Path);
                }

                break;
            }
            return (root, selectedPaths);
        }
    }
}