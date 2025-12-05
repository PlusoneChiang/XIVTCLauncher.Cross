using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FFXIVSimpleLauncher.Dalamud;

namespace FFXIVSimpleLauncher.Services;

/// <summary>
/// Dalamud service for local Dalamud builds with official asset support.
/// Downloads assets from goatcorp server, uses local Dalamud build.
/// </summary>
public class DalamudService
{
    // Ottercorp (CN) asset server - compatible with yanmucorp Dalamud
    private const string ASSET_URL = "https://aonyx.ffxiv.wang/Dalamud/Asset/Meta";

    private readonly DirectoryInfo _configDirectory;
    private readonly DirectoryInfo _runtimeDirectory;
    private readonly DirectoryInfo _assetDirectory;
    private readonly DotNetRuntimeManager _runtimeManager;

    private FileInfo? _runner;
    private DirectoryInfo? _currentAssetDirectory;

    public enum DalamudState
    {
        NotReady,
        Checking,
        Downloading,
        DownloadingRuntime,
        Ready,
        Failed
    }

    public DalamudState State { get; private set; } = DalamudState.NotReady;
    public string? ErrorMessage { get; private set; }
    public event Action<string>? StatusChanged;
    public event Action<double>? ProgressChanged;

    /// <summary>
    /// Path to local Dalamud build directory
    /// </summary>
    public string? LocalDalamudPath { get; set; }

    /// <summary>
    /// Whether to use CN mirror for downloads (faster for CN/TW users)
    /// </summary>
    public bool UseCnMirror { get; set; } = true;

    /// <summary>
    /// Required .NET Runtime version for Dalamud (fetched from server)
    /// </summary>
    public string? RuntimeVersion => _runtimeManager.RequiredVersion;

    public DalamudService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var baseDir = Path.Combine(appDataPath, "FFXIVSimpleLauncher", "Dalamud");

        _configDirectory = new DirectoryInfo(Path.Combine(baseDir, "Config"));
        _runtimeDirectory = new DirectoryInfo(Path.Combine(baseDir, "Runtime"));
        _assetDirectory = new DirectoryInfo(Path.Combine(baseDir, "Assets"));

        // Initialize runtime manager
        _runtimeManager = new DotNetRuntimeManager(_runtimeDirectory, useCnMirror: true);
        _runtimeManager.StatusChanged += status => StatusChanged?.Invoke(status);
        _runtimeManager.ProgressChanged += progress => ProgressChanged?.Invoke(progress);

