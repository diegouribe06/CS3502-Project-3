# OwlTech File Manager

A cross-platform desktop file manager built with [Avalonia UI](https://avaloniaui.net/) and .NET 10.

## Features

- **Tree view** – Browse your file system in a resizable tree panel that lazy-loads children as you expand folders.
- **File operations** – Copy, cut, paste, add file, add folder, rename, move, and delete files and folders via toolbar buttons or right-click context menus.
- **Search/filter** – Type in the search box to filter the tree in real time.
- **File information panel** – Select any item to see its name, extension, path, type, last modified date, creation date, and permissions.
- **File viewer** – Open a file to view or edit its text content in a separate window.
- **Permissions display** – Read/Write/Execute flags are shown for every item using platform-native APIs (ACL on Windows, `stat` on Unix).
- **Configurable startup folder** – The app remembers the last root folder you opened; first-run defaults to the bundled `ExamplesRoot` directory.
- **Light/dark mode toggle** – Switch between Fluent light and dark themes at any time.
- **Persistent tree state** – Expand/collapse and selection state are preserved across folder refreshes.

## Prerequisites

| Requirement | Version |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 or later |

The application targets `net10.0` and uses Avalonia 12.0.0. No additional runtime installation is needed beyond the .NET SDK; all NuGet packages are restored automatically.

## Getting the source

```bash
git clone https://github.com/diegouribe06/CS3502-Project-3.git
cd CS3502-Project-3
```

## Building

```bash
dotnet build AvaloniaApplication1.sln
```

A `Debug` build is produced by default. To create an optimised `Release` build:

```bash
dotnet build AvaloniaApplication1.sln -c Release
```

## Running

```bash
dotnet run --project AvaloniaApplication1/AvaloniaApplication1.csproj
```

Or, after building, run the output binary directly:

```bash
# Debug (default)
./AvaloniaApplication1/bin/Debug/net10.0/AvaloniaApplication1

# Release
./AvaloniaApplication1/bin/Release/net10.0/AvaloniaApplication1
```

> **Windows users:** the binary is named `AvaloniaApplication1.exe`.

## Publishing a self-contained executable

Use `dotnet publish` to produce a single, deployable output folder. Replace `<RID>` with your target [runtime identifier](https://learn.microsoft.com/dotnet/core/rid-catalog) (e.g. `win-x64`, `linux-x64`, `osx-arm64`):

```bash
dotnet publish AvaloniaApplication1/AvaloniaApplication1.csproj \
    -c Release \
    -r <RID> \
    --self-contained true \
    -o ./publish
```

The runnable binary is placed in `./publish/`.

## Project structure

```
CS3502-Project-3/
├── AvaloniaApplication1.sln          # Solution file
└── AvaloniaApplication1/
    ├── Models/                        # Data models (ListableItem, StartupConfiguration, …)
    ├── Views/                         # Avalonia XAML views and code-behind
    ├── Services/                      # Business logic (file operations, tree state, config, …)
    ├── Providers/Permissions/         # Cross-platform permission providers
    ├── ExamplesRoot/                  # Sample directory tree bundled with the app
    ├── App.axaml / App.axaml.cs       # Application entry point and styling
    └── Program.cs                     # Host builder
```

## Configuration

On first launch the app creates a JSON config file at:

| Platform | Path |
|---|---|
| Windows | `%APPDATA%\AvaloniaApplication1\startup-config.json` |
| Linux / macOS | `~/.config/AvaloniaApplication1/startup-config.json` |

The file stores the initial root folder that is loaded on startup. You can also change it at any time from within the app using the **Change Startup Folder** button.

## Dependencies

| Package | Version |
|---|---|
| Avalonia | 12.0.0 |
| Avalonia.Desktop | 12.0.0 |
| Avalonia.Themes.Fluent | 12.0.0 |
| Avalonia.Fonts.Inter | 12.0.0 |
| System.IO.FileSystem.AccessControl | 5.0.0 |
| AvaloniaUI.DiagnosticsSupport *(debug only)* | 2.2.0 |
