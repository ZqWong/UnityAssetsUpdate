using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text;
using Esp.VersionCheck.DataModule.Json;
using UnityEditor;
using UnityEngine.Networking;
using LitJson;

namespace Esp.VersionCheck.LocalVersionInfo
{

    public class LocalVersionInfoManager : MonoBehaviour
    {
        public const string LAUNCH_PROJECT_NAME_FORMAT = "{0}_{1}";

        private const string CONFIG_FILE_PATH = "/LocalVersion";
        private const string FILE_NAME = "/ResourcesVersion.json";

        public Action OnJsonFileCopyAllReady;

        private string m_persistentDataPath;

        private string m_streamingAssetsPath;

        private string jsonString;
        private ItemData m_itemDate = new ItemData();
        public ItemData itemData
        {
            get
            {
                return m_itemDate;
            }
            set
            {
                m_itemDate = value;
            }
        }

        private static LocalVersionInfoManager instance;

        public static LocalVersionInfoManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameObject("LocalVersionInfoManager").AddComponent<LocalVersionInfoManager>();
                }
                return instance;
            }
        }

        public void Initialize()
        {

            m_persistentDataPath = Application.persistentDataPath + CONFIG_FILE_PATH + FILE_NAME;
            m_streamingAssetsPath = Application.streamingAssetsPath + CONFIG_FILE_PATH + FILE_NAME;

            if (!File.Exists(m_persistentDataPath))
            {
                if (!Directory.Exists(Application.persistentDataPath + CONFIG_FILE_PATH))
                {
                    Directory.CreateDirectory(Application.persistentDataPath + CONFIG_FILE_PATH);
                }
                ItemData LocalVersionInfoData = new ItemData();

                var content = JsonMapper.ToJson(LocalVersionInfoData);
                File.WriteAllText(m_persistentDataPath, content, Encoding.UTF8);
                Debug.Log("<color=yellow>" + "写入资源版本信息：" + content + "\nfullPath : " + m_persistentDataPath + "</color>");
            }
            ReadJsonFile();
        }


        public IEnumerator CopyConfigFileToWriteablePath(string srcFileName, string destFileName)
        {
#if UNITY_ANDROID

#else
            destFileName = @"file:///" + destFileName;
#endif
            UnityWebRequest unityWebRequest = new UnityWebRequest(srcFileName, UnityWebRequest.kHttpVerbGET);
            unityWebRequest.downloadHandler = new DownloadHandlerFile(destFileName);
            yield return unityWebRequest.SendWebRequest();
        }

        //public string LaunchedProjectNameForLocalVersion
        //{
        //    get { return string.Format(LAUNCH_PROJECT_NAME_FORMAT, SystemConfig.Instance.ProjectItemName, CourseConfig.Instance.SimulatorType); }
        //}



        private void ReadJsonFile()
        {
            StreamReader jsonStream = new StreamReader(m_persistentDataPath);
            string jsonString = jsonStream.ReadToEnd();
            jsonStream.Close();
            itemData = JsonUtility.FromJson<ItemData>(jsonString);

            Debug.Log("item data :" + itemData.LocalVersionInfoData.Count);

            if (OnJsonFileCopyAllReady != null)
            {
                OnJsonFileCopyAllReady();
            }
        }

        public void UpdateVersionInfo(string gameVersion, string branchName, string assetVersion)
        {
            var matched = itemData.LocalVersionInfoData.FirstOrDefault(i => i.GameVersion == gameVersion && i.BranchName == branchName);;
            if (null == matched)
            {
                LocalVersionInfoDataModule localVersionInfoDataModule = new LocalVersionInfoDataModule();
                localVersionInfoDataModule.GameVersion = gameVersion;
                localVersionInfoDataModule.BranchName = branchName;
                localVersionInfoDataModule.AssetVersion = assetVersion;
                itemData.LocalVersionInfoData.Add(localVersionInfoDataModule);
              
            }
            else
            {
                matched.AssetVersion = assetVersion;
            }
            Serailize(itemData);
        }

        public string GetVersionInfo(string gameVersion, string branchName)
        {
            string version = "";
            foreach (var item in itemData.LocalVersionInfoData)
            {
                if (item.GameVersion == gameVersion && item.BranchName == branchName)
                    version = item.AssetVersion;
            }
            if (version == "") return null;
            return version;
        }

        private void Serailize(ItemData item)
        {
            string jsonPath = m_persistentDataPath;
            string json = JsonUtility.ToJson(item);
            File.WriteAllText(jsonPath, json);
        }

        public void CheckTheConfigFileExists()
        {
            if (!File.Exists(m_persistentDataPath))
            {
                //File.Copy(StreamingAssetsPath + s_CONFIG_FILE_PATH, StreamingAssetsPath + s_CONFIG_FILE_PATH, true);
            }
        }

        IEnumerator ReadData(string path, Action<byte[]> action)
        {
            WWW www = new WWW(path);
            yield return www;
            while (www.isDone == false)
            {
                yield return new WaitForEndOfFrame();
            }
            action(www.bytes);
            yield return new WaitForEndOfFrame();
        }
    }
}
