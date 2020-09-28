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

        public DownloadInfo Info => m_Info;

        public override float GetProcess() => m_Progress;

        public string GetProcessText() => ((m_Progress * 100.0) % 100).ToString() + "%";

        public bool NeedUncompress() => m_Url.EndsWith(".zip");

        public float Size() => m_Patch.Size / 1024f;

        public DownLoadFileItem(
          Patch patch,
          string path,
          string unpackPath,
          Action<string, float> onUpdateProgress,
          bool zipFileCreateEnable,
          Action<string> onError)
          : base(patch, path)
        {
            m_UnpackPath = unpackPath;
            m_ZipFileCreateEnable = zipFileCreateEnable;
            OnError = onError;
            m_Info = new DownloadInfo(m_FileName, NeedUncompress(), onUpdateProgress, zipFileCreateEnable);
        }

        public new IEnumerator Download(Action completeCallback = null)
        {
            m_StartDownLoad = true;
            m_Progress = 0.0f;
            m_Info.CurrentState = DownloadInfo.State.Download;
            m_WebRequest = UnityWebRequest.Get(m_Url);
            m_WebRequest.SendWebRequest();
            while (m_WebRequest.downloadProgress < 1.0)
            {
                if (m_WebRequest.error != null)
                {
                    if (OnError != null)
                        OnError("下载中断，请重启App " + FileName + "       " + m_WebRequest.error);
                    yield return new WaitForEndOfFrame();
                    Debug.LogError(m_WebRequest.error);
                }
                yield return new WaitForEndOfFrame();
                m_Progress = m_WebRequest.downloadProgress;
                m_Info.DownloadProgress = m_WebRequest.downloadProgress;
            }
            if (m_WebRequest.error != null)
            {
                if (OnError != null)
                    OnError("下载异常，请重启App " + FileName + "       " + m_WebRequest.error);
                yield return new WaitForEndOfFrame();
                Debug.LogError(m_WebRequest.error);
            }
            yield return new WaitForEndOfFrame();
            m_StartDownLoad = false;
            m_Progress = 1f;
            m_Info.DownloadProgress = 1f;
            yield return new WaitForEndOfFrame();
            if (m_WebRequest.isDone && m_WebRequest.error == null)
            {
                yield return new WaitForEndOfFrame();
                string dir = Application.persistentDataPath;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                yield return new WaitForEndOfFrame();
                Debug.Log((m_Patch.Name + "   NeedUncompress : " + NeedUncompress().ToString()));
                if (NeedUncompress())
                {
                    m_Info.CurrentState = DownloadInfo.State.UnpackZip;
                    yield return Singleton<ZipUtil>.Instance.SaveZip(m_UnpackPath, m_WebRequest.downloadHandler.data, OnUnpackZipProgressUpdate);
                    if (m_ZipFileCreateEnable)
                    {
                        m_Info.CurrentState = DownloadInfo.State.CreateFile;
                        yield return Singleton<FileUtil>.Instance.CreateFile(m_SaveFilePath, m_WebRequest.downloadHandler.data, OnCreateFileProgressUpdate);
                    }
                }
                else
                {
                    m_Info.CurrentState = DownloadInfo.State.CreateFile;
                    yield return Singleton<FileUtil>.Instance.CreateFile(m_SaveFilePath, m_WebRequest.downloadHandler.data, OnCreateFileProgressUpdate);
                }
                yield return new WaitForEndOfFrame();
                m_Info.CurrentState = DownloadInfo.State.Completed;
                yield return new WaitForEndOfFrame();
                if (completeCallback != null)
                    completeCallback();
                dir = null;
            }
        }

        public void OnUnpackZipProgressUpdate(float progress) => m_Info.UnpackZipProgress = progress;

        public void OnCreateFileProgressUpdate(float progress) => m_Info.CreateFileProgress = progress;

        public override long GetCurLength() => (long)(m_WebRequest != null ? m_WebRequest.downloadedBytes : 0L);

        public override long GetLength() => 0;

        public override void Destory()
        {
            if (m_WebRequest == null)
                return;
            m_WebRequest.Dispose();
            m_WebRequest = null;
        }
    }
}
