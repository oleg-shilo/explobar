using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using static System.Environment;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;

namespace Explobar
{
    static class GenericExtesnions
    {
        public static bool IsEmpty(this string text) => string.IsNullOrEmpty(text);

        public static string IfEmpty(this string text, string alternative) => string.IsNullOrEmpty(text) ? alternative : text;

        public static bool HasText(this string text) => !string.IsNullOrEmpty(text);

        public static bool SameAsEither(this string text, params string[] patterns)
        {
            if (text.IsEmpty())
                return false;

            foreach (var p in patterns)
                if (p.HasText())
                {
                    if (p.Equals(text, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            return false;
        }

        public static bool EndsWithEither(this string text, params string[] patterns)
        {
            if (text.IsEmpty())
                return false;

            foreach (var p in patterns)
                if (p.HasText())
                {
                    if (text.EndsWith(p, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            return false;
        }

        public static string GetFileNameWithoutExtension(this string path) => Path.GetFileNameWithoutExtension(path);

        public static string GetFileName(this string path) => Path.GetFileName(path);

        public static string GetDirName(this string path) => Path.GetDirectoryName(path);

        public static string GetPath(this SpecialFolder folder) => Environment.GetFolderPath(folder);

        public static string Combine(this string folder, string path, params string[] paths)
            => Path.Combine(folder, path, Path.Combine(paths));

        public static string Combine(this SpecialFolder folder, string path, params string[] paths)
            => Path.Combine(Environment.GetFolderPath(folder), path, Path.Combine(paths));

        public static Process GetProcess(this string id)
        {
            try
            {
                var pid = int.Parse(id);
                return Process.GetProcessById(pid);
            }
            catch
            {
                return null;
            }
        }

        public static Control NetxInFocusChain(this Control control, bool forward)
        {
            var controls = control.Controls.Cast<Control>().ToList();
            if (forward)
                return controls.SkipWhile(c => c.Focused).Skip(1).FirstOrDefault()
                                ?? controls.FirstOrDefault();
            else
                return controls.TakeWhile(c => !c.Focused).LastOrDefault()
                                ?? controls.LastOrDefault();
        }

        public static void InUIThread(this Control control, Action action) => control.Invoke(action);

        public static void Run(this ApartmentState state, Action action)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                finally
                {
                }
            });
            thread.SetApartmentState(state);
            thread.Start();
        }

        public static string NextAvailableName(this string folder, string desiredName)
        {
            string fullPath = Path.Combine(folder, desiredName);

            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                return Path.Combine(folder, desiredName);

            // Separate name and extension
            string extension = Path.GetExtension(desiredName);
            string nameWithoutExt = Path.GetFileNameWithoutExtension(desiredName);

            // Start with (2) and increment until we find an available name
            int counter = 2;

            while (true)
            {
                string newName;

                if (extension.HasText())
                {
                    // It's a file: "filename (n).ext"
                    newName = $"{nameWithoutExt} ({counter}){extension}";
                }
                else
                {
                    // It's a folder: "foldername (n)"
                    newName = $"{desiredName} ({counter})";
                }

                fullPath = Path.Combine(folder, newName);

                if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                    return Path.Combine(folder, newName);

                counter++;

                // Safety limit to prevent infinite loops
                if (counter > 9999)
                    return Path.Combine(folder, $"{nameWithoutExt}_{Guid.NewGuid()}{extension}");
            }
        }

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

        public static string EnsureDir(this string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public static void DeleteIfExists(this string path)
        {
            if (File.Exists(path))
                File.Delete(path);
            if (Directory.Exists(path))
                Directory.Delete(path);
        }

        public static string EnsureFileDir(this string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            return path;
        }

        public static void CreateShortcut(this string targetPath, string shortcutPath)
        {
            // Using IWshRuntimeLibrary (add reference to COM: Windows Script Host Object Model)
            // Or use simpler approach with shell32:

            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell = Activator.CreateInstance(shellType);
            var shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
            shortcut.Description = "Explobar - Windows Explorer Toolbar";
            shortcut.Save();

            System.Runtime.InteropServices.Marshal.ReleaseComObject(shortcut);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
        }

        public static bool DirExists(this string path)
        {
            try
            {
                return Directory.Exists(path);
            }
            catch
            {
                return false;
            }
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

        public static FileSystemWatcher WatchForChanges(this string path, FileSystemEventHandler onChange)
        {
            try
            {
                var watcher = new FileSystemWatcher
                {
                    Path = path.GetDirName(),
                    Filter = path.GetFileName(),
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                    EnableRaisingEvents = true
                };
                watcher.Changed += onChange;
                watcher.Created += onChange;
                watcher.Deleted += onChange;
                Runtime.Output($"File watcher initialized for: {path}");
                return watcher;
            }
            catch (Exception ex)
            {
                Runtime.Output($"Failed to initialize watcher for plugin {path}: {ex.Message}");
            }
            return null;
        }
    }

    static class Profiler
    {
        static void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        static Stopwatch _sw = null;

        static Stopwatch sw
        {
            get
            {
                if (_sw == null)
                {
                    _sw = Stopwatch.StartNew();
                    prevCall = 0;
                    Profiler.WriteLine("> Profiler start");
                }
                return _sw;
            }
        }

        static long prevCall = 0;

        public static void Reset()
        {
            _sw = null;
        }

        public static void Start([CallerMemberName] string source = null)
        {
            Reset();
            var dummy = sw; // force creation of _sw
        }

        public static void Log(string context = null, [CallerMemberName] string source = null)
        {
            Profiler.WriteLine($">   {source} ({(context ?? "...")}): since-prev: {sw.ElapsedMilliseconds - prevCall}, since-start: {sw.ElapsedMilliseconds}");
            prevCall = sw.ElapsedMilliseconds;
        }
    }

    public static class Runtime
    {
        public static Action<string> ShowInfo = (message) => ShowMessageBox(message, MessageBoxIcon.Information);
        public static Action<string> ShowWarning = (message) => ShowMessageBox(message, MessageBoxIcon.Warning);
        public static Func<string, bool> UserDecision = (message) => ShowMessageBox(message, MessageBoxIcon.Warning, MessageBoxButtons.OKCancel) == DialogResult.OK;
        public static Func<string, bool> UserDecisionYesNo = (message) => ShowMessageBox(message, MessageBoxIcon.Question, MessageBoxButtons.YesNo) == DialogResult.Yes;
        public static Action<string> ShowError = (message) => ShowMessageBox(message, MessageBoxIcon.Error);
        public static Action<string> Output = output;
        public static Action<string> Log = log;

        static Icon _icon;
        static Bitmap _logo;
        static readonly object messageBoxLock = new object();
        static bool isMessageBoxShowing = false;

        const string MESSAGE_BOX_MARKER = "Explobar_MessageBox_Marker_7F8E9A2B";

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public static bool AnyOtherExplobarMessageBoxShowing()
        {
            try
            {
                // Search for a window with our distinctive title
                IntPtr hWnd = FindWindow(null, MESSAGE_BOX_MARKER);

                // If found and it's not zero, another Explobar instance has a message box showing
                return hWnd != IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Output($"Error checking for message box window: {ex.Message}");
                return false; // Assume not showing on error
            }
        }

        public static Icon AppIcon
        {
            get
            {
                if (_icon == null)
                {
                    var image = (Bitmap)System.Reflection.Assembly.GetExecutingAssembly().Location.ExtractIcon(0, 48);
                    _icon = Icon.FromHandle(image.GetHicon());
                }
                return _icon;
            }
        }

        public static Bitmap AppLogo
        {
            get
            {
                if (_logo == null)
                {
                    _logo = (Bitmap)System.Reflection.Assembly.GetExecutingAssembly().Location.ExtractIcon(0, 256);
                }
                return _logo;
            }
        }

        static DialogResult ShowMessageBox(string message, MessageBoxIcon icon = MessageBoxIcon.None, MessageBoxButtons buttons = MessageBoxButtons.OK)
        {
            // Prevent multiple message boxes from showing simultaneously
            lock (messageBoxLock)
            {
                if (isMessageBoxShowing)
                {
                    // A message box is already showing in this process - output and return default result
                    Output($"[MessageBox blocked - same process] Another message box is already showing. Message: {message}");

                    // Return safe default based on button type
                    return buttons == MessageBoxButtons.OKCancel ? DialogResult.Cancel : DialogResult.OK;
                }

                // Check if another Explobar instance is showing a message box
                if (AnyOtherExplobarMessageBoxShowing())
                {
                    Output($"[MessageBox blocked - other process] Another Explobar instance is showing a message box. Message: {message}");
                    return buttons == MessageBoxButtons.OKCancel ? DialogResult.Cancel : DialogResult.OK;
                }

                try
                {
                    isMessageBoxShowing = true;

                    // Create a hidden topmost owner form with distinctive title
                    using (var topMostForm = new Form())
                    {
                        topMostForm.Text = MESSAGE_BOX_MARKER; // Distinctive title for detection
                        topMostForm.TopMost = true;
                        topMostForm.StartPosition = FormStartPosition.Manual;
                        topMostForm.Location = new Point(-32000, -32000); // Far off-screen
                        topMostForm.Size = new Size(1, 1);
                        topMostForm.ShowInTaskbar = false;
                        topMostForm.FormBorderStyle = FormBorderStyle.None;
                        topMostForm.Opacity = 0; // Make it invisible
                        topMostForm.Show();

                        // Small delay to ensure window is registered with the system
                        Application.DoEvents();

                        return MessageBox.Show(
                            topMostForm,
                            message,
                            "Explobar",
                            buttons,
                            icon);
                    }
                }
                finally
                {
                    isMessageBoxShowing = false;
                }
            }
        }

        static void output(string message)
        {
            Console.WriteLine(message);
        }

        static void log(string message)
        {
            Console.WriteLine(message);
            ClearLogIfTooLarge();
            File.AppendAllText(LogFilePath, $"{DateTime.Now.ToString("s")}:  {message}{Environment.NewLine}");
        }

        static void ClearLogIfTooLarge()
        {
            var info = new FileInfo(LogFilePath);
            if (info.Exists && info.Length > 1 * 1024 * 1024) // 1 MB
            {
                try
                {
                    File.Copy(LogFilePath, LogFilePath + ".bk", true);
                    File.WriteAllText(LogFilePath, $"[Output cleared at {DateTime.Now} due to size limit]\n");
                }
                catch
                {
                    // Ignore errors when trying to clear output
                }
            }
        }

        public static string LogFilePath = SpecialFolder.LocalApplicationData.Combine("Explobar", "output.txt");
    }

    static class SingleInstanceApp
    {
        static Mutex _singleInstanceMutex;

        public static bool AnotherInstanceDetected()
        {
            bool createdNew;
            _singleInstanceMutex = new Mutex(true, "Global\\Explobar_SingleInstance", out createdNew);
            return !createdNew;
        }

        public static void Clear()
        {
            try
            {
                _singleInstanceMutex?.ReleaseMutex();
            }
            catch { }
            _singleInstanceMutex?.Dispose();
        }
    }
}