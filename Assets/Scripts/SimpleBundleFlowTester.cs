using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace SideProject.SimpleBundleFlow
{
    public class SimpleBundleFlowTester : MonoBehaviour
    {
        [Header("測試參數")]
        public string testAssetPath = "TestPrefab.prefab";
        public string testScenePath = "TestLoadScene.unity";

        private Transform contentParent;
        private GameObject testAsset;
        private bool isInitialized = false;
        private string statusMessage = "Ready, please click buttons to test functions";
        private float downloadProgress = 0f;

        private void Awake()
        {
            contentParent = transform;
        }

        private void OnGUI()
        {
            GUI.skin.button.fontSize = 14;
            GUI.skin.label.fontSize = 14;

            float areaWidth = 400;
            float areaHeight = 500;
            float areaX = (Screen.width - areaWidth) / 2;
            float areaY = (Screen.height - areaHeight) / 2;

            GUI.Box(new Rect(areaX - 10, areaY - 10, areaWidth + 20, areaHeight + 20), "SimpleBundleFlow Tester");

            GUILayout.BeginArea(new Rect(areaX, areaY, areaWidth, areaHeight));

            GUILayout.Space(10);

            var statusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            GUILayout.Label(statusMessage, statusStyle, GUILayout.Height(30));

            GUILayout.Space(10);

            if (downloadProgress > 0)
            {
                GUILayout.Label($"Download Progress: {downloadProgress * 100:F0}%", statusStyle);
                Rect progressRect = GUILayoutUtility.GetRect(areaWidth - 40, 20);
                GUI.Box(progressRect, "");
                GUI.Box(new Rect(progressRect.x, progressRect.y, progressRect.width * downloadProgress, progressRect.height), "");
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Download Asset Bundle", GUILayout.Height(40)))
            {
                OnDownload();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Load Test Asset", GUILayout.Height(40)))
            {
                OnLoadAsset();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Load Test Scene", GUILayout.Height(40)))
            {
                OnLoadScene();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Instantiate Test Prefab", GUILayout.Height(40)))
            {
                OnInstantiate();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Release Test Asset", GUILayout.Height(40)))
            {
                OnRelease();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Clear Cache", GUILayout.Height(40)))
            {
                OnClearCache();
            }

            GUILayout.EndArea();
        }

        private void OnDownload()
        {
            UpdateStatus("Starting asset bundle download...");
            downloadProgress = 0.01f;
            SimpleBundleFlow.Instance.DownloadBundle(OnProgress, OnComplete, OnFailed);

            void OnProgress(float current, float total)
            {
                downloadProgress = current / total;
                UpdateStatus($"Downloading: {current}/{total}");
            }

            void OnComplete()
            {
                UpdateStatus("Asset bundle download completed!");
                isInitialized = true;
            }

            void OnFailed()
            {
                UpdateStatus("Asset bundle download failed!");
                isInitialized = false;
            }
        }

        private void OnLoadAsset()
        {
            if (!CheckInitialized())
            {
                return;
            }

            UpdateStatus("Loading asset...");
            SimpleBundleFlow.Instance.LoadAsset<GameObject>(testAssetPath, OnComplete, OnFailed);

            void OnComplete(GameObject asset)
            {
                UpdateStatus($"Asset loaded: {asset.name}");
            }

            void OnFailed()
            {
                UpdateStatus($"Asset load failed, {testAssetPath}");
            }
        }

        private void OnLoadScene()
        {
            if (!CheckInitialized())
            {
                return;
            }

            UpdateStatus("Loading scene...");
            SimpleBundleFlow.Instance.LoadScene(testScenePath, OnComplete, OnFailed);

            void OnComplete(string scenePath)
            {
                if (scenePath == null)
                {
                    UpdateStatus("Scene load failed");
                    return;
                }
#if UNITY_EDITOR
                var parameter = new LoadSceneParameters(LoadSceneMode.Single);
                EditorSceneManager.LoadSceneInPlayMode(scenePath, parameter);
#else
                UnityEngine.SceneManagement.SceneManager.LoadScene(scenePath);
#endif
                UpdateStatus($"Scene loaded: {scenePath}");
            }

            void OnFailed()
            {
                UpdateStatus($"Scene load failed, {testScenePath}");
            }
        }

        private void OnInstantiate()
        {
            if (!CheckInitialized())
            {
                return;
            }

            UpdateStatus("Instantiating prefab...");
            SimpleBundleFlow.Instance.InstantiatePrefab(testAssetPath, contentParent, OnComplete, OnFailed);

            void OnComplete(GameObject obj)
            {
                if (obj == null)
                {
                    UpdateStatus("Instantiation failed");
                }
                else
                {
                    testAsset = obj;
                    UpdateStatus($"Prefab instantiated: {obj.name}");
                }
            }

            void OnFailed()
            {
                UpdateStatus($"Prefab instantiation failed, {testAssetPath}");
            }
        }

        private void OnRelease()
        {
            if (testAsset != null)
            {
                SimpleBundleFlow.Instance.ReleaseAsset(testAsset);
                Destroy(testAsset);
                testAsset = null;
                UpdateStatus("Asset released");
            }
            else
            {
                UpdateStatus("No asset to release");
            }
        }

        private void OnClearCache()
        {
            SimpleBundleFlow.Instance.ClearCache();
            UpdateStatus("Cache cleared");
            isInitialized = false;
        }

        private bool CheckInitialized()
        {
            if (!isInitialized)
            {
                UpdateStatus("Please download asset bundle first");
                return false;
            }
            return true;
        }

        private void UpdateStatus(string message)
        {
            statusMessage = message;
            Debug.Log($"[SimpleBundleFlowTester] {message}");
        }
    }
}
