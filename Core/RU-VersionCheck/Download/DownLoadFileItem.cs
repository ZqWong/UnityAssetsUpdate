using System;
using System.Collections;
using System.IO;
using RU.Core.Utils.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace RU.Core.Download
{
    public class DownLoadFileItem : BaseDownLoadItem
    {
        private Action<string> OnError;
        private DownloadInfo m_Info;
        private UnityWebRequest m_WebRequest;
        private float m_Progress = 0.0f;
        private string m_UnpackPath;
        private bool m_ZipFileCreateEnable;

        public DownloadInfo Info => this.m_Info;

        public override float GetProcess() => this.m_Progress;

        public string GetProcessText() => ((int)((double)this.m_Progress * 100.0) % 100).ToString() + "%";

        public bool NeedUncompress() => this.m_Url.EndsWith(".zip");

        public float Size() => this.m_Patch.Size / 1024f;

        public DownLoadFileItem(
          Patch patch,
          string path,
          string unpackPath,
          Action<string, float> onUpdateProgress,
          bool zipFileCreateEnable,
          Action<string> onError)
          : base(patch, path)
        {
            this.m_UnpackPath = unpackPath;
            this.m_ZipFileCreateEnable = zipFileCreateEnable;
            this.OnError = onError;
            this.m_Info = new DownloadInfo(this.m_FileName, this.NeedUncompress(), onUpdateProgress, zipFileCreateEnable);
        }

        public new IEnumerator Download(Action completeCallback = null)
        {
            this.m_StartDownLoad = true;
            this.m_Progress = 0.0f;
            this.m_Info.CurrentState = DownloadInfo.State.Download;
            this.m_WebRequest = UnityWebRequest.Get(this.m_Url);
            this.m_WebRequest.SendWebRequest();
            while ((double)this.m_WebRequest.downloadProgress < 1.0)
            {
                if (this.m_WebRequest.error != null)
                {
                    if (this.OnError != null)
                        this.OnError("下载中断，请重启App " + this.FileName + "       " + this.m_WebRequest.error);
                    yield return (object)new WaitForEndOfFrame();
                    Debug.LogError((object)this.m_WebRequest.error);
                }
                yield return (object)new WaitForEndOfFrame();
                this.m_Progress = this.m_WebRequest.downloadProgress;
                this.m_Info.DownloadProgress = this.m_WebRequest.downloadProgress;
            }
            if (this.m_WebRequest.error != null)
            {
                if (this.OnError != null)
                    this.OnError("下载异常，请重启App " + this.FileName + "       " + this.m_WebRequest.error);
                yield return (object)new WaitForEndOfFrame();
                Debug.LogError((object)this.m_WebRequest.error);
            }
            yield return (object)new WaitForEndOfFrame();
            this.m_StartDownLoad = false;
            this.m_Progress = 1f;
            this.m_Info.DownloadProgress = 1f;
            yield return (object)new WaitForEndOfFrame();
            if (this.m_WebRequest.isDone && this.m_WebRequest.error == null)
            {
                yield return (object)new WaitForEndOfFrame();
                string dir = Application.persistentDataPath;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                yield return (object)new WaitForEndOfFrame();
                Debug.Log((object)(this.m_Patch.Name + "   NeedUncompress : " + this.NeedUncompress().ToString()));
                if (this.NeedUncompress())
                {
                    this.m_Info.CurrentState = DownloadInfo.State.UnpackZip;
                    yield return (object)Singleton<ZipUtil>.Instance.SaveZip(this.m_UnpackPath, this.m_WebRequest.downloadHandler.data, new Action<float>(this.OnUnpackZipProgressUpdate));
                    if (this.m_ZipFileCreateEnable)
                    {
                        this.m_Info.CurrentState = DownloadInfo.State.CreateFile;
                        yield return (object)Singleton<FileUtil>.Instance.CreateFile(this.m_SaveFilePath, this.m_WebRequest.downloadHandler.data, new Action<float>(this.OnCreateFileProgressUpdate));
                    }
                }
                else
                {
                    this.m_Info.CurrentState = DownloadInfo.State.CreateFile;
                    yield return (object)Singleton<FileUtil>.Instance.CreateFile(this.m_SaveFilePath, this.m_WebRequest.downloadHandler.data, new Action<float>(this.OnCreateFileProgressUpdate));
                }
                yield return (object)new WaitForEndOfFrame();
                this.m_Info.CurrentState = DownloadInfo.State.Completed;
                yield return (object)new WaitForEndOfFrame();
                if (completeCallback != null)
                    completeCallback();
                dir = (string)null;
            }
        }

        public void OnUnpackZipProgressUpdate(float progress) => this.m_Info.UnpackZipProgress = progress;

        public void OnCreateFileProgressUpdate(float progress) => this.m_Info.CreateFileProgress = progress;

        public override long GetCurLength() => this.m_WebRequest != null ? (long)this.m_WebRequest.downloadedBytes : 0L;

        public override long GetLength() => 0;

        public override void Destory()
        {
            if (this.m_WebRequest == null)
                return;
            this.m_WebRequest.Dispose();
            this.m_WebRequest = (UnityWebRequest)null;
        }
    }
}