        EnsureDirectories();
    }

    private void EnsureDirectories()
    {
        if (!_configDirectory.Exists) _configDirectory.Create();
        if (!_runtimeDirectory.Exists) _runtimeDirectory.Create();
        if (!_assetDirectory.Exists) _assetDirectory.Create();
    }

    private void ReportStatus(string status)
    {
        StatusChanged?.Invoke(status);
    }

    private void ReportProgress(double progress)
    {
        ProgressChanged?.Invoke(progress);
    }

    public async Task EnsureDalamudAsync()
    {
        if (State == DalamudState.Ready)
            return;

        State = DalamudState.Checking;
        ErrorMessage = null;

        try
        {
            if (string.IsNullOrEmpty(LocalDalamudPath))
                throw new InvalidOperationException("Local Dalamud path is not set. Please configure it in Settings.");

            ReportStatus("Validating local Dalamud build...");
            await Task.Run(() => ValidateLocalDalamudInternal());

            // Ensure .NET Runtime is downloaded
            State = DalamudState.DownloadingRuntime;
            ReportStatus("Checking .NET Runtime...");
            await _runtimeManager.EnsureRuntimeAsync();

            ReportStatus("Checking assets...");
            await EnsureAssetsAsync();

            State = DalamudState.Ready;
            ReportStatus("Dalamud ready!");
        }
        catch (Exception ex)
        {
            State = DalamudState.Failed;
            ErrorMessage = ex.Message;
            ReportStatus($"Failed: {ex.Message}");
            throw;
        }
    }

    private void ValidateLocalDalamudInternal()
    {
        var localDir = new DirectoryInfo(LocalDalamudPath!);
        if (!localDir.Exists)
            throw new DirectoryNotFoundException($"Local Dalamud directory not found: {LocalDalamudPath}");

        var injectorPath = new FileInfo(Path.Combine(LocalDalamudPath!, "Dalamud.Injector.exe"));
        if (!injectorPath.Exists)
            throw new FileNotFoundException($"Dalamud.Injector.exe not found in: {LocalDalamudPath}");

        _runner = injectorPath;

        var requiredFiles = new[] { "Dalamud.dll", "FFXIVClientStructs.dll" };
        foreach (var file in requiredFiles)
        {
            var filePath = Path.Combine(LocalDalamudPath!, file);
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Required file not found: {file}");
        }

        ReportStatus("Local Dalamud validated");
    }

    private async Task EnsureAssetsAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };

        ReportStatus("Fetching asset info from goatcorp...");
        var assetInfoJson = await client.GetStringAsync(ASSET_URL);
        var assetInfo = JsonSerializer.Deserialize<AssetInfo>(assetInfoJson);

        if (assetInfo == null)
            throw new Exception("Failed to parse asset info");

        var localVerFile = Path.Combine(_assetDirectory.FullName, "asset.ver");
        var localVer = File.Exists(localVerFile) ? int.Parse(File.ReadAllText(localVerFile)) : 0;

        var currentDir = new DirectoryInfo(Path.Combine(_assetDirectory.FullName, assetInfo.Version.ToString()));

        if (localVer >= assetInfo.Version && currentDir.Exists)
        {
            _currentAssetDirectory = currentDir;
            ReportStatus("Assets up to date");
            return;
        }

        State = DalamudState.Downloading;
        ReportStatus("Downloading assets...");

        if (currentDir.Exists)
            currentDir.Delete(true);
        currentDir.Create();

        if (!string.IsNullOrEmpty(assetInfo.PackageUrl))
        {
            // Download package ZIP
            var tempFile = Path.GetTempFileName();
            try
            {
                await DownloadFileAsync(assetInfo.PackageUrl, tempFile);
                ReportStatus("Extracting assets...");
                ZipFile.ExtractToDirectory(tempFile, currentDir.FullName);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
        else if (assetInfo.Assets != null && assetInfo.Assets.Count > 0)
        {
            // Download individual assets
            var totalAssets = assetInfo.Assets.Count;
            var downloadedAssets = 0;

            foreach (var asset in assetInfo.Assets)
            {
                var assetPath = Path.Combine(currentDir.FullName, asset.FileName);
                var assetDir = Path.GetDirectoryName(assetPath);
                if (!string.IsNullOrEmpty(assetDir))
                    Directory.CreateDirectory(assetDir);

                ReportStatus($"Downloading: {asset.FileName}");

                try
                {
                    await DownloadFileAsync(asset.Url, assetPath);
                }
                catch (Exception ex)
                {
                    ReportStatus($"Warning: Failed to download {asset.FileName}: {ex.Message}");
                }

                downloadedAssets++;
                ReportProgress((double)downloadedAssets / totalAssets * 100);
            }
        }

        File.WriteAllText(localVerFile, assetInfo.Version.ToString());
        _currentAssetDirectory = currentDir;

        // Create dev directory (copy of current)
        var devDir = new DirectoryInfo(Path.Combine(_assetDirectory.FullName, "dev"));
        if (devDir.Exists)
            devDir.Delete(true);
        CopyDirectory(currentDir.FullName, devDir.FullName);

        // Cleanup old versions
        foreach (var dir in _assetDirectory.GetDirectories())
        {
            if (dir.Name != assetInfo.Version.ToString() && dir.Name != "dev")
            {
                try { dir.Delete(true); } catch { }
            }
        }

        ReportStatus("Assets ready");
    }

    private async Task DownloadFileAsync(string url, string destinationPath)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };

        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var downloadedBytes = 0L;

        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
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

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }

    public bool ValidateLocalDalamud()
    {
        if (string.IsNullOrEmpty(LocalDalamudPath))
            return false;

        var requiredFiles = new[]
        {
            "Dalamud.Injector.exe",
            "Dalamud.dll",
            "FFXIVClientStructs.dll"
        };

        return requiredFiles.All(f => File.Exists(Path.Combine(LocalDalamudPath, f)));
    }

    /// <summary>
    /// Launch game and inject Dalamud.
    /// </summary>
    public Process? LaunchGameWithDalamud(string gameExePath, string gameArgs, string gameVersion, int injectionDelay = 0)
    {
        if (State != DalamudState.Ready || _runner == null)
            throw new InvalidOperationException("Dalamud is not ready");

        var workingDir = _runner.Directory?.FullName ?? "";

        // Use current asset directory
        var assetDir = _currentAssetDirectory?.FullName
            ?? Path.Combine(_assetDirectory.FullName, "dev");
        Directory.CreateDirectory(assetDir);

        var pluginDirectory = Path.Combine(_configDirectory.FullName, "installedPlugins");
        var devPluginDirectory = Path.Combine(_configDirectory.FullName, "devPlugins");
        var configPath = Path.Combine(_configDirectory.FullName, "dalamudConfig.json");
        var logPath = Path.Combine(_configDirectory.FullName, "logs");

        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(devPluginDirectory);
        Directory.CreateDirectory(logPath);

        // Set environment variables for runtime
        var runtimePath = FindDotNetRuntime();
        if (!string.IsNullOrEmpty(runtimePath))
        {
            Environment.SetEnvironmentVariable("DALAMUD_RUNTIME", runtimePath);
            Environment.SetEnvironmentVariable("DOTNET_ROOT", runtimePath);
        }

        var gameWorkingDir = Path.GetDirectoryName(gameExePath) ?? "";
        ReportStatus("Starting game...");

        // Launch game first
        var gameProcess = Process.Start(new ProcessStartInfo
        {
            FileName = gameExePath,
            WorkingDirectory = gameWorkingDir,
            Arguments = gameArgs,
            UseShellExecute = true
        });

        if (gameProcess == null)
            throw new Exception("Failed to start game process");

        // Wait for game window
        ReportStatus("Waiting for game window...");
        var windowWaitStart = DateTime.Now;
        var maxWindowWait = TimeSpan.FromSeconds(60);

        while (gameProcess.MainWindowHandle == IntPtr.Zero && !gameProcess.HasExited)
        {
            if (DateTime.Now - windowWaitStart > maxWindowWait)
            {
                throw new Exception("Game window did not appear within 60 seconds");
            }
            Thread.Sleep(500);
            gameProcess.Refresh();
        }

        if (gameProcess.HasExited)
        {
            throw new Exception("Game exited before injection could complete");
        }

        // Additional wait after window appears
        var additionalWait = Math.Max(injectionDelay, 3000);
        ReportStatus($"Window found. Waiting {additionalWait / 1000}s before injection...");
        Thread.Sleep(additionalWait);

        // Inject Dalamud
        ReportStatus("Injecting Dalamud...");

        try
        {
            InjectDalamud(
                _runner,
                gameProcess.Id,
                workingDir,
                configPath,
                pluginDirectory,
                devPluginDirectory,
                assetDir,
                4, // Language: 4 = ChineseTraditional (Taiwan)
                injectionDelay > 0 ? injectionDelay : 10000,
                runtimePath
            );
            ReportStatus("Dalamud injected successfully!");
        }
        catch (Exception ex)
        {
            ReportStatus($"Dalamud injection failed: {ex.Message}");
            // Game is still running, just without Dalamud
        }

        return gameProcess;
    }

    /// <summary>
    /// Find .NET runtime path (check local runtime directory or system-installed)
    /// </summary>
    private string? FindDotNetRuntime()
    {
        // First check our managed runtime (downloaded by DotNetRuntimeManager)
        var managedRuntime = _runtimeManager.GetRuntimePath();
        if (!string.IsNullOrEmpty(managedRuntime))
        {
            ReportStatus($"Using managed .NET Runtime: {managedRuntime}");
            return managedRuntime;
        }

        // Fallback: check our local runtime directory manually
        if (_runtimeDirectory.Exists)
        {
            var hostFxr = _runtimeDirectory.GetDirectories("host", SearchOption.TopDirectoryOnly)
                .FirstOrDefault()?.GetDirectories("fxr").FirstOrDefault();
            if (hostFxr?.Exists == true && hostFxr.GetDirectories().Any())
            {
                return _runtimeDirectory.FullName;
            }
        }

        // Fallback: check XIVLauncher's runtime directory
        var xivLauncherRuntime = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XIVLauncher", "runtime");
        if (Directory.Exists(xivLauncherRuntime))
        {
            var hostFxr = Path.Combine(xivLauncherRuntime, "host", "fxr");
            if (Directory.Exists(hostFxr) && Directory.GetDirectories(hostFxr).Length > 0)
            {
                ReportStatus($"Using XIVLauncher .NET Runtime: {xivLauncherRuntime}");
                return xivLauncherRuntime;
            }
        }

        // Fallback: check system .NET installation
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var systemDotNet = Path.Combine(programFiles, "dotnet");
        if (Directory.Exists(systemDotNet))
        {
            ReportStatus($"Using system .NET Runtime: {systemDotNet}");
            return systemDotNet;
        }

        return null;
    }

    /// <summary>
    /// Check if the .NET Runtime is installed.
    /// </summary>
    public bool IsRuntimeInstalled() => _runtimeManager.IsRuntimeInstalled();

    /// <summary>
    /// Force re-download of the .NET Runtime.
    /// </summary>
    public async Task ForceUpdateRuntimeAsync() => await _runtimeManager.ForceUpdateAsync();

    /// <summary>
    /// Get the runtime directory path.
    /// </summary>
    public string GetRuntimeDirectoryPath() => _runtimeDirectory.FullName;

    /// <summary>
    /// Inject Dalamud using command-line arguments.
    /// </summary>
    private void InjectDalamud(
        FileInfo runner,
        int gamePid,
        string workingDirectory,
        string configurationPath,
        string pluginDirectory,
        string devPluginDirectory,
        string assetDirectory,
        int language,
        int delayInitializeMs,
        string? runtimePath,
        bool safeMode = false)
    {
        var launchArguments = new List<string>
        {
            "inject",
            "-v",
            gamePid.ToString(),
            $"--dalamud-working-directory=\"{workingDirectory}\"",
            $"--dalamud-configuration-path=\"{configurationPath}\"",
            $"--dalamud-plugin-directory=\"{pluginDirectory}\"",
            $"--dalamud-dev-plugin-directory=\"{devPluginDirectory}\"",
            $"--dalamud-asset-directory=\"{assetDirectory}\"",
            $"--dalamud-client-language={language}",
            $"--dalamud-delay-initialize={delayInitializeMs}"
        };

        if (safeMode)
        {
            launchArguments.Add("--no-plugin");
        }

        var argumentString = string.Join(" ", launchArguments);
        ReportStatus($"Running: Dalamud.Injector.exe inject -v {gamePid} ...");

        var psi = new ProcessStartInfo(runner.FullName)
        {
            Arguments = argumentString,
            WorkingDirectory = runner.Directory?.FullName ?? "",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Set environment variables
        if (!string.IsNullOrEmpty(runtimePath))
        {
            psi.Environment["DALAMUD_RUNTIME"] = runtimePath;
            psi.Environment["DOTNET_ROOT"] = runtimePath;
        }

        var dalamudProcess = Process.Start(psi);
        if (dalamudProcess == null)
            throw new Exception("Failed to start Dalamud.Injector.exe");

        // Read output
        var output = new StringBuilder();
        var error = new StringBuilder();

        dalamudProcess.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                ReportStatus($"[Injector] {e.Data}");
            }
        };
        dalamudProcess.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                error.AppendLine(e.Data);
            }
        };

        dalamudProcess.BeginOutputReadLine();
        dalamudProcess.BeginErrorReadLine();

        if (!dalamudProcess.WaitForExit(60000))
        {
            dalamudProcess.Kill();
            throw new Exception("Dalamud.Injector.exe timed out");
        }

        if (dalamudProcess.ExitCode != 0)
        {
            var errorMsg = error.ToString().Trim();
            if (string.IsNullOrEmpty(errorMsg))
                errorMsg = output.ToString().Trim();
            throw new Exception($"Injection failed (exit code {dalamudProcess.ExitCode}): {errorMsg}");
        }
    }

    public bool IsGameVersionSupported(string gameVersion)
    {
        return true;
    }

    public bool IsExactVersionMatch(string gameVersion)
    {
        // Local builds don't have version info, always return true
        return true;
    }

    public string? GetSupportedGameVersion()
    {
        // Local builds don't have version info
        return null;
    }
}
