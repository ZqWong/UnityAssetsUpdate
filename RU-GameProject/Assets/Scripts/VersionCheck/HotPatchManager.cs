using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Esp.Core.Download;
using Esp.Core.Utils.Core;

using UnityEngine;
using UnityEngine.Networking;
#if JSON
using LitJson;
using Esp.VersionCheck.DataModule.Json;
using Esp.VersionCheck.LocalVersionInfo;

#elif XML
using Esp.VersionCheck.DataModule.Xml;

#endif

namespace Esp.Core.VersionCheck
{
    public class HotPatchManager : Singleton<HotPatchManager>
    {
        private readonly string LOCAL_VERSION_INFO_FILE_PATH = Application.streamingAssetsPath + "/LocalVersion/AppVersion.json";

        private MonoBehaviour m_mono;
        private string m_unPackPath = Application.persistentDataPath + "/Origin";
        private string m_downLoadPath = Application.persistentDataPath + "/DownLoad";
        
        /// <summary>
        /// 当前APP Version
        /// </summary>
        private string m_curVersion;
        /// <summary>
        /// 当前APP Package Name
        /// </summary>
        private string m_curPackName;

#if JSON
        /// <summary>
        /// (Server) 服务器Server.json本地缓存路径
        /// </summary>
        private string m_serverJsonPath = Application.persistentDataPath + "/ServerInfo.json";
        /// <summary>
        /// (Local) Server.json本地存储路径
        /// </summary>
        private string m_localJsonPath = Application.persistentDataPath + "/LocalInfo.json";
        
        /// <summary>
        /// (Server) 服务端的版本信息
        /// </summary>
        private ServerInfoDataModule m_serverInfoDataModule;

        /// <summary>
        /// (Local) 本地的版本信息
        /// </summary>
        private ServerInfoDataModule m_localInfoDataModule;


        private List<Patches> m_needUpdatePatchesInfos = new List<Patches>();

        /// <summary>
        /// (Local) 本地对应热更版本的资源版本号 LocalVersion.json -> GameVersion(matched) -> Version
        /// </summary>
        private string m_localVersion = "0";

        /// <summary>
        /// (Server) 应该更新到的版本
        /// </summary>
        private string m_targetVersion = "0";

        /// <summary>
        /// (Local) 本地与服务器更新列表中,当前资源版本序号;
        /// </summary>
        private int m_localVersionIndex = 0;

        /// <summary>
        /// (Server) 当前APP Version 与 Branch 所对应的所有更新信息
        /// </summary>
        private Branches m_currentBranch;

        /// <summary>
        /// JSON 模式弃用的,请使用 m_needUpdatePatchesInfos 代替;
        /// </summary>
        [Obsolete]
        private Patches m_currentPatches;

        //当前热更Patches    
        public List<Patches> CurrentPatches
        {
            get { return m_needUpdatePatchesInfos; }
        }

        /// <summary>
        /// 当前版本所有需要下载的文件列表
        /// </summary>
        private Dictionary<string, Patch> m_hmrDic = new Dictionary<string, Patch>();

        private List<Patch> m_downLoadList = new List<Patch>();

        private Dictionary<string, Patch> m_downLoadDic = new Dictionary<string, Patch>();

        private UnpackMd5InfoDataModule m_zipMd5Data;

#elif XML
        private string m_serverXmlPath = Application.persistentDataPath + "/ServerInfo.xml";
        private string m_localXmlPath = Application.persistentDataPath + "/LocalInfo.xml";

        private ServerInfo m_serverInfo;
        private ServerInfo m_localInfo;
        private VersionInfo m_gameVersion;
        private Patches m_currentPatches;

        /// <summary>
        /// 当前版本所有需要下载的文件列表
        /// </summary>
        private Dictionary<string, Patch> m_hotFixDic = new Dictionary<string, Patch>();

        /// <summary>
        /// Need download patches
        /// </summary>
        private List<Patch> m_downLoadList = new List<Patch>();

        /// <summary>
        /// Need download patch dic
        /// </summary>
        private Dictionary<string, Patch> m_downLoadDic = new Dictionary<string, Patch>();

        //当前热更Patches    
        public Patches CurrentPatches
        {
            get { return m_currentPatches; }
        }

        private ZipMd5Data m_zipMd5Data;
#endif

        /// <summary>
        /// Current branch name;
        /// </summary>
        private string m_branchName;

