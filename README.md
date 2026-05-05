# DayZ Helper

A modern, lightweight Windows utility designed to streamline your DayZ experience. Launch the game, manage favorites, and keep your installation clean.

![DayZ Helper Icon](DayZHelper/app.ico)

## Features

### 🚀 Fast Launching
- **One-Click Start:** Launch DayZ directly via Steam with optimized parameters.
- **Direct Connect:** Join servers instantly by entering an IP and Port.
- **DZSA Integration:** Full support for `dzsal://` protocol links, allowing you to use it as a companion to other launchers.

### 🧹 Maintenance & Cleanup
- **Log Purge:** Automatically scan and delete stale `.log`, `.rpt`, and `.mdmp` files that clutter your drive.
- **Workshop Cleanup:** Scans both the game folder and Steam Workshop directories to reclaim disk space.
- **Cleanup Logs:** Keeps a history of what was deleted and how much space was saved.

### 🌐 Server Management
- **Last Server Detection:** Automatically pulls the last server you played on from your DayZ configuration.
- **Live Pinging:** Real-time latency checks for your favorite servers.
- **Favorites List:** Save your most-played servers for quick access.

### 🎨 Modern Interface
- **Theming:** Supports Light, Dark, and System themes.
- **Custom Accents:** Personalize the UI with a variety of accent colors.
- **Visual Effects:** Utilizes modern Windows effects like Mica and Acrylic for a native feel.
- **Custom Title Bar:** A sleek, integrated title bar that respects your chosen theme.

### 🛠️ Smart Tools
- **Launch Monitor:** Optionally watches for the game to start and closes itself to save resources, or alerts you if Steam fails to hand off the launch.
- **Path Resolver:** Automatically detects Steam and DayZ installation paths.

## Tech Stack

- **Framework:** .NET 10.0 (WPF)
- **Language:** C#
- **Styling:** Custom XAML templates with modern Windows aesthetics.

## Installation / Building

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10/11 (for modern UI effects)

### Build from Source
1. Clone the repository.
2. Open `DayZHelper.sln` in Visual Studio 2022 or JetBrains Rider.
3. Restore NuGet packages.
4. Build and run the `DayZHelper` project.

## Usage

1. **Start DayZ:** Click the "Start DayZ" button to launch the game via Steam.
2. **Clean Up:** Use "Clean Up Files" to remove temporary game logs and crash dumps.
3. **Favorites:** Click the star icon next to the "Last Server" to save it to your favorites list.
4. **Settings:** Use the hamburger menu to configure paths or re-register the `dzsal://` protocol.

## License

This project is licensed under the MIT License - see the LICENSE file for details (or add one).

---
*Note: DayZ Helper is not affiliated with Bohemia Interactive or Valve Corporation.*
