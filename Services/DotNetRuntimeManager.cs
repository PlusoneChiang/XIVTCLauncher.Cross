using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using FFXIVSimpleLauncher.Dalamud;

namespace FFXIVSimpleLauncher.Services;

/// <summary>
/// Manages .NET Runtime downloads for Dalamud injection.
/// Downloads both .NET Core and Windows Desktop runtimes.
/// </summary>
public class DotNetRuntimeManager
{
    // Runtime download endpoints (same as XIVLauncher)
    private const string RUNTIME_BASE_URL = "https://kamori.goats.dev/Dalamud/Release/Runtime";
    private const string DOTNET_URL_TEMPLATE = "{0}/DotNet/{1}";
    private const string DESKTOP_URL_TEMPLATE = "{0}/WindowsDesktop/{1}";

    // Alternative mirror (ottercorp for CN users)
    private const string RUNTIME_BASE_URL_CN = "https://aonyx.ffxiv.wang/Dalamud/Release/Runtime";

    // Version info endpoints
    private const string VERSION_INFO_URL = "https://kamori.goats.dev/Dalamud/Release/VersionInfo?track=release";
    private const string VERSION_INFO_URL_CN = "https://aonyx.ffxiv.wang/Dalamud/Release/VersionInfo?track=release";

    private readonly DirectoryInfo _runtimeDirectory;
    private readonly HttpClient _httpClient;
    private readonly bool _useCnMirror;

    public event Action<string>? StatusChanged;
    public event Action<double>? ProgressChanged;

    /// <summary>
    /// The required .NET runtime version for Dalamud.
    /// This is fetched from the version info API.
    /// </summary>
    public string? RequiredVersion { get; private set; }

    /// <summary>
    /// Whether runtime is required (from version info).
    /// </summary>
    public bool RuntimeRequired { get; private set; } = true;

    public DotNetRuntimeManager(DirectoryInfo runtimeDirectory, bool useCnMirror = false)
    {
        _runtimeDirectory = runtimeDirectory;
        _useCnMirror = useCnMirror;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        _httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
    }

    private string GetBaseUrl() => _useCnMirror ? RUNTIME_BASE_URL_CN : RUNTIME_BASE_URL;
    private string GetVersionInfoUrl() => _useCnMirror ? VERSION_INFO_URL_CN : VERSION_INFO_URL;

    private void ReportStatus(string status) => StatusChanged?.Invoke(status);
    private void ReportProgress(double progress) => ProgressChanged?.Invoke(progress);

