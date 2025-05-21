using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;

namespace SideProject.SimpleBundleFlow
{
    public sealed partial class SimpleBundleFlow
    {
        /// <summary>
        /// 目錄檔主結構，包含版本及資源包資訊
        /// </summary>
        [Serializable]
        private class CatalogInfo
        {
            public int version;
            public Dictionary<string, BundleInfo> bundles;
        }

        /// <summary>
        /// 資源包資訊結構，包含包名、包路徑、自行計算的雜測值(MD5)、大小及包含的資源
        /// </summary>
        [Serializable]
        private class BundleInfo
        {
            public string name;
            public string path;
            public string hash;
            public long size;
            public List<string> assets;
        }

        /// <summary>
        /// 下載狀態
        /// </summary>
        private enum DownloadState
        {
            None = 0,
            Success = 1,
            Failed = 2
        }

        private DownloadState downloadState = DownloadState.None;
        private int currentVersion = 0;
        private int finish = 0;
        private List<string> downloadList = new List<string>();
        private CatalogInfo catalogInfo;

        private const string CatalogName = "catalog.json";

        /// <summary>
        /// 下載資源包的內部方法
        /// </summary>
        /// <param name="progressCallback">進度回呼(當前數量, 總數量)</param>
        /// <param name="completionCallback">完成回呼</param>
        /// <param name="failedCallback">失敗回呼</param>
        private void DownloadBundleInternal(Action<float, float> progressCallback, Action completionCallback, Action failedCallback)
        {
            if (downloadState == DownloadState.Success)
            {
                return;
            }

            ResetVariables();

            TryGetMainManifest();

            if (!TryGetCatalogInfo())
            {
                failedCallback?.Invoke();
                return;
            }

            downloadList = GetDownloadList();
            total = downloadList.Count;

            bool needDownload = downloadList.Count > 0;
            if (!needDownload)
            {
                RefreshFullAssetsMap();
                progressCallback?.Invoke(1, 1);
                completionCallback?.Invoke();
                SaveCurrentVersion(currentVersion);
                downloadState = DownloadState.Success;
                return;
            }

            StartCoroutine(Run());

            IEnumerator Run()
            {
                foreach (string name in downloadList)
                {
                    RequestBundle(name, progressCallback, completionCallback, failedCallback);
                    if (downloadState == DownloadState.Failed)
                    {
                        failedCallback?.Invoke();
                        yield break;
                    }

                    yield return new WaitForSeconds(0.1f);
                }

                TryGetMainManifest();
                if (manifest == null)
                {
                    failedCallback?.Invoke();
                    SimpleBundleFlowUtility.LogError("Download failed, cannot get manifest");
                    yield break;
                }

                RefreshFullAssetsMap();
                yield return new WaitForSeconds(0.1f);

                completionCallback?.Invoke();
                SaveCurrentVersion(currentVersion);
                downloadState = DownloadState.Success;
            }
        }

