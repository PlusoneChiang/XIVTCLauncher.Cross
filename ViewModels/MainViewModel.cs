using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFXIVSimpleLauncher.Models;
using FFXIVSimpleLauncher.Services;
using FFXIVSimpleLauncher.Views;

namespace FFXIVSimpleLauncher.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly LoginService _loginService;
    private readonly DalamudService _dalamudService;
    private readonly CredentialService _credentialService;
    private LauncherSettings _settings;

    [ObservableProperty]
    private bool _isLoggingIn;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public MainViewModel()
    {
        _settingsService = new SettingsService();
        _loginService = new LoginService();
        _dalamudService = new DalamudService();
        _credentialService = new CredentialService();
        _settings = _settingsService.Load();

        // Subscribe to Dalamud status updates
        _dalamudService.StatusChanged += status => StatusMessage = status;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.GamePath))
        {
            StatusMessage = "Please set game path in settings first";
            return;
        }

        // If Dalamud is enabled, ensure it's ready before login
        if (_settings.EnableDalamud)
        {
            try
            {
                // Configure Dalamud source mode
                _dalamudService.SourceMode = _settings.DalamudSourceMode;
                _dalamudService.LocalDalamudPath = _settings.LocalDalamudPath;

                StatusMessage = _settings.DalamudSourceMode == DalamudSourceMode.AutoDownload
                    ? "準備 Dalamud..."
                    : "載入本地 Dalamud...";
                await _dalamudService.EnsureDalamudAsync();
            }
            catch (Exception ex)
            {
                var result = MessageBox.Show(
                    $"Failed to prepare Dalamud: {ex.Message}\n\nDo you want to launch without Dalamud?",
                    "Dalamud Error",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    StatusMessage = "Launch cancelled";
                    return;
                }

                // Disable Dalamud for this launch
                _settings.EnableDalamud = false;
            }
        }

        // Load saved credentials
        string? savedEmail = _settings.Username;
        string? savedPassword = null;
        if (!string.IsNullOrEmpty(savedEmail) && _settings.RememberPassword)
        {
            savedPassword = _credentialService.GetPassword(savedEmail);
        }

        // Open WebView2 login window with saved credentials
        var webLoginWindow = new WebLoginWindow(_settings.GamePath, savedEmail, savedPassword);
        var dialogResult = webLoginWindow.ShowDialog();

        if (dialogResult == true && !string.IsNullOrEmpty(webLoginWindow.SessionId))
        {
            // Save credentials if user chose to remember
            if (!string.IsNullOrEmpty(webLoginWindow.LastEmail))
            {
                _settings.Username = webLoginWindow.LastEmail;
                _settings.RememberPassword = true;
                _credentialService.SavePassword(webLoginWindow.LastEmail, webLoginWindow.LastPassword ?? "");
                _settingsService.Save(_settings);
            }
            else if (webLoginWindow.LastEmail == null && !string.IsNullOrEmpty(_settings.Username))
            {
                // User unchecked remember me, clear saved password
                _credentialService.DeletePassword(_settings.Username);
                _settings.RememberPassword = false;
                _settingsService.Save(_settings);
            }

            StatusMessage = "Login successful! Launching game...";

            try
            {
                if (_settings.EnableDalamud && _dalamudService.State == DalamudService.DalamudState.Ready)
                {
                    LaunchGameWithDalamud(webLoginWindow.SessionId);
                    // Don't close launcher immediately - let user see injection status
                    StatusMessage += "\n\nYou can close this launcher now.";
                }
                else
                {
                    _loginService.LaunchGame(_settings.GamePath, webLoginWindow.SessionId);
                    // Close the launcher after launching the game (no Dalamud)
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to launch game: {ex.Message}";
            }
        }
        else
        {
            StatusMessage = "Login cancelled";
        }
    }

    private void LaunchGameWithDalamud(string sessionId)
    {
        var gameExePath = System.IO.Path.Combine(_settings.GamePath, "game", "ffxiv_dx11.exe");
        var gameVersion = _loginService.GetGameVersion(_settings.GamePath);

        // Build game arguments (Taiwan version)
        var gameArgs = string.Join(" ",
            "DEV.LobbyHost01=neolobby01.ffxiv.com.tw",
            "DEV.LobbyPort01=54994",
            "DEV.GMServerHost=frontier.ffxiv.com.tw",
            $"DEV.TestSID={sessionId}",
            "SYS.resetConfig=0",
            "DEV.SaveDataBankHost=config-dl.ffxiv.com.tw"
        );

        // Check if game version matches exactly
        var supportedVersion = _dalamudService.GetSupportedGameVersion();
        if (supportedVersion != null && supportedVersion != gameVersion)
        {
            var result = MessageBox.Show(
                $"台版遊戲版本與 Dalamud 不完全匹配。\n\n" +
                $"遊戲版本: {gameVersion}\n" +
                $"Dalamud 支持: {supportedVersion}\n\n" +
                $"這可能導致 Dalamud 無法正常工作或遊戲崩潰。\n" +
                $"建議：如果遊戲崩潰，請關閉 Dalamud 功能。\n\n" +
                $"是否繼續使用 Dalamud？",
                "版本不匹配警告",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                // Fall back to normal launch
                _loginService.LaunchGame(_settings.GamePath, sessionId);
                return;
            }
        }

        _dalamudService.LaunchGameWithDalamud(
            gameExePath,
            gameArgs,
            gameVersion,
            _settings.DalamudInjectionDelay);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var settingsWindow = new SettingsWindow(_settings);
        if (settingsWindow.ShowDialog() == true)
        {
            _settings = settingsWindow.Settings;
            _settingsService.Save(_settings);
            StatusMessage = "Settings saved";
        }
    }

    [RelayCommand]
    private async Task TestInjectAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.GamePath))
        {
            StatusMessage = "Please set game path in settings first";
            return;
        }

        if (!_settings.EnableDalamud)
        {
            StatusMessage = "Dalamud is not enabled. Enable it in Settings first.";
            return;
        }

        try
        {
            // Configure Dalamud source mode
            _dalamudService.SourceMode = _settings.DalamudSourceMode;
            _dalamudService.LocalDalamudPath = _settings.LocalDalamudPath;

            StatusMessage = _settings.DalamudSourceMode == DalamudSourceMode.AutoDownload
                ? "準備 Dalamud..."
                : "載入本地 Dalamud...";
            await _dalamudService.EnsureDalamudAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"準備 Dalamud 失敗: {ex.Message}";
            return;
        }

        StatusMessage = "以測試 Session 啟動遊戲 (會在大廳斷線)...";

        try
        {
            // Use a fake session ID - game will launch but disconnect at lobby
            var fakeSessionId = "TEST_SESSION_FOR_DALAMUD_INJECT";
            LaunchGameWithDalamud(fakeSessionId);
            StatusMessage = "Game launched with Dalamud!\n\nNote: Using fake session - you will be disconnected at lobby.\nThis is for testing Dalamud injection only.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to launch game: {ex.Message}";
        }
    }
}