    /// <summary>
    /// Fetch the required runtime version from the Dalamud version info API.
    /// </summary>
    public async Task<string?> FetchRequiredVersionAsync()
    {
        try
        {
            ReportStatus("Fetching Dalamud version info...");
            var url = GetVersionInfoUrl();
            var json = await _httpClient.GetStringAsync(url);
            var versionInfo = JsonSerializer.Deserialize<DalamudVersionInfo>(json);

            if (versionInfo != null)
            {
                RequiredVersion = versionInfo.RuntimeVersion;
                RuntimeRequired = versionInfo.RuntimeRequired;
                ReportStatus($"Required .NET Runtime: {RequiredVersion}");
                return RequiredVersion;
            }
        }
        catch (Exception ex)
        {
            ReportStatus($"Failed to fetch version info: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Check if the runtime is already installed and valid.
    /// </summary>
    public bool IsRuntimeInstalled()
    {
        if (string.IsNullOrEmpty(RequiredVersion))
            return false;

        var requiredDirs = GetRuntimeDirectories(RequiredVersion);
        return requiredDirs.All(d => d.Exists && d.GetFiles().Length > 0);
    }

    /// <summary>
    /// Check if a specific version is installed.
    /// </summary>
    public bool IsVersionInstalled(string version)
    {
        var requiredDirs = GetRuntimeDirectories(version);
        return requiredDirs.All(d => d.Exists && d.GetFiles().Length > 0);
    }

    /// <summary>
    /// Get the runtime directories for a specific version.
    /// </summary>
    private DirectoryInfo[] GetRuntimeDirectories(string version)
    {
        return new[]
        {
            new DirectoryInfo(Path.Combine(_runtimeDirectory.FullName, "host", "fxr", version)),
            new DirectoryInfo(Path.Combine(_runtimeDirectory.FullName, "shared", "Microsoft.NETCore.App", version)),
            new DirectoryInfo(Path.Combine(_runtimeDirectory.FullName, "shared", "Microsoft.WindowsDesktop.App", version))
        };
    }

    /// <summary>
    /// Ensure the .NET runtime is downloaded and ready.
    /// </summary>
    public async Task EnsureRuntimeAsync()
    {
        // First fetch the required version if not already set
        if (string.IsNullOrEmpty(RequiredVersion))
        {
            await FetchRequiredVersionAsync();
        }

        if (string.IsNullOrEmpty(RequiredVersion))
        {
            throw new Exception("Could not determine required .NET Runtime version");
        }

        if (!RuntimeRequired)
        {
            ReportStatus(".NET Runtime not required for this Dalamud version");
            return;
        }

        if (!_runtimeDirectory.Exists)
            _runtimeDirectory.Create();

        var version = RequiredVersion;
        ReportStatus($"Checking .NET Runtime {version}...");

        // Check if runtime already exists
        if (IsRuntimeInstalled())
        {
            ReportStatus($".NET Runtime {version} is already installed");
            return;
        }

        // Check version file
        var versionFile = new FileInfo(Path.Combine(_runtimeDirectory.FullName, "version"));
        var localVersion = versionFile.Exists ? await File.ReadAllTextAsync(versionFile.FullName) : null;

        if (localVersion?.Trim() == version && IsRuntimeInstalled())
        {
            ReportStatus($".NET Runtime {version} verified");
            return;
        }

        // Need to download
        ReportStatus($"Downloading .NET Runtime {version}...");
        await DownloadRuntimeAsync(version);

        // Save version file
        await File.WriteAllTextAsync(versionFile.FullName, version);
        ReportStatus($".NET Runtime {version} ready");
    }

    /// <summary>
    /// Download and extract the .NET runtime.
    /// </summary>
    private async Task DownloadRuntimeAsync(string version)
    {
        var baseUrl = GetBaseUrl();
        var tempFile = Path.GetTempFileName();

        try
        {
            // Download .NET Core runtime
            var dotnetUrl = string.Format(DOTNET_URL_TEMPLATE, baseUrl, version);
            ReportStatus($"Downloading .NET Core Runtime {version}...");
            ReportStatus($"URL: {dotnetUrl}");
            await DownloadFileAsync(dotnetUrl, tempFile);

            ReportStatus("Extracting .NET Core Runtime...");
            await ExtractRuntimeAsync(tempFile);

            // Download Windows Desktop runtime
            var desktopUrl = string.Format(DESKTOP_URL_TEMPLATE, baseUrl, version);
            ReportStatus($"Downloading Windows Desktop Runtime {version}...");
            ReportStatus($"URL: {desktopUrl}");
            await DownloadFileAsync(desktopUrl, tempFile);

            ReportStatus("Extracting Windows Desktop Runtime...");
            await ExtractRuntimeAsync(tempFile);

            // Cleanup old versions
            CleanupOldVersions(version);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Download a file with progress reporting.
    /// </summary>
    private async Task DownloadFileAsync(string url, string destinationPath)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to download from {url}: {response.StatusCode} - {content}");
        }

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var downloadedBytes = 0L;

        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

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
    /// Extract runtime ZIP to the runtime directory.
    /// </summary>
    private async Task ExtractRuntimeAsync(string zipPath)
    {
        await Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                    continue;

                var destPath = Path.Combine(_runtimeDirectory.FullName, entry.FullName);
                var destDir = Path.GetDirectoryName(destPath);

                if (!string.IsNullOrEmpty(destDir))
                    Directory.CreateDirectory(destDir);

                // Overwrite existing files
                entry.ExtractToFile(destPath, overwrite: true);
            }
        });
    }

    /// <summary>
    /// Clean up old runtime versions.
    /// </summary>
    private void CleanupOldVersions(string currentVersion)
    {
        try
        {
            // Clean old fxr versions
            var fxrDir = new DirectoryInfo(Path.Combine(_runtimeDirectory.FullName, "host", "fxr"));
            if (fxrDir.Exists)
            {
                foreach (var dir in fxrDir.GetDirectories())
                {
                    if (dir.Name != currentVersion)
                    {
                        try { dir.Delete(true); } catch { }
                    }
                }
            }

            // Clean old NETCore.App versions
            var netcoreDir = new DirectoryInfo(Path.Combine(_runtimeDirectory.FullName, "shared", "Microsoft.NETCore.App"));
            if (netcoreDir.Exists)
            {
                foreach (var dir in netcoreDir.GetDirectories())
                {
                    if (dir.Name != currentVersion)
                    {
                        try { dir.Delete(true); } catch { }
                    }
                }
            }

            // Clean old WindowsDesktop.App versions
            var desktopDir = new DirectoryInfo(Path.Combine(_runtimeDirectory.FullName, "shared", "Microsoft.WindowsDesktop.App"));
            if (desktopDir.Exists)
            {
                foreach (var dir in desktopDir.GetDirectories())
                {
                    if (dir.Name != currentVersion)
                    {
                        try { dir.Delete(true); } catch { }
                    }
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Get the full path to the runtime directory if it's valid.
    /// </summary>
    public string? GetRuntimePath()
    {
        if (IsRuntimeInstalled())
            return _runtimeDirectory.FullName;

        return null;
    }

    /// <summary>
    /// Force re-download of the runtime.
    /// </summary>
    public async Task ForceUpdateAsync()
    {
        // Delete version file to force re-download
        var versionFile = new FileInfo(Path.Combine(_runtimeDirectory.FullName, "version"));
        if (versionFile.Exists)
            versionFile.Delete();

        // Fetch latest version
        await FetchRequiredVersionAsync();

        if (!string.IsNullOrEmpty(RequiredVersion))
        {
            // Delete existing runtime directories
            var requiredDirs = GetRuntimeDirectories(RequiredVersion);
            foreach (var dir in requiredDirs)
            {
                if (dir.Exists)
                {
                    try { dir.Delete(true); } catch { }
                }
            }
        }

        await EnsureRuntimeAsync();
    }
}
