using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Explobar
{
    class UserInputMonitor
    {
        static UserInputMonitor _inputMonitor;

        public static void StartMonitor(Action<Keys> handler)
        {
            _inputMonitor = new UserInputMonitor();
            _inputMonitor.OnShortcutPressed += handler;
            _inputMonitor.Start();
        }

        public static void StopMonitor() => _inputMonitor?.Stop();

        LowLevelKeyboardHook _keyboardHook;
        Keys _configuredKey = Keys.Escape;
        bool _requireShift = true;
        bool _requireCtrl = false;
        bool _requireAlt = false;

        // Toolbar item shortcuts
        Dictionary<string, ToolbarItem> _shortcuts = new Dictionary<string, ToolbarItem>();

        public event Action<Keys> OnShortcutPressed;

        public void Start()
        {
            // Load configured shortcut key
            ParseShortcutKey(ToolbarItems.Settings.ShortcutKey);

            // Load toolbar item shortcuts
            LoadToolbarShortcuts();

            // Set up keyboard hook
            _keyboardHook = new LowLevelKeyboardHook();
            _keyboardHook.OnKeyPressed += KeyboardHook_OnKeyPressed;
            _keyboardHook.HookKeyboard();
        }

        public void Stop()
        {
            _keyboardHook?.UnhookKeyboard();
            _keyboardHook = null;
        }

        void LoadToolbarShortcuts()
        {
            _shortcuts.Clear();

            foreach (var item in ToolbarItems.Items)
            {
                if (!string.IsNullOrWhiteSpace(item.Shortcut))
                {
                    var key = NormalizeShortcut(item.Shortcut);
                    if (!_shortcuts.ContainsKey(key))
                    {
                        _shortcuts[key] = item;
                        Runtime.Output($"Registered shortcut: {item.Shortcut} for {item.Path}");
                    }
                    else
                    {
                        Runtime.Output($"Warning: Duplicate shortcut '{item.Shortcut}' ignored for {item.Path}");
                    }
                }
            }
        }

        string NormalizeShortcut(string shortcut)
        {
            // Parse and normalize the shortcut to a consistent format
            var parts = shortcut.Split('+').Select(p => p.Trim()).ToArray();
            var modifiers = new List<string>();
            var mainKey = "";

            foreach (var part in parts)
            {
                if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                    modifiers.Add("Shift");
                else if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                         part.Equals("Control", StringComparison.OrdinalIgnoreCase))
                    modifiers.Add("Ctrl");
                else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                    modifiers.Add("Alt");
                else
                    mainKey = part;
            }

            // Sort modifiers for consistent key
            modifiers.Sort();

            if (modifiers.Count > 0)
                return string.Join("+", modifiers) + "+" + mainKey;
            else
                return mainKey;
        }

        string GetCurrentShortcut(Keys key)
        {
            var modifiers = new List<string>();

            if ((Desktop.GetAsyncKeyState(Desktop.VK_SHIFT) & 0x8000) != 0)
                modifiers.Add("Shift");
            if ((Desktop.GetAsyncKeyState(Desktop.VK_CONTROL) & 0x8000) != 0)
                modifiers.Add("Ctrl");
            if ((Desktop.GetAsyncKeyState(Desktop.VK_MENU) & 0x8000) != 0)
                modifiers.Add("Alt");

            modifiers.Sort();

            if (modifiers.Count > 0)
                return string.Join("+", modifiers) + "+" + key.ToString();
            else
                return key.ToString();
        }

        void KeyboardHook_OnKeyPressed(Keys key)
        {
            // Reload configured key if config changed
            if (!ConfigManager.IsConfigUpToDate)
            {
                ParseShortcutKey(ToolbarItems.Settings.ShortcutKey);
                LoadToolbarShortcuts();
            }

            // If toolbar is visible and user presses Escape, hide it
            if (key == Keys.Escape && ToolbarForm.Instance != null && ToolbarForm.Instance.Visible)
            {
                ToolbarForm.Instance.BeginInvoke((Action)(() => ToolbarForm.Instance.HideToolbar()));
                return;
            }

            // Check for main shortcut
            if (key == _configuredKey && AreModifiersPressed())
            {
                OnShortcutPressed?.Invoke(key);
                return;
            }

            // Check for toolbar item shortcuts
            var currentShortcut = GetCurrentShortcut(key);
            // Runtime.Output($"Checking shortcut: {currentShortcut}");

            if (_shortcuts.TryGetValue(currentShortcut, out ToolbarItem item))
            {
                Runtime.Output($"Shortcut triggered: {currentShortcut}");

                // Execute on a separate thread
                ApartmentState.STA.Run(() =>
                {
                    try
                    {
                        Profiler.Log($"Processing: {currentShortcut}");

                        ExplorerContext context = null;

                        // Only get Explorer context if not system-wide
                        if (!item.SystemWide)
                        {
                            (var root, var selection, var window) = Explorer.GetSelection();
                            if (root != null)
                            {
                                context = new ExplorerContext(root, selection, window);
                            }
                            else
                            {
                                Runtime.Output($"No Explorer window found for shortcut: {currentShortcut}");
                                return;
                            }
                        }
                        else
                        {
                            // Create empty context for system-wide shortcuts
                            context = new ExplorerContext();
                        }

                        ExecuteToolbarItem(item, context);
                        Profiler.Log($"Processed: {currentShortcut}");
                    }
                    catch (Exception ex)
                    {
                        Runtime.Output($"Error executing shortcut: {ex.Message}");
                    }
                    finally
                    {
                        Profiler.Reset();
                    }
                });
            }
        }

        void ExecuteToolbarItem(ToolbarItem item, ExplorerContext context)
        {
            try
            {
                // Handle stock buttons
                if (item.Path.StartsWith("{") && item.Path.EndsWith("}"))
                {
                    if (StockToolbarControls.Items.TryGetValue(item.Path, out var factory))
                    {
                        var button = factory();
                        if (button is ICustomButton customButton)
                        {
                            customButton.OnClick(new ClickArgs { Context = context });
                        }
                    }
                }
                else
                {
                    item.Execute(context);
                }
            }
            catch (Exception ex)
            {
                Runtime.Output($"Error executing toolbar item: {ex.Message}");
            }
        }

        void ParseShortcutKey(string keyConfig)
        {
            // Reset modifiers
            _requireShift = false;
            _requireCtrl = false;
            _requireAlt = false;
            _configuredKey = Keys.Escape;

            if (string.IsNullOrWhiteSpace(keyConfig))
            {
                Runtime.Output("Empty shortcut key, using default: Escape");
                return;
            }

            try
            {
                // Split by + to get modifiers and key
                var parts = keyConfig.Split('+').Select(p => p.Trim()).ToArray();

                // Last part is the main key, everything before are modifiers
                var mainKeyStr = parts[parts.Length - 1];
                var modifiers = parts.Take(parts.Length - 1).ToArray();

                // Parse modifiers
                foreach (var modifier in modifiers)
                {
                    if (modifier.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                        _requireShift = true;
                    else if (modifier.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                             modifier.Equals("Control", StringComparison.OrdinalIgnoreCase))
                        _requireCtrl = true;
                    else if (modifier.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                        _requireAlt = true;
                    else
                        Runtime.Output($"Unknown modifier '{modifier}' in shortcut key '{keyConfig}'");
                }

                // Parse main key
                if (Enum.TryParse<Keys>(mainKeyStr, true, out Keys result))
                {
                    _configuredKey = result;
                }
                else if (Enum.TryParse<Keys>(mainKeyStr + "Key", true, out result))
                {
                    _configuredKey = result;
                }
                else
                {
                    Runtime.Output($"Invalid main key '{mainKeyStr}' in shortcut key '{keyConfig}', falling back to Escape");
                    _configuredKey = Keys.Escape;
                }

                var modifierStr = string.Join("+",
                    new[] { _requireShift ? "Shift" : null, _requireCtrl ? "Ctrl" : null, _requireAlt ? "Alt" : null }
                    .Where(m => m != null));
                var fullKeyStr = modifierStr.HasText() ? $"{modifierStr}+{_configuredKey}" : _configuredKey.ToString();
                Runtime.Output($"Shortcut key configured: {fullKeyStr}");
            }
            catch (Exception ex)
            {
                Runtime.Output($"Error parsing shortcut key '{keyConfig}': {ex.Message}");
                _configuredKey = Keys.Escape;
                _requireShift = false;
                _requireCtrl = false;
                _requireAlt = false;
            }
        }

        bool AreModifiersPressed()
        {
            bool shiftPressed = (Desktop.GetAsyncKeyState(Desktop.VK_SHIFT) & 0x8000) != 0;
            bool ctrlPressed = (Desktop.GetAsyncKeyState(Desktop.VK_CONTROL) & 0x8000) != 0;
            bool altPressed = (Desktop.GetAsyncKeyState(Desktop.VK_MENU) & 0x8000) != 0;

            // Check if required modifiers match exactly
            if (_requireShift != shiftPressed)
                return false;
            if (_requireCtrl != ctrlPressed)
                return false;
            if (_requireAlt != altPressed)
                return false;

            return true;
        }
    }
}