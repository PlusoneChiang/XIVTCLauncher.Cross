namespace FFXIVSimpleLauncher.Models;

public class LauncherSettings
{
    public string Username { get; set; } = string.Empty;
    public bool UseOtp { get; set; } = false;
    public bool RememberPassword { get; set; } = false;
    public string GamePath { get; set; } = string.Empty;

    // Dalamud settings
    public bool EnableDalamud { get; set; } = false;
    public int DalamudInjectionDelay { get; set; } = 0;

    // Local Dalamud path (Taiwan version requires local build with custom signatures)
    public string LocalDalamudPath { get; set; } = string.Empty;
}