        /// <summary>
        /// All download file name and its MD5 value dic
        /// </summary>
        private Dictionary<string, string> m_downLoadMD5Dic = new Dictionary<string, string>();        

        /// <summary>
        /// Retries count cache
        /// </summary>        
        private int m_tryDownCount = 0;
        /// <summary>
        /// Max retries count
        /// </summary>
        private const int DOWNLOADCOUNT = 2;

        /// <summary>
        /// Downloading file
        /// </summary>
        private DownLoadFileItem m_curDownload = null;

        private float m_versionCheckProgress = 0.0f;
        private float m_targetVersionCheckProgress = 0.0f;


        public Action<string, float> OnProgressSliderChange;
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
        public bool ZipFileCreateEnable = false;
        //是否开始解压
        public bool StartUnPack = false;

        public string CurVersion
        {
            get { return m_curVersion; }
        }

        public DownLoadFileItem CurrentDownLoadFileItem
        {
            get { return m_curDownload; }
        }

        // 需要下载的资源总个数
        public int LoadFileCount { get; set; }
        // 需要下载资源的总大小 KB
        public float LoadSumSize { get; set; }
        // 解压文件总大小
        public float UnPackSumSize { get; set; }
        // 已解压大小
        public float AlreadyUnPackSize { get; set; }

        private void ChangeProgressSlider(string info, float progress)
        {
            if (OnProgressSliderChange == null)
                return;
            OnProgressSliderChange(info, progress);
        }

        /// <summary>
        /// 热更新初始化,开始热更流程
        /// </summary>
        /// <param name="mono">当前调用热更Mono脚本</param>
        /// <param name="remoteServerInfoUrl">资源服务器ServerInfo.json路径</param>
        /// <param name="branchName">资源分支名</param>
        /// <param name="versionCheckConfirmHandler">确认进行热更按钮回调事件</param>
        /// <param name="unPackPath">文件加压路径</param>
        /// <param name="downLoadPath">文件下载路径</param>
        /// <param name="localServerInfoPath">本地ServerInfo.json比对文件路径</param>
        /// <param name="localInfoPath">本地ServerInfo.json下载路径</param>
        /// <param name="progressSliderChangeHandler">进度更新回调</param>
        /// <param name="itemErrorHandler">错误信息回调</param>
        public void Initialize(
          MonoBehaviour mono,
          string remoteServerInfoUrl,
          string branchName,
          Action<bool> versionCheckConfirmHandler,
          string unPackPath = "",
          string downLoadPath = "",
          string localServerInfoPath = "",
          string localInfoPath = "",
          Action<string, float> progressSliderChangeHandler = null,
          Action<string> itemErrorHandler = null)
        {
            m_mono = mono;

            m_branchName = branchName;

            m_unPackPath = !string.IsNullOrEmpty(unPackPath) ? unPackPath : Application.persistentDataPath + "/Origin";
            m_downLoadPath = !string.IsNullOrEmpty(downLoadPath) ? downLoadPath : Application.persistentDataPath + "/DownLoad";

#if XML
            m_serverXmlPath = !string.IsNullOrEmpty(localServerInfoPath) ? localServerInfoPath : Application.persistentDataPath + "/ServerInfo.xml";
            m_localXmlPath = !string.IsNullOrEmpty(localInfoPath) ? localInfoPath : Application.persistentDataPath + "/LocalInfo.xml";
#elif JSON
            m_serverJsonPath = !string.IsNullOrEmpty(localServerInfoPath) ? localServerInfoPath : Application.persistentDataPath + "/ServerInfo.json";
            m_localJsonPath = !string.IsNullOrEmpty(localInfoPath) ? localInfoPath : Application.persistentDataPath + "/LocalInfo.json";
#endif

            OnProgressSliderChange = progressSliderChangeHandler;
            ItemError = itemErrorHandler;
            m_mono.StartCoroutine(CheckVersionCoroutine(remoteServerInfoUrl, versionCheckConfirmHandler));
        }

