using System.Text.Json.Serialization;

namespace FFXIVSimpleLauncher.Dalamud;

public class AssetInfo
{
    [JsonPropertyName("Version")]
    public int Version { get; set; }

    [JsonPropertyName("PackageUrl")]
    public string? PackageUrl { get; set; }

    [JsonPropertyName("Assets")]
    public List<AssetEntry>? Assets { get; set; }
}

public class AssetEntry
{
    [JsonPropertyName("Url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("FileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("Hash")]
    public string? Hash { get; set; }
}
