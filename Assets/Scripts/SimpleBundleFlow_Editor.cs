using UnityEngine;
using System;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif
namespace SideProject.SimpleBundleFlow
{
    public sealed partial class SimpleBundleFlow
    {
#if UNITY_EDITOR
        public static float SimulateLoadTime = 0.05f;

        /// <summary>
        /// 編輯器模式下加載資源的模擬方法
        /// </summary>
        /// <typeparam name="T">資源類型</typeparam>
        /// <param name="path">資源路徑</param>
        /// <param name="callback">加載成功回呼</param>
        public void LoadAssetForEditor<T>(string path, Action<T> callback) where T : UnityEngine.Object
        {
            StartCoroutine(Run(path, callback));

            static IEnumerator Run(string path, Action<T> callback)
            {
                string fullPath = $"Assets/AssetBundles/{path}";
                T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(fullPath);

                if (asset == null)
                {
                    throw new SimpleBundleFlowException($"Asset not found: {fullPath}");
                }

                yield return new WaitForSeconds(SimulateLoadTime);
                callback?.Invoke(asset);
            }
        }

        /// <summary>
        /// 編輯器模式下加載場景的模擬方法
        /// </summary>
        /// <param name="path">場景路徑</param>
        /// <param name="callback">加載成功回呼</param>
        public void LoadSceneForEditor(string path, Action<string> callback)
        {
            StartCoroutine(Run(path, callback));

            static IEnumerator Run(string path, Action<string> callback)
            {
                string filePath = $"Assets/AssetBundles/{path}";

                if (!System.IO.File.Exists(filePath))
                {
                    throw new SimpleBundleFlowException($"Scene not found: {filePath}");
                }

                yield return new WaitForSeconds(SimulateLoadTime);
                string fullPath = $"Assets/AssetBundles/{path}";

                callback?.Invoke(fullPath);
            }
        }

        /// <summary>
        /// 編輯器模式下實例化預製件的模擬方法
        /// </summary>
        /// <param name="path">預製件路徑</param>
        /// <param name="parent">父物件Transform</param>
        /// <param name="callback">實例化成功回呼</param>
        public void InstantiatePrefabForEditor(string path, Transform parent = null, Action<GameObject> callback = null)
        {
            StartCoroutine(Run(path, parent, callback));

            static IEnumerator Run(string path, Transform parent, Action<GameObject> callback)
            {
                string fullPath = $"Assets/AssetBundles/{path}";
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);

                if (prefab == null)
                {
                    throw new SimpleBundleFlowException($"Prefab not found: {fullPath}");
                }

                yield return new WaitForSeconds(SimulateLoadTime);
                GameObject instance = Instantiate(prefab, parent);
                callback?.Invoke(instance);
            }
        }
#endif
    }
}