        public IEnumerator CheckVersionCoroutine(string serverInfoRemotePath, Action<bool> versionCheckConfirmHandler = null)
        {
            if (!Directory.Exists(Application.persistentDataPath))
                Directory.CreateDirectory(Application.persistentDataPath);
            
            Debug.Log("正在检查版本信息 " + serverInfoRemotePath);
            ChangeProgressSlider("正在获取版本信息", 0.0f);
            m_tryDownCount = 0;

#if JSON
            m_hmrDic.Clear();
#elif XML
            m_hotFixDic.Clear();
#endif

            // Get current version & pack name
            ReadVersion();
            ChangeProgressSlider("正在获取版本信息", 0.5f);

#if JSON

            yield return ReadServerInfoJson(serverInfoRemotePath, () =>
            {
                Debug.Log("<color=green>Read server info (json) from remote server complete!</color>");
                if (null == m_serverInfoDataModule)
                {
                    Debug.LogError("Server parsing failed, null == m_serverInfoDataModule");
                    if (null == ServerInfoError)
                        return;
                    ServerInfoError();
                }
                else
                {
                    var matchedVersionInfo = m_serverInfoDataModule.GameVersionInfos.FirstOrDefault(i => i.GameVersion == m_curVersion);
                    Debug.Assert(null != matchedVersionInfo, "Get matched server info failed");
                    if (null != matchedVersionInfo)
                    {
                        var matchedBranch = matchedVersionInfo.Branches.FirstOrDefault(i => i.BranchName == m_branchName);
                        m_currentBranch = matchedBranch;
                    }
                    //GetHotAB();
                }
            });
#elif XML
            yield return ReadServerInfoXml(serverInfoRemotePath, () =>
            {
                Debug.Log("ReadServerInfoXml Over");
                if (m_serverInfo == null)
                {
                    Debug.LogError("m_serverInfo == null ");
                    if (ServerInfoError == null)
                        return;
                    ServerInfoError();
                }
                else
                {
                    foreach (VersionInfo versionInfo in m_serverInfo.GameVersion)
                    {
                        if (versionInfo.Version == m_curVersion)
                        {
                            m_gameVersion = versionInfo;
                            break;
                        }
                    }
                    GetHotAB();
                }
            });
#endif

            ChangeProgressSlider("正在获取版本信息", 1f);

            if (CheckLocalAndServerPatch())
            {
                Debug.Log("需要更新，检查需要更新哪些文件");
                yield return ComputeDownload(true);
#if XML
                if (File.Exists(m_serverXmlPath))
                {
                    if (File.Exists(m_localXmlPath))
                        File.Delete(m_localXmlPath);
                    File.Move(m_serverXmlPath, m_localXmlPath);
                }
                else
                    Debug.LogError(("Do not exists file : " + m_serverXmlPath));
#elif JSON
                if (File.Exists(m_serverJsonPath))
                {
                    if (File.Exists(m_localJsonPath))
                        File.Delete(m_localJsonPath);
                    File.Move(m_serverJsonPath, m_localJsonPath);
                }
                else
                    Debug.LogError(("Do not exists file : " + m_serverJsonPath));
#endif
            }
            else
            {
                if (File.Exists(m_serverJsonPath))
                    File.Delete(m_serverJsonPath);
                Debug.Log("不需要更新，但是仍然要检查一下文件的完整性");
                yield return ComputeDownload(true);
            }
            LoadFileCount = m_downLoadList.Count;
            LoadSumSize = m_downLoadList.Sum(x => float.Parse(x.Size.ToString()));
            m_targetVersionCheckProgress = 1f;
            if (null != versionCheckConfirmHandler)
            {
                LocalVersionInfoManager.Instance.UpdateVersionInfo(m_curVersion, m_branchName, m_targetVersion);
                versionCheckConfirmHandler.Invoke(m_downLoadList.Count > 0);
            }
        }

