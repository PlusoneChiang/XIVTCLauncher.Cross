# XIVTCLauncher (FFXIVSimpleLauncher)

[English](README.md) | [繁體中文](README_zh-TW.md)

專為 Final Fantasy XIV 台灣版設計的快速啟動器，靈感來自 [FFXIVQuickLauncher](https://github.com/goatcorp/FFXIVQuickLauncher)。

## 為什麼選擇 XIVTCLauncher？

- **快速登入** - 儲存帳號密碼，一鍵登入遊戲
- **OTP 支援** - 支援一次性密碼驗證
- **網頁登入** - 內建 WebView2 瀏覽器進行網頁驗證
- **Dalamud 整合** - 支援 Dalamud 插件框架，增強遊戲體驗
- **現代化介面** - 採用 Material Design 設計風格
- **設定管理** - 自訂遊戲路徑與啟動選項

## 系統需求

- Windows 10/11
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)
- 已安裝 Final Fantasy XIV 台灣版

## 安裝方式

1. 下載最新版本
2. 解壓縮到任意資料夾
3. 執行 `FFXIVSimpleLauncher.exe`
4. 在設定中配置遊戲路徑

## 從原始碼編譯

```bash
# 複製專案
git clone https://github.com/your-repo/XIVTCLauncher.git
cd XIVTCLauncher

# 編譯專案
dotnet build

# 執行應用程式
dotnet run
```

## 設定

設定檔儲存於 `%APPDATA%/FFXIVSimpleLauncher/settings.json`

### 遊戲路徑

台灣版 FFXIV 預設安裝路徑：

```
C:\Program Files\USERJOY GAMES\FINAL FANTASY XIV TC
```

## Dalamud 插件支援

本啟動器支援 [Dalamud](https://github.com/goatcorp/Dalamud) 插件注入，並針對台灣/中國版客戶端進行相容性調整。

### 推薦的 Dalamud 分支

針對台灣/中國版客戶端，我們推薦使用 [yanmucorp/Dalamud](https://github.com/yanmucorp/Dalamud)：

- **Dalamud CN**: [yanmucorp/Dalamud](https://github.com/yanmucorp/Dalamud) - 持續維護的中文客戶端 Dalamud 分支
- **FFXIVClientStructs**: 使用 [Dalamud-DailyRoutines/FFXIVClientStructs](https://github.com/Dalamud-DailyRoutines/FFXIVClientStructs) 作為子模組

### 從原始碼編譯 Dalamud

```bash
# 包含子模組一起複製
git clone --recursive https://github.com/yanmucorp/Dalamud.git

# 或者已經複製過，初始化子模組
git submodule update --init --recursive

# 編譯
dotnet build -c Release
```

### 使用方式

1. 從原始碼編譯 Dalamud（參考上方步驟）或從 [yanmucorp releases](https://github.com/yanmucorp/Dalamud/releases) 下載
2. 在設定中，將 **Local Dalamud Path** 設為你的 Dalamud 編譯目錄（例如：`E:\FFXIV\Dalamud\bin\Release`）
3. 在設定中啟用 Dalamud
4. 如有需要，調整注入延遲時間（預設值適用於大多數情況）
5. 啟動遊戲 - Assets 會自動從 ottercorp 下載，然後 Dalamud 會被注入

> **注意**：Assets 從 [ottercorp](https://aonyx.ffxiv.wang) 下載，與 yanmucorp Dalamud 相容。Dalamud 本體需要自行提供。

## 這樣做安全嗎？

XIVTCLauncher 本身只是一個登入工具，它使用與官方啟動器相同的方式來啟動遊戲。

如果你選擇使用 Dalamud 插件，請注意這是對遊戲的第三方修改。據我們所知，目前沒有人因為使用此類工具而遇到問題，但我們無法保證這一點。

## 免責聲明

XIVTCLauncher 的使用可能不符合遊戲的服務條款。我們盡力讓它對所有人都是安全的，據我們所知，目前沒有人因為使用 XIVTCLauncher 而遇到問題，但請注意這仍有可能發生。

**使用風險自負。**

### 插件使用準則

如果你使用 Dalamud 插件，請遵守以下準則：

- 不要使用以不公平方式自動化遊戲操作的插件
- 不要使用以未經授權方式與遊戲伺服器互動的插件
- 不要使用繞過任何遊戲限制或付費牆的插件

## 致謝

- [goatcorp/FFXIVQuickLauncher](https://github.com/goatcorp/FFXIVQuickLauncher) - 原始靈感來源與 Dalamud 框架
- [goatcorp/Dalamud](https://github.com/goatcorp/Dalamud) - 插件框架
- [yanmucorp/Dalamud](https://github.com/yanmucorp/Dalamud) - 中文客戶端 Dalamud 分支
- [ottercorp/FFXIVQuickLauncher](https://github.com/ottercorp/FFXIVQuickLauncher) - 中文客戶端啟動器與 asset 伺服器
- [Dalamud-DailyRoutines/FFXIVClientStructs](https://github.com/Dalamud-DailyRoutines/FFXIVClientStructs) - 中文客戶端結構
- [MaterialDesignInXAML](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) - UI 工具包

## 法律聲明

XIVTCLauncher 與 SQUARE ENIX CO., LTD. 或 Gameflier International Corp. 無任何關聯或背書。

FINAL FANTASY 是 Square Enix Holdings Co., Ltd. 的註冊商標。

所有遊戲素材和商標均為其各自所有者的財產。

## 授權

本專案以原樣提供，僅供教育和個人使用。
