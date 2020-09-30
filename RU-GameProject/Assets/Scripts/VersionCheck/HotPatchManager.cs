using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RU.Core.Download;
using RU.Core.Utils.Core;
using UnityEngine;
using UnityEngine.Networking;
using RU.Assets.Scripts.Utils.Core.StaticJsonFile;

namespace RU.Core.VersionCheck
{
    public class HotPatchManager : Singleton<HotPatchManager>
    {
        private MonoBehaviour m_mono;
        private string m_unPackPath = Application.persistentDataPath + "/Origin";
        private string m_downLoadPath = Application.persistentDataPath + "/DownLoad";
        private string m_curVersion;
        private string m_curPackName;
        private string m_serverXmlPath = Application.persistentDataPath + "/ServerInfo.xml";
        private string m_localXmlPath = Application.persistentDataPath + "/LocalInfo.xml";
        private ServerInfo m_serverInfo;
        private ServerInfo m_localInfo;
        private VersionInfo m_gameVersion;
        private Patches m_currentPatches;

        /// <summary>
        /// All hot fix patches dic
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

        /// <summary>
        /// All download file name and its MD5 value dic
        /// </summary>
        private Dictionary<string, string> m_downLoadMD5Dic = new Dictionary<string, string>();        

        /// <summary>
        /// number of retries
        /// </summary>        
        private int m_tryDownCount = 0;
        private const int DOWNLOADCOUNT = 2;

        /// <summary>
        /// Downloading file
        /// </summary>
        private DownLoadFileItem m_curDownload = null;
        
        private ZipMd5Data m_zipMd5Data;

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

        //当前热更Patches    
        public Patches CurrentPatches
        {
            get { return m_currentPatches; }
        }

        public DownLoadFileItem CurrentDownLoadFileItem
        {
            get { return m_curDownload; }
        }

        // 需要下载的资源总个数
        public int LoadFileCount { get; set; } = 0;
        // 需要下载资源的总大小 KB
        public float LoadSumSize { get; set; } = 0.0f;
        //解压文件总大小
        public float UnPackSumSize { get; set; } = 0.0f;
        //已解压大小
        public float AlreadyUnPackSize { get; set; } = 0.0f;

        private void ChangeProgressSlider(string info, float progress)
        {
            if (OnProgressSliderChange == null)
                return;
            OnProgressSliderChange(info, progress);
        }

        public void Initialize(
          MonoBehaviour mono,
          string serverInfoXmlUrl,
          Action<bool> versionCheckConfirmHandler,
          string unPackPath = "",
          string downLoadPath = "",
          string serverXmlPath = "",
          string localXmlPath = "",
          Action<string, float> progressSliderChangeHandler = null,
          Action<string> itemErrorHandler = null)
        {
            m_mono = mono;
            m_unPackPath = !string.IsNullOrEmpty(unPackPath) ? unPackPath : Application.persistentDataPath + "/Origin";
            m_downLoadPath = !string.IsNullOrEmpty(downLoadPath) ? downLoadPath : Application.persistentDataPath + "/DownLoad";
            m_serverXmlPath = !string.IsNullOrEmpty(serverXmlPath) ? serverXmlPath : Application.persistentDataPath + "/ServerInfo.xml";
            m_localXmlPath = !string.IsNullOrEmpty(localXmlPath) ? localXmlPath : Application.persistentDataPath + "/LocalInfo.xml";
            OnProgressSliderChange = progressSliderChangeHandler;
            ItemError = itemErrorHandler;
            m_mono.StartCoroutine(CheckVersionCoroutine(serverInfoXmlUrl, versionCheckConfirmHandler));
        }

