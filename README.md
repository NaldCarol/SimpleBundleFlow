# Simple Bundle Flow

## 目錄
- [介紹](#介紹)
- [包含部件](#包含部件)
    - [SimpleBundleFlow.unitypackage](#SimpleBundleFlow.unitypackage)
- [基本配置](#基本配置)
- [測試操作](#測試操作)
- [重點欄位](#重點欄位)
	- [編輯器工具](#編輯器工具)
	- [測試工具](#測試工具)
	- [加載](#加載)
- [使用/呼叫方法](#使用/呼叫方法)
	- [下載](#下載)
	- [加載](#加載)
	- [實例化](#實例化)
	- [卸載](#卸載)
- [授權](#授權)

## 介紹

SimpleBundleFlow 是一款為 Unity 專案設計的輕量級資源管理工具，提供了一個簡明易用的系統，幫助開發者管理和載入 AssetBundle 資源。
這個套件內含完整的示範場景和測試工具，使用者可透過 SimpleBundleFlow.unitypackage 快速整合到自己的專案中，簡易實現資源下載、加載與釋放，而無需先行了解複雜的底層機制。系統支援資源的自動依賴處理、版本控制與增量更新，並提供了簡易的打包工具。此工具採用模組化設計，適合小型專案和初學者入門 AssetBundle 系統。

## 包含部件

### SimpleBundleFlow.unitypackage

此包為開發必需的核心部件，請在發布頁面（release page）下載。
- Asset
  - AssetBundles: 供測試專案使用的資源。
    - Image
        - TestImage.png: TestPrefab上的圖檔，用於測試相依是否正常。
    - Prefab
        - TestPrefab.prefab: 用於測試預製物載入。
    - TestLoadScene.unity: 用於測試場景載入。
  - Scene
    - TestScene: 為本專案提供的測試場景，包含用於演示的示範物件。
  - Script
    - Editor
        - **SimpleBundleFlowToolWindow**: 簡易打包工具。
    - **SimpleBundleFlow**: 主腳本，包含所有公開方法及初始化，為部分類別。
    - **SimpleBundleFlow_Download**: 下載腳本，包含所有下載相關私有方法，為部分類別。
    - **SimpleBundleFlow_Editor**: 編輯器運行(editor runtime)時相關內容腳本，為部分類別。
    - **SimpleBundleFlow_Loader**: 加載腳本，包含所有加載及實例化相關私有方法，為部分類別。
    - **SimpleBundleFlow_Release**: 卸載腳本，包含所有卸載、清空下載暫存相關私有方法，為部分類別。
    - SimpleBundleFlowUtility: 資源管理使用的工具類腳本。
    - SimpleBundleFlowTester: 測試用介面腳本，需掛載於 GameObject 上。

## 基本配置
- **匯入unitypackage**：
    - 請到發布頁面（release page）找到 `SimpleBundleFlow.unitypackage`，並將其匯入至您的 Unity 專案中。匯入後，請檢查並確保所有的組件都完整無缺。
- **開啟測試場景**：
    - 打開 TestScene，確保場景配置正確。正確的場景配置如下所示圖片。
    - 若要測試模擬正式讀取資源，請建置Windows執行檔。

## 測試操作要點

- 初次啟動(editor runtime play或啟動Windows執行檔)後，請先點擊"Download Asset Bundle"，再進行其餘測試。
- 點擊"Load Test Scene"後請直接結束或關閉執行檔
- 點擊"Clear Cache"後，請先點擊"Download Asset Bundle"，再進行其餘測試，否則會遇到加載失敗。

## 重點欄位

### 編輯器工具

- Platform：資源包目標平台
- Version：資源包版本
- Build Asset Bundle：建立資源包，建立的內容會放置在StreamingAssets資料夾下，會一並建立目錄檔
- Clear Asset Labels：清空資源標籤

```csharp
private const string RootPath = "Assets/AssetBundles";
```
- 所有待建立資源包的資源根目錄，若有資料夾結構變動，請修改此處。

### 測試工具

- Test Asset Path：測試用預置物路徑，從Asset/AssetBundles/的下一層開始，含副檔名。
- Test Scene Path：測試用預打包場景路徑，從Asset/AssetBundles/的下一層開始，含副檔名。

### 加載

```csharp
private const string RootPath = "Assets/AssetBundles/";
```
- 這是所有資源包的資源根目錄，若建立資源包時，有資料夾結構變動，請一並修改此處。

## 方法總覽

| 方法名稱 | 功能描述 |
| :----- | :----- |
| `DownloadBundle` | 下載資源包，根據版本和雜湊值進行增量下載，只下載變更的資源 |
| `LoadAsset<T>` | 同步加載指定類型的資源，透過回呼回傳物件，支援泛型以加載不同類型 |
| `LoadScene` | 同步加載預打包的場景資源，透過回呼回傳場景路徑，加載後可通過場景管理器切換 |
| `UnloadAssetBundle` | 卸載指定名稱的資源包，可選擇是否同時卸載已加載的資源 |
| `InstantiatePrefab` | 實例化預製件，可指定父物件，會自動處理資源加載 |
| `ReleaseAsset` | 釋放指定資源，減少記憶體佔用 |
| `ReleaseUnusedAssets` | 釋放所有未被使用的資源，適合在場景轉換時調用 |
| `ClearCache` | 清空下載緩存和所有已加載的資源，適合在應用退出前調用 |

## 使用/呼叫方法

```csharp
// 獲取SimpleBundleFlow實例
SimpleBundleFlow resourceFlow = SimpleBundleFlow.Instance;
```

### 下載

- 下載資源包，提供進度、完成和失敗的回呼
```csharp
resourceFlow.DownloadBundle(
    // 進度回呼，提供當前數量和總數量
    (current, total) => {
        float progress = current / total;
        Debug.Log($"下載進度：{progress * 100}%");
    },
    // 完成回呼
    () => {
        Debug.Log("資源包下載完成");
        // 下載完成後，可以開始加載資源
    },
    // 失敗回呼
    () => {
        Debug.LogError("資源包下載失敗");
        // 處理下載失敗的情況
    }
);
```

### 加載

- 加載預製物資源
```csharp
resourceFlow.LoadAsset<GameObject>(
    "Prefab/TestPrefab.prefab",  // 資源路徑，相對於Asset/AssetBundles/目錄
    // 加載成功回呼
    (prefab) => {
        Debug.Log($"預製體 {prefab.name} 加載成功");
        // 使用加載的預製體
    },
    // 加載失敗回呼
    () => {
        Debug.LogError("預製體加載失敗");
        // 處理加載失敗的情況
    }
);
```

- 加載預打包場景
```csharp
resourceFlow.LoadScene(
    "TestLoadScene.unity",  // 場景路徑，相對於Asset/AssetBundles/目錄
    // 加載成功回呼，參數為場景路徑
    (scenePath) => {
        Debug.Log($"場景 {scenePath} 加載成功");
        // 使用 Unity 的場景管理器載入場景
        UnityEngine.SceneManagement.SceneManager.LoadScene(scenePath);
    },
    // 加載失敗回呼
    () => {
        Debug.LogError("場景加載失敗");
        // 處理加載失敗的情況
    }
);
```

### 實例化

- 實例化預製體，若有需求，先加載
```csharp
// 獲取當前場景中的一個容器物件作為父物件
Transform containerTransform = GameObject.Find("Container").transform;

resourceFlow.InstantiatePrefab(
    "Prefab/TestPrefab.prefab",  // 預製體路徑，相對於AssetBundles目錄
    containerTransform,          // 可選，例如生成UI物件時，將Canvas作為父物件帶入，無父物件需求可不帶
    // 實例化成功回呼
    (gameObject) => {
        Debug.Log($"預製體成功實例化：{gameObject.name}");
    },
    // 實例化失敗回呼
    () => {
        Debug.LogError("預製體實例化失敗");
        // 處理實例化失敗的情況
    }
);
```

### 卸載

- 卸載特定的資源包
```csharp
string bundleName = "assets$assetbundles$prefab$testprefab¥prefab.bundle";  // 資源包名稱
resourceFlow.UnloadAssetBundle(bundleName, false);  // 第二個參數表示是否同時卸載已加載的資源

// 釋放特定資源
// 假設我們有一個已加載的資源
GameObject loadedAsset = null;
resourceFlow.LoadAsset<GameObject>(
    "Prefab/TestPrefab.prefab",
    (asset) => { loadedAsset = asset; },
    () => {}
);

// 當不再需要此資源時
resourceFlow.ReleaseAsset(loadedAsset);

// 釋放所有未使用的資源
resourceFlow.ReleaseUnusedAssets();
```

- 清空下載緩存和所有已加載的資源
```csharp
resourceFlow.ClearCache();
```

> **注意**：在釋放資源或卸載資源包後，如果需要再次使用，必須重新下載或加載。建議在場景轉換或應用退出前釋放不需要的資源，以避免記憶體洩漏。

## 授權

此專案基於 GPLv3 授權條款，詳情請參閱 LICENSE 文件。
