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
- Downloads from `kamori.goats.dev` (official Dalamud distribution server)
- Supports version checking and integrity verification via MD5 hashes
- Injects via `Dalamud.Injector.exe` with entrypoint mode

**Settings**:
- `EnableDalamud` - Toggle plugin support
- `DalamudInjectionDelay` - Delay in milliseconds before injection

## Notes

- The launcher targets the Taiwan FFXIV version specifically
- Game executable expected at: `{GamePath}/game/ffxiv_dx11.exe`
- Taiwan server endpoints: `neolobby01.ffxiv.com.tw`, `frontier.ffxiv.com.tw`
