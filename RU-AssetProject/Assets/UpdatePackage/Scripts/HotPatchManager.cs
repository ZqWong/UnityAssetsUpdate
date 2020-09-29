using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;

public class HotPatchManager : Singleton<HotPatchManager>
{
    private MonoBehaviour m_Mono;
    private string m_UnPackPath = Application.persistentDataPath + "/Origin";
    private string m_DownLoadPath = Application.persistentDataPath + "/DownLoad";
    private string m_CurVersion;
    public string CurVersion
    {
        get { return m_CurVersion; }
    }
    private string m_CurPackName;
    private string m_ServerXmlPath = Application.persistentDataPath + "/ServerInfo.xml";
    private string m_LocalXmlPath = Application.persistentDataPath + "/LocalInfo.xml";
    private ServerInfo m_ServerInfo;
    private ServerInfo m_LocalInfo;
    private VersionInfo m_GameVersion;
    //当前热更Patches
    private Patches m_CurrentPatches;
    public Patches CurrentPatches
    {
        get { return m_CurrentPatches; }
    }
    //所有热更的东西
    private Dictionary<string, Patch> m_HotFixDic = new Dictionary<string, Patch>();
    //所有需要下载的东西
    private List<Patch> m_DownLoadList = new List<Patch>();
    //所有需要下载的东西的Dic
    private Dictionary<string, Patch> m_DownLoadDic = new Dictionary<string, Patch>();
    //服务器上的资源名对应的MD5，用于下载后MD5校验
    private Dictionary<string, string> m_DownLoadMD5Dic = new Dictionary<string, string>();
    //计算需要解压的文件
    private List<string> m_UnPackedList = new List<string>();
    //原包记录的MD5码
    private Dictionary<string, AssetBase> m_PackedMd5 = new Dictionary<string, AssetBase>();
    //服务器列表获取错误回调
    public Action ServerInfoError;
    //文件下载出错回调
    public Action<string> ItemError;
    //下载完成回调
    public Action LoadOver;
    //储存已经下载的资源
    public List<Patch> m_AlreadyDownList = new List<Patch>();
    //下载完成
    public bool DownloadFinish = false;
    //是否开始下载
    public bool StartDownload = false;
    //尝试重新下载次数
    private int m_TryDownCount = 0;
    private const int DOWNLOADCOUNT = 4;
    //当前正在下载的资源
    private DownLoadFileItem m_CurDownload = null;

    // 需要下载的资源总个数
    public int LoadFileCount  = 0;
    // 需要下载资源的总大小 KB
    public float LoadSumSize  = 0;
    //是否开始解压
    public bool StartUnPack = false;
    //解压文件总大小
    public float UnPackSumSize  = 0;
    //已解压大小
    public float AlreadyUnPackSize = 0;

    public void Init(MonoBehaviour mono)
    {
        m_Mono = mono;
        //ReadMD5();
    }

    /// <summary>
    /// 读取本地资源MD5码
    /// </summary>
    void ReadMD5()
    {
        m_PackedMd5.Clear();
        TextAsset md5 = Resources.Load<TextAsset>("AssetsMd5");
        if (md5 == null)
        {
            Debug.LogError("未读取到本地MD5");
            return;
        }

        using (MemoryStream stream = new MemoryStream(md5.bytes))
        {
            BinaryFormatter bf = new BinaryFormatter();
            AssetsMd5 assetsMd5 = bf.Deserialize(stream) as AssetsMd5;
            foreach (AssetBase abmd5Base in assetsMd5.ABMD5List)
            {
                m_PackedMd5.Add(abmd5Base.Name, abmd5Base);
            }
        }
    }

    /// <summary>
    /// 计算需要解压的文件
    /// </summary>
    /// <returns></returns>
    public bool ComputeUnPackFile()
    {
#if UNITY_ANDROID
        if (!Directory.Exists(m_UnPackPath))
        {
            Directory.CreateDirectory(m_UnPackPath);
        }
        m_UnPackedList.Clear();
        //遍历本地资源MD5字典名称
        foreach (string fileName in m_PackedMd5.Keys)
        {
            Debug.Log("key :" + fileName);
            string filePath = m_UnPackPath + "/" + fileName;
            if (File.Exists(filePath))
            {
                string md5 = MD5Manager.Instance.BuildFileMd5(filePath);
                //对比MD5是否相同
                if (m_PackedMd5[fileName].Md5 != md5)
                {
                    m_UnPackedList.Add(fileName);
                }
                else
                {
                    Debug.Log("Same " + fileName);
                }
            }
            else
            {
                m_UnPackedList.Add(fileName);
            }
        }

        foreach (string fileName in m_UnPackedList)
        {
            if (m_PackedMd5.ContainsKey(fileName))
            {
                UnPackSumSize += m_PackedMd5[fileName].Size;
            }
        }

        return m_UnPackedList.Count > 0;
#else
        return false;
#endif
    }

