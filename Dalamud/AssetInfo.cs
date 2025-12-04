using System.Text.Json.Serialization;

namespace FFXIVSimpleLauncher.Dalamud;

public class AssetInfo
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("packageUrl")]
    public string? PackageUrl { get; set; }

    [JsonPropertyName("assets")]
    public List<AssetEntry>? Assets { get; set; }
}

public class AssetEntry
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("hash")]
    public string? Hash { get; set; }
}