        /// <summary>
        /// 检查本地热更信息与服务器热更信息比较
        /// </summary>
        /// <returns> false 不需要更新, true 需要更新</returns>
        private bool CheckLocalAndServerPatch()
        {
#if JSON
            //if (!File.Exists(m_localJsonPath))
            //    return true;

            Branches branch = null;

            var branches = m_serverInfoDataModule.GameVersionInfos.FirstOrDefault(i => i.GameVersion == m_curVersion);
            Debug.Assert(null != branches, "未找到与当前App Version 所匹配的更新信息 : App Version " + m_curVersion);

            branch = branches.Branches.FirstOrDefault(i => i.BranchName == m_branchName);
            Debug.Assert(null != branches, "未找到与当前Branch Name 所匹配的更新信息 : Branch Name " + m_branchName);


            var assetVersion = LocalVersionInfoManager.Instance.GetVersionInfo(m_curVersion, m_branchName);
            if (null != assetVersion && "" != assetVersion)
            {
                // 获取与本地版本相匹配的版本序号
                var localPatch = branch.Patches.FirstOrDefault(i => i.Version == assetVersion);
                m_localVersionIndex = branch.Patches.IndexOf(localPatch) + 1;
                m_localVersion = branch.Patches[m_localVersionIndex - 1].Version;
                
                Debug.Log("当前本地资源版本 : " + m_localVersion);
            }

            m_targetVersion = branch.Patches[m_currentBranch.Patches.Count - 1].Version;

            return "" != assetVersion &&
                   null != m_currentBranch.Patches &&
                   assetVersion != m_currentBranch.Patches[m_currentBranch.Patches.Count - 1].Version;

         //   // 读取本地版本信息
         //   StreamReader sr = new StreamReader(m_localJsonPath);
         //   var content = sr.ReadToEnd();
         //   m_localInfoDataModule = new ServerInfoDataModule(JsonMapper.ToObject(content));
         //   sr.Close();

         //   // 匹配与当前APP Version所匹配的资源信息
         //   //GameVersionInfo matchInfo = null;
         //   if (null != m_localInfoDataModule)
         //   {
         //       var info = m_localInfoDataModule.GameVersionInfos.FirstOrDefault(i => i.GameVersion == m_curVersion);
         //       if (null != info)
         //       {
         //           // 获取当前本地资源版本

         //           branch = info.Branches.FirstOrDefault(i => i.BranchName == m_branchName);


         //           m_localVersionIndex = branch.Patches.Count;
         //           m_localVersion = branch.Patches[m_localVersionIndex - 1].Version;
         //           Debug.Log("当前本地资源版本 : " + m_localVersion);
         //       }                
	        //}

         //   // 与服务端对应的APP Version相关信息进行比对, 如果当前的热更好与服务器端最新的一样就返回true;
         //   return null != branch &&
         //          null != m_currentBranch.Patches &&
         //          (null != branch.Patches && m_currentBranch.Patches.Count != 0) &&
         //          m_currentBranch.Patches[m_currentBranch.Patches.Count - 1].Version != m_localVersion;
#elif XML
            if (!File.Exists(m_localXmlPath))
                return true;
            m_localInfo = BinarySerializeOpt.XmlDeserialize(m_localXmlPath, typeof(ServerInfo)) as ServerInfo;
            VersionInfo versionInfo1 = null;
            if (m_localInfo != null)
            {
                foreach (VersionInfo versionInfo2 in m_localInfo.GameVersion)
                {
                    if (versionInfo2.Version == m_curVersion)
                    {
                        versionInfo1 = versionInfo2;
                        break;
                    }
                }
            }
            return versionInfo1 != null &&
                   m_gameVersion.Patches != null &&
                   (versionInfo1.Patches != null && m_gameVersion.Patches.Length != 0) &&
                   m_gameVersion.Patches[m_gameVersion.Patches.Length - 1].Version != versionInfo1.Patches[versionInfo1.Patches.Length - 1].Version;
#endif
        }

        /// <summary>
        /// 读取打包时的版本
        /// </summary>
        private void ReadVersion()
        {
#if JSON
            StreamReader sr = new StreamReader(LOCAL_VERSION_INFO_FILE_PATH);
            var content = sr.ReadToEnd();
            var localVersionInfo = new VersionInfoDataModule(JsonMapper.ToObject(content));
            sr.Close();

            // 从本地文件获取打包信息
            m_curVersion = localVersionInfo.Version;
            m_curPackName = localVersionInfo.PackageName;

            Debug.Log(string.Format("Get local version info, Version : {0} PackageName : {1}.", m_curVersion, m_curPackName));

#elif XML
            TextAsset textAsset = Resources.Load<TextAsset>("Version");
            if (textAsset == null)
            {
                Debug.LogError("未读到本地版本！");
            }
            else
            {
                string[] strArray1 = textAsset.text.Split('\n');
                if (strArray1.Length <= 0U)
                    return;
                string[] strArray2 = strArray1[0].Split(';');
                if (strArray2.Length >= 2)
                {
                    m_curVersion = strArray2[0].Split('|')[1];
                    m_curPackName = strArray2[1].Split('|')[1];
                }
            }
#endif
        }


#if XML
        /// <summary>
        /// 从服务器端读取Server.xml数据
        /// </summary>
        /// <param name="xmlUrl"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        private IEnumerator ReadServerInfoXml(string xmlUrl, Action callBack)
        {
            Debug.Log("ReadServerInfoXml " + xmlUrl);
            UnityWebRequest webRequest = UnityWebRequest.Get(xmlUrl);
            yield return webRequest.SendWebRequest();
            if (webRequest.isNetworkError)
            {
                Debug.Log("Download Error" + webRequest.error);
            }
            else
            {
                yield return FileUtil.Instance.CreateFile(m_serverXmlPath, webRequest.downloadHandler.data, null);
                yield return new WaitForEndOfFrame();
                if (File.Exists(m_serverXmlPath))
                    m_serverInfo = BinarySerializeOpt.XmlDeserialize(m_serverXmlPath, typeof(ServerInfo)) as ServerInfo;
                else
                    Debug.LogError("热更配置读取错误！");
            }
            yield return WaitProgressAdd(0.02f);
            callBack?.Invoke();            
        }

#endif

#if JSON

