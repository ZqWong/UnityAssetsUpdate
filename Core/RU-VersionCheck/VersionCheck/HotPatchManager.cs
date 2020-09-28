using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RU.Core.Download;
using RU.Core.Utils.Scripts;
using RU.Core.Utils.Scripts.BaseFrame;
using UnityEngine;
using UnityEngine.Networking;
using VersionUpdate.Utils.Core;

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
        private Dictionary<string, Patch> m_HotFixDic = new Dictionary<string, Patch>();
        private List<Patch> m_DownLoadList = new List<Patch>();
        private Dictionary<string, Patch> m_DownLoadDic = new Dictionary<string, Patch>();
        private Dictionary<string, string> m_DownLoadMD5Dic = new Dictionary<string, string>();
        public Action ServerInfoError;
        public Action<string> ItemError;
        public Action LoadOver;
        public List<Patch> m_AlreadyDownList = new List<Patch>();
        public bool DownloadFinish = false;
        public bool StartDownload = false;
        private int m_TryDownCount = 0;
        private const int DOWNLOADCOUNT = 2;
        private DownLoadFileItem m_CurDownload = (DownLoadFileItem)null;
        public bool ZipFileCreateEnable = false;
        public bool StartUnPack = false;
        private ZipMd5Data m_ZipMd5Data;
        public Action<string, float> OnProgressSliderChange;
        private float m_VersionCheckProgress = 0.0f;
        private float m_TargetVersionCheckProgress = 0.0f;

        public string CurVersion => this.m_CurVersion;

        public Pathces CurrentPatches => this.m_CurrentPatches;

        public DownLoadFileItem CurrentDownLoadFileItem => this.m_CurDownload;

        public int LoadFileCount { get; set; } = 0;

        public float LoadSumSize { get; set; } = 0.0f;

        public float UnPackSumSize { get; set; } = 0.0f;

        public float AlreadyUnPackSize { get; set; } = 0.0f;

        private void ChangeProgressSlider(string info, float progress)
        {
            if (this.OnProgressSliderChange == null)
                return;
            this.OnProgressSliderChange(info, progress);
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
            this.m_Mono = mono;
            this.m_UnPackPath = !string.IsNullOrEmpty(unPackPath) ? unPackPath : Application.persistentDataPath + "/Origin";
            this.m_DownLoadPath = !string.IsNullOrEmpty(downLoadPath) ? downLoadPath : Application.persistentDataPath + "/DownLoad";
            this.m_ServerXmlPath = !string.IsNullOrEmpty(serverXmlPath) ? serverXmlPath : Application.persistentDataPath + "/ServerInfo.xml";
            this.m_LocalXmlPath = !string.IsNullOrEmpty(localXmlPath) ? localXmlPath : Application.persistentDataPath + "/LocalInfo.xml";
            this.OnProgressSliderChange = OnProgressSliderChange;
            this.ItemError = OnItemError;
            this.m_Mono.StartCoroutine(this.CheckVersionCoroutine(serverInfoXmlUrl, hotCallBack));
        }

        public IEnumerator CheckVersionCoroutine(string xmlUrl, Action<bool> hotCallBack = null)
        {
            if (!Directory.Exists(Application.persistentDataPath))
                Directory.CreateDirectory(Application.persistentDataPath);
            Debug.Log((object)("正在检查版本信息 " + xmlUrl));
            this.ChangeProgressSlider("正在获取版本信息", 0.0f);
            this.m_TryDownCount = 0;
            this.m_HotFixDic.Clear();
            this.ReadVersion();
            this.ChangeProgressSlider("正在获取版本信息", 0.5f);
            yield return (object)this.ReadServerInfoXml(xmlUrl, (Action)(() =>
            {
                Debug.Log((object)"ReadServerInfoXml Over");
                if (this.m_ServerInfo == null)
                {
                    Debug.LogError((object)"m_ServerInfo == null ");
                    if (this.ServerInfoError == null)
                        return;
                    this.ServerInfoError();
                }
                else
                {
                    foreach (VersionInfo versionInfo in this.m_ServerInfo.GameVersion)
                    {
                        if (versionInfo.Version == this.m_CurVersion)
                        {
                            this.m_GameVersion = versionInfo;
                            break;
                        }
                    }
                    this.GetHotAB();
                }
            }));
            this.ChangeProgressSlider("正在获取版本信息", 1f);
            if (this.CheckLocalAndServerPatch())
            {
                Debug.Log((object)"需要更新，检查需要更新哪些文件");
                yield return (object)this.ComputeDownload(true);
                if (File.Exists(this.m_ServerXmlPath))
                {
                    if (File.Exists(this.m_LocalXmlPath))
                        File.Delete(this.m_LocalXmlPath);
                    File.Move(this.m_ServerXmlPath, this.m_LocalXmlPath);
                }
                else
                    Debug.LogError((object)("Do not exists file : " + this.m_ServerXmlPath));
            }
            else
            {
                Debug.Log((object)"不需要更新，但是仍然要检查一下文件的完整性");
                yield return (object)this.ComputeDownload(true);
            }
            this.LoadFileCount = this.m_DownLoadList.Count;
            this.LoadSumSize = this.m_DownLoadList.Sum<Patch>((Func<Patch, float>)(x => x.Size));
            this.m_TargetVersionCheckProgress = 1f;
            if (hotCallBack != null)
                hotCallBack(this.m_DownLoadList.Count > 0);
        }

        private bool CheckLocalAndServerPatch()
        {
            if (!File.Exists(this.m_LocalXmlPath))
                return true;
            this.m_LocalInfo = BinarySerializeOpt.XmlDeserialize(this.m_LocalXmlPath, typeof(ServerInfo)) as ServerInfo;
            VersionInfo versionInfo1 = (VersionInfo)null;
            if (this.m_LocalInfo != null)
            {
                foreach (VersionInfo versionInfo2 in this.m_LocalInfo.GameVersion)
                {
                    if (versionInfo2.Version == this.m_CurVersion)
                    {
                        versionInfo1 = versionInfo2;
                        break;
                    }
                }
            }
            return versionInfo1 != null && this.m_GameVersion.Pathces != null && (versionInfo1.Pathces != null && this.m_GameVersion.Pathces.Length != 0) && this.m_GameVersion.Pathces[this.m_GameVersion.Pathces.Length - 1].Version != versionInfo1.Pathces[versionInfo1.Pathces.Length - 1].Version;
        }

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
                    this.m_CurVersion = strArray2[0].Split('|')[1];
                    this.m_CurPackName = strArray2[1].Split('|')[1];
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
                yield return (object)Singleton<FileUtil>.Instance.CreateFile(this.m_ServerXmlPath, webRequest.downloadHandler.data, (Action<float>)null);
                yield return (object)new WaitForEndOfFrame();
                if (File.Exists(this.m_ServerXmlPath))
                    this.m_ServerInfo = BinarySerializeOpt.XmlDeserialize(this.m_ServerXmlPath, typeof(ServerInfo)) as ServerInfo;
                else
                    Debug.LogError((object)"热更配置读取错误！");
            }
            yield return (object)this.WaitProgressAdd(0.02f);
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
            get => this.m_VersionCheckProgress;
            set
            {
                this.m_VersionCheckProgress = value;
                if ((double)this.m_VersionCheckProgress == 1.0)
                    this.ChangeProgressSlider("文件校验完毕", 1f);
                else
                    this.ChangeProgressSlider("正在校验文件", this.m_VersionCheckProgress);
            }
        }

        private IEnumerator WaitProgressAdd(float delta)
        {
            while ((double)this.m_TargetVersionCheckProgress > (double)this.VersionCheckProgress)
            {
                this.VersionCheckProgress += delta;
                if ((double)this.VersionCheckProgress >= 1.0)
                    this.VersionCheckProgress = 1f;
                yield return (object)new WaitForEndOfFrame();
            }
            yield return (object)null;
        }

        private void OnAddDownLoadListProgressUpdate(float progress)
        {
            if ((double)progress > 1.0)
                progress = 1f;
            this.m_TargetVersionCheckProgress = progress;
        }

        private void GetHotAB()
        {
            if (this.m_GameVersion == null || this.m_GameVersion.Pathces == null || (uint)this.m_GameVersion.Pathces.Length <= 0U)
                return;
            Pathces pathce = this.m_GameVersion.Pathces[this.m_GameVersion.Pathces.Length - 1];
            if (pathce != null && pathce.Files != null)
            {
                foreach (Patch file in pathce.Files)
                    this.m_HotFixDic.Add(file.Name, file);
            }
        }

        private IEnumerator ComputeDownload(bool checkZip)
        {
            Debug.Log((object)"计算下载的文件");
            this.m_DownLoadList.Clear();
            this.m_DownLoadDic.Clear();
            this.m_DownLoadMD5Dic.Clear();
            this.m_VersionCheckProgress = 0.0f;
            if (this.m_GameVersion != null && this.m_GameVersion.Pathces != null && (uint)this.m_GameVersion.Pathces.Length > 0U)
            {
                this.m_CurrentPatches = this.m_GameVersion.Pathces[this.m_GameVersion.Pathces.Length - 1];
                int fileCount = this.m_CurrentPatches.Files.Count;
                float delta = 1f / (float)fileCount;
                this.m_ZipMd5Data = new ZipMd5Data();
                this.m_ZipMd5Data.ZipMd5List = new List<ZipMd5>();
                this.m_ZipMd5Data.ZipMd5List.Clear();
                if (checkZip)
                    yield return (object)this.ReadUnpackMd5File();
                if (this.m_CurrentPatches.Files != null && fileCount > 0)
                {
                    int index = 0;
                    Debug.Log((object)string.Format("当前更新包需要检查{0}个文件包", (object)this.m_CurrentPatches.Files.Count));
                    foreach (Patch file in this.m_CurrentPatches.Files)
                    {
                        Patch patch = file;
                        Debug.Log((object)patch.Name);
                        this.AddDownLoadList(patch, checkZip, this.GetZipPatchFileMd5(patch));
                        this.OnAddDownLoadListProgressUpdate((float)(index + 1) * delta);
                        yield return (object)this.WaitProgressAdd(0.02f);
                        ++index;
                        patch = (Patch)null;
                    }
                }
            }
        }

        private IEnumerator ReadUnpackMd5File()
        {
            string checkFilePath = this.m_DownLoadPath + "/UnpackMd5_" + this.CurVersion + "_" + (object)this.CurrentPatches.Version + ".xml";
            Debug.Log((object)("checkFilePath :" + checkFilePath));
            yield return (object)this.ReadUnpackMd5Xml(this.m_CurrentPatches.Files[0].Url, checkFilePath);
            this.m_ZipMd5Data = BinarySerializeOpt.XmlDeserialize(checkFilePath, typeof(ZipMd5Data)) as ZipMd5Data;
            yield return (object)null;
        }

        private List<ZipBase> GetZipPatchFileMd5(Patch patch)
        {
            string name = patch.Name;
            if (name.EndsWith(".zip") && this.m_ZipMd5Data != null)
            {
                foreach (ZipMd5 zipMd5 in this.m_ZipMd5Data.ZipMd5List)
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
                str1 = this.m_DownLoadPath + "/" + patch.Name;
            else
                str1 = this.m_DownLoadPath + "/" + patch.RelativePath + "/" + patch.Name;
            if (patch.Name.EndsWith(".zip"))
            {
                if (!checkZip)
                    return;
                Debug.Log((object)(patch.Name + "  校验解压出来的文件md5是否相同"));
                bool flag = true;
                foreach (ZipBase zipBase in zipBaseList)
                {
                    string str2 = !(zipBase.UnpackPath == "") ? string.Format("{0}/{1}/{2}", (object)this.m_UnPackPath, (object)zipBase.UnpackPath, (object)zipBase.Name) : string.Format("{0}/{1}", (object)this.m_UnPackPath, (object)zipBase.Name);
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
                    this.m_DownLoadList.Add(patch);
                    this.m_DownLoadDic.Add(patch.Name, patch);
                    this.m_DownLoadMD5Dic.Add(patch.Name, patch.Md5);
                }
            }
            else if (File.Exists(str1))
            {
                string str2 = Singleton<MD5Manager>.Instance.BuildFileMd5(str1);
                if (patch.Md5 != str2)
                {
                    this.m_DownLoadList.Add(patch);
                    this.m_DownLoadDic.Add(patch.Name, patch);
                    this.m_DownLoadMD5Dic.Add(patch.Name, patch.Md5);
                }
            }
            else
            {
                this.m_DownLoadList.Add(patch);
                this.m_DownLoadDic.Add(patch.Name, patch);
                this.m_DownLoadMD5Dic.Add(patch.Name, patch.Md5);
            }
        }

        public float GetProgress() => this.GetLoadSize() / this.LoadSumSize;

        public float GetLoadSize()
        {
            float num1 = this.m_AlreadyDownList.Sum<Patch>((Func<Patch, float>)(x => x.Size));
            float num2 = 0.0f;
            if (this.m_CurDownload != null)
            {
                Patch patchByGamePath = this.FindPatchByGamePath(this.m_CurDownload.FileName);
                if (patchByGamePath != null && !this.m_AlreadyDownList.Contains(patchByGamePath))
                    num2 = this.m_CurDownload.GetProcess() * patchByGamePath.Size;
            }
            return num1 + num2;
        }

        public void StartDownLoad(Action callBack, List<Patch> allPatch = null) => this.m_Mono.StartCoroutine(this.StartDownLoadAssets(callBack, allPatch));

        private IEnumerator StartDownLoadAssets(Action callBack, List<Patch> allPatch = null)
        {
            this.m_AlreadyDownList.Clear();
            this.StartDownload = true;
            if (allPatch == null)
                allPatch = this.m_DownLoadList;
            if (!Directory.Exists(this.m_DownLoadPath))
                Directory.CreateDirectory(this.m_DownLoadPath);
            List<DownLoadFileItem> downLoadFileItemList = new List<DownLoadFileItem>();
            foreach (Patch patch1 in allPatch)
            {
                Patch patch = patch1;
                Debug.Log((object)("downLoad : " + patch.Url + " --- " + this.m_DownLoadPath));
                downLoadFileItemList.Add(new DownLoadFileItem(patch, m_DownLoadPath, m_UnPackPath, new Action<string, float>(this.ChangeProgressSlider), this.ZipFileCreateEnable, this.ItemError));
                patch = (Patch)null;
            }
            Debug.Log((object)string.Format("本次下载{0}个文件", (object)downLoadFileItemList.Count));
            foreach (DownLoadFileItem downLoadFileItem in downLoadFileItemList)
            {
                DownLoadFileItem downLoad = downLoadFileItem;
                this.m_CurDownload = downLoad;
                yield return (object)this.m_Mono.StartCoroutine(downLoad.Download((Action)null));
                Patch patch = this.FindPatchByGamePath(downLoad.FileName);
                if (patch != null)
                    this.m_AlreadyDownList.Add(patch);
                this.m_CurDownload = (DownLoadFileItem)null;
                downLoad.Destory();
                patch = (Patch)null;
                downLoad = (DownLoadFileItem)null;
            }
            yield return (object)this.VerifyMD5(downLoadFileItemList, callBack);
        }

        private IEnumerator VerifyMD5(List<DownLoadFileItem> downLoadAssets, Action callBack)
        {
            yield return (object)this.ComputeDownload(true);
            if (this.m_DownLoadList.Count <= 0)
            {
                this.m_DownLoadMD5Dic.Clear();
                this.StartDownload = false;
                this.DownloadFinish = true;
                if (callBack != null)
                    callBack();
                if (this.LoadOver != null)
                    this.LoadOver();
                Debug.Log((object)"LoadOver 检查完毕，文件完整！");
            }
            else if (this.m_TryDownCount >= 2)
            {
                string allName = "";
                this.StartDownload = false;
                foreach (Patch downLoad in this.m_DownLoadList)
                {
                    Patch patch = downLoad;
                    allName = allName + patch.Name + ";";
                    patch = (Patch)null;
                }
                Debug.LogError((object)string.Format("资源重复下载{0}次MD5校验都失败，请检查资源 : {1}", (object)2, (object)allName));
                Action<string> itemError = this.ItemError;
                if (itemError != null)
                    itemError("资源重复下载失败，请检查资源");
                allName = (string)null;
            }
            else
            {
                ++this.m_TryDownCount;
                this.m_DownLoadMD5Dic.Clear();
                foreach (Patch downLoad in this.m_DownLoadList)
                {
                    Patch patch = downLoad;
                    this.m_DownLoadMD5Dic.Add(patch.Name, patch.Md5);
                    patch = (Patch)null;
                }
                this.StartDownLoad(callBack, this.m_DownLoadList);
            }
        }

        private Patch FindPatchByGamePath(string name)
        {
            Patch patch;
            this.m_DownLoadDic.TryGetValue(name, out patch);
            return patch;
        }
    }
}