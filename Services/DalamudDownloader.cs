using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using SevenZipExtractor;

namespace FFXIVSimpleLauncher.Services;

/// <summary>
/// Downloads and manages Dalamud from yanmucorp/Dalamud releases.
/// </summary>
public class DalamudDownloader
{
    // yanmucorp/Dalamud release API
    private const string RELEASES_API_URL = "https://api.github.com/repos/yanmucorp/Dalamud/releases/latest";
    private const string RELEASES_URL = "https://github.com/yanmucorp/Dalamud/releases";

    private readonly DirectoryInfo _dalamudDirectory;
    private readonly HttpClient _httpClient;

    public event Action<string>? StatusChanged;
    public event Action<double>? ProgressChanged;

    /// <summary>
    /// Current installed version tag.
    /// </summary>
    public string? InstalledVersion { get; private set; }

    /// <summary>
    /// Latest available version tag.
    /// </summary>
    public string? LatestVersion { get; private set; }

    public DalamudDownloader(DirectoryInfo dalamudDirectory)
    {
        _dalamudDirectory = dalamudDirectory;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        _httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("XIVTCLauncher/1.0");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
    }

    private void ReportStatus(string status) => StatusChanged?.Invoke(status);
    private void ReportProgress(double progress) => ProgressChanged?.Invoke(progress);

    /// <summary>
    /// Get the Dalamud installation directory.
    /// </summary>
    public string GetDalamudPath() => _dalamudDirectory.FullName;

    /// <summary>
    /// Check if Dalamud is installed and valid.
    /// </summary>
    public bool IsDalamudInstalled()
    {
        if (!_dalamudDirectory.Exists)
            return false;

        var requiredFiles = new[]
        {
            "Dalamud.Injector.exe",
            "Dalamud.dll",
            "FFXIVClientStructs.dll"
        };

        return requiredFiles.All(f => File.Exists(Path.Combine(_dalamudDirectory.FullName, f)));
    }

    /// <summary>
    /// Load installed version from version file.
    /// </summary>
    public void LoadInstalledVersion()
    {
        var versionFile = Path.Combine(_dalamudDirectory.FullName, "version.txt");
        if (File.Exists(versionFile))
        {
            InstalledVersion = File.ReadAllText(versionFile).Trim();
        }
    }