    /// <summary>
    /// 获取解压进度
    /// </summary>
    /// <returns></returns>
    public float GetUnpackProgress()
    {
        return AlreadyUnPackSize / UnPackSumSize;
    }

    /// <summary>
    /// 开始解压文件
    /// </summary>
    /// <param name="callBack"></param>
    public void StartUnackFile(Action callBack)
    {
        StartUnPack = true;
        m_Mono.StartCoroutine(UnPackToPersistentDataPath(callBack));
    }

    /// <summary>
    /// 将包里的原始资源解压到本地
    /// </summary>
    /// <param name="callBack"></param>
    /// <returns></returns>
    IEnumerator UnPackToPersistentDataPath(Action callBack)
    {
        foreach (string fileName in m_UnPackedList)
        {
            UnityWebRequest unityWebRequest = UnityWebRequest.Get(Application.streamingAssetsPath + "/" + fileName);
            unityWebRequest.timeout = 30;
            yield return unityWebRequest.SendWebRequest();
            if (unityWebRequest.isNetworkError)
            {
                Debug.Log("UnPack Error" + unityWebRequest.error);
            }
            else
            {
                byte[] bytes = unityWebRequest.downloadHandler.data;
                yield return FileTool.Instance.CreateFile(m_UnPackPath + "/" + fileName, bytes);
            }

            if (m_PackedMd5.ContainsKey(fileName))
            {
                AlreadyUnPackSize += m_PackedMd5[fileName].Size;
            }

            unityWebRequest.Dispose();
        }

        if (callBack != null)
        {
            callBack();
        }

        StartUnPack = false;
    }

    /// <summary>
    /// 计算AB包路径
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public string ComputeABPath(string name)
    {
        Patch patch = null;
        m_HotFixDic.TryGetValue(name, out patch);
        if (patch != null)
        {
            return m_DownLoadPath + "/" + name;
        }
        return "";
    }

    /// <summary>
    /// 检测版本是否热更
    /// </summary>
    /// <param name="hotCallBack"></param>
    public void CheckVersion(Action<bool> hotCallBack = null)
    {
        if (!Directory.Exists(Application.persistentDataPath))
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            AppStart._instance.HotConfirmDialog.AddShow("CreateDirectory");
        }

