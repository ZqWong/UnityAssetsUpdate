using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Esp.VersionCheck.DataModule.Json;
using Esp.VersionCheck.LocalVersionInfo;
using LitJson;

public class VersionTxtManager
{
    //private static string VersionTxtName = "AppVersion.txt";
    private static string VersionJsonFileName = "AppVersion.json";
    private static string s_resourceVersionFileName = "ResourcesVersion.json";

    private static string s_filePath = Application.streamingAssetsPath + "/LocalVersion";

    [MenuItem("Tools/VersionManager/Create APP Version File")]
    public static void WriteAppVersion()
    {
        SaveAppVersion(PlayerSettings.bundleVersion, PlayerSettings.applicationIdentifier);
    }

    static void SaveAppVersion(string version, string package)
    {
#if JSON
        VersionInfoDataModule data = new VersionInfoDataModule();
        data.Version = PlayerSettings.bundleVersion;
        data.PackageName = PlayerSettings.applicationIdentifier;
        var content = JsonMapper.ToJson(data);
        
        if (!Directory.Exists(s_filePath))
        {
            Directory.CreateDirectory(s_filePath);
        }
        var fullPath = Path.Combine(s_filePath, VersionJsonFileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        File.WriteAllText(fullPath, content, Encoding.UTF8);

        Debug.Log("<color=yellow>" + "写入App版本信息：" + content + "\nfullPath : "+ fullPath + "</color>");

#elif XML
        string content = "Version|" + version + ";PackageName|" + package + ";";
        string savePathDirectory = Application.dataPath + "/Resources";
        string savePath = savePathDirectory + "/" + VersionTxtName;
        if (!Directory.Exists(savePathDirectory))
        {
            Directory.CreateDirectory(savePathDirectory);
        }
        string oneLine = "";
        string all = "";
        using (FileStream fs = new FileStream(savePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            using (StreamReader sr = new StreamReader(fs, System.Text.Encoding.UTF8))
            {
                all = sr.ReadToEnd();
                oneLine = all.Split('\r')[0];
            }
        }
        using (FileStream fs = new FileStream(savePath, FileMode.OpenOrCreate))
        {
            using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8))
            {
                if (string.IsNullOrEmpty(all))
                {
                    all = content;
                }
                else
                {
                    all = all.Replace(oneLine, content);
                }
                sw.Write(all);
                Debug.Log("<color=yellow>" + "写入版本信息：" + all + "</color>");
            }
        }
#endif
    }

    [MenuItem("Tools/VersionManager/Create Resources Version File")]
    static void WriteResourceVersion()
    {
        ItemData LocalVersionInfoData = new ItemData();

        //List<LocalVersionInfoDataModule> LocalVersionInfoData = new List<LocalVersionInfoDataModule>();
        //LocalVersionInfoData.Add(new LocalVersionInfoDataModule());
        var content = JsonMapper.ToJson(LocalVersionInfoData);

        if (!Directory.Exists(s_filePath))
        {
            Directory.CreateDirectory(s_filePath);
        }
        var fullPath = Path.Combine(s_filePath, s_resourceVersionFileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        File.WriteAllText(fullPath, content, Encoding.UTF8);

        Debug.Log("<color=yellow>" + "写入资源版本信息：" + content + "\nfullPath : " + fullPath + "</color>");
    }

}
