# PocketLaunch

A minimalist Windows tray app for launching your favorite folders and applications — no more digging through Explorer or the Start Menu.

Give each shortcut a friendly name and a short description, pin the ones you use daily, and pull up the list from the system tray whenever you need it.

## Features

- 📌 **Tray-first** — lives in the system tray, opens with a click, hides on close instead of exiting
- 📁 **Folders and apps** — add shortcuts to either, each with a custom name and description
- 🎨 **Auto icons** — shortcut icons are pulled straight from Windows Explorer/the app itself
- 📱 **Compact popup** — small, phone-sized window instead of a full desktop app
- 💾 **Backup & restore** — export your list to a JSON file and import it on another PC
- 🪶 **Lightweight** — plain JSON storage, no database, no background services

## Screenshots

_(add a screenshot of the tray popup here)_

## Getting started

### Requirements

- Windows 10/11 (x64)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) — only if you use the framework-dependent build (see below)

### Run from source

```bash
git clone https://github.com/f0nziePT/PocketLaunch.git
cd PocketLaunch
dotnet run
```

### Build a distributable

Two flavors, depending on whether you want a single dependency-free file or a smaller one:

**Self-contained** (bundles the .NET runtime, ~166 MB, no install required on the target machine):

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

**Framework-dependent** (~1 MB, requires the .NET 10 Desktop Runtime already installed):

```bash
dotnet publish -c Release -p:RuntimeIdentifier=win-x64 -p:SelfContained=false -p:PublishSingleFile=true -o publish-slim
```

Either way, the result is a single `PocketLaunch.exe` you can copy anywhere.

## Usage

- **Left-click** the tray icon to open/close the popup
- **Right-click** the tray icon for **Open** / **Close**
- **+** button to add a folder or application shortcut
- **×** on a shortcut card to remove it
- **↑ / ↓** buttons in the header to import/export a backup of your list

## Where your data lives

Shortcuts are stored as plain JSON at:

```
%AppData%\PocketLaunch\shortcuts.json
```

Use the export/import buttons to move your list between computers.

## Project structure

```
PocketLaunch/
  App.xaml(.cs)              Application startup, tray icon setup
  MainWindow.xaml(.cs)       Main popup window and shortcut list
  Models/                    Shortcut data model
  Services/                  JSON persistence, backup, shell icon extraction
  Views/                     Add-shortcut dialog
  Assets/                    App icon
```

## License

[MIT](LICENSE)