        AppStart._instance.HotConfirmDialog.AddShow("正在检查版本信息 ");
        Debug.Log("正在检查版本信息");
        m_TryDownCount = 0;
        m_HotFixDic.Clear();
        ReadVersion();
        m_Mono.StartCoroutine(ReadXml(() =>
        {
            if (m_ServerInfo == null)
            {
                if (ServerInfoError != null)
                {
                    ServerInfoError();
                }
                return;
            }
            //从服务器读取 m_ServerInfo 遍历
            foreach (VersionInfo version in m_ServerInfo.GameVersion)
            {
                if (version.Version == m_CurVersion)
                {
                    m_GameVersion = version;
                    break;
                }
            }
            AppStart._instance.HotConfirmDialog.AddShow("m_GameVersion : " + m_GameVersion.Version);

            GetHotAB();
            if (CheckLocalAndServerPatch())
            {
                AppStart._instance.HotConfirmDialog.AddShow("CheckLocalAndServerPatch  TRUE ");

                ComputeDownload();
                if (File.Exists(m_ServerXmlPath))
                {
                    if (File.Exists(m_LocalXmlPath))
                    {
                        File.Delete(m_LocalXmlPath);
                    }
                    File.Move(m_ServerXmlPath, m_LocalXmlPath);
                }
                else
                {
                    Debug.LogError("Do not exists file : " + m_ServerXmlPath);
                }
            }
            else
            {
                AppStart._instance.HotConfirmDialog.AddShow("CheckLocalAndServerPatch  FASLE ");

                ComputeDownload();
            }
            LoadFileCount = m_DownLoadList.Count;
            LoadSumSize = m_DownLoadList.Sum(x => x.Size);

            AppStart._instance.HotConfirmDialog.AddShow("LoadFileCount   " + LoadFileCount + "  LoadSumSize   " + LoadSumSize);

            if (hotCallBack != null)
            {
                hotCallBack(m_DownLoadList.Count > 0);
            }
        }));
    }

    /// <summary>
    /// 检查本地热更信息与服务器热更信息比较
    /// </summary>
    /// <returns></returns>
    bool CheckLocalAndServerPatch()
    {
        //检查本地是否有XML热更文件
        if (!File.Exists(m_LocalXmlPath))
            return true;
        //本地热更信息 包含若干版本
        m_LocalInfo = BinarySerializeOpt.XmlDeserialize(m_LocalXmlPath, typeof(ServerInfo)) as ServerInfo;
        //本地热更信息 对应其中一个版本
        VersionInfo localGameVesion = null;
        if (m_LocalInfo != null)
        {
            foreach (VersionInfo version in m_LocalInfo.GameVersion)
            {
                if (version.Version == m_CurVersion)
                {
                    localGameVesion = version;
                    break;
                }
            }
        }
        // 
        if (localGameVesion != null && m_GameVersion.Patches != null && localGameVesion.Patches != null && m_GameVersion.Patches.Length > 0 && m_GameVersion.Patches[m_GameVersion.Patches.Length - 1].Version != localGameVesion.Patches[localGameVesion.Patches.Length - 1].Version)
            return true;

        return false;
    }

    /// <summary>
    /// 读取打包时的版本
    /// </summary>
    void ReadVersion()
    {
        TextAsset versionTex = Resources.Load<TextAsset>("Version");
        if (versionTex == null)
        {
            Debug.LogError("未读到本地版本！");
            return;
        }
        string[] all = versionTex.text.Split('\n');
        if (all.Length > 0)
        {
            string[] infoList = all[0].Split(';');
            if (infoList.Length >= 2)
            {
                m_CurVersion = infoList[0].Split('|')[1];
                m_CurPackName = infoList[1].Split('|')[1];
            }
        }
    }

    IEnumerator ReadXml(Action callBack)
    {
        string xmlUrl = "http://172.16.16.4:8080/resourcemgr2/Download/Hot/ServerInfo.xml";
        AppStart._instance.HotConfirmDialog.AddShow("ReadXml");

        UnityWebRequest webRequest = UnityWebRequest.Get(xmlUrl);
        webRequest.timeout = 30;
        yield return webRequest.SendWebRequest();
        AppStart._instance.HotConfirmDialog.AddShow("SendWebRequest");

        if (webRequest.isNetworkError)
        {
            Debug.Log("Download Error" + webRequest.error);
        }
        else
        {
            yield return FileTool.Instance.CreateFile(m_ServerXmlPath, webRequest.downloadHandler.data);
            if (File.Exists(m_ServerXmlPath))
            {
                m_ServerInfo = BinarySerializeOpt.XmlDeserialize(m_ServerXmlPath, typeof(ServerInfo)) as ServerInfo;
                AppStart._instance.HotConfirmDialog.AddShow("m_ServerInfo");

            }
            else
            {
                Debug.LogError("热更配置读取错误！");
            }
        }

        if (callBack != null)
        {
            callBack();
        }
    }

    /// <summary>
    /// 获取所有热更包信息
    /// </summary>
    void GetHotAB()
    {
        if (m_GameVersion != null && m_GameVersion.Patches != null && m_GameVersion.Patches.Length > 0)
        {
            Patches lastPatches = m_GameVersion.Patches[m_GameVersion.Patches.Length - 1];
            if (lastPatches != null && lastPatches.Files != null)
            {
                foreach (Patch patch in lastPatches.Files)
                {
                    m_HotFixDic.Add(patch.Name, patch);
                }
            }
        }
    }

    /// <summary>
    /// 计算下载的资源
    /// </summary>
    void ComputeDownload()
    {
        Debug.Log("正在检查资源");
        AppStart._instance.HotConfirmDialog.AddShow("正在检查资源");

        m_DownLoadList.Clear();
        m_DownLoadDic.Clear();
        m_DownLoadMD5Dic.Clear();
        AppStart._instance.HotConfirmDialog.AddShow("m_GameVersion.Patches.Length" + m_GameVersion.Patches.Length);

        if (m_GameVersion != null && m_GameVersion.Patches != null && m_GameVersion.Patches.Length > 0)
        {
            //最后一个热更包
            m_CurrentPatches = m_GameVersion.Patches[m_GameVersion.Patches.Length - 1];
            
            if (m_CurrentPatches.Files != null && m_CurrentPatches.Files.Count > 0)
            {
                foreach (Patch patch in m_CurrentPatches.Files)
                {
                    if (((Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor))&& (patch.Platform.Contains("PC") || patch.Platform.Contains("VR")))
                    {
                        AddDownLoadList(patch);
                    }
                    else if ((Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.WindowsEditor) && patch.Platform.Contains("Android"))
                    {
                        AddDownLoadList(patch);
                    }
                    else if ((Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.WindowsEditor) && patch.Platform.Contains("iOS"))
                    {
                        AddDownLoadList(patch);
                    }
                }
            }
        }
    }

    void AddDownLoadList(Patch patch)
    {
        string filePath = m_DownLoadPath + "/" + patch.Name;
        //存在这个文件时进行对比看是否与服务器MD5码一致，不一致放到下载队列，如果不存在直接放入下载队列
        if (File.Exists(filePath))
        {
            string md5 = MD5Manager.Instance.BuildFileMd5(filePath);
            if (patch.Md5 != md5)
            {
                m_DownLoadList.Add(patch);
                m_DownLoadDic.Add(patch.Name, patch);
                m_DownLoadMD5Dic.Add(patch.Name, patch.Md5);
            }
        }
        else
        {
            m_DownLoadList.Add(patch);
            m_DownLoadDic.Add(patch.Name, patch);
            m_DownLoadMD5Dic.Add(patch.Name, patch.Md5);
        }
    }

    /// <summary>
    /// 获取下载进度
    /// </summary>
    /// <returns></returns>
    public float GetProgress()
    {
        return GetLoadSize() / LoadSumSize;
    }

    /// <summary>
    /// 获取已经下载总大小
    /// </summary>
    /// <returns></returns>
    public float GetLoadSize()
    {
        float alreadySize = m_AlreadyDownList.Sum(x => x.Size);
        float curAlreadySize = 0;
        if (m_CurDownload != null)
        {
            Patch patch = FindPatchByGamePath(m_CurDownload.FileName);
            if (patch != null && !m_AlreadyDownList.Contains(patch))
            {
                curAlreadySize = m_CurDownload.GetProcess() * patch.Size;
            }
        }
        return alreadySize + curAlreadySize;
    }

    /// <summary>
    /// 开始下载资源包
    /// </summary>
    /// <param name="callBack"></param>
    /// <returns></returns>
    public IEnumerator StartDownLoadAB(Action callBack, List<Patch> allPatch = null)
    {
        m_AlreadyDownList.Clear();
        StartDownload = true;
        if (allPatch == null)
        {
            allPatch = m_DownLoadList;
        }
        if (!Directory.Exists(m_DownLoadPath))
        {
            Directory.CreateDirectory(m_DownLoadPath);
        }

        List<DownLoadFileItem> downLoadAssetBundles = new List<DownLoadFileItem>();
        foreach (Patch patch in allPatch)
        {
            Debug.Log("downLoad : " + patch.Url +" --- " + m_DownLoadPath);
            downLoadAssetBundles.Add(new DownLoadFileItem(patch.Url, m_DownLoadPath));
        }

        foreach (DownLoadFileItem downLoad in downLoadAssetBundles)
        {
            m_CurDownload = downLoad;
            yield return m_Mono.StartCoroutine(downLoad.Download());
            Patch patch = FindPatchByGamePath(downLoad.FileName);
            if (patch != null)
            {
                m_AlreadyDownList.Add(patch);
            }
            downLoad.Destory();
        }

        if (callBack != null)
        {
            callBack();
        }
        //MD5码校验,如果校验没通过，自动重新下载没通过的文件，重复下载计数，达到一定次数后，反馈某某文件下载失败
        //VerifyMD5(downLoadAssetBundles, callBack);
    }

    /// <summary>
    /// Md5码校验
    /// </summary>
    /// <param name="downLoadAssets"></param>
    /// <param name="callBack"></param>
    void VerifyMD5(List<DownLoadFileItem> downLoadAssets, Action callBack)
    {
        Debug.Log("下载完成，校验文件");
        AppStart._instance.HotConfirmDialog.AddShow("下载完成，校验文件");

        List<Patch> downLoadList = new List<Patch>();
        foreach (DownLoadFileItem downLoad in downLoadAssets)
        {
            string md5 = "";
            if (m_DownLoadMD5Dic.TryGetValue(downLoad.FileName, out md5))
            {
                if (MD5Manager.Instance.BuildFileMd5(downLoad.SaveFilePath) != md5)
                {
                    Debug.Log(string.Format("此文件{0}MD5校验失败，即将重新下载", downLoad.FileName));
                    AppStart._instance.HotConfirmDialog.AddShow(string.Format("此文件{0}MD5校验失败，即将重新下载", downLoad.FileName));

                    Patch patch = FindPatchByGamePath(downLoad.FileName);
                    if (patch != null)
                    {
                        downLoadList.Add(patch);
                    }
                }
            }
        }

        if (downLoadList.Count <= 0)
        {
            m_DownLoadMD5Dic.Clear();
            StartDownload = false;
            DownloadFinish = true;
            if (callBack != null)
            {
                
                callBack();
            }
            if (LoadOver != null)
            {
                LoadOver();
            }
        }
        else
        {
            if (m_TryDownCount >= DOWNLOADCOUNT)
            {
                string allName = "";
                StartDownload = false;
                foreach (Patch patch in downLoadList)
                {
                    allName += patch.Name + ";";
                }
                Debug.LogError("资源重复下载4次MD5校验都失败，请检查资源" + allName);
                if (ItemError != null)
                {
                    ItemError(allName);
                }
            }
            else
            {
                m_TryDownCount++;
                m_DownLoadMD5Dic.Clear();
                foreach (Patch patch in downLoadList)
                {
                    m_DownLoadMD5Dic.Add(patch.Name, patch.Md5);
                }
                //自动重新下载校验失败的文件
                m_Mono.StartCoroutine(StartDownLoadAB(callBack, downLoadList));
            }
        }
    }

    /// <summary>
    /// 根据名字查找对象的热更Patch
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    Patch FindPatchByGamePath(string name)
    {
        Patch patch = null;
        m_DownLoadDic.TryGetValue(name, out patch);
        return patch;
    }


    public IEnumerator UnCompressZip()
    {
        foreach (var alreadyDown in m_AlreadyDownList)
        {
            if (alreadyDown.Name.EndsWith(".zip"))
            {
                string url = m_DownLoadPath + "/" + alreadyDown.Name;

                yield return Util_DownloadZip._instance.DownloadURL(url, Application.persistentDataPath, null,
                    () =>
                    {
                        AppStart._instance.HotConfirmDialog.Show(alreadyDown.Name+ "  UnCompressZip!!");
                        Debug.Log(alreadyDown.Name + "   UnCompressZip!!");
                    });
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
            }
        }

        yield return null;
    }
}


