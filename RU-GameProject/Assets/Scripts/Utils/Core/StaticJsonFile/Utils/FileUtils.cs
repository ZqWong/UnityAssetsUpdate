using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;


namespace RU.Scripts.Utils.Core.StaticJsonFile.Utils
{

    public static class FileUtils
    {
#if UNITY_STANDALONE
        private static void OpenInMacFileBrowser(string path)
        {
            bool openInsidesOfFolder = false;
            // try mac
            string macPath = path.Replace("\\", "/"); // mac finder doesn't like backward slashes

            if (Directory.Exists(macPath)) // if path requested is a folder, automatically open insides of that folder
            {
                openInsidesOfFolder = true;
            }

            //Debug.Log("macPath: " + macPath);
            //Debug.Log("openInsidesOfFolder: " + openInsidesOfFolder);

            if (!macPath.StartsWith("\""))
            {
                macPath = "\"" + macPath;
            }

            if (!macPath.EndsWith("\""))
            {
                macPath = macPath + "\"";
            }

            string arguments = (openInsidesOfFolder ? "" : "-R ") + macPath;
            Debug.Log("arguments: " + arguments);

            try
            {
                System.Diagnostics.Process.Start("open", arguments);
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                // tried to open mac finder in windows
                // just silently skip error
                // we currently have no platform define for the current OS we are in, so we resort to this
                e.HelpLink = ""; // do anything with this variable to silence warning about not using it
            }
        }

        private static void OpenInWinFileBrowser(string path)
        {
            bool openInsidesOfFolder = false;
            // try windows
            string winPath = path.Replace("/", "\\"); // windows explorer doesn't like forward slashes

            if (Directory.Exists(winPath)) // if path requested is a folder, automatically open insides of that folder
            {
                openInsidesOfFolder = true;
            }

            try
            {
                System.Diagnostics.Process.Start("explorer.exe",
                    (openInsidesOfFolder ? "/root," : "/select,") + winPath);
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                // tried to open win explorer in mac
                // just silently skip error
                // we currently have no platform define for the current OS we are in, so we resort to this
                e.HelpLink = ""; // do anything with this variable to silence warning about not using it
            }
        }

        public static void OpenInFileBrowser(string path)
        {
            if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
            {
                OpenInMacFileBrowser(path);
            }
            else if (Application.platform == RuntimePlatform.WindowsPlayer ||
                     Application.platform == RuntimePlatform.WindowsEditor)
            {
                OpenInWinFileBrowser(path);
            }
        }
#endif

        public const string PATH_SEPARATOR = "/";

        public static string CombinePath(string parent, string child)
        {
            if (null == parent)
            {
                parent = string.Empty;
            }

            if (null == child)
            {
                child = string.Empty;
            }

            int maximumLength = parent.Length + child.Length + 1;
            System.Text.StringBuilder builder = new System.Text.StringBuilder(maximumLength);
            builder.Append(parent);
            if (!parent.EndsWith(PATH_SEPARATOR) && !child.StartsWith(PATH_SEPARATOR))
            {
                builder.Append(PATH_SEPARATOR);
            }

            builder.Append(child);

            return builder.ToString();
        }

        public static T DecryptJSONDataFromFile<T>(string filePath) where T : class
        {
            T data = null;
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    string jsonText = System.IO.File.ReadAllText(filePath);
                    //byte[] encryptArr = Convert.FromBase64String(jsonText);
                    //string encryptStr = LocalCryptoGraphy.Decrypt(encryptArr);
                    data = LitJson.JsonMapper.ToObject<T>(jsonText);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Failed reading user data from: " + filePath + "\nerror: " + ex.ToString());
                }
            }
            return data;
        }

        public static void EncryptJSONDataInFile<T>(T data, string filePath)
        {
            try
            {
                string userDataText = LitJson.JsonMapper.ToJson(data);
#if DEVELOPMENT
			string ext = Path.GetExtension(filePath);
			string debugPath = filePath.Substring(0, filePath.Length - ext.Length) + ".clear" + ext;
			File.WriteAllText(debugPath, userDataText);
#endif
                FileUtils.DeleteFileIfExists(filePath);
                //byte[] encryptArr = LocalCryptoGraphy.Encrypt(userDataText);
                //string encryptStr = Convert.ToBase64String(encryptArr);
                File.WriteAllText(filePath, userDataText);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed writing user data to: " + filePath + "\nerror: " + ex.ToString());
            }
        }

        public static string EnsureToGetPersistentDataDirectory(string directoryName)
        {
            string directoryDataPath = Path.Combine(Application.persistentDataPath, directoryName);
            if (CreateDirectoryIfNotExists(directoryDataPath))
            {
#if UNITY_IPHONE && !UNITY_EDITOR
			Debugx.Log("Calling IPhone.SetNoBackupFlag @: " + directoryDataPath);
#if UNITY_5
			UnityEngine.iOS.Device.SetNoBackupFlag(directoryDataPath);
#else
			iPhone.SetNoBackupFlag(directoryDataPath);
#endif
#endif
            }

            return directoryDataPath;
        }

        public static bool CreateDirectoryIfNotExists(string directoryPath)
        {
            bool result = !Directory.Exists(directoryPath);
            if (result)
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                }
                catch (IOException ex)
                {
                    Debug.LogError("Failed creating directory @ " + directoryPath + ":\n" + ex.ToString());
                }
            }

            return result;
        }

        public static void DeleteFileIfExists(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public static void WriteAllText(string path, string contents, System.Text.Encoding encoding)
        {
            using (var streamWriter = new System.IO.StreamWriter(path, false, encoding))
            {
                streamWriter.Write(contents);
            }
        }

        public static string[] GetFileNameListInPath(string path, string pattern = "")
        {
            if (!Directory.Exists(path))
            {
                return null;
            }

            return Directory.GetFiles(path, pattern);
        }
    }
}