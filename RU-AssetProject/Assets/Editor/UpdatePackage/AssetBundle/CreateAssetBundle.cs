using System.Collections.Generic;
using UnityEditor;
using System.IO;
using UnityEngine;

public class CreateAssetBundle : MonoBehaviour
{
    private const string ASSET_BUNDLE_FILE_SAVE_LOCATION = Application.dataPath + "/../out/AssetBundle/" + PlatformInfoManager.GetCurrentPlatformPath();
    private const string EXTRACT_ZIP_CACHE_PATH = Application.dataPath + "/../out/ZipCache";
    private const string VERSION_MD5_PATH = Application.dataPath + "/../out/Version/" + GetTargetPlatformPath();
    private const string HOT_OUT_PATH = Application.dataPath + "/../out/Hot/" + GetTargetPlatformPath();

    //储存读出来MD5信息
    private static Dictionary<string, AssetBase> m_PackedMd5 = new Dictionary<string, AssetBase>();

    # region Path & File

    public void Clear_Files(string path)
    {
        if (Directory.Exists(path))
        {
            //获取该路径下的文件路径
            string[] filePathList = Directory.GetFiles(path);
            foreach (string filePath in filePathList)
            {
                File.Delete(filePath);
            }
        }
    }

    public static void Clear_Directors(string path)
    {
        if (Directory.Exists(path))
        {
            //获取该路径下的文件夹路径
            string[] directorsList = Directory.GetDirectories(path);
            foreach (string directory in directorsList)
            {
                Directory.Delete(directory, true);//删除该文件夹及该文件夹下包含的文件
            }
        }
        Debug.Log("清空目录 " + path);
    }


    /// <summary>
    /// 获取文件相对路径
    /// </summary>
    /// <param name="fullDic"></param>
    /// <param name="relativeFileName"></param>
    /// <returns></returns>
    public static string GetFileRelativePath(string fullName, string relativeFileName)
    {
        string[] fileArray = fullName.Split('\\');
        int index = -1;
        for (int i = 0; i < fileArray.Length; i++)
        {
            if (fileArray[i] == relativeFileName)
            {
                index = i;
            }
        }
        string RelativePath = "";
        for (int i = index + 1; i < fileArray.Length-1; i++)
        {
            RelativePath += fileArray[i];
            if (i != fileArray.Length - 2)
            {
                RelativePath += "/";
            }
        }
        return RelativePath;
    }

    /// <summary>
    /// 获取文件夹相对路径
    /// </summary>
    /// <param name="fullDic"></param>
    /// <param name="relativeFileName"></param>
    /// <returns></returns>
    public static string GetDirRelativePath(string fullDic,string relativeFileName)
    {
        string[] fileArray = fullDic.Split('\\');
        int index = -1;
        for (int i = 0; i < fileArray.Length; i++)
        {
            if (fileArray[i] == relativeFileName)
            {
                index = i;
            }
        }

        string RelativePath = "";
        for (int i = index + 1; i < fileArray.Length; i++)
        {
            RelativePath += fileArray[i];
            if (i != fileArray.Length - 1)
            {
                RelativePath += "/";
            }
        }
        return RelativePath;
    }

    #endregion

    #region Create a normal asset bundle.

    /// <summary>
    /// The tool to start the pipeline
    /// </summary>
    [MenuItem("Tools/AssetBundle/Create Normal AssetBundle")]
    static void StartBuildNormalPipeline()
    {
        Debug.Log("StartBuildNormalPipeline");
        BuildNormalAssetBundle();
    }

    public static void BuildNormalAssetBundle()
    {
        if (!Directory.Exists(ASSET_BUNDLE_FILE_SAVE_LOCATION))
        {
            Directory.CreateDirectory(ASSET_BUNDLE_FILE_SAVE_LOCATION);
        }

        BuildPipeline.BuildAssetBundles(
            ASSET_BUNDLE_FILE_SAVE_LOCATION,
            BuildAssetBundleOptions.UncompressedAssetBundle | BuildAssetBundleOptions.ForceRebuildAssetBundle,
            PlatformInfoManager.GetCurrentPlatformBuildTarget()
        );
    }
    #endregion

    #region Create a standard asset bundle.

    [MenuItem("Tools/AssetBundle/Create Standard AssetBundle")]
    static void StartBuildStandardPipeline()
    {
        Debug.Log("StartBuildStandardPipeline");
        BuildStandardAssetBundle();
    }

