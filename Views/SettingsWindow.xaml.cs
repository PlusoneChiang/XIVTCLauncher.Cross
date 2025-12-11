using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using FFXIVSimpleLauncher.Models;
using FFXIVSimpleLauncher.Services;
using Microsoft.Win32;

namespace FFXIVSimpleLauncher.Views;

public partial class SettingsWindow : Window
{
    public LauncherSettings Settings { get; private set; }
    private readonly OtpService _otpService;
    private readonly bool _isFirstRun;

    public SettingsWindow(LauncherSettings settings, bool isFirstRun = false)
    {
        InitializeComponent();
        _isFirstRun = isFirstRun;

        Settings = new LauncherSettings
        {
            Username = settings.Username,
            UseOtp = settings.UseOtp,
            RememberPassword = settings.RememberPassword,
            GamePath = settings.GamePath,
            EnableDalamud = settings.EnableDalamud,
            DalamudInjectionDelay = settings.DalamudInjectionDelay,
            DalamudSourceMode = settings.DalamudSourceMode,
            LocalDalamudPath = settings.LocalDalamudPath,
            AutoOtp = settings.AutoOtp
        };

        // Initialize OTP service
        _otpService = new OtpService();
        _otpService.OtpCodeChanged += OnOtpCodeChanged;
        _otpService.SecondsRemainingChanged += OnSecondsRemainingChanged;
        _otpService.Initialize();

        // Load settings into UI
        GamePathTextBox.Text = Settings.GamePath;
        EnableDalamudCheckBox.IsChecked = Settings.EnableDalamud;
        InjectionDelayTextBox.Text = Settings.DalamudInjectionDelay.ToString();
        LocalDalamudPathTextBox.Text = Settings.LocalDalamudPath;

        // Set Dalamud source mode
        AutoDownloadRadio.IsChecked = Settings.DalamudSourceMode == DalamudSourceMode.AutoDownload;
        LocalPathRadio.IsChecked = Settings.DalamudSourceMode == DalamudSourceMode.LocalPath;

        // Set OTP settings
        AutoOtpCheckBox.IsChecked = Settings.AutoOtp;
        UpdateOtpDisplay();

        // 首次使用模式：修改標題和提示
        if (_isFirstRun)
        {
            Title = "首次設定 - XIV TC Launcher";
            TitleText.Text = "首次設定";
            FirstRunCard.Visibility = Visibility.Visible;
            SaveButton.Content = "開始使用";
            CancelButton.Visibility = Visibility.Collapsed;
        }
    }

    private void OnOtpCodeChanged(string code)
    {
        Dispatcher.Invoke(() =>
        {
            CurrentOtpText.Text = string.IsNullOrEmpty(code) ? "------" : code;
        });
    }

    private void OnSecondsRemainingChanged(int seconds)
    {
        Dispatcher.Invoke(() =>
        {
            OtpCountdownText.Text = $"({seconds}s)";
        });
    }

    private void UpdateOtpDisplay()
    {
        if (_otpService.IsConfigured)
        {
            OtpSecretTextBox.Text = "********（已設定）";
            OtpSecretTextBox.IsReadOnly = true;
            CurrentOtpText.Text = _otpService.CurrentCode;
            OtpCountdownText.Text = $"({_otpService.SecondsRemaining}s)";
        }
        else
        {
            OtpSecretTextBox.Text = "";
            OtpSecretTextBox.IsReadOnly = false;
            CurrentOtpText.Text = "------";
            OtpCountdownText.Text = "(--s)";
        }
    }

    private void SaveOtpSecret_Click(object sender, RoutedEventArgs e)
    {
        if (_otpService.IsConfigured)
        {
            MessageBox.Show("OTP 密鑰已設定。如需更換，請先清除現有密鑰。", "已設定", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var secret = OtpSecretTextBox.Text.Trim();
        if (string.IsNullOrEmpty(secret))
        {
            MessageBox.Show("請輸入 OTP 密鑰。", "需要密鑰", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_otpService.SetSecret(secret))
        {
            MessageBox.Show("OTP 密鑰已儲存！現在會自動產生 OTP 驗證碼。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateOtpDisplay();
        }
        else
        {
            MessageBox.Show("無效的 OTP 密鑰格式。請確認輸入的是 Base32 編碼的密鑰。", "格式錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ClearOtpSecret_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "確定要清除 OTP 密鑰嗎？\n\n清除後需要重新輸入密鑰才能使用自動 OTP 功能。",
            "確認清除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _otpService.ClearSecret();
            UpdateOtpDisplay();
            MessageBox.Show("OTP 密鑰已清除。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "選擇 FFXIV 安裝資料夾"
        };

        if (dialog.ShowDialog() == true)
        {
            GamePathTextBox.Text = dialog.FolderName;
        }
    }

    private void InjectionDelayTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Only allow digits
        e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
    }

    private void BrowseLocalDalamud_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "選擇本地 Dalamud 資料夾"
        };

        if (dialog.ShowDialog() == true)
        {
            var path = dialog.FolderName;
            var injectorPath = System.IO.Path.Combine(path, "Dalamud.Injector.exe");

            if (System.IO.File.Exists(injectorPath))
            {
                LocalDalamudPathTextBox.Text = path;
                Settings.LocalDalamudPath = path;
            }
            else
            {
                MessageBox.Show(
                    "在選擇的資料夾中找不到 Dalamud.Injector.exe\n\n請選擇包含有效 Dalamud 建置的資料夾。",
                    "無效的 Dalamud 路徑",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(GamePathTextBox.Text))
        {
            MessageBox.Show("請選擇遊戲路徑。", "路徑無效", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var gamePath = GamePathTextBox.Text;
        var exePath = System.IO.Path.Combine(gamePath, "game", "ffxiv_dx11.exe");

        if (!System.IO.File.Exists(exePath))
        {
            var result = MessageBox.Show(
                $"在以下位置找不到 ffxiv_dx11.exe：\n{exePath}\n\n確定這是正確的路徑嗎？",
                "警告",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        Settings.GamePath = gamePath;
        Settings.EnableDalamud = EnableDalamudCheckBox.IsChecked ?? false;
        Settings.DalamudInjectionDelay = int.TryParse(InjectionDelayTextBox.Text, out var delay) ? delay : 0;
        Settings.DalamudSourceMode = AutoDownloadRadio.IsChecked == true
            ? DalamudSourceMode.AutoDownload
            : DalamudSourceMode.LocalPath;
        Settings.LocalDalamudPath = LocalDalamudPathTextBox.Text;
        Settings.AutoOtp = AutoOtpCheckBox.IsChecked ?? false;

        // Validate local Dalamud path if using local path mode
        if (Settings.EnableDalamud && Settings.DalamudSourceMode == DalamudSourceMode.LocalPath)
        {
            if (string.IsNullOrWhiteSpace(Settings.LocalDalamudPath))
            {
                MessageBox.Show(
                    "請選擇本地 Dalamud 資料夾。",
                    "需要 Dalamud 路徑",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var injectorPath = System.IO.Path.Combine(Settings.LocalDalamudPath, "Dalamud.Injector.exe");
            if (!System.IO.File.Exists(injectorPath))
            {
                MessageBox.Show(
                    "選擇的本地 Dalamud 資料夾中沒有 Dalamud.Injector.exe。",
                    "無效的本地 Dalamud",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