        /// <summary>
        /// 从服务器端读取Server.json数据
        /// </summary>
        /// <param name="jsonUrl"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        private IEnumerator ReadServerInfoJson(string jsonUrl, Action callBack)
        {
            Debug.Log("ReadServerInfoJson :" + jsonUrl);
            UnityWebRequest webRequest = UnityWebRequest.Get(jsonUrl);
            yield return webRequest.SendWebRequest();
            if (webRequest.isNetworkError)
            {
                Debug.Log("Download Error :" + webRequest.error);
            }
            else
            {
                yield return FileUtil.Instance.CreateFile(m_serverJsonPath, webRequest.downloadHandler.data, null);
                yield return new WaitForEndOfFrame();
                if (File.Exists(m_serverJsonPath))
                {
                    StreamReader sr = new StreamReader(m_serverJsonPath);
                    var content = sr.ReadToEnd();
                    m_serverInfoDataModule = new ServerInfoDataModule(JsonMapper.ToObject(content));
                    sr.Close();
                }
                else
                {
                    Debug.LogError("热更配置读取错误！");
                }
            }

            yield return WaitProgressAdd(0.02f);
            if (null != callBack)
            {
                callBack.Invoke();
            }
        }

#endif

        private IEnumerator ReadUnpackMd5Inifo(string url, string localPath)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();
            if (webRequest.isNetworkError)
            {
                Debug.Log(("Download Error" + webRequest.error));
            }
            else
            {
                yield return FileUtil.Instance.CreateFile(localPath, webRequest.downloadHandler.data, null);
                yield return new WaitForEndOfFrame();
            }
        }

        private float VersionCheckProgress
        {
            get { return m_versionCheckProgress;}
            set
            {
                m_versionCheckProgress = value;
                if (m_versionCheckProgress == 1.0)
                    ChangeProgressSlider("文件校验完毕", 1f);
                else
                    ChangeProgressSlider("正在校验文件", m_versionCheckProgress);
            }
        }

        private IEnumerator WaitProgressAdd(float delta)
        {
            while (m_targetVersionCheckProgress > VersionCheckProgress)
            {
                VersionCheckProgress += delta;
                if (VersionCheckProgress >= 1.0)
                    VersionCheckProgress = 1f;
                yield return new WaitForEndOfFrame();
            }
            yield return null;
        }

        private void OnAddDownLoadListProgressUpdate(float progress)
        {
            if (progress > 1.0)
                progress = 1f;
            m_targetVersionCheckProgress = progress;
        }

