#define JSON

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Esp.VersionCheck.DataModule.Json;
using LitJson;

public class VersionTxtManager
{
    public static string VersionTxtName = "Version.txt";
    public static string VersionJsonFileName = "Version.json";

    [MenuItem("Tools/VersionManager/Create Version Txt")]
    public static void WriteVersion()
    {
        SaveVersion(PlayerSettings.bundleVersion, PlayerSettings.applicationIdentifier);
    }

    static void SaveVersion(string version, string package)
    {
#if JSON
        VersionInfoDataModule data = new VersionInfoDataModule();
        data.Version = PlayerSettings.bundleVersion;
        data.PackageName = PlayerSettings.applicationIdentifier;
        var content = JsonMapper.ToJson(data);
        
        var path = Application.streamingAssetsPath + "/LocalVersion";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        var fullPath = Path.Combine(path, VersionJsonFileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        File.WriteAllText(fullPath, content, Encoding.UTF8);

        Debug.Log("<color=yellow>" + "写入版本信息：" + content + "\nfullPath : "+ fullPath + "</color>");

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
}