        /// <summary>
        /// 嘗試獲取目錄資訊
        /// </summary>
        /// <returns>是否成功獲取目錄資訊</returns>
        private bool TryGetCatalogInfo()
        {
            string sourceHashFile = Path.Combine(sourcePath, CatalogName);
            if (!File.Exists(sourceHashFile))
            {
                SimpleBundleFlowUtility.LogError($"Try get Download List in {sourceHashFile} failed");
                return false;
            }

            catalogInfo = SimpleBundleFlowUtility.GetJsonFromText<CatalogInfo>(sourceHashFile);
            if (catalogInfo == null)
            {
                SimpleBundleFlowUtility.LogError($"Try get Download List in {sourceHashFile} failed");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 獲取需要下載的資源包列表
        /// </summary>
        /// <returns>需要下載的資源包名稱列表</returns>
        private List<string> GetDownloadList()
        {
            var needNames = new List<string>();

            var sourceHashMap = catalogInfo.bundles;
            if (manifest == null)
            {
                needNames = new List<string>(sourceHashMap.Keys);
                SimpleBundleFlowUtility.LogMessage($"No manifest, download all.");
                return needNames;
            }

            currentVersion = GetCurrentVersion();
            if (currentVersion != catalogInfo.version)
            {
                needNames = new List<string>(sourceHashMap.Keys);
                SimpleBundleFlowUtility.LogMessage($"Current version {currentVersion} is not equal to catalog version {catalogInfo.version}");
                return needNames;
            }

            string[] paths = Directory.GetFiles(persistPath);
            Dictionary<string, string> persistHashMap = GetHashMap(paths);

            foreach (var source in sourceHashMap)
            {
                Compare(source);
            }

            return needNames;


            void Compare(KeyValuePair<string, BundleInfo> source)
            {
                string name = source.Value.name;
                if (!persistHashMap.ContainsKey(name))
                {
                    needNames.Add(name);
                    return;
                }

                string persistHash = persistHashMap[name];
                string sourceHash = source.Value.hash;
                if (persistHash != sourceHash)
                {
                    needNames.Add(name);
                }
            }
        }

        /// <summary>
        /// 取得目前的版本號
        /// </summary>
        /// <returns>本地保存的版本號，若不存在則返回0</returns>
        private int GetCurrentVersion()
        {
            string versionFile = Path.Combine(persistPath, "version.txt");
            if (!File.Exists(versionFile))
            {
                return 0;
            }

            string versionStr = File.ReadAllText(versionFile);
            if (int.TryParse(versionStr, out int version))
            {
                return version;
            }

            return 0;
        }

        /// <summary>
        /// 儲存當前版本號到本地
        /// </summary>
        /// <param name="version">要儲存的版本號</param>
        private void SaveCurrentVersion(int version)
        {
            string versionFile = Path.Combine(persistPath, "version.txt");
            File.WriteAllText(versionFile, version.ToString());
        }

        /// <summary>
        /// 嘗試獲取主要的AssetBundleManifest
        /// </summary>
        private void TryGetMainManifest()
        {
            string manifestPath = Path.Combine(persistPath, platform);
            if (!File.Exists(manifestPath))
            {
                return;
            }

            AssetBundle bundle;
            if (loadedBundles.ContainsKey(platform))
            {
                bundle = loadedBundles[platform];
                manifest = bundle.LoadAsset<AssetBundleManifest>(ManifestName);
                return;
            }

            bundle = AssetBundle.LoadFromFile(manifestPath);
            if (bundle == null)
            {
                return;
            }

            manifest = bundle.LoadAsset<AssetBundleManifest>(ManifestName);
            if (manifest == null)
            {
                bundle.Unload(false);
                return;
            }

            bundle.Unload(false);
        }

        /// <summary>
        /// 獲取檔案雜湊值表
        /// </summary>
        /// <param name="paths">檔案路徑陣列</param>
        /// <returns>檔案名稱到雜湊值的映射表</returns>
        private Dictionary<string, string> GetHashMap(string[] paths)
        {
            if (paths.Length <= 0)
            {
                return new Dictionary<string, string>();
            }

            var resultTable = new Dictionary<string, string>();
            foreach (string path in paths)
            {
                var resultPair = GetHash(path);
                if (!resultTable.ContainsKey(resultPair.Key))
                {
                    resultTable.Add(resultPair.Key, resultPair.Value);
                }
            }

            return resultTable;
        }

        /// <summary>
        /// 取得指定檔案的雜湊值
        /// </summary>
        /// <param name="path">檔案路徑</param>
        /// <returns>檔案名稱和雜湊值的鍵值對</returns>
        private KeyValuePair<string, string> GetHash(string path)
        {
            string hashString = SimpleBundleFlowUtility.ComputeFileHash(path);

            string name = Path.GetFileName(path);
            var pair = new KeyValuePair<string, string>(name, hashString);
            return pair;
        }

        /// <summary>
        /// 請求下載資源包
        /// </summary>
        /// <param name="name">資源包名稱</param>
        /// <param name="progressCallback">進度回呼(當前數量, 總數量)</param>
        /// <param name="completionCallback">完成回呼</param>
        /// <param name="failedCallback">失敗回呼</param>
        private void RequestBundle(string name, Action<float, float> progressCallback, Action completionCallback, Action failedCallback)
        {
            string sourceFile = Path.Combine(sourcePath, name);
            var request = UnityWebRequest.Get(sourceFile);

            string downloadPath = Path.Combine(persistPath, name);
            request.downloadHandler = new DownloadHandlerFile(downloadPath);

            var operation = request.SendWebRequest();
            operation.completed += OnComplete;
            void OnComplete(AsyncOperation _)
            {
                UnityWebRequest.Result result = request.result;
                if (result != UnityWebRequest.Result.Success)
                {
                    SimpleBundleFlowUtility.LogError($"{result}, in RequestDownload, by {sourceFile}");
                    downloadState = DownloadState.Failed;
                    request.Dispose();
                    return;
                }

                RefreshProgress(progressCallback, completionCallback, failedCallback);
                request.Dispose();
            }
        }

        /// <summary>
        /// 更新下載進度
        /// </summary>
        /// <param name="progressCallback">進度回呼(當前數量, 總數量)</param>
        /// <param name="completionCallback">完成回呼</param>
        /// <param name="failedCallback">失敗回呼</param>
        private void RefreshProgress(Action<float, float> progressCallback, Action completionCallback, Action failedCallback)
        {
            finish += 1;
            progressCallback?.Invoke(finish, total);
        }

        /// <summary>
        /// 刷新資源路徑到資源包名稱的映射表
        /// </summary>
        private void RefreshFullAssetsMap()
        {
            foreach (var bundle in catalogInfo.bundles.Values)
            {
                foreach (string assetName in bundle.assets)
                {
                    if (assetsBundleMap.ContainsKey(assetName))
                    {
                        continue;
                    }
                    assetsBundleMap.Add(assetName, bundle.name);
                }
            }
        }
    }
}