        /// <summary>
        /// 获取所有热更包信息
        /// </summary>
        private void GetHotAB()
        {
#if JSON
            // Json
            if (null == m_currentBranch || null == m_currentBranch.Patches || m_currentBranch.Patches.Count <= 0)
                return;

            //TODO :只获取了最后一个版本的更新信息, 应该与当前版本做匹配去中间所有未更新的版本信息

            var needUpdatePatchesInfos = m_currentBranch.Patches.Skip(m_localVersionIndex).ToList();

            //var needUpdatePatchesInfos = m_currentBranch.PatchInfos.GetRange(m_localVersionIndex,
            //    m_currentBranch.PatchInfos.Count);

            for (int i = needUpdatePatchesInfos.Count - 1; i >= 1; i--)
            {
                foreach (var latestVersionFile in needUpdatePatchesInfos[i].Files)
                {
                    for (int j = needUpdatePatchesInfos.Count - i - 1; j >= 0; j--)
                    {
                        var matchedItem = needUpdatePatchesInfos[j].Files.FirstOrDefault(item =>
                            item.Name == latestVersionFile.Name &&
                            item.Platform == latestVersionFile.Platform);
                        var removeComplete = needUpdatePatchesInfos[j].Files.Remove(matchedItem);
                        if (removeComplete)
                            Debug.Log(string.Format("Found a asset with the same name ({0}) in an older version. [REMOVE] :{1}", latestVersionFile.Name, removeComplete.ToString()));
                    }
                }
            }

            foreach (Patches patch in needUpdatePatchesInfos)
            {
                foreach (var file in patch.Files)
                {
                    Debug.Log(string.Format("Patch : {0}, File Name:{1}, Md5:{2}", patch.Version, file.Name, file.Md5));
                }
            }


            if (null != needUpdatePatchesInfos)
            {
                foreach (var patchesInfo in needUpdatePatchesInfos)
                {
                    if (null != patchesInfo && null != patchesInfo.Files)
                    {
                        foreach (var file in patchesInfo.Files)
                        {
                            m_hmrDic.Add(file.Name, file);
                        }
                    }
                }
            }
#elif XML
            // Xml
            if (m_gameVersion == null || m_gameVersion.Patches == null || m_gameVersion.Patches.Length <= 0U)
                return;
            Patches patche = m_gameVersion.Patches[m_gameVersion.Patches.Length - 1];
            if (patche != null && patche.Files != null)
            {
                foreach (Patch file in patche.Files)
                    m_hotFixDic.Add(file.Name, file);
            }
#endif
        }

        /// <summary>
        /// 计算下载的资源
        /// </summary>
        private IEnumerator ComputeDownload(bool checkZip)
        {
            Debug.Log((object)"计算下载的文件");

            m_downLoadList.Clear();
            m_downLoadDic.Clear();
            m_downLoadMD5Dic.Clear();
            m_versionCheckProgress = 0.0f;

#if XML
            if (m_gameVersion != null && m_gameVersion.Patches != null && m_gameVersion.Patches.Length > 0U)
            {
                m_currentPatches = m_gameVersion.Patches[m_gameVersion.Patches.Length - 1];
                int fileCount = m_currentPatches.Files.Count;
                float delta = 1f / fileCount;
                m_zipMd5Data = new ZipMd5Data();
                m_zipMd5Data.ZipMd5List = new List<ZipMd5>();
                m_zipMd5Data.ZipMd5List.Clear();
                if (checkZip)
                    yield return ReadUnpackMd5File();
                if (m_currentPatches.Files != null && fileCount > 0)
                {
                    int index = 0;
                    Debug.Log(string.Format("当前更新包需要检查{0}个文件包", m_currentPatches.Files.Count));
                    foreach (Patch file in m_currentPatches.Files)
                    {
                        Patch patch = file;
                        Debug.Log(patch.Name);
                        AddDownLoadList(patch, checkZip, GetZipPatchFileMd5(patch));
                        OnAddDownLoadListProgressUpdate((index + 1) * delta);
                        yield return WaitProgressAdd(0.02f);
                        ++index;
                        patch = null;
                    }
                }
            }
#elif JSON
            if (null != m_currentBranch && null != m_currentBranch.Patches && m_currentBranch.Patches.Count > 0)
            {
                m_needUpdatePatchesInfos = m_currentBranch.Patches.Skip(m_localVersionIndex).ToList();
                m_needUpdatePatchesInfos = m_needUpdatePatchesInfos.OrderBy(i => i.Version).ToList();
                //var needUpdatePatchesInfos = m_currentBranch.PatchInfos.GetRange(m_localVersionIndex,
                //    m_currentBranch.PatchInfos.Count);

                for (int i = m_needUpdatePatchesInfos.Count - 1; i >= 1; i--)
                {
                    foreach (var latestVersionFile in m_needUpdatePatchesInfos[i].Files)
                    {
                        for (int j = i - 1; j >= 0; j--)
                        {
                            var matchedItem = m_needUpdatePatchesInfos[j].Files.FirstOrDefault(item =>
                                item.Name == latestVersionFile.Name &&
                                item.Platform == latestVersionFile.Platform);

                            var removeComplete = m_needUpdatePatchesInfos[j].Files.Remove(matchedItem);
                            if (removeComplete)
                                Debug.Log(string.Format("Found a asset with the same name ({0}) in an older version. [REMOVE] :{1}", latestVersionFile.Name, removeComplete.ToString()));
                        }
                    }
                }

                int fileCount = m_needUpdatePatchesInfos.Sum(i => i.Files.Count);
                float delta = 1f / fileCount;

                if (checkZip)
                    yield return ReadUnpackMd5File();

                if (null != m_needUpdatePatchesInfos)
                {
                    var index = 0;
                    Debug.Log(string.Format("<color=green>当前更新包需要检查{0}个文件包</color>", fileCount));

                    foreach (var patchesInfo in m_needUpdatePatchesInfos)
                    {
                        foreach (var file in patchesInfo.Files)
                        {
                            Debug.Log("Create download task, File name :" + file.Name);
                            AddDownLoadList(file, checkZip, GetZipPatchFileMd5(file));
                            OnAddDownLoadListProgressUpdate((index + 1) * delta);
                            yield return WaitProgressAdd(0.02f);
                            ++index;
                        }
                    }
                }
            }
#endif
        }

