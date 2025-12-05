using System.Text.Json.Serialization;

namespace FFXIVSimpleLauncher.Dalamud;

/// <summary>
/// Version information from Dalamud release server.
/// </summary>
public class DalamudVersionInfo
{
    [JsonPropertyName("AssemblyVersion")]
    public string? AssemblyVersion { get; set; }

    [JsonPropertyName("SupportedGameVer")]
    public string? SupportedGameVer { get; set; }

    [JsonPropertyName("RuntimeVersion")]
    public string? RuntimeVersion { get; set; }

    [JsonPropertyName("RuntimeRequired")]
    public bool RuntimeRequired { get; set; }

    [JsonPropertyName("Hash")]
    public string? Hash { get; set; }

    [JsonPropertyName("downloadUrl")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("track")]
    public string? Track { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }
}
