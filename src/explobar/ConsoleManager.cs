using System;
using System.Runtime.InteropServices;

namespace Explobar
{
    static class ConsoleManager
    {
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AttachConsole(uint dwProcessId);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        static bool _isConsoleVisible = false;

        public static bool IsConsoleVisible => _isConsoleVisible;

        public static void Show()
        {
            if (_isConsoleVisible)
                return;

            var handle = GetConsoleWindow();

            if (handle == IntPtr.Zero)
            {
                // Allocate a new console if one doesn't exist
                Allocate();
                _isConsoleVisible = true;
            }
            else
            {
                ShowWindow(handle, SW_SHOW);
                _isConsoleVisible = true;
            }
        }

        const uint ATTACH_PARENT_PROCESS = 0x0ffffffff; // (uint)-1;

        public static void Allocate()
        {
            // Try to attach to an existing console (e.g., if launched from CMD)
            if (!AttachConsole(ATTACH_PARENT_PROCESS))
            {
                // If that fails, create a new console window
                AllocConsole();
            }
        }

        public static void AllocateVisible()
        {
            ConsoleManager.Allocate();
            _isConsoleVisible = true;
        }

        public static void AllocateHidden()
        {
            ConsoleManager.Allocate();
            _isConsoleVisible = true;
            ConsoleManager.Hide();
        }

        public static void Hide()
        {
            if (!_isConsoleVisible)
                return;

            var handle = GetConsoleWindow();
            if (handle != IntPtr.Zero)
            {
                ShowWindow(handle, SW_HIDE);
                _isConsoleVisible = false;
            }
        }

        public static void Toggle()
        {
            if (_isConsoleVisible)
                Hide();
            else
                Show();
        }
    }
}

static class ConsoleHelper
{
    [DllImport("kernel32.dll")]
    static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    static extern bool FreeConsole();

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    public static void AttachConsole()
    {
        // Prevent creating multiple consoles
        if (GetConsoleWindow() != IntPtr.Zero)
            return;

        AllocConsole();

        // Rebind standard output so Console.WriteLine works
        var stdout = Console.OpenStandardOutput();
        Console.SetOut(new System.IO.StreamWriter(stdout) { AutoFlush = true });

        var stderr = Console.OpenStandardError();
        Console.SetError(new System.IO.StreamWriter(stderr) { AutoFlush = true });
    }

    public static void DetachConsole()
    {
        FreeConsole();
    }
}