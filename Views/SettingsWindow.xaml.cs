using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using FFXIVSimpleLauncher.Models;
using Microsoft.Win32;

namespace FFXIVSimpleLauncher.Views;

public partial class SettingsWindow : Window
{
    public LauncherSettings Settings { get; private set; }

    public SettingsWindow(LauncherSettings settings)
    {
        InitializeComponent();
        Settings = new LauncherSettings
        {
            Username = settings.Username,
            UseOtp = settings.UseOtp,
            RememberPassword = settings.RememberPassword,
            GamePath = settings.GamePath,
            EnableDalamud = settings.EnableDalamud,
            DalamudInjectionDelay = settings.DalamudInjectionDelay,
            LocalDalamudPath = settings.LocalDalamudPath
        };

        GamePathTextBox.Text = Settings.GamePath;
        EnableDalamudCheckBox.IsChecked = Settings.EnableDalamud;
        InjectionDelayTextBox.Text = Settings.DalamudInjectionDelay.ToString();
        LocalDalamudPathTextBox.Text = Settings.LocalDalamudPath;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select FFXIV Installation Folder"
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
            Title = "Select Local Dalamud Folder"
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
                    "Dalamud.Injector.exe not found in the selected folder.\n\nPlease select a folder containing a valid Dalamud build.",
                    "Invalid Dalamud Path",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(GamePathTextBox.Text))
        {
            MessageBox.Show("Please select a game path.", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var gamePath = GamePathTextBox.Text;
        var exePath = System.IO.Path.Combine(gamePath, "game", "ffxiv_dx11.exe");

        if (!System.IO.File.Exists(exePath))
        {
            var result = MessageBox.Show(
                $"ffxiv_dx11.exe not found at:\n{exePath}\n\nAre you sure this is the correct path?",
                "Warning",
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
        Settings.LocalDalamudPath = LocalDalamudPathTextBox.Text;

        // Validate local Dalamud path if Dalamud is enabled
        if (Settings.EnableDalamud)
        {
            if (string.IsNullOrWhiteSpace(Settings.LocalDalamudPath))
            {
                MessageBox.Show(
                    "Please select a local Dalamud folder.\n\n台版需要自行編譯的 Dalamud 才能使用。",
                    "Local Dalamud Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var injectorPath = System.IO.Path.Combine(Settings.LocalDalamudPath, "Dalamud.Injector.exe");
            if (!System.IO.File.Exists(injectorPath))
            {
                MessageBox.Show(
                    "The selected local Dalamud folder does not contain Dalamud.Injector.exe.",
                    "Invalid Local Dalamud",
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
