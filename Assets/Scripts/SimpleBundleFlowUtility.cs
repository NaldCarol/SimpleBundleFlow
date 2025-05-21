using UnityEngine;
using System;
using System.IO;
using System.Security.Cryptography;

namespace SideProject.SimpleBundleFlow
{
    public static class SimpleBundleFlowUtility
    {
        private const string dash = "-";

        /// <summary>
        /// 從文件路徑讀取並反序列化JSON數據
        /// </summary>
        /// <typeparam name="T">要反序列化成的目標類型</typeparam>
        /// <param name="path">JSON文件路徑</param>
        /// <returns>反序列化後的對象，若文件不存在則返回null</returns>
        public static T GetJsonFromText<T>(string path) where T : class
        {
            T data = null;
            if (!File.Exists(path))
            {
                return data;
            }

            string rawContent = File.ReadAllText(path);
            data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(rawContent);

            return data;
        }

        /// <summary>
        /// 將對象序列化為JSON並寫入指定路徑
        /// </summary>
        /// <typeparam name="T">要序列化的對象類型</typeparam>
        /// <param name="path">目標目錄路徑</param>
        /// <param name="fileName">文件名</param>
        /// <param name="contentObject">要序列化的對象</param>
        public static void SetJsonToText<T>(string path, string fileName, T contentObject) where T : class
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = Path.Combine(path, fileName);
            string rawContent = Newtonsoft.Json.JsonConvert.SerializeObject(contentObject);
            File.WriteAllText(path, rawContent);
        }

        /// <summary>
        /// 計算文件的MD5雜湊值
        /// </summary>
        /// <param name="path">文件路徑</param>
        /// <returns>MD5雜湊值的十六進制字符串</returns>
        public static string ComputeFileHash(string path)
        {
            byte[] bytes;
            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    using (var hasher = MD5.Create())
                    {
                        bytes = hasher.ComputeHash(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"ComputeHash failed for {path}: {ex.Message}");
                bytes = Array.Empty<byte>();
            }

            string hashString = BitConverter.ToString(bytes);
            hashString = hashString.Replace(dash, string.Empty);
            hashString = hashString.ToLower();

            return hashString;
        }

        /// <summary>
        /// 輸出一般日誌訊息，附帶系統標示
        /// </summary>
        /// <param name="message">日誌訊息</param>
        public static void LogMessage(string message)
        {
            Debug.Log($"[SimpleBundleFlow] {message}");
        }

        /// <summary>
        /// 輸出警告日誌訊息，附帶系統標示
        /// </summary>
        /// <param name="message">警告訊息</param>
        public static void LogWarning(string message)
        {
            Debug.LogWarning($"[SimpleBundleFlow] {message}");
        }

        /// <summary>
        /// 輸出錯誤日誌訊息，附帶系統標示
        /// </summary>
        /// <param name="message">錯誤訊息</param>
        public static void LogError(string message)
        {
            Debug.LogError($"[SimpleBundleFlow] {message}");
        }
    }
}
