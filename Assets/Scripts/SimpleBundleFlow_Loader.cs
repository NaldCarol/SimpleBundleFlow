using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace SideProject.SimpleBundleFlow
{
    public sealed partial class SimpleBundleFlow
    {
        private const string RootPath = "Assets/AssetBundles/";
        private const string PathSymbol = "/";
        private const string NewPathSymbol = "$";
        private const string FileSymbol = ".";
        private const string NewFileSymbol = "¥";
        private const string Variant = ".bundle";

        /// <summary>
        /// 同步加載資源的內部方法，透過Callback回傳物件
        /// </summary>
        /// <typeparam name="T">資源類型</typeparam>
        /// <param name="path">資源路徑，相對於AssetBundles目錄</param>
        /// <param name="completeCallback">加載成功回呼</param>
        /// <param name="failedCallback">加載失敗回呼</param>
        private void LoadAssetInternal<T>(string path, Action<T> completeCallback, Action failedCallback) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            if (Application.isEditor)
            {
                LoadAssetForEditor(path, completeCallback);
                return;
            }
#endif
            if (manifest == null)
            {
                failedCallback?.Invoke();
                return;
            }

            string fullPath = Path.Combine(RootPath, path);

            if (!assetsBundleMap.ContainsKey(fullPath))
            {
                failedCallback?.Invoke();
                return;
            }
            string bundleName = assetsBundleMap[fullPath];
            LoadDependentBundle(bundleName, GetAsset, failedCallback);

            void GetAsset()
            {
                if (!loadedBundles.ContainsKey(bundleName))
                {
                    SimpleBundleFlowUtility.LogError($"Bundle {bundleName} not loaded yet, cannot load asset.");
                    failedCallback?.Invoke();
                    return;
                }

                GetAssetAsync(bundleName, fullPath, completeCallback, failedCallback);
            }
        }

        /// <summary>
        /// 同步加載場景的內部方法，透過Callback回傳物件
        /// </summary>
        /// <param name="path">場景路徑，相對於AssetBundles目錄</param>
        /// <param name="completeCallback">加載成功回呼，參數為場景路徑</param>
        /// <param name="failedCallback">加載失敗回呼</param>
        private void LoadSceneInternal(string path, Action<string> completeCallback, Action failedCallback)
        {
#if UNITY_EDITOR
            if (Application.isEditor)
            {
                LoadSceneForEditor(path, completeCallback);
                return;
            }
#endif
            if (manifest == null)
            {
                failedCallback?.Invoke();
                return;
            }

            string fullPath = Path.Combine(RootPath, path);

            if (!assetsBundleMap.ContainsKey(fullPath))
            {
                failedCallback?.Invoke();
                return;
            }

            string bundleName = assetsBundleMap[fullPath];
            LoadDependentBundle(bundleName, GetAsset, failedCallback);

            void GetAsset()
            {
                if (!loadedBundles.ContainsKey(bundleName))
                {
                    SimpleBundleFlowUtility.LogError($"Bundle {bundleName} not loaded yet, cannot load asset.");
                    failedCallback?.Invoke();
                    return;
                }

                GetScenePathInBundle(bundleName, completeCallback, failedCallback);
            }
        }

        /// <summary>
        /// 實例化預製件的內部方法
        /// </summary>
        /// <param name="prefabPath">預製件路徑，相對於AssetBundles目錄</param>
        /// <param name="parent">父物件Transform</param>
        /// <param name="callback">實例化成功回呼</param>
        /// <param name="failedCallback">實例化失敗回呼</param>
        private void InstantiatePrefabInternal(string prefabPath, Transform parent, Action<GameObject> callback, Action failedCallback)
        {
#if UNITY_EDITOR
            if (Application.isEditor)
            {
                InstantiatePrefabForEditor(prefabPath, parent, callback);
                return;
            }
#endif
            if (manifest == null)
            {
                failedCallback?.Invoke();
                return;
            }
            LoadAsset<GameObject>(prefabPath, Callback, failedCallback);

            void Callback(GameObject prefeb)
            {
                if (prefeb == null)
                {
                    failedCallback?.Invoke();
                    return;
                }
                var gameInstance = Instantiate(prefeb, parent);
                callback?.Invoke(gameInstance);
            }
        }

        /// <summary>
        /// 將路徑轉換為資源包名稱
        /// </summary>
        /// <param name="path">原始路徑</param>
        /// <returns>轉換後的資源包名稱</returns>
        private string TransferPathToName(string path)
        {
            string fullName = path.Replace(PathSymbol, NewPathSymbol);
            fullName = fullName.Replace(FileSymbol, NewFileSymbol);
            fullName = fullName + Variant;
            fullName = fullName.ToLower();
            fullName = RootPath + fullName;

            return fullName;
        }

        /// <summary>
        /// 加載指定資源包及其所有依賴
        /// </summary>
        /// <param name="fullName">資源包名稱</param>
        /// <param name="completionCallback">加載完成回呼</param>
        /// <param name="failedCallback">加載失敗回呼</param>
        private void LoadDependentBundle(string fullName, Action completionCallback, Action failedCallback)
        {
            string[] dependencies = manifest.GetAllDependencies(fullName);
            var bundles = new List<string>(dependencies)
            {
                fullName
            };

            total += bundles.Count;
            foreach (string name in bundles)
            {
                LoadBundle(name, failedCallback);
            }
            completionCallback?.Invoke();
        }

        /// <summary>
        /// 加載單個資源包
        /// </summary>
        /// <param name="name">資源包名稱</param>
        /// <param name="failedCallback">加載失敗回呼</param>
        private void LoadBundle(string name, Action failedCallback)
        {
            string path = Path.Combine(persistPath, name);

            if (loadedBundles.ContainsKey(name))
            {
                return;
            }

            try
            {
                var bundle = AssetBundle.LoadFromFile(path);

                if (bundle == null)
                {
                    string message = $"Load asset named {path} from path {name} failed, asset";
                    throw new SimpleBundleFlowException(message);
                }

                loadedBundles.Add(name, bundle);
            }
            catch (SimpleBundleFlowException ex)
            {
                SimpleBundleFlowUtility.LogError(ex.Message);
                failedCallback?.Invoke();
            }
        }

        /// <summary>
        /// 從資源包非同步加載資源
        /// </summary>
        /// <typeparam name="T">資源類型</typeparam>
        /// <param name="bundleName">資源包名稱</param>
        /// <param name="assetName">資源名稱</param>
        /// <param name="callback">加載成功回呼</param>
        /// <param name="failedCallback">加載失敗回呼</param>
        private void GetAssetAsync<T>(string bundleName, string assetName, Action<T> callback, Action failedCallback) where T : UnityEngine.Object
        {
            if (!loadedBundles.TryGetValue(bundleName, out AssetBundle bundle))
            {
                failedCallback?.Invoke();
                return;
            }
            var operation = bundle.LoadAssetAsync<T>(assetName);
            operation.completed += Callback;

            void Callback(AsyncOperation _)
            {
                var resource = operation.asset as T;
                if (resource == null)
                {
                    SimpleBundleFlowUtility.LogError($"Load asset named {assetName} from bundle {bundleName} failed, asset is null.");
                    failedCallback?.Invoke();
                    return;
                }
                callback?.Invoke(resource);
            }
        }

        /// <summary>
        /// 從資源包獲取場景路徑
        /// </summary>
        /// <param name="bundleName">資源包名稱</param>
        /// <param name="callback">獲取成功回呼，參數為場景路徑</param>
        /// <param name="failedCallback">獲取失敗回呼</param>
        private void GetScenePathInBundle(string bundleName, Action<string> callback, Action failedCallback)
        {
            AssetBundle bundle = loadedBundles[bundleName];
            if (bundle == null)
            {
                failedCallback?.Invoke();
                return;
            }

            string[] scenePath = bundle.GetAllScenePaths();
            if (scenePath.Length == 0)
            {
                failedCallback?.Invoke();
                return;
            }
            callback?.Invoke(scenePath[0]);
        }
    }
}