    /// <summary>
    /// Fetch the latest release info from GitHub.
    /// </summary>
    public async Task<GitHubRelease?> FetchLatestReleaseAsync()
    {
        try
        {
            ReportStatus("Checking for Dalamud updates...");
            var json = await _httpClient.GetStringAsync(RELEASES_API_URL);
            var release = JsonSerializer.Deserialize<GitHubRelease>(json);

            if (release != null)
            {
                LatestVersion = release.TagName;
                ReportStatus($"Latest Dalamud version: {LatestVersion}");
            }

            return release;
        }
        catch (Exception ex)
        {
            ReportStatus($"Failed to check for updates: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Check if an update is available.
    /// </summary>
    public async Task<bool> IsUpdateAvailableAsync()
    {
        LoadInstalledVersion();
        var release = await FetchLatestReleaseAsync();

        if (release == null || string.IsNullOrEmpty(LatestVersion))
            return false;

        if (string.IsNullOrEmpty(InstalledVersion))
            return true;

        return InstalledVersion != LatestVersion;
    }

    /// <summary>
    /// Ensure Dalamud is downloaded and up-to-date.
    /// </summary>
    public async Task EnsureDalamudAsync()
    {
        LoadInstalledVersion();

        // Check if already installed and up-to-date
        if (IsDalamudInstalled() && !string.IsNullOrEmpty(InstalledVersion))
        {
            var release = await FetchLatestReleaseAsync();
            if (release != null && InstalledVersion == release.TagName)
            {
                ReportStatus($"Dalamud {InstalledVersion} is up-to-date");
                return;
            }
        }

        // Need to download
        await DownloadAndInstallAsync();
    }

    /// <summary>
    /// Download and install the latest Dalamud release.
    /// </summary>
    public async Task DownloadAndInstallAsync()
    {
        var release = await FetchLatestReleaseAsync();
        if (release == null)
        {
            throw new Exception("Failed to fetch release information from GitHub");
        }

        // Find latest.7z asset
        var asset = release.Assets?.FirstOrDefault(a =>
            a.Name?.Equals("latest.7z", StringComparison.OrdinalIgnoreCase) == true);

        if (asset == null || string.IsNullOrEmpty(asset.BrowserDownloadUrl))
        {
            throw new Exception("Could not find latest.7z in release assets");
        }

        ReportStatus($"Downloading Dalamud {release.TagName}...");

        // Create temp file for download
        var tempFile = Path.Combine(Path.GetTempPath(), $"dalamud-{Guid.NewGuid()}.7z");

        try
        {
            // Download the 7z file
            await DownloadFileAsync(asset.BrowserDownloadUrl, tempFile);

            // Clean existing installation
            if (_dalamudDirectory.Exists)
            {
                ReportStatus("Removing old Dalamud installation...");
                try
                {
                    _dalamudDirectory.Delete(true);
                }
                catch (Exception ex)
                {
                    ReportStatus($"Warning: Could not fully clean directory: {ex.Message}");
                }
            }
            _dalamudDirectory.Create();

            // Extract 7z file
            ReportStatus("Extracting Dalamud...");
            await ExtractArchiveAsync(tempFile, _dalamudDirectory.FullName);

            // Save version file
            var versionFile = Path.Combine(_dalamudDirectory.FullName, "version.txt");
            await File.WriteAllTextAsync(versionFile, release.TagName ?? "unknown");
            InstalledVersion = release.TagName;

            ReportStatus($"Dalamud {release.TagName} installed successfully");
        }
        finally
        {
            // Cleanup temp file
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { }
            }
        }
    }

    /// <summary>
    /// Force re-download Dalamud.
    /// </summary>
    public async Task ForceUpdateAsync()
    {
        InstalledVersion = null;
        var versionFile = Path.Combine(_dalamudDirectory.FullName, "version.txt");
        if (File.Exists(versionFile))
        {
            File.Delete(versionFile);
        }

        await DownloadAndInstallAsync();
    }

    /// <summary>
    /// Download a file with progress reporting.
    /// </summary>
    private async Task DownloadFileAsync(string url, string destinationPath)
    {
        ReportStatus($"Downloading from: {url}");

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to download: {response.StatusCode}");
        }

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var downloadedBytes = 0L;

        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

        var buffer = new byte[81920];
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead);
            downloadedBytes += bytesRead;

            if (totalBytes > 0)
            {
                ReportProgress((double)downloadedBytes / totalBytes * 100);
            }
        }
    }

    /// <summary>
    /// Extract 7z archive using SevenZipExtractor.
    /// </summary>
    private async Task ExtractArchiveAsync(string archivePath, string destinationPath)
    {
        await Task.Run(() =>
        {
            ReportStatus("Opening archive...");
            using var archive = new ArchiveFile(archivePath);

            var entries = archive.Entries.Where(e => !e.IsFolder).ToList();
            var totalEntries = entries.Count;
            var extractedEntries = 0;

            ReportStatus($"Extracting {totalEntries} files...");

            foreach (var entry in entries)
            {
                var destPath = Path.Combine(destinationPath, entry.FileName);
                var destDir = Path.GetDirectoryName(destPath);

                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                entry.Extract(destPath);

                extractedEntries++;

                // Report progress every 10 files to avoid too many updates
                if (extractedEntries % 10 == 0 || extractedEntries == totalEntries)
                {
                    ReportProgress((double)extractedEntries / totalEntries * 100);
                    ReportStatus($"Extracting... ({extractedEntries}/{totalEntries})");
                }
            }

            ReportStatus("Extraction complete");
        });
    }
}

/// <summary>
/// GitHub release API response model.
/// </summary>
public class GitHubRelease
{
    [System.Text.Json.Serialization.JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("published_at")]
    public DateTime? PublishedAt { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("assets")]
    public List<GitHubAsset>? Assets { get; set; }
}

/// <summary>
/// GitHub release asset model.
/// </summary>
public class GitHubAsset
{
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("size")]
    public long Size { get; set; }
}
