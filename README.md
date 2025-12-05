# XIVTCLauncher (FFXIVSimpleLauncher)

[English](README.md) | [繁體中文](README_zh-TW.md)

A faster launcher for Final Fantasy XIV Taiwan version, inspired by [FFXIVQuickLauncher](https://github.com/goatcorp/FFXIVQuickLauncher).

## Features

- **Fast Login** - Streamlined login process with saved credentials
- **OTP Support** - One-Time Password authentication support
- **Web Login** - Integrated WebView2 browser for web-based authentication
- **Dalamud Integration** - Optional plugin framework support for enhanced gameplay
- **Modern UI** - Clean Material Design interface
- **Settings Management** - Customizable game path and launch options

## Requirements

- Windows 10/11
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)
- Final Fantasy XIV Taiwan version installed

## Installation

1. Download the latest release
2. Extract to a folder of your choice
3. Run `FFXIVSimpleLauncher.exe`
4. Configure your game path in Settings

## Building from Source

```bash
# Clone the repository
git clone https://github.com/your-repo/XIVTCLauncher.git
cd XIVTCLauncher

# Build the project
dotnet build

# Run the application
dotnet run
```

## Configuration

Settings are stored in `%APPDATA%/FFXIVSimpleLauncher/settings.json`

### Game Path

Default installation path for FFXIV Taiwan version:

```
C:\Program Files\USERJOY GAMES\FINAL FANTASY XIV TC
```

### Dalamud Plugin Support

The launcher supports [Dalamud](https://github.com/goatcorp/Dalamud) plugin injection with Taiwan/Chinese client compatibility.

#### Recommended Dalamud Fork

For Taiwan/Chinese clients, we recommend using [yanmucorp/Dalamud](https://github.com/yanmucorp/Dalamud):

- **Dalamud CN**: [yanmucorp/Dalamud](https://github.com/yanmucorp/Dalamud) - Maintained fork with Chinese client support
- **FFXIVClientStructs**: Uses [Dalamud-DailyRoutines/FFXIVClientStructs](https://github.com/Dalamud-DailyRoutines/FFXIVClientStructs) as submodule

#### Building Dalamud from Source

```bash
# Clone with submodules
git clone --recursive https://github.com/yanmucorp/Dalamud.git

# Or if already cloned, initialize submodules
git submodule update --init --recursive

# Build
dotnet build -c Release
```

#### Usage

1. Build Dalamud from source (see above) or download from [yanmucorp releases](https://github.com/yanmucorp/Dalamud/releases)
2. In Settings, set the **Local Dalamud Path** to your Dalamud build directory (e.g., `E:\FFXIV\Dalamud\bin\Release`)
3. Enable Dalamud in Settings
4. Configure injection delay if needed (default works for most users)
5. Launch the game - Assets will be automatically downloaded from ottercorp, then Dalamud will be injected

> **Note**: Assets are downloaded from [ottercorp](https://aonyx.ffxiv.wang) which is compatible with yanmucorp Dalamud. The Dalamud build must be provided locally.

## Disclaimer

XIVTCLauncher is not in-line with the game's Terms of Service. We are doing our best to make it safe to use for everyone, and to our knowledge, no one has gotten into trouble for using XIVTCLauncher, but please be aware that it is a possibility.

**Use at your own risk.**

### Plugin Guidelines

If you use Dalamud plugins, please follow these guidelines:

- Do not use plugins that automate gameplay in ways that provide unfair advantages
- Do not use plugins that interact with the game servers in unauthorized ways
- Do not use plugins that circumvent any game restrictions or paywalls

## Acknowledgments

- [goatcorp/FFXIVQuickLauncher](https://github.com/goatcorp/FFXIVQuickLauncher) - Original inspiration and Dalamud framework
- [goatcorp/Dalamud](https://github.com/goatcorp/Dalamud) - Plugin framework
- [yanmucorp/Dalamud](https://github.com/yanmucorp/Dalamud) - Chinese client Dalamud fork
- [ottercorp/FFXIVQuickLauncher](https://github.com/ottercorp/FFXIVQuickLauncher) - Chinese client launcher and asset server
- [Dalamud-DailyRoutines/FFXIVClientStructs](https://github.com/Dalamud-DailyRoutines/FFXIVClientStructs) - Chinese client structures
- [MaterialDesignInXAML](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) - UI toolkit

## Legal

XIVTCLauncher is not affiliated with or endorsed by SQUARE ENIX CO., LTD. or Gameflier International Corp.

FINAL FANTASY is a registered trademark of Square Enix Holdings Co., Ltd.

All game assets and trademarks are the property of their respective owners.

## License

This project is provided as-is for educational and personal use.
