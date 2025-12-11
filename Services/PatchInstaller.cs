using System;
using System.IO;
using FFXIVSimpleLauncher.Patching.ZiPatch;
using FFXIVSimpleLauncher.Patching.ZiPatch.Util;

namespace FFXIVSimpleLauncher.Services;

/// <summary>
/// 補丁安裝器
/// 參考 XIVLauncher.Common.Patching.RemotePatchInstaller
/// </summary>
public static class PatchInstaller
{
    /// <summary>
    /// 安裝單一補丁檔案
    /// </summary>
    /// <param name="patchPath">補丁檔案路徑</param>
    /// <param name="gamePath">遊戲目錄路徑 (game 資料夾)</param>
    public static void InstallPatch(string patchPath, string gamePath)
    {
        if (!File.Exists(patchPath))
            throw new FileNotFoundException("補丁檔案不存在", patchPath);

        if (!Directory.Exists(gamePath))
            Directory.CreateDirectory(gamePath);

        using var patchFile = ZiPatchFile.FromFileName(patchPath);

        using var store = new SqexFileStreamStore();
        var config = new ZiPatchConfig(gamePath) { Store = store };

        foreach (var chunk in patchFile.GetChunks())
        {
            chunk.ApplyChunk(config);
        }
    }

    /// <summary>
    /// 安裝補丁並報告進度
    /// </summary>
    public static void InstallPatchWithProgress(
        string patchPath,
        string gamePath,
        Action<string>? statusCallback = null,
        Action<double>? progressCallback = null)
    {
        if (!File.Exists(patchPath))
            throw new FileNotFoundException("補丁檔案不存在", patchPath);

        if (!Directory.Exists(gamePath))
            Directory.CreateDirectory(gamePath);

        statusCallback?.Invoke($"開啟補丁檔案: {Path.GetFileName(patchPath)}");

        using var patchFile = ZiPatchFile.FromFileName(patchPath);

        using var store = new SqexFileStreamStore();
        var config = new ZiPatchConfig(gamePath) { Store = store };

        // 先計算 chunk 總數 (需要先讀取一次)
        var chunks = new System.Collections.Generic.List<Patching.ZiPatch.Chunk.ZiPatchChunk>();
        foreach (var chunk in patchFile.GetChunks())
        {
            chunks.Add(chunk);
        }

        statusCallback?.Invoke($"套用 {chunks.Count} 個 chunks...");

        for (int i = 0; i < chunks.Count; i++)
        {
            chunks[i].ApplyChunk(config);
            progressCallback?.Invoke((double)(i + 1) / chunks.Count * 100);
        }

        statusCallback?.Invoke("補丁套用完成");
    }
}