        private IEnumerator ReadUnpackMd5File()
        {
#if XML
            string checkFilePath = m_downLoadPath + "/UnpackMd5_" + CurVersion + "_" + CurrentPatches.Version + ".xml";
            Debug.Log("checkFilePath :" + checkFilePath);
            yield return ReadUnpackMd5Inifo(m_currentPatches.Files[0].Url, checkFilePath);
            m_zipMd5Data = BinarySerializeOpt.XmlDeserialize(checkFilePath, typeof(ZipMd5Data)) as ZipMd5Data;

#elif JSON
            m_zipMd5Data = new UnpackMd5InfoDataModule();
            foreach (Patches patches in m_needUpdatePatchesInfos)
            {
                string checkFilePath = m_downLoadPath + "/UnpackMd5_" + CurVersion + "_" + patches.Version + ".json";
                Debug.Log("checkFilePath :" + checkFilePath);
                //if (patches.Files[0].Name.StartsWith("UnpackMd5_"))
                //{
                    yield return ReadUnpackMd5Inifo(patches.Files[0].Url, checkFilePath);
                    StreamReader sr = new StreamReader(checkFilePath);
                    var content = sr.ReadToEnd();
                    var zipMd5DataCache = new UnpackMd5InfoDataModule(JsonMapper.ToObject(content));
                    m_zipMd5Data.ZipMd5List = m_zipMd5Data.ZipMd5List.Concat(zipMd5DataCache.ZipMd5List).ToList();
                    sr.Close();
                //}
            }
#endif
            yield return null;
        }

        private List<ZipBase> GetZipPatchFileMd5(Patch patch)
        {
            string name = patch.Name;
            if (name.EndsWith(".zip") && m_zipMd5Data != null)
            {
                foreach (ZipMd5 zipMd5 in m_zipMd5Data.ZipMd5List)
                {
                    if (zipMd5.ZipName == name)
                        return zipMd5.FileList;
                }
            }
            return null;
        }

