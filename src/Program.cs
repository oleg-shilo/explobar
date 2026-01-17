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
                (var root, var selection) = UserInputListener.GetExplorerSelection();

                if (root != null)
                {
                    foreach (var item in selection)
                    {
                        Console.WriteLine(item);
                    }

                    Desktop.GetCursorPos(out Desktop.POINT cursorPos);
                    Desktop.ShowSelectionForm(root, selection, cursorPos.X, cursorPos.Y);

                    // Wait a bit to avoid showing multiple forms
                    Thread.Sleep(2000);
                }
            }
        }
    }
}

