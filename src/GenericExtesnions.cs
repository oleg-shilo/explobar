using System;
using System.IO;
using System.Windows.Forms;
using static System.Environment;

namespace Explobar
{
    static class GenericExtesnions
    {
        public static bool IsEmpty(this string text) => string.IsNullOrEmpty(text);
        public static string IfEmpty(this string text, string alternative) => string.IsNullOrEmpty(text) ? alternative : text;

        public static bool HasText(this string text) => !string.IsNullOrEmpty(text);

        public static string GetFileName(this string path) => Path.GetFileName(path);
        public static string GetPath(this SpecialFolder folder) => Environment.GetFolderPath(folder);

        public static string Combine(this SpecialFolder folder, string path, params string[] paths)
            => Path.Combine(Environment.GetFolderPath(folder), path, Path.Combine(paths));
    }

    static class Runtime
    {

        public static Action<string> ShowWarning = (message) => showMessage(message, MessageBoxIcon.Warning);
        public static Action<string> Log = log;

        static void showMessage(string message, MessageBoxIcon icon = MessageBoxIcon.None) => MessageBox.Show(message, "Explobar", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        static void log(string message)
        {
            Console.WriteLine("[Explobar] " + message);
        }
    }
}