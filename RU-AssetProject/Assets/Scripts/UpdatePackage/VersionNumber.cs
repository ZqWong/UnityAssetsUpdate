using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class VersionNumber : MonoBehaviour
{
    [MenuItem("Tools/CreateVersionFile")]
    public static void CreateVersionFile()
    {
        SaveVersionNum(PlayerSettings.bundleVersion, PlayerSettings.applicationIdentifier);
    }

    /// <summary>
    /// Save the asset version num
    /// </summary>
    /// <param name="version">project version</param>
    /// <param name="package">company name & product name</param>
    public static void SaveVersionNum(string version, string package)
    {
        var contentFormat = "Version|{0};PackageName|{1};";
        string content = string.Format(contentFormat, version, package);
        string savePath = Application.dataPath + "/Resources/Version.txt";
        string all = "";
        string oneLine = "";
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
                all = string.IsNullOrEmpty(all) ? content : all.Replace(oneLine, content);
                sw.Write(all);
            }
        }
        Debug.Log("[VersionNumber] SaveVersionNum Create a version num file - version :" + version + " package :" + package);
    }
}
