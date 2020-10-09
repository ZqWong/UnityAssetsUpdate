#define JSON

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Esp.Assets.Scripts.Utils.Core.StaticJsonFile.VersionInfoData.DataModule;
using Esp.Scripts.Utils.Core.StaticJsonFile.Utils;

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
        FileUtils.WriteJSONDataInFile(data, Application.dataPath + "/Resources/" + VersionJsonFileName);

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
