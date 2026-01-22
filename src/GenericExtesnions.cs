using System;
using System.Collections.Generic;
using static System.Environment;
using System.IO;
using System.Windows.Forms;

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

        public static string ExpandEnvars(this string text) => Environment.ExpandEnvironmentVariables(text);

        static Dictionary<string, string> specialFolders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "This PC", "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}" },
            { "Computer", "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}" },
            { "Network", "::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}" },
            { "Recycle Bin", "::{645FF040-5081-101B-9F08-00AA002F954E}" },
            { "Control Panel", "::{26EE0668-A00A-44D7-9371-BEB064C98683}" },
            { "Desktop", "::{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}" },
            { "Documents", "::{d3762f95-5f60-447b-8218-a2e03e28cb82}" },
            { "Downloads", "::{088e3905-0323-4b02-9826-5d99428e115f}" },
            { "Music", "::{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}" },
            { "Pictures", "::{24ad3ad4-a569-4530-98e1-ab02f9417aa8}" },
            { "Videos", "::{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}" },
            { "Recent Places", "::{22877a6d-37a1-461a-91b0-dbda5aaebc99}" },
            { "Libraries", "::{031E4825-7B94-4dc3-B131-E946B44C8DD5}" },
            { "HomeGroup", "::{B4FB3F98-C1EA-428d-A78A-D1F5659CBA93}" },
            { "User Files", "::{59031a47-3f72-44a7-89c5-5595fe6b30ee}" },
            { "Quick Access", "::{679f85cb-0220-4080-b29b-5540cc05aab6}" }
        };

        public static string GetSpecialFolderName(this string clsid)
        {
            if (clsid.HasText())
            {
                foreach (var kvp in specialFolders)
                {
                    if (kvp.Value.Equals(clsid, StringComparison.OrdinalIgnoreCase))
                        return kvp.Key;
                }
            }
            return clsid;
        }

        public static string GetSpecialFolderCLSID(this string folderName)
        {
            if (folderName.HasText())
            {
                if (specialFolders.TryGetValue(folderName, out string clsid))
                    return clsid;
            }
            return folderName;
        }
    }

    static class Runtime
    {
        public static Action<string> ShowWarning = (message) => showMessage(message, MessageBoxIcon.Warning);
        public static Action<string> ShowError = (message) => showMessage(message, MessageBoxIcon.Error);
        public static Action<string> Log = log;

        static void showMessage(string message, MessageBoxIcon icon = MessageBoxIcon.None)
            => MessageBox.Show(
                   new Form
                   {
                       TopMost = true,
                       StartPosition = FormStartPosition.CenterScreen
                   },
                   message, "Explobar", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        static void log(string message)
        {
            Console.WriteLine("[Explobar] " + message);
        }
    }
}