    public static void BuildStandardAssetBundle()
    {
        WriteABMD5();
    }

    #endregion

    #region 


    [MenuItem("Tools/AssetBundle/Create Hot Packages",false,10)]
    static void CreateHotBundle()
    {
        HotPackageDialog window = (HotPackageDialog) EditorWindow.GetWindow(typeof(HotPackageDialog), false, "热更包界面", true);
        window.Show();
    }



    #endregion

    #region MD5 Parsing
    public static void WriteABMD5()
    {
        if (!Directory.Exists(ASSET_BUNDLE_FILE_SAVE_LOCATION))
        {
            Directory.CreateDirectory(ASSET_BUNDLE_FILE_SAVE_LOCATION);
        }
        DirectoryInfo directoryInfo = new DirectoryInfo(ASSET_BUNDLE_FILE_SAVE_LOCATION);
        FileInfo[] files = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
        AssetsMd5 assetsMD5 = new AssetsMd5();
        assetsMD5.ABMD5List = new List<AssetBase>();
        for (int i = 0; i < files.Length; i++)
        {
            if (!files[i].Name.EndsWith(".meta"))
            {
                Debug.Log("标准包文件：" + files[i].Name);
                AssetBase assetBase = new AssetBase();
                assetBase.Name = files[i].Name;
                assetBase.Md5 = MD5Manager.Instance.BuildFileMd5(files[i].FullName);
                assetBase.Size = files[i].Length / 1024.0f;
                //assetBase.UnpackPath = GetFileRelativePath(files[i].FullName, GetTargetPlatformPath());
                assetsMD5.ABMD5List.Add(assetBase);
            }
        }
        string ABMD5Path = Application.dataPath + "/Resources/AssetsMD5.bytes";
        BinarySerializeOpt.BinarySerilize(ABMD5Path, assetsMD5);
        //将打版的版本拷贝到外部进行储存
        if (!Directory.Exists(VERSION_MD5_PATH))
        {
            Directory.CreateDirectory(VERSION_MD5_PATH);
        }
        string targetPath = VERSION_MD5_PATH + "/AssetsMd5_" + PlayerSettings.bundleVersion + ".bytes";
        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }
        File.Copy(ABMD5Path, targetPath);
        Debug.Log("<color=yellow>" + "WriteAssetsMd5 : " + targetPath + "</color>");
    }

    public static void ReadMd5Com(string abmd5Path, string hotCount, string des)
    {
        m_PackedMd5.Clear();
        using (FileStream fileStream = new FileStream(abmd5Path, FileMode.Open, FileAccess.Read))
        {
            BinaryFormatter bf = new BinaryFormatter();
            AssetsMd5 assetsMd5 = bf.Deserialize(fileStream) as AssetsMd5;
            foreach (AssetBase abmd5Base in assetsMd5.ABMD5List)
            {
                m_PackedMd5.Add(abmd5Base.Name, abmd5Base);
            }
        }

        List<FileInfoExtend> changeList = new List<FileInfoExtend>();

        DirectoryInfo directory = new DirectoryInfo(ASSET_BUNDLE_FILE_SAVE_LOCATION);
        FileInfo[] files = directory.GetFiles("*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            EditorUtility.DisplayProgressBar("检查热更文件", "正在检查" + files[i].Name + "... ...", 1.0f / files.Length * i);
            if (!files[i].Name.EndsWith(".meta"))
            {
                string name = files[i].Name;
                //新生成文件的MD5
                string md5 = MD5Manager.Instance.BuildFileMd5(files[i].FullName);
                AssetBase assetBase = null;

                if (!m_PackedMd5.ContainsKey(name))
                {
                    string RelativePath = GetDirRelativePath(files[i].DirectoryName, GetTargetPlatformPath());
                    changeList.Add(new FileInfoExtend(files[i], RelativePath));
                    Debug.Log("<color=yellow>" + "发现新增热更文件 ： " + name + "</color>");
                }
                else
                {
                    //以前的文件对比MD5是否相同
                    if (m_PackedMd5.TryGetValue(name, out assetBase))
                    {
                        if (md5 != assetBase.Md5)
                        {
                            string RelativePath = GetDirRelativePath(files[i].DirectoryName, GetTargetPlatformPath());

                            changeList.Add(new FileInfoExtend(files[i], RelativePath));
                            Debug.Log("<color=yellow>" + "发现修改热更文件 ： " + name + "</color>");
                        }
                    }
                }
            }
        }
        UnpackChangeZip(changeList, hotCount);
        CopyABAndGeneratXml(bundleUnPackMd5Path, changeList, hotCount , des);        
    }
    
    public static void WriteUnpackFileMD5(string hotCount)
    {
        Debug.Log("检查内部文件MD5");
        DirectoryInfo directoryInfo = new DirectoryInfo(EXTRACT_ZIP_CACHE_PATH);
        DirectoryInfo[] diA = directoryInfo.GetDirectories();//获得了所有一级子目录
        ZipMd5Data data = new ZipMd5Data();
        data.ZipMd5List = new List<ZipMd5>();


        for (int i = 0; i < diA.Length; i++)
        {
            EditorUtility.DisplayProgressBar("解压至缓存", "正在解压" + diA[i].Name + "... ...", 1.0f / diA.Length * i);

            ZipMd5 zipMd5 = new ZipMd5();
            zipMd5.ZipName = diA[i].Name;
            zipMd5.FileList = new List<ZipBase>();
            FileInfo[] files = diA[i].GetFiles("*", SearchOption.AllDirectories);
            for (int j = 0; j < files.Length; j++)
            {
                if (!files[j].Name.EndsWith(".meta"))
                {
                    ZipBase zipBase = new ZipBase();
                    zipBase.Name = files[j].Name;
                    zipBase.Md5 = MD5Manager.Instance.BuildFileMd5(files[j].FullName);
                    //Debug.Log(diA[i].Name);
                    zipBase.ZipName = diA[i].Name;
                    zipBase.UnpackPath = GetFileRelativePath(files[j].FullName, diA[i].Name);
                    zipMd5.FileList.Add(zipBase);
                }
            }
            data.ZipMd5List.Add(zipMd5);
        }

        string unPackCachepath = EXTRACT_ZIP_CACHE_PATH + "/UnpackMd5_" + PlayerSettings.bundleVersion + "_" + hotCount + ".xml";
        //BinarySerializeOpt.BinarySerilize(unPackCachepath, data);
        BinarySerializeOpt.Xmlserialize(unPackCachepath, data);

        bundleUnPackMd5Path = ASSET_BUNDLE_FILE_SAVE_LOCATION + "/UnpackMd5_" + PlayerSettings.bundleVersion+ "_" + hotCount + ".xml";
        if (File.Exists(bundleUnPackMd5Path))
        {
            File.Delete(bundleUnPackMd5Path);
        }
        File.Copy(unPackCachepath, bundleUnPackMd5Path);

        Debug.Log("<color=yellow>" + "Zip内部文件MD5写入 : " + bundleUnPackMd5Path + "</color>");
    }


    #endregion

    #region Zip Parsing
    public static void UnpackChangeZip(List<FileInfoExtend> changeList, string hotCount)
    {
        Debug.Log("开始检查ZIP内部文件");
        if (!Directory.Exists(EXTRACT_ZIP_CACHE_PATH))
        {
            Directory.CreateDirectory(EXTRACT_ZIP_CACHE_PATH);
        }
        else
        {
            Clear_Directors(EXTRACT_ZIP_CACHE_PATH);
        }

        DirectoryInfo di = new DirectoryInfo(ASSET_BUNDLE_FILE_SAVE_LOCATION);
        FileInfo[] fiA = di.GetFiles(); //获得了所有起始目录下的文件

        for (int i = 0; i < changeList.Count; i++)
        {
            EditorUtility.DisplayProgressBar("解压至缓存", "正在解压" + changeList[i].FileInfo.Name + "... ...", 1.0f / changeList.Count * i);

            if (changeList[i].FileInfo.Name.EndsWith(".zip"))
            {
                string dicName = changeList[i].FileInfo.Name;
                //Debug.Log(" 解压至缓存  " + EXTRACT_ZIP_CACHE_PATH + "/" + dicName);
                Util_Zip.ExtractZip(changeList[i].FileInfo.FullName, EXTRACT_ZIP_CACHE_PATH + "/" + dicName);
            }
        }

        Debug.Log(" 全部changeList zip解压至缓存  ");

        WriteUnpackFileMD5(hotCount);
    }

    #endregion

    #region Asset Bundle version info xml

    /// <summary>
    /// 拷贝筛选的AB包及自动生成服务器配置表
    /// </summary>
    /// <param name="changeList"></param>
    /// <param name="hotCount"></param>
    static void CopyABAndGeneratXml(string bundleUnPackMd5Path, List<FileInfoExtend> changeList, string hotCount,string des = "")
    {
        if (!Directory.Exists(HOT_OUT_PATH))
        {
            Directory.CreateDirectory(HOT_OUT_PATH);
        }
        
        Clear_Directors(HOT_OUT_PATH);
        Pathces pathces = new Pathces();
        pathces.Version = int.Parse(hotCount);
        pathces.Des = des;
        pathces.Files = new List<Patch>();

        ////unpack.bytes////
        FileInfo unpackFileInfo = new FileInfo(bundleUnPackMd5Path);
        string RelativePath = GetDirRelativePath(unpackFileInfo.DirectoryName, GetTargetPlatformPath());
        FileInfoExtend unpackFileInfoExtend = new FileInfoExtend(unpackFileInfo, RelativePath);
        string unpackHotDic = "";

        if (unpackFileInfoExtend.RelativePath == "")
        {
            unpackHotDic = PlayerSettings.bundleVersion + "/" + hotCount;
        }
        else
        {
            unpackHotDic = PlayerSettings.bundleVersion + "/" + hotCount + "/" + unpackFileInfoExtend.RelativePath;
        }

        string dest = HOT_OUT_PATH + "/" + unpackHotDic +"/" + unpackFileInfoExtend.FileInfo.Name;

        if (!Directory.Exists(HOT_OUT_PATH + "/" + unpackHotDic))
        {
            Directory.CreateDirectory(HOT_OUT_PATH + "/" + unpackHotDic);
        }
        File.Copy(sourceFileName: unpackFileInfoExtend.FileInfo.FullName, destFileName: dest);

        Patch unpackPatch = new Patch();
        unpackPatch.Md5 = MD5Manager.Instance.BuildFileMd5(dest);
        unpackPatch.Name = unpackFileInfoExtend.FileInfo.Name;
        unpackPatch.Size = unpackFileInfoExtend.FileInfo.Length / 1024.0f;
        unpackPatch.Platform = GetTargetPlatformPath();
        unpackPatch.Url = GetServerUrl() + GetTargetPlatformPath() + "/" + unpackHotDic + "/" + unpackFileInfoExtend.FileInfo.Name;
        pathces.Files.Add(unpackPatch);
        File.Delete(unpackFileInfoExtend.FileInfo.FullName);
        ///////////////////

        for (int i = 0; i < changeList.Count; i++)
        {
            EditorUtility.DisplayProgressBar("生成服务器配置表", "正在生成" + changeList[i].FileInfo.Name + "... ...", 1.0f / changeList.Count * i);

            if (changeList[i] != null)
            {
                string hotDic = "";
                if (changeList[i].RelativePath == "")
                {
                    hotDic = PlayerSettings.bundleVersion + "/" + hotCount;
                }
                else
                {
                    hotDic = PlayerSettings.bundleVersion + "/" + hotCount + "/" + changeList[i].RelativePath;
                }

                if (!Directory.Exists(HOT_OUT_PATH + "/" + hotDic))
                {
                    Directory.CreateDirectory(HOT_OUT_PATH + "/" + hotDic);
                }
                if (File.Exists(HOT_OUT_PATH + "/" + hotDic + "/" + changeList[i].FileInfo.Name))
                {
                    File.Delete(HOT_OUT_PATH + "/" + hotDic + "/" + changeList[i].FileInfo.Name);
                }

                File.Copy(sourceFileName: changeList[i].FileInfo.FullName, destFileName: HOT_OUT_PATH + "/" + hotDic + "/" + changeList[i].FileInfo.Name);

                Patch patch = new Patch();
                patch.Md5 = MD5Manager.Instance.BuildFileMd5(changeList[i].FileInfo.FullName);
                patch.Name = changeList[i].FileInfo.Name;
                patch.Size = changeList[i].FileInfo.Length / 1024.0f;
                patch.Platform = GetTargetPlatformPath();
                patch.Url = GetServerUrl() + GetTargetPlatformPath() + "/" + hotDic + "/" + changeList[i].FileInfo.Name;
                patch.RelativePath = changeList[i].RelativePath;
                pathces.Files.Add(patch);
            }
        }

        string patchPath = HOT_OUT_PATH + "/" + PlayerSettings.bundleVersion + "/" + hotCount + "/Patch.xml";
        BinarySerializeOpt.Xmlserialize(patchPath, pathces);
        EditorUtility.ClearProgressBar();
        Debug.Log("<color=yellow>"+"生成新的服务器配置表 ： " + patchPath + " </color>");
    }
    #endregion
}