using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Shell32;

namespace Explobar
{
    public static class ExplorerHistory
    {
        static string HistoryFilePath
            => Environment.SpecialFolder.LocalApplicationData.Combine("Explobar", "explorer-history.txt");

        static List<string> _history = null;
        static object _lock = new object();
        static Thread _monitorThread;
        static HashSet<string> _lastKnownPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        static bool _stopMonitoring = false;

        static List<string> History
        {
            get
            {
                // lock (_lock)
                {
                    if (_history == null)
                        LoadHistory();
                    return _history;
                }
            }
        }

        public static void StartMonitor()
        {
            if (_monitorThread != null)
                return;

            _stopMonitoring = false;

            _monitorThread = new Thread(() =>
            {
                try
                {
                    while (!_stopMonitoring)
                    {
                        try { ScanExplorerWindows(); }
                        catch { }

                        // Sleep in small chunks to allow quick exit
                        for (int i = 0; i < 30 && !_stopMonitoring; i++)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }
                catch { }
            });
            _monitorThread.SetApartmentState(ApartmentState.STA);
            _monitorThread.IsBackground = true; // Keep this!
            _monitorThread.Start();
            Runtime.Output("Explorer history monitoring started");
        }

        public static void StopMonitor()
        {
            _stopMonitoring = true; // Signal stop instead of Abort
            _monitorThread = null;
            Runtime.Output("Explorer history monitoring stopped");
        }

        static void ScanExplorerWindows()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var currentPaths = GetAllExplorerPaths();
                var newPaths = currentPaths.Except(_lastKnownPaths).ToList();

                foreach (var path in newPaths)
                {
                    AddLocation(path, silent: true);
                }

                _lastKnownPaths = currentPaths;

                if (newPaths.Any())
                    Runtime.Output($"Found {newPaths.Count} new Explorer location(s)");
            }
            catch (Exception ex)
            {
                Runtime.Output($"Error scanning explorer windows: {ex.Message}");
            }
            finally
            {
                sw.Stop();
                Runtime.Output($"Explorer scan completed in {sw.ElapsedMilliseconds} ms");
            }
        }

        static HashSet<string> GetAllExplorerPaths()
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Shell shell = null;

            try
            {
                shell = new Shell();
                foreach (dynamic window in shell.Windows())
                {
                    try
                    {
                        if (!window.FullName.EndsWith("explorer.exe", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (window.Document == null)
                            continue;

                        string path = window.Document.Folder?.Self?.Path?.ToString();
                        if (!string.IsNullOrWhiteSpace(path))
                            paths.Add(path);
                    }
                    catch
                    {
                        // Skip windows that throw errors
                    }
                }
            }
            catch (Exception ex)
            {
                Runtime.Output($"Error enumerating explorer windows: {ex.Message}");
            }
            finally
            {
                if (shell != null)
                    Marshal.ReleaseComObject(shell);
            }

            return paths;
        }

        public static void AddLocation(string path, bool silent = false)
        {
            if (path.IsEmpty() || !path.DirExists())
                return;

            lock (_lock)
            {
                // Remove if already exists to move it to the front
                History.Remove(path);

                // Add to the beginning
                History.Insert(0, path);

                // Trim to max size
                int maxSize = ToolbarItems.Settings.HistorySize;
                if (History.Count > maxSize)
                    History.RemoveRange(maxSize, History.Count - maxSize);

                SaveHistory();

                if (!silent)
                    Runtime.Output($"Added to history: {path}");
            }
        }

        public static List<string> GetRecentLocations(int count = -1)
        {
            // lock (_lock)
            {
                if (count <= 0)
                    return new List<string>(History);

                return History.Take(count).ToList();
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                History.Clear();
                SaveHistory();
            }
        }

        static void LoadHistory()
        {
            _history = new List<string>();

            try
            {
                if (File.Exists(HistoryFilePath))
                {
                    var lines = File.ReadAllLines(HistoryFilePath);
                    _history = lines
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .Distinct()
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Runtime.Output($"Error loading history: {ex.Message}");
            }
        }

        static void SaveHistory()
        {
            try
            {
                HistoryFilePath.EnsureFileDir();

                File.WriteAllLines(HistoryFilePath, _history);
            }
            catch (Exception ex)
            {
                Runtime.Output($"Error saving history: {ex.Message}");
            }
        }
    }
}