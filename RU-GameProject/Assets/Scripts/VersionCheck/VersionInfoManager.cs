using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class VersionTxtManager
{
    public static string VersionTxtName = "Version.txt";

    [MenuItem("Tools/VersionManager/Create Version Txt")]
    public static void WriteVersion()
    {
        SaveVersion(PlayerSettings.bundleVersion, PlayerSettings.applicationIdentifier);
    }

    static void SaveVersion(string version, string package)
    {
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
    }



}
