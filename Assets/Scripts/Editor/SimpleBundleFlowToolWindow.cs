using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SideProject.SimpleBundleFlow.Editor
{
    public sealed class SimpleBundleFlowToolWindow : EditorWindow
    {
        /// <summary>
        /// 目錄檔主結構，包含版本及資源包資訊
        /// </summary>
        [Serializable]
        public class CatalogInfo
        {
            public int version;
            public Dictionary<string, BundleInfo> bundles;
        }

        /// <summary>
        /// 資源包資訊結構，包含包名、包路徑、自行計算的雜測值(MD5)、大小及包含的資源
        /// </summary>
        [Serializable]
        public class BundleInfo
        {
            public string name;
            public string path;
            public string hash;
            public long size;
            public List<string> assets;
        }

        private string outputPath = "";

        private int version = 0;
        private BuildTarget buildTarget = BuildTarget.StandaloneWindows64;
        private const string WindowTitle = "SimpleBundleFlow Tool";
        private const string TargetLabel = "Platform";
        private const string VersionLabel = "Version";
        private const string BuildLabel = "Build Asset Bundles";
        private const string ClearLabel = "Clear Asset Labels";
        private static readonly Vector2 WindowSize = new Vector2(350, 250);

        private const string RootPath = "Assets/AssetBundles";
        private const string PackagePath = "Packages/";
        private const string ComponentExtention = ".cs";
        private const string ShaderExtention = ".hlsl";
        private const string PathSymbol = "/";
        private const string NewPathSymbol = "$";
        private const string FileSymbol = ".";
        private const string NewFileSymbol = "¥";
        private const string VariantName = "bundle";
        private const string Variant = ".bundle";
        private const string OutputFormat = "{0}/AssetBundles/{1}";
        private const string CatalogName = "catalog.json";

        /// <summary>
        /// 開啟資源包工具視窗
        /// </summary>
        [MenuItem("Tools/AssetBundle/OpenToolWindow")]
        public static void ShowWindow()
        {
            var window = GetWindow<SimpleBundleFlowToolWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = WindowSize;
        }

        /// <summary>
        /// 繪製資源包工具視窗
        /// </summary>
        private void OnGUI()
        {
            GUILayout.Label(WindowTitle, EditorStyles.boldLabel);
            EditorGUILayout.Space();

            buildTarget = (BuildTarget)EditorGUILayout.EnumPopup(TargetLabel, buildTarget);
            EditorGUILayout.Space();

            version = EditorGUILayout.IntField(VersionLabel, version);
            EditorGUILayout.Space();

            if (GUILayout.Button(BuildLabel))
            {
                BuildAssetBundles();
            }

            if (GUILayout.Button(ClearLabel))
            {
                ClearAssetLabels();
            }
        }

        /// <summary>
        /// 清除資源包標籤
        /// </summary>
        [MenuItem("Tools/AssetBundle/Clear Asset Labels")]
        public static void ClearAssetLabels()
        {
            string[] bundleNames = AssetDatabase.GetAllAssetBundleNames();

            foreach (string bundleName in bundleNames)
            {
                string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
                foreach (string assetPath in assetPaths)
                {
                    var importer = AssetImporter.GetAtPath(assetPath);
                    importer.SetAssetBundleNameAndVariant(null, null);
                }
                AssetDatabase.RemoveAssetBundleName(bundleName, true);
            }

            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 建立資源包
        /// </summary>
        private void BuildAssetBundles()
        {
            ClearAssetNames();
            AssetDatabase.Refresh();

            var pathNameMap = OrganizeNeedBuilds();
            AssetDatabase.Refresh();

            string target = buildTarget.ToString();
            SetOutputPath(target);
            BuildBundle(outputPath);

            var catalogInfo = CreateCatalogInfo(pathNameMap, target, outputPath);

            SimpleBundleFlowUtility.SetJsonToText(outputPath, CatalogName, catalogInfo);
            LogMessage($"Write asset bundles catalog: {outputPath}");

        }

        /// <summary>
        /// 創建資源包目錄資訊
        /// </summary>
        /// <param name="pathNameMap">路徑名稱映射</param>
        /// <param name="target">目標平台</param>
        /// <param name="outputPath">輸出路徑</param>
        /// <returns>目錄資訊</returns>
        private CatalogInfo CreateCatalogInfo(Dictionary<string, string> pathNameMap, string target, string outputPath)
        {
            var bundleInfos = new Dictionary<string, BundleInfo>();
            foreach (var pair in pathNameMap)
            {
                string name = pair.Value + Variant;
                var info = RefreshBundleInfo(pair.Key, name, outputPath);
                bundleInfos.Add(name, info);
            }

            var manifestInfo = RefreshBundleInfo(target, target, outputPath);
            bundleInfos.Add(target, manifestInfo);

            var catalogInfo = new CatalogInfo()
            {
                version = version,
                bundles = bundleInfos,
            };

            return catalogInfo;
        }

        /// <summary>
        /// 清除資源包名稱
        /// </summary>
        private void ClearAssetNames()
        {
            string[] assetBundleNames = AssetDatabase.GetAllAssetBundleNames();

            foreach (string assetBundleName in assetBundleNames)
            {
                AssetDatabase.RemoveAssetBundleName(assetBundleName, true);
            }
        }

        /// <summary>
        /// 組織需要建立的資源包
        /// </summary>
        /// <returns>資源包路徑對應名稱的映射表</returns>
        private Dictionary<string, string> OrganizeNeedBuilds()
        {
            string[] assetPaths = AssetDatabase.GetAllAssetPaths();
            var pathNameMap = new Dictionary<string, string>();

            foreach (string assetPath in assetPaths)
            {
                if (CanBasicIgnore(assetPath))
                {
                    continue;
                }

                if (!assetPath.StartsWith(RootPath))
                {
                    continue;
                }

                if (Directory.Exists(assetPath))
                {
                    continue;
                }
                string assetLabel = SetBundleName(assetPath);
                pathNameMap.Add(assetPath, assetLabel);

                OrganizeDependencies(assetPath);
            }

            return pathNameMap;


            void OrganizeDependencies(string assetPath)
            {
                string[] dependencies = AssetDatabase.GetDependencies(assetPath);
                foreach (string path in dependencies)
                {
                    if (CanBasicIgnore(path))
                    {
                        continue;
                    }

                    string label = SetBundleName(path);
                    pathNameMap.Add(path, label);
                }
            }

            bool CanBasicIgnore(string path)
            {
                if (pathNameMap.ContainsKey(path))
                {
                    return true;
                }

                if (path.StartsWith(PackagePath))
                {
                    return true;
                }

                if (path.EndsWith(ComponentExtention))
                {
                    return true;
                }

                if (path.EndsWith(ShaderExtention))
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 設置輸出路徑
        /// </summary>
        /// <param name="target">平台</param>
        private void SetOutputPath(string target)
        {
            outputPath = string.Format(OutputFormat, Application.streamingAssetsPath, target);
        }

        /// <summary>
        /// 建立資源包
        /// </summary>
        /// <param name="outputPath">輸出路徑</param>
        private void BuildBundle(string outputPath)
        {
            Directory.CreateDirectory(outputPath);
            BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.None, buildTarget);
            LogMessage($"Build asset bundles: {outputPath}");
        }

        /// <summary>
        /// 刷新資源包資訊
        /// </summary>
        /// <param name="path">資源包路徑</param>
        /// <param name="name">資源包名稱</param>
        /// <param name="outputPath">輸出路徑</param>
        private BundleInfo RefreshBundleInfo(string path, string name, string outputPath)
        {
            var info = new BundleInfo()
            {
                name = name,
                path = path,
                assets = new List<string>() { path },
            };

            string bundlePath = Path.Combine(outputPath, name);
            info.hash = SimpleBundleFlowUtility.ComputeFileHash(bundlePath);

            info.size = new FileInfo(bundlePath).Length;
            return info;
        }

        /// <summary>
        /// 設置資源包名稱
        /// </summary>
        /// <param name="path">資源包路徑</param>
        private string SetBundleName(string path)
        {
            var importer = AssetImporter.GetAtPath(path);
            string label = path.Replace(PathSymbol, NewPathSymbol);
            label = label.Replace(FileSymbol, NewFileSymbol);
            importer.SetAssetBundleNameAndVariant(label, VariantName);
            return importer.assetBundleName;
        }

        /// <summary>
        /// 轉換路徑為名稱
        /// </summary>
        /// <param name="path">路徑</param>
        private string TransferPathToName(string path)
        {
            string label = path.Replace(PathSymbol, NewPathSymbol);
            label = label.Replace(FileSymbol, NewFileSymbol);
            label = label + Variant;
            return label;
        }

        /// <summary>
        /// 輸出一般日誌訊息
        /// </summary>
        /// <param name="message">日誌訊息</param>
        private static void LogMessage(string message)
        {
            SimpleBundleFlowUtility.LogMessage(message);
        }

        /// <summary>
        /// 輸出錯誤日誌訊息
        /// </summary>
        /// <param name="message">錯誤訊息</param>
        private static void LogError(string message)
        {
            SimpleBundleFlowUtility.LogError(message);
        }
    }
}