public class FileTool : Singleton<FileTool>
{
    public long SpeedLimit = 1000000;


    /// <summary>
    /// 创建文件
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="bytes"></param>
    public IEnumerator CreateFile(string filePath, byte[] bytes)
    {
        Debug.Log("CreateFile： " + filePath);
        yield return new WaitForSeconds(0.5f);
        AppStart._instance.HotConfirmDialog.AddShow("创建文件： " + filePath);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        int size = 2048;
        byte[] buffer = new byte[size];
        FileStream fs = File.Create(filePath);
        Stream stream = new MemoryStream(bytes);

        long tempWrite = 0;
        long alreadyWrite = 0;
        long totalSize = bytes.Length;

        while (true)
        {
            size = stream.Read(buffer, 0, buffer.Length);
            if (size > 0)
            {
                tempWrite += size;
                alreadyWrite += size;
                fs.Write(buffer, 0, size); //解决读取不完整情况 
                if (tempWrite > SpeedLimit)
                {
                    float per = (float) alreadyWrite / totalSize;
                    per = per * 100;
                    AppStart._instance.HotConfirmDialog.Show("创建文件： " + per.ToString("F0") + "  %");
                    yield return new WaitForEndOfFrame();
                    tempWrite = 0;
                }

            }
            else
            {
                AppStart._instance.HotConfirmDialog.Show("创建文件： 100  %");
                yield return new WaitForEndOfFrame();
                break;
            }
        }
        stream.Close();
        stream.Dispose();
        fs.Close();
        fs.Dispose();
        yield return null;
    }



    //public static void CreateFile(string filePath, byte[] bytes)
    //{
    //    if (File.Exists(filePath))
    //    {
    //        File.Delete(filePath);
    //    }
    //    FileInfo file = new FileInfo(filePath);
    //    Stream stream = file.Create();

    //    stream.Write(bytes, 0, bytes.Length);
    //    stream.Close();
    //    stream.Dispose();
    //}
}
