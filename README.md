# Explobar

Windows Explorer toolbar extension with keyboard shortcuts.

## Features
- Quick Access Toolbar (Shift+Escape)
- Stock Buttons (new file, folder, tab, etc.)
- Custom Commands with placeholders
- Global Shortcuts
- Plugin Support (.NET assemblies)
- Favorites & Applications
- History Tracking
- Icon Browser

## Installation
1. Download from GitHub Releases
2. Extract and run Explobar.exe
3. Application runs in system tray

## Quick Start
1. Launch Explobar (runs in tray)
2. Open Windows Explorer
3. Press Shift+Escape
4. Click buttons or press Escape to hide

## Configuration
File: %LocalAppData%\Explobar\toolbar-items.yaml

Settings:
  ButtonSize: 24
  HistorySize: 10
  ShortcutKey: 'Shift+Escape'
  ShowConsoleAtStartup: false

## Stock Buttons
{new-tab} - New Explorer tab
{new-file} - New text file
{new-folder} - New folder
{from-clipboard} - Navigate from clipboard
{recent} - Recent folders
{favorites} - Favorite folders
{application} - Favorite apps
{props} - File properties
{separator} - Visual separator
{app-config} - Configuration menu

## Plugin Support
See PLUGINS.md for plugin development guide.

Format: {C:\Path\To\Plugin.dll,ClassName}

## Links
GitHub: https://github.com/oleg-shilo/explobar
Author: Oleg Shilo