        public IEnumerator CheckVersionCoroutine(string xmlUrl, Action<bool> versionCheckConfirmHandler = null)
        {
            if (!Directory.Exists(Application.persistentDataPath))
                Directory.CreateDirectory(Application.persistentDataPath);
            
            Debug.Log("正在检查版本信息 " + xmlUrl);
            ChangeProgressSlider("正在获取版本信息", 0.0f);
            m_tryDownCount = 0;
            m_hotFixDic.Clear();

            // Get current version & pack name
            ReadVersion();
            ChangeProgressSlider("正在获取版本信息", 0.5f);
            yield return ReadServerInfoXml(xmlUrl, () =>
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
            ChangeProgressSlider("正在获取版本信息", 1f);
            if (CheckLocalAndServerPatch())
            {
                Debug.Log((object)"需要更新，检查需要更新哪些文件");
                yield return (object)ComputeDownload(true);
                if (File.Exists(m_serverXmlPath))
                {
                    if (File.Exists(m_localXmlPath))
                        File.Delete(m_localXmlPath);
                    File.Move(m_serverXmlPath, m_localXmlPath);
                }
                else
                    Debug.LogError((object)("Do not exists file : " + m_serverXmlPath));
            }
            else
            {
                Debug.Log((object)"不需要更新，但是仍然要检查一下文件的完整性");
                yield return (object)ComputeDownload(true);
            }
            LoadFileCount = m_downLoadList.Count;
            LoadSumSize = m_downLoadList.Sum<Patch>((Func<Patch, float>)(x => x.Size));
            m_targetVersionCheckProgress = 1f;
            versionCheckConfirmHandler?.Invoke(m_downLoadList.Count > 0);            
        }

        /// <summary>
        /// 检查本地热更信息与服务器热更信息比较
        /// </summary>
        /// <returns></returns>
        private bool CheckLocalAndServerPatch()
        {
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
            return versionInfo1 != null && m_gameVersion.Patches != null && (versionInfo1.Patches != null && m_gameVersion.Patches.Length != 0) && m_gameVersion.Patches[m_gameVersion.Patches.Length - 1].Version != versionInfo1.Patches[versionInfo1.Patches.Length - 1].Version;
        }

        /// <summary>
        /// 读取打包时的版本
        /// </summary>
        private void ReadVersion()
        {
            // 从本地文件获取打包信息
            m_curVersion = StaticJsonManager.Instance.VersionInfo.VersionInfo.Version;
            m_curPackName = StaticJsonManager.Instance.VersionInfo.VersionInfo.Version;
            
            // TextAsset textAsset = Resources.Load<TextAsset>("Version");
            // if (textAsset == null)
            // {
            //     Debug.LogError("未读到本地版本！");
            // }
            // else
            // {
            //     string[] strArray1 = textAsset.text.Split('\n');
            //     if (strArray1.Length <= 0U)
            //         return;
            //     string[] strArray2 = strArray1[0].Split(';');
            //     if (strArray2.Length >= 2)
            //     {
            //         m_curVersion = strArray2[0].Split('|')[1];
            //         m_curPackName = strArray2[1].Split('|')[1];
            //     }
            // }
        }

        /// <summary>
        /// Get version info from remote server
        /// </summary>
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

        private IEnumerator ReadUnpackMd5Xml(string xmlUrl, string downloadFilePath)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(xmlUrl);
            yield return webRequest.SendWebRequest();
            if (webRequest.isNetworkError)
            {
                Debug.Log(("Download Error" + webRequest.error));
            }
            else
            {
                yield return FileUtil.Instance.CreateFile(downloadFilePath, webRequest.downloadHandler.data, null);
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
            if (m_gameVersion == null || m_gameVersion.Patches == null || m_gameVersion.Patches.Length <= 0U)
                return;
            Patches patche = m_gameVersion.Patches[m_gameVersion.Patches.Length - 1];
            if (patche != null && patche.Files != null)
            {
                foreach (Patch file in patche.Files)
                    m_hotFixDic.Add(file.Name, file);
            }
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
        }

        private IEnumerator ReadUnpackMd5File()
        {
            string checkFilePath = m_downLoadPath + "/UnpackMd5_" + CurVersion + "_" + CurrentPatches.Version + ".xml";
            Debug.Log("checkFilePath :" + checkFilePath);
            yield return ReadUnpackMd5Xml(m_currentPatches.Files[0].Url, checkFilePath);
            m_zipMd5Data = BinarySerializeOpt.XmlDeserialize(checkFilePath, typeof(ZipMd5Data)) as ZipMd5Data;
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
                    string str2 = !(zipBase.UnpackPath == "") ? string.Format("{0}/{1}/{2}", m_unPackPath, zipBase.UnpackPath, zipBase.Name) : string.Format("{0}/{1}", m_unPackPath, zipBase.Name);
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
            float num1 = m_AlreadyDownList.Sum(x => x.Size);
            float num2 = 0.0f;
            if (m_curDownload != null)
            {
                Patch patchByGamePath = FindPatchByGamePath(m_curDownload.FileName);
                if (patchByGamePath != null && !m_AlreadyDownList.Contains(patchByGamePath))
                    num2 = m_curDownload.GetProcess() * patchByGamePath.Size;
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
            else if (m_tryDownCount >= 2)
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