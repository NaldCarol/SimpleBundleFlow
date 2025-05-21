using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace SideProject.SimpleBundleFlow
{
    public sealed partial class SimpleBundleFlow
    {
        /// <summary>
        /// 卸載AssetBundle包的內部方法
        /// </summary>
        /// <param name="bundleName">資源包名稱</param>
        /// <param name="unloadAllLoadedObjects">是否同時卸載所有從此資源包加載的資源</param>
        private void UnloadAssetBundleInternal(string bundleName, bool unloadAllLoadedObjects)
        {
            if (!loadedBundles.ContainsKey(bundleName))
            {
                Debug.LogWarning($"Bundle {bundleName} not loaded.");
                return;
            }

            var bundle = loadedBundles[bundleName];

            loadedBundles.Remove(bundleName);
            bundle.Unload(unloadAllLoadedObjects);
        }

        /// <summary>
        /// 釋放單個資源的內部方法
        /// </summary>
        /// <param name="asset">要釋放的資源</param>
        private void ReleaseAssetInternal(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return;
            }

            Resources.UnloadAsset(asset);
        }

        /// <summary>
        /// 釋放所有未使用資源的內部方法
        /// </summary>
        private void ReleaseUnusedAssetsInternal()
        {
            GC.Collect();
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// 清空下載緩存和所有已加載資源的內部方法
        /// </summary>
        private void ClearCacheInternal()
        {
            if (!Caching.ClearCache())
            {
                SimpleBundleFlowUtility.LogMessage("Clear cache failed or already in progress.");
            }

            try
            {
                foreach (var bundleName in new List<string>(loadedBundles.Keys))
                {
                    UnloadAssetBundleInternal(bundleName, true);
                }

                loadedBundles.Clear();
                assetsBundleMap.Clear();

                if (Directory.Exists(persistPath))
                {
                    DirectoryInfo di = new DirectoryInfo(persistPath);
                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete();
                    }

                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }

                    Directory.CreateDirectory(persistPath);
                }

                ResetVariables();

                SimpleBundleFlowUtility.LogMessage("Cache and all downloaded bundles cleared successfully.");
            }
            catch (Exception ex)
            {
                SimpleBundleFlowUtility.LogError($"Failed to clear cache: {ex.Message}");
            }
        }

        /// <summary>
        /// 重置系統所有變數為初始狀態
        /// </summary>
        private void ResetVariables()
        {
            manifest = null;
            catalogInfo = null;
            downloadState = DownloadState.None;
            downloadList.Clear();
            currentVersion = 0;
            finish = 0;
            total = 0;
        }
    }
}