        private void AddDownLoadList(Patch patch, bool checkZip, List<ZipBase> zipBaseList)
        {
            Debug.Log("AddDownLoadList : " + patch.Name);
            string str1;
            if (string.IsNullOrEmpty(patch.RelativePath))
                str1 = m_downLoadPath + "/" + patch.Name;
            else
                str1 = m_downLoadPath + "/" + patch.RelativePath + "/" + patch.Name;
            if (patch.Name.EndsWith(".zip"))
            {
                if (!checkZip)
                    return;
                Debug.Log((patch.Name + "  校验解压出来的文件md5是否相同"));
                bool flag = true;
                foreach (ZipBase zipBase in zipBaseList)
                {
                    string str2 = zipBase.UnpackPath != "" ? string.Format("{0}/{1}/{2}", m_unPackPath, zipBase.UnpackPath, zipBase.Name) : string.Format("{0}/{1}", m_unPackPath, zipBase.Name);
                    if (File.Exists(str2))
                    {
                        if (MD5Manager.Instance.BuildFileMd5(str2) != zipBase.Md5)
                        {
                            Debug.Log("<color=yellow>文件发生改变： </color>" + str2);
                            flag = false;
                            break;
                        }
                    }
                    else
                    {
                        Debug.Log("<color=red>文件不存在： </color>" + str2);
                        flag = false;
                        break;
                    }
                }
                if (!flag)
                {
                    m_downLoadList.Add(patch);
                    m_downLoadDic.Add(patch.Name, patch);
                    m_downLoadMD5Dic.Add(patch.Name, patch.Md5);
                }
            }
            else if (File.Exists(str1))
            {
                string str2 = MD5Manager.Instance.BuildFileMd5(str1);
                if (patch.Md5 != str2)
                {
                    m_downLoadList.Add(patch);
                    m_downLoadDic.Add(patch.Name, patch);
                    m_downLoadMD5Dic.Add(patch.Name, patch.Md5);
                }
            }
            else
            {
                m_downLoadList.Add(patch);
                m_downLoadDic.Add(patch.Name, patch);
                m_downLoadMD5Dic.Add(patch.Name, patch.Md5);
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
            float num1 = m_AlreadyDownList.Sum(x => float.Parse(x.Size.ToString()));
            float num2 = 0.0f;
            if (m_curDownload != null)
            {
                Patch patchByGamePath = FindPatchByGamePath(m_curDownload.FileName);
                if (patchByGamePath != null && !m_AlreadyDownList.Contains(patchByGamePath))
                    num2 = m_curDownload.GetProcess() * float.Parse(patchByGamePath.Size.ToString());
            }
            return num1 + num2;
        }

        /// <summary>
        /// 开始下载资源包
        /// </summary>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public void StartDownLoad(Action callBack, List<Patch> allPatch = null)
        {
            m_mono.StartCoroutine(StartDownLoadAssets(callBack, allPatch));
        }

        private IEnumerator StartDownLoadAssets(Action callBack, List<Patch> allPatch = null)
        {
            m_AlreadyDownList.Clear();
            StartDownload = true;
            if (allPatch == null)
                allPatch = m_downLoadList;
            if (!Directory.Exists(m_downLoadPath))
                Directory.CreateDirectory(m_downLoadPath);
            List<DownLoadFileItem> downLoadFileItemList = new List<DownLoadFileItem>();
            foreach (Patch patch1 in allPatch)
            {
                Patch patch = patch1;
                Debug.Log("downLoad : " + patch.Url + " --- " + m_downLoadPath);
                downLoadFileItemList.Add(new DownLoadFileItem(patch, m_downLoadPath, m_unPackPath, ChangeProgressSlider, ZipFileCreateEnable, ItemError));
                patch = null;
            }
            Debug.Log(string.Format("本次下载{0}个文件", downLoadFileItemList.Count));
            foreach (DownLoadFileItem downLoadFileItem in downLoadFileItemList)
            {
                DownLoadFileItem downLoad = downLoadFileItem;
                m_curDownload = downLoad;
                yield return m_mono.StartCoroutine(downLoad.Download(null));
                Patch patch = FindPatchByGamePath(downLoad.FileName);
                if (patch != null)
                    m_AlreadyDownList.Add(patch);
                m_curDownload = null;
                downLoad.Destory();
                patch = null;
                downLoad = null;
            }
            yield return VerifyMD5(downLoadFileItemList, callBack);
        }

        /// <summary>
        /// Md5码校验
        /// </summary>
        /// <param name="downLoadAssets"></param>
        /// <param name="callBack"></param>
        private IEnumerator VerifyMD5(List<DownLoadFileItem> downLoadAssets, Action callBack)
        {
            yield return ComputeDownload(true);
            if (m_downLoadList.Count <= 0)
            {
                m_downLoadMD5Dic.Clear();
                StartDownload = false;
                DownloadFinish = true;
                if (callBack != null)
                    callBack();
                if (LoadOver != null)
                    LoadOver();
                Debug.Log((object)"LoadOver 检查完毕，文件完整！");
            }
            else if (m_tryDownCount >= DOWNLOADCOUNT)
            {
                string allName = "";
                StartDownload = false;
                foreach (Patch downLoad in m_downLoadList)
                {
                    Patch patch = downLoad;
                    allName = allName + patch.Name + ";";
                    patch = null;
                }
                Debug.LogError(string.Format("资源重复下载{0}次MD5校验都失败，请检查资源 : {1}", 2, allName));
                Action<string> itemError = ItemError;
                if (itemError != null)
                    itemError("资源重复下载失败，请检查资源");
                allName = null;
            }
            else
            {
                ++m_tryDownCount;
                m_downLoadMD5Dic.Clear();
                foreach (Patch downLoad in m_downLoadList)
                {
                    Patch patch = downLoad;
                    m_downLoadMD5Dic.Add(patch.Name, patch.Md5);
                    patch = null;
                }
                StartDownLoad(callBack, m_downLoadList);
            }
        }


        /// <summary>
        /// 根据名字查找对象的热更Patch
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Patch FindPatchByGamePath(string name)
        {
            Patch patch;
            m_downLoadDic.TryGetValue(name, out patch);
            return patch;
        }
    }
}