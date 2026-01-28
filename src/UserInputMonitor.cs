using System;
using System.Linq;
using System.Windows.Forms;

namespace Explobar
{
    class UserInputMonitor
    {
        LowLevelKeyboardHook _keyboardHook;
        Keys _configuredKey = Keys.Escape;
        bool _requireShift = true;
        bool _requireCtrl = false;
        bool _requireAlt = false;

        public event Action<Keys> OnShortcutPressed;

        public void Start()
        {
            // Load configured shortcut key
            ParseShortcutKey(ToolbarItems.Settings.ShortcutKey);

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

        void KeyboardHook_OnKeyPressed(Keys key)
        {
            // Reload configured key if config changed
            if (!ToolbarItems.IsConfigUpToDate)
            {
                ParseShortcutKey(ToolbarItems.Settings.ShortcutKey);
            }

            if (key == _configuredKey && AreModifiersPressed())
            {
                OnShortcutPressed?.Invoke(key);
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
                Runtime.Log("Empty shortcut key, using default: Escape");
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
                        Runtime.Log($"Unknown modifier '{modifier}' in shortcut key '{keyConfig}'");
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
                    Runtime.Log($"Invalid main key '{mainKeyStr}' in shortcut key '{keyConfig}', falling back to Escape");
                    _configuredKey = Keys.Escape;
                }

                var modifierStr = string.Join("+",
                    new[] { _requireShift ? "Shift" : null, _requireCtrl ? "Ctrl" : null, _requireAlt ? "Alt" : null }
                    .Where(m => m != null));
                var fullKeyStr = modifierStr.HasText() ? $"{modifierStr}+{_configuredKey}" : _configuredKey.ToString();
                Runtime.Log($"Shortcut key configured: {fullKeyStr}");
            }
            catch (Exception ex)
            {
                Runtime.Log($"Error parsing shortcut key '{keyConfig}': {ex.Message}");
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