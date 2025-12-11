# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Run Commands

```bash
# Build the project
dotnet build

# Run the application
dotnet run

# Build and run
dotnet build && dotnet run
```

## Project Overview

FFXIVSimpleLauncher is a WPF desktop application (.NET 8.0) for launching Final Fantasy XIV Taiwan version. It provides a simplified login interface as an alternative to the official launcher, with optional Dalamud plugin support.

## Architecture

**Pattern**: MVVM (Model-View-ViewModel) with WPF data binding

**Key Dependencies**:
- CommunityToolkit.Mvvm - MVVM framework with source generators
- MaterialDesignThemes - UI styling
- Microsoft.Web.WebView2 - Embedded browser for web-based login flow
- Newtonsoft.Json - JSON serialization for Dalamud integration

**Project Structure**:
- `Models/` - Data classes (LauncherSettings)
- `Views/` - WPF windows and dialogs (MainWindow, SettingsWindow, OtpDialog, WebLoginWindow)
- `ViewModels/` - View models using CommunityToolkit.Mvvm (MainViewModel)
- `Services/` - Business logic (CredentialService, SettingsService, LoginService, DalamudService)
- `Dalamud/` - Dalamud integration classes (DalamudStartInfo, DalamudVersionInfo, etc.)
- `Converters/` - WPF value converters

**Data Storage**:
- Settings: `%APPDATA%/FFXIVSimpleLauncher/settings.json`
- Credentials: Windows Credential Manager via `advapi32.dll` P/Invoke
- Dalamud: `%APPDATA%/FFXIVSimpleLauncher/Dalamud/` (Hooks, Runtime, Assets, Config)

## Dalamud Integration

The launcher integrates with Dalamud plugin framework (from XIVLauncher/goatcorp):

- `DalamudService` handles downloading and updating Dalamud, .NET Runtime, and Assets
- `DotNetRuntimeManager` manages automatic .NET Runtime downloads
- Downloads from `kamori.goats.dev` (official) or `aonyx.ffxiv.wang` (CN mirror)
- Supports version checking and integrity verification via MD5 hashes
- Injects via `Dalamud.Injector.exe` with entrypoint mode

**.NET Runtime Auto-Download**:
- Automatically downloads .NET Core Runtime and Windows Desktop Runtime
- Stored in `%APPDATA%/FFXIVSimpleLauncher/Dalamud/Runtime/`
- Directory structure:
  - `Runtime/host/fxr/{version}/` - Host FXR
  - `Runtime/shared/Microsoft.NETCore.App/{version}/` - .NET Core
  - `Runtime/shared/Microsoft.WindowsDesktop.App/{version}/` - Windows Desktop
- Falls back to XIVLauncher's runtime or system .NET if available

**Settings**:
- `EnableDalamud` - Toggle plugin support
- `DalamudInjectionDelay` - Delay in milliseconds before injection

## Game Update System

The launcher includes automatic game update detection and patching:

**Version Check API**:
```
POST http://patch-gamever.ffxiv.com.tw/http/win32/ffxivtc_release_tc_game/{baseVersion}/

Request Body (TC Region format):
\n                              ← 開頭換行符 (跳過 boot version hash)
ex1\t{ex1_version}\n
ex2\t{ex2_version}\n
...

Response:
- 204 No Content = 不需要更新
- 200 OK = 回傳 multipart/mixed 補丁清單
```

**Patch List Format** (multipart/mixed body):
```
{size}\t{totalSize}\t{count}\t{parts}\t{version}\t{hashType}\t{blockSize}\t{hashes}\t{url}
```

**Key Files**:
- `Services/GameUpdateService.cs` - Version check and update coordination
- `Services/PatchListParser.cs` - Local version reading (fallback for v2.txt)
- `Services/PatchInstaller.cs` - ZiPatch application
- `Patching/ZiPatch/` - ZiPatch implementation (from XIVLauncher.Common)

**Version Files**:
- `{GamePath}/game/ffxivgame.ver` - Base game (ex0)
- `{GamePath}/game/sqpack/ex{n}/ex{n}.ver` - Expansions (ex1-ex5)

**Reference**: Based on [XIV-on-Mac-in-TC](https://github.com/PlusoneChiang/XIV-on-Mac-in-TC) and [FFXIVQuickLauncher](https://github.com/PlusoneChiang/FFXIVQuickLauncher/tree/tc_region)

## Notes

- The launcher targets the Taiwan FFXIV version specifically
- Game executable expected at: `{GamePath}/game/ffxiv_dx11.exe`
- Taiwan server endpoints: `neolobby01.ffxiv.com.tw`, `frontier.ffxiv.com.tw`
- Patch server: `patch-gamever.ffxiv.com.tw`, `patch-dl.ffxiv.com.tw`
