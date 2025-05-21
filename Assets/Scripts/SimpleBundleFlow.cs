using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

namespace SideProject.SimpleBundleFlow
{
    public sealed partial class SimpleBundleFlow : MonoBehaviour
    {
        public class SimpleBundleFlowException : Exception
        {
            public SimpleBundleFlowException(string message) : base(message) { }
        }

        private static SimpleBundleFlow _instance;

        public static SimpleBundleFlow Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SimpleBundleFlow");
                    _instance = go.AddComponent<SimpleBundleFlow>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private int total = 0;
        private string platform = "";
        private string persistPath = "";
        private string sourcePath = "";
        private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
        private Dictionary<string, string> assetsBundleMap = new Dictionary<string, string>();
        private AssetBundleManifest manifest;

        private const string ManifestName = "AssetBundleManifest";
        private const string WindowsFolderName = "StandaloneWindows64";
        private const string AppleiOSFolderName = "iOS";
        private const string AndroidFolderName = "Android";
        private const string PathFormat = "{0}/AssetBundles/{1}";

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            platform = PlatformToBuildTarget(Application.platform);

            sourcePath = string.Format(PathFormat, Application.streamingAssetsPath, platform);
            persistPath = string.Format(PathFormat, Application.persistentDataPath, platform);
            if (!Directory.Exists(persistPath))
            {
                Directory.CreateDirectory(persistPath);
            }
        }

        /// <summary>
        /// 下載資源包，會根據版本和雜湊值進行增量下載
        /// </summary>
        /// <param name="progressCallback">進度回呼(當前數量, 總數量)</param>
        /// <param name="completionCallback">完成回呼</param>
        /// <param name="failedCallback">失敗回呼</param>
        public void DownloadBundle(Action<float, float> progressCallback, Action completionCallback, Action failedCallback)
        {
            DownloadBundleInternal(progressCallback, completionCallback, failedCallback);
        }

        /// <summary>
        /// 同步加載資源，透過回呼方法回傳物件
        /// </summary>
        /// <typeparam name="T">資源類型</typeparam>
        /// <param name="path">資源路徑，相對於Asset/AssetBundles/目錄</param>
        /// <param name="callback">加載成功回呼</param>
        /// <param name="failedCallback">加載失敗回呼</param>
        public void LoadAsset<T>(string path, Action<T> callback, Action failedCallback) where T : UnityEngine.Object
        {
            LoadAssetInternal(path, callback, failedCallback);
        }

        /// <summary>
        /// 同步加載場景，透過回呼方法回傳物件
        /// </summary>
        /// <param name="path">場景路徑，相對於Asset/AssetBundles/目錄</param>
        /// <param name="callback">加載成功回呼，參數為場景路徑</param>
        /// <param name="failedCallback">加載失敗回呼</param>
        public void LoadScene(string path, Action<string> callback, Action failedCallback)
        {
            LoadSceneInternal(path, callback, failedCallback);
        }

        /// <summary>
        /// 卸載指定的資源包
        /// </summary>
        /// <param name="bundleName">資源包名稱</param>
        /// <param name="unloadAllLoadedObjects">是否同時卸載所有從此資源包加載的資源</param>
        public void UnloadAssetBundle(string bundleName, bool unloadAllLoadedObjects = false)
        {
            UnloadAssetBundleInternal(bundleName, unloadAllLoadedObjects);
        }

        /// <summary>
        /// 實例化預製物，透過回呼方法回傳實例化後的物件
        /// </summary>
        /// <param name="prefabPath">預製物路徑，相對於Asset/AssetBundles/目錄</param>
        /// <param name="parent">父物件Transform</param>
        /// <param name="callback">實例化成功回呼</param>
        /// <param name="failedCallback">實例化失敗回呼</param>
        public void InstantiatePrefab(string prefabPath, Transform parent = null, Action<GameObject> callback = null, Action failedCallback = null)
        {
            InstantiatePrefabInternal(prefabPath, parent, callback, failedCallback);
        }

        /// <summary>
        /// 釋放指定資源
        /// </summary>
        /// <param name="asset">要釋放的資源</param>
        public void ReleaseAsset(UnityEngine.Object asset)
        {
            ReleaseAssetInternal(asset);
        }

        /// <summary>
        /// 釋放所有未使用的資源
        /// </summary>
        public void ReleaseUnusedAssets()
        {
            ReleaseUnusedAssetsInternal();
        }

        /// <summary>
        /// 清空下載緩存和所有已加載的資源
        /// </summary>
        public void ClearCache()
        {
            ClearCacheInternal();
        }

        /// <summary>
        /// 將運行時平台轉換為對應的資源目錄名稱
        /// </summary>
        private string PlatformToBuildTarget(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.IPhonePlayer:
                    return AppleiOSFolderName;
                case RuntimePlatform.Android:
                    return AndroidFolderName;
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                default:
                    return WindowsFolderName;
            }
        }
    }
}
