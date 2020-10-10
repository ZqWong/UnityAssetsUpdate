using System;
using System.Collections;
using System.IO;
using Esp.Core.Utils.Core;

using UnityEngine;
using UnityEngine.Networking;

#if JSON
using Esp.VersionCheck.DataModule.Json;
#elif XML
using Esp.VersionCheck.DataModule.Xml;
#endif

namespace Esp.Core.Download
{
    public class DownLoadFileItem : DownLoadItemBase
    {
        /// <summary>
        /// Action on download error throw.
        /// </summary>
        private Action<string> OnErrorThrown;


        /// <summary>
        /// Current download file info
        /// </summary>
        private DownloadInfo m_info;

        /// <summary>
        /// Unity web request handler
        /// </summary>
        private UnityWebRequest m_webRequest;

        /// <summary>
        /// File download progress value
        /// </summary>
        private float m_progress = 0.0f;

        /// <summary>
        /// The path to unpack the zip
        /// </summary>
        private string m_unpackPath;

        /// <summary>
        /// 
        /// </summary>
        private bool m_zipFileCreateEnable;

        /// <summary>
        /// Get current download file info
        /// </summary>
        public DownloadInfo Info { get { return m_info; } }

        /// <summary>
        /// Get current download progress value
        /// </summary>
        /// <returns></returns>
        public override float GetProcess()
        {
            return m_progress;
        }

        /// <summary>
        /// Get current download progress value with percent formmat
        /// </summary>
        /// <returns></returns>
        public string GetProcessText()
        {
            return ((m_progress * 100.0) % 100).ToString() + "%";
        }

        /// <summary>
        /// Get need unpack
        /// </summary>
        /// <returns></returns>
        public bool NeedUncompress()
        {
            return m_url.EndsWith(".zip");
        }

        /// <summary>
        /// Get file size (M) useless!
        /// </summary>
        /// <returns></returns>
        public float Size()
        {
            return float.Parse(m_patch.Size.ToString()) / 1024f;
        }

        /// <summary>
        /// Initialize download task
        /// </summary>
        /// <param name="patch">Patch info</param>
        /// <param name="path">Save path</param>
        /// <param name="unpackPath">unpack path</param>
        /// <param name="onUpdateProgress">progress update event</param>
        /// <param name="zipFileCreateEnable"></param>
        /// <param name="onErrorThrown">Error event</param>
        public DownLoadFileItem(Patch patch, string path, string unpackPath, Action<string, float> onUpdateProgress, bool zipFileCreateEnable, Action<string> onErrorThrown) : base(patch, path)
        {
            m_unpackPath = unpackPath;
            m_zipFileCreateEnable = zipFileCreateEnable;
            OnErrorThrown = onErrorThrown;
            m_info = new DownloadInfo(m_fileName, NeedUncompress(), onUpdateProgress, zipFileCreateEnable);
        }

        /// <summary>
        /// Download enumerator
        /// </summary>
        /// <param name="completeCallback"></param>
        /// <returns></returns>
        public new IEnumerator Download(Action completeCallback = null)
        {
            m_startDownLoad = true;
            m_progress = 0.0f;
            m_info.CurrentState = DownloadInfo.State.DOWNLOAD;

            m_webRequest = UnityWebRequest.Get(m_url);
            m_webRequest.SendWebRequest();

            while (m_webRequest.downloadProgress < 1.0)
            {
                if (m_webRequest.error != null)
                {
                    if (null != OnErrorThrown)
                    {
                        OnErrorThrown.Invoke("下载中断，请重启App :" + FileName + " " + m_webRequest.error);
                    }
                    //if (OnErrorThrown != null)
                    //    OnErrorThrown("下载中断，请重启App " + FileName + "       " + m_webRequest.error);
                    yield return new WaitForEndOfFrame();

                    Debug.LogError("<color=red> Download Failed! File Name :" + FileName + " Error Message :" + m_webRequest.error + "</color>");
                }
                yield return new WaitForEndOfFrame();

                m_progress = m_webRequest.downloadProgress;
                m_info.DownloadProgress = m_webRequest.downloadProgress;
            }
            if (m_webRequest.error != null)
            {
                if (null != OnErrorThrown)
                {
                    OnErrorThrown.Invoke("下载中断，请重启App :" + FileName + " " + m_webRequest.error);
                }
                //if (OnErrorThrown != null)
                //    OnErrorThrown("下载异常，请重启App " + FileName + "       " + m_webRequest.error);
                yield return new WaitForEndOfFrame();

                Debug.LogError("<color=red> Download Failed! File Name :" + FileName + " Error Message :" + m_webRequest.error + "</color>");
            }
            yield return new WaitForEndOfFrame();

            m_startDownLoad = false;
            m_progress = 1f;
            m_info.DownloadProgress = 1f;

            yield return new WaitForEndOfFrame();

            if (m_webRequest.isDone && m_webRequest.error == null)
            {
                yield return new WaitForEndOfFrame();

                string dir = Application.persistentDataPath;

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                yield return new WaitForEndOfFrame();

                Debug.Log((m_patch.Name + "   NeedUncompress : " + NeedUncompress().ToString()));

                if (NeedUncompress())
                {
                    m_info.CurrentState = DownloadInfo.State.UNPACK_ZIP;
                    yield return ZipUtil.Instance.SaveZip(m_unpackPath, m_webRequest.downloadHandler.data, OnUnpackZipProgressUpdate);
                    if (m_zipFileCreateEnable)
                    {
                        m_info.CurrentState = DownloadInfo.State.CREATE_FILE;
                        yield return FileUtil.Instance.CreateFile(m_saveFilePath, m_webRequest.downloadHandler.data, OnCreateFileProgressUpdate);
                    }
                }
                else
                {
                    m_info.CurrentState = DownloadInfo.State.CREATE_FILE;
                    yield return FileUtil.Instance.CreateFile(m_saveFilePath, m_webRequest.downloadHandler.data, OnCreateFileProgressUpdate);
                }
                yield return new WaitForEndOfFrame();
                m_info.CurrentState = DownloadInfo.State.COMPLETE;
                yield return new WaitForEndOfFrame();
                if (null != completeCallback)
                {
                    completeCallback.Invoke();
                }
                //if (completeCallback != null)
                //    completeCallback();
                dir = null;
            }
        }

        public void OnUnpackZipProgressUpdate(float progress)
        {
            m_info.UnpackZipProgress = progress;
        }

        public void OnCreateFileProgressUpdate(float progress)
        {
            m_info.CreateFileProgress = progress;
        }

        public override long GetCurLength()
        {
            return (long)(m_webRequest != null ? m_webRequest.downloadedBytes : 0L);
        }

        public override long GetLength()
        {
            return 0;
        }

        public override void Destory()
        {
            if (m_webRequest == null)
                return;
            m_webRequest.Dispose();
            m_webRequest = null;
        }
    }
}
