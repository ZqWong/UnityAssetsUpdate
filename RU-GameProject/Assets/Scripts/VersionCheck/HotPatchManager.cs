using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RU.Core.Download;
using RU.Core.Utils.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace RU.Core.VersionCheck
{
    public class HotPatchManager : Singleton<HotPatchManager>
    {
        private MonoBehaviour m_Mono;
        private string m_UnPackPath = Application.persistentDataPath + "/Origin";
        private string m_DownLoadPath = Application.persistentDataPath + "/DownLoad";
        private string m_CurVersion;
        private string m_CurPackName;
        private string m_ServerXmlPath = Application.persistentDataPath + "/ServerInfo.xml";
        private string m_LocalXmlPath = Application.persistentDataPath + "/LocalInfo.xml";
        private ServerInfo m_ServerInfo;
        private ServerInfo m_LocalInfo;
        private VersionInfo m_GameVersion;
        private Pathces m_CurrentPatches;
        //所有热更的东西
        private Dictionary<string, Patch> m_HotFixDic = new Dictionary<string, Patch>();        
        //所有需要下载的东西
        private List<Patch> m_DownLoadList = new List<Patch>();
         //所有需要下载的东西的Dic
        private Dictionary<string, Patch> m_DownLoadDic = new Dictionary<string, Patch>();
        //服务器上的资源名对应的MD5，用于下载后MD5校验
        private Dictionary<string, string> m_DownLoadMD5Dic = new Dictionary<string, string>();
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
        private const int DOWNLOADCOUNT = 2;
         //当前正在下载的资源
        private DownLoadFileItem m_CurDownload = null;
        public bool ZipFileCreateEnable = false;
        //是否开始解压
        public bool StartUnPack = false;
        private ZipMd5Data m_ZipMd5Data;
        public Action<string, float> OnProgressSliderChange;
        private float m_VersionCheckProgress = 0.0f;
        private float m_TargetVersionCheckProgress = 0.0f;

        public string CurVersion => m_CurVersion;

        //当前热更Patches    
        public Pathces CurrentPatches => m_CurrentPatches;

        public DownLoadFileItem CurrentDownLoadFileItem => m_CurDownload;

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
          Action<bool> hotCallBack,
          string unPackPath = "",
          string downLoadPath = "",
          string serverXmlPath = "",
          string localXmlPath = "",
          Action<string, float> OnProgressSliderChange = null,
          Action<string> OnItemError = null)
        {
            m_Mono = mono;
            m_UnPackPath = !string.IsNullOrEmpty(unPackPath) ? unPackPath : Application.persistentDataPath + "/Origin";
            m_DownLoadPath = !string.IsNullOrEmpty(downLoadPath) ? downLoadPath : Application.persistentDataPath + "/DownLoad";
            m_ServerXmlPath = !string.IsNullOrEmpty(serverXmlPath) ? serverXmlPath : Application.persistentDataPath + "/ServerInfo.xml";
            m_LocalXmlPath = !string.IsNullOrEmpty(localXmlPath) ? localXmlPath : Application.persistentDataPath + "/LocalInfo.xml";
            OnProgressSliderChange = OnProgressSliderChange;
            ItemError = OnItemError;
            m_Mono.StartCoroutine(CheckVersionCoroutine(serverInfoXmlUrl, hotCallBack));
        }

        public IEnumerator CheckVersionCoroutine(string xmlUrl, Action<bool> hotCallBack = null)
        {
            if (!Directory.Exists(Application.persistentDataPath))
                Directory.CreateDirectory(Application.persistentDataPath);
            Debug.Log((object)("正在检查版本信息 " + xmlUrl));
            ChangeProgressSlider("正在获取版本信息", 0.0f);
            m_TryDownCount = 0;
            m_HotFixDic.Clear();
            ReadVersion();
            ChangeProgressSlider("正在获取版本信息", 0.5f);
            yield return (object)ReadServerInfoXml(xmlUrl, (Action)(() =>
            {
                Debug.Log((object)"ReadServerInfoXml Over");
                if (m_ServerInfo == null)
                {
                    Debug.LogError((object)"m_ServerInfo == null ");
                    if (ServerInfoError == null)
                        return;
                    ServerInfoError();
                }
                else
                {
                    foreach (VersionInfo versionInfo in m_ServerInfo.GameVersion)
                    {
                        if (versionInfo.Version == m_CurVersion)
                        {
                            m_GameVersion = versionInfo;
                            break;
                        }
                    }
                    GetHotAB();
                }
            }));
            ChangeProgressSlider("正在获取版本信息", 1f);
            if (CheckLocalAndServerPatch())
            {
                Debug.Log((object)"需要更新，检查需要更新哪些文件");
                yield return (object)ComputeDownload(true);
                if (File.Exists(m_ServerXmlPath))
                {
                    if (File.Exists(m_LocalXmlPath))
                        File.Delete(m_LocalXmlPath);
                    File.Move(m_ServerXmlPath, m_LocalXmlPath);
                }
                else
                    Debug.LogError((object)("Do not exists file : " + m_ServerXmlPath));
            }
            else
            {
                Debug.Log((object)"不需要更新，但是仍然要检查一下文件的完整性");
                yield return (object)ComputeDownload(true);
            }
            LoadFileCount = m_DownLoadList.Count;
            LoadSumSize = m_DownLoadList.Sum<Patch>((Func<Patch, float>)(x => x.Size));
            m_TargetVersionCheckProgress = 1f;
            if (hotCallBack != null)
                hotCallBack(m_DownLoadList.Count > 0);
        }

        /// <summary>
        /// 检查本地热更信息与服务器热更信息比较
        /// </summary>
        /// <returns></returns>
        private bool CheckLocalAndServerPatch()
        {
            if (!File.Exists(m_LocalXmlPath))
                return true;
            m_LocalInfo = BinarySerializeOpt.XmlDeserialize(m_LocalXmlPath, typeof(ServerInfo)) as ServerInfo;
            VersionInfo versionInfo1 = (VersionInfo)null;
            if (m_LocalInfo != null)
            {
                foreach (VersionInfo versionInfo2 in m_LocalInfo.GameVersion)
                {
                    if (versionInfo2.Version == m_CurVersion)
                    {
                        versionInfo1 = versionInfo2;
                        break;
                    }
                }
            }
            return versionInfo1 != null && m_GameVersion.Pathces != null && (versionInfo1.Pathces != null && m_GameVersion.Pathces.Length != 0) && m_GameVersion.Pathces[m_GameVersion.Pathces.Length - 1].Version != versionInfo1.Pathces[versionInfo1.Pathces.Length - 1].Version;
        }

        /// <summary>
        /// 读取打包时的版本
        /// </summary>
        private void ReadVersion()
        {
            TextAsset textAsset = (TextAsset)Resources.Load<TextAsset>("Version");
            if (textAsset == null)
            {
                Debug.LogError((object)"未读到本地版本！");
            }
            else
            {
                string[] strArray1 = textAsset.text.Split('\n');
                if ((uint)strArray1.Length <= 0U)
                    return;
                string[] strArray2 = strArray1[0].Split(';');
                if (strArray2.Length >= 2)
                {
                    m_CurVersion = strArray2[0].Split('|')[1];
                    m_CurPackName = strArray2[1].Split('|')[1];
                }
            }
        }

        private IEnumerator ReadServerInfoXml(string xmlUrl, Action callBack)
        {
            Debug.Log((object)("ReadServerInfoXml " + xmlUrl));
            UnityWebRequest webRequest = UnityWebRequest.Get(xmlUrl);
            yield return (object)webRequest.SendWebRequest();
            if (webRequest.isNetworkError)
            {
                Debug.Log((object)("Download Error" + webRequest.error));
            }
            else
            {
                yield return (object)Singleton<FileUtil>.Instance.CreateFile(m_ServerXmlPath, webRequest.downloadHandler.data, (Action<float>)null);
                yield return (object)new WaitForEndOfFrame();
                if (File.Exists(m_ServerXmlPath))
                    m_ServerInfo = BinarySerializeOpt.XmlDeserialize(m_ServerXmlPath, typeof(ServerInfo)) as ServerInfo;
                else
                    Debug.LogError((object)"热更配置读取错误！");
            }
            yield return (object)WaitProgressAdd(0.02f);
            if (callBack != null)
                callBack();
        }

        private IEnumerator ReadUnpackMd5Xml(string xmlUrl, string downloadFilePath)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(xmlUrl);
            yield return (object)webRequest.SendWebRequest();
            if (webRequest.isNetworkError)
            {
                Debug.Log((object)("Download Error" + webRequest.error));
            }
            else
            {
                yield return (object)Singleton<FileUtil>.Instance.CreateFile(downloadFilePath, webRequest.downloadHandler.data, (Action<float>)null);
                yield return (object)new WaitForEndOfFrame();
            }
        }

        private float VersionCheckProgress
        {
            get => m_VersionCheckProgress;
            set
            {
                m_VersionCheckProgress = value;
                if ((double)m_VersionCheckProgress == 1.0)
                    ChangeProgressSlider("文件校验完毕", 1f);
                else
                    ChangeProgressSlider("正在校验文件", m_VersionCheckProgress);
            }
        }

        private IEnumerator WaitProgressAdd(float delta)
        {
            while ((double)m_TargetVersionCheckProgress > (double)VersionCheckProgress)
            {
                VersionCheckProgress += delta;
                if ((double)VersionCheckProgress >= 1.0)
                    VersionCheckProgress = 1f;
                yield return (object)new WaitForEndOfFrame();
            }
            yield return (object)null;
        }

        private void OnAddDownLoadListProgressUpdate(float progress)
        {
            if ((double)progress > 1.0)
                progress = 1f;
            m_TargetVersionCheckProgress = progress;
        }

        /// <summary>
        /// 获取所有热更包信息
        /// </summary>
        private void GetHotAB()
        {
            if (m_GameVersion == null || m_GameVersion.Pathces == null || (uint)m_GameVersion.Pathces.Length <= 0U)
                return;
            Pathces pathce = m_GameVersion.Pathces[m_GameVersion.Pathces.Length - 1];
            if (pathce != null && pathce.Files != null)
            {
                foreach (Patch file in pathce.Files)
                    m_HotFixDic.Add(file.Name, file);
            }
        }

        /// <summary>
        /// 计算下载的资源
        /// </summary>
        private IEnumerator ComputeDownload(bool checkZip)
        {
            Debug.Log((object)"计算下载的文件");
            m_DownLoadList.Clear();
            m_DownLoadDic.Clear();
            m_DownLoadMD5Dic.Clear();
            m_VersionCheckProgress = 0.0f;
            if (m_GameVersion != null && m_GameVersion.Pathces != null && (uint)m_GameVersion.Pathces.Length > 0U)
            {
                m_CurrentPatches = m_GameVersion.Pathces[m_GameVersion.Pathces.Length - 1];
                int fileCount = m_CurrentPatches.Files.Count;
                float delta = 1f / (float)fileCount;
                m_ZipMd5Data = new ZipMd5Data();
                m_ZipMd5Data.ZipMd5List = new List<ZipMd5>();
                m_ZipMd5Data.ZipMd5List.Clear();
                if (checkZip)
                    yield return (object)ReadUnpackMd5File();
                if (m_CurrentPatches.Files != null && fileCount > 0)
                {
                    int index = 0;
                    Debug.Log((object)string.Format("当前更新包需要检查{0}个文件包", (object)m_CurrentPatches.Files.Count));
                    foreach (Patch file in m_CurrentPatches.Files)
                    {
                        Patch patch = file;
                        Debug.Log((object)patch.Name);
                        AddDownLoadList(patch, checkZip, GetZipPatchFileMd5(patch));
                        OnAddDownLoadListProgressUpdate((float)(index + 1) * delta);
                        yield return (object)WaitProgressAdd(0.02f);
                        ++index;
                        patch = (Patch)null;
                    }
                }
            }
        }

        private IEnumerator ReadUnpackMd5File()
        {
            string checkFilePath = m_DownLoadPath + "/UnpackMd5_" + CurVersion + "_" + (object)CurrentPatches.Version + ".xml";
            Debug.Log((object)("checkFilePath :" + checkFilePath));
            yield return (object)ReadUnpackMd5Xml(m_CurrentPatches.Files[0].Url, checkFilePath);
            m_ZipMd5Data = BinarySerializeOpt.XmlDeserialize(checkFilePath, typeof(ZipMd5Data)) as ZipMd5Data;
            yield return (object)null;
        }

        private List<ZipBase> GetZipPatchFileMd5(Patch patch)
        {
            string name = patch.Name;
            if (name.EndsWith(".zip") && m_ZipMd5Data != null)
            {
                foreach (ZipMd5 zipMd5 in m_ZipMd5Data.ZipMd5List)
                {
                    if (zipMd5.ZipName == name)
                        return zipMd5.FileList;
                }
            }
            return (List<ZipBase>)null;
        }

        private void AddDownLoadList(Patch patch, bool checkZip, List<ZipBase> zipBaseList)
        {
            Debug.Log((object)("AddDownLoadList : " + patch.Name));
            string str1;
            if (string.IsNullOrEmpty(patch.RelativePath))
                str1 = m_DownLoadPath + "/" + patch.Name;
            else
                str1 = m_DownLoadPath + "/" + patch.RelativePath + "/" + patch.Name;
            if (patch.Name.EndsWith(".zip"))
            {
                if (!checkZip)
                    return;
                Debug.Log((object)(patch.Name + "  校验解压出来的文件md5是否相同"));
                bool flag = true;
                foreach (ZipBase zipBase in zipBaseList)
                {
                    string str2 = !(zipBase.UnpackPath == "") ? string.Format("{0}/{1}/{2}", (object)m_UnPackPath, (object)zipBase.UnpackPath, (object)zipBase.Name) : string.Format("{0}/{1}", (object)m_UnPackPath, (object)zipBase.Name);
                    if (File.Exists(str2))
                    {
                        if (Singleton<MD5Manager>.Instance.BuildFileMd5(str2) != zipBase.Md5)
                        {
                            Debug.Log((object)("<color=yellow>文件发生改变： </color>" + str2));
                            flag = false;
                            break;
                        }
                    }
                    else
                    {
                        Debug.Log((object)("<color=red>文件不存在： </color>" + str2));
                        flag = false;
                        break;
                    }
                }
                if (!flag)
                {
                    m_DownLoadList.Add(patch);
                    m_DownLoadDic.Add(patch.Name, patch);
                    m_DownLoadMD5Dic.Add(patch.Name, patch.Md5);
                }
            }
            else if (File.Exists(str1))
            {
                string str2 = Singleton<MD5Manager>.Instance.BuildFileMd5(str1);
                if (patch.Md5 != str2)
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
        public float GetProgress() => GetLoadSize() / LoadSumSize;

        /// <summary>
        /// 获取已经下载总大小
        /// </summary>
        /// <returns></returns>
        public float GetLoadSize()
        {
            float num1 = m_AlreadyDownList.Sum<Patch>((Func<Patch, float>)(x => x.Size));
            float num2 = 0.0f;
            if (m_CurDownload != null)
            {
                Patch patchByGamePath = FindPatchByGamePath(m_CurDownload.FileName);
                if (patchByGamePath != null && !m_AlreadyDownList.Contains(patchByGamePath))
                    num2 = m_CurDownload.GetProcess() * patchByGamePath.Size;
            }
            return num1 + num2;
        }

        /// <summary>
        /// 开始下载资源包
        /// </summary>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public void StartDownLoad(Action callBack, List<Patch> allPatch = null) => m_Mono.StartCoroutine(StartDownLoadAssets(callBack, allPatch));

        private IEnumerator StartDownLoadAssets(Action callBack, List<Patch> allPatch = null)
        {
            m_AlreadyDownList.Clear();
            StartDownload = true;
            if (allPatch == null)
                allPatch = m_DownLoadList;
            if (!Directory.Exists(m_DownLoadPath))
                Directory.CreateDirectory(m_DownLoadPath);
            List<DownLoadFileItem> downLoadFileItemList = new List<DownLoadFileItem>();
            foreach (Patch patch1 in allPatch)
            {
                Patch patch = patch1;
                Debug.Log((object)("downLoad : " + patch.Url + " --- " + m_DownLoadPath));
                downLoadFileItemList.Add(new DownLoadFileItem(patch, m_DownLoadPath, m_UnPackPath, new Action<string, float>(ChangeProgressSlider), ZipFileCreateEnable, ItemError));
                patch = (Patch)null;
            }
            Debug.Log((object)string.Format("本次下载{0}个文件", (object)downLoadFileItemList.Count));
            foreach (DownLoadFileItem downLoadFileItem in downLoadFileItemList)
            {
                DownLoadFileItem downLoad = downLoadFileItem;
                m_CurDownload = downLoad;
                yield return (object)m_Mono.StartCoroutine(downLoad.Download((Action)null));
                Patch patch = FindPatchByGamePath(downLoad.FileName);
                if (patch != null)
                    m_AlreadyDownList.Add(patch);
                m_CurDownload = (DownLoadFileItem)null;
                downLoad.Destory();
                patch = (Patch)null;
                downLoad = (DownLoadFileItem)null;
            }
            yield return (object)VerifyMD5(downLoadFileItemList, callBack);
        }

        /// <summary>
        /// Md5码校验
        /// </summary>
        /// <param name="downLoadAssets"></param>
        /// <param name="callBack"></param>
        private IEnumerator VerifyMD5(List<DownLoadFileItem> downLoadAssets, Action callBack)
        {
            yield return (object)ComputeDownload(true);
            if (m_DownLoadList.Count <= 0)
            {
                m_DownLoadMD5Dic.Clear();
                StartDownload = false;
                DownloadFinish = true;
                if (callBack != null)
                    callBack();
                if (LoadOver != null)
                    LoadOver();
                Debug.Log((object)"LoadOver 检查完毕，文件完整！");
            }
            else if (m_TryDownCount >= 2)
            {
                string allName = "";
                StartDownload = false;
                foreach (Patch downLoad in m_DownLoadList)
                {
                    Patch patch = downLoad;
                    allName = allName + patch.Name + ";";
                    patch = (Patch)null;
                }
                Debug.LogError((object)string.Format("资源重复下载{0}次MD5校验都失败，请检查资源 : {1}", (object)2, (object)allName));
                Action<string> itemError = ItemError;
                if (itemError != null)
                    itemError("资源重复下载失败，请检查资源");
                allName = (string)null;
            }
            else
            {
                ++m_TryDownCount;
                m_DownLoadMD5Dic.Clear();
                foreach (Patch downLoad in m_DownLoadList)
                {
                    Patch patch = downLoad;
                    m_DownLoadMD5Dic.Add(patch.Name, patch.Md5);
                    patch = (Patch)null;
                }
                StartDownLoad(callBack, m_DownLoadList);
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
            m_DownLoadDic.TryGetValue(name, out patch);
            return patch;
        }
    }
}