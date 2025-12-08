namespace FFXIVSimpleLauncher.Models;

public enum DalamudSourceMode
{
    /// <summary>
    /// Automatically download from yanmucorp/Dalamud releases.
    /// </summary>
    AutoDownload,

    /// <summary>
    /// Use a manually specified local path.
    /// </summary>
    LocalPath
}

public class LauncherSettings
{
    public string Username { get; set; } = string.Empty;
    public bool UseOtp { get; set; } = false;
    public bool RememberPassword { get; set; } = false;
    public string GamePath { get; set; } = string.Empty;

    // Dalamud settings
    public bool EnableDalamud { get; set; } = false;
    public int DalamudInjectionDelay { get; set; } = 0;

    /// <summary>
    /// How Dalamud should be sourced.
    /// </summary>
    public DalamudSourceMode DalamudSourceMode { get; set; } = DalamudSourceMode.AutoDownload;

    /// <summary>
    /// Local Dalamud path (only used when DalamudSourceMode is LocalPath).
    /// </summary>
    public string LocalDalamudPath { get; set; } = string.Empty;
}
