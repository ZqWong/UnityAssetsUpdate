#define JSON

using System;
using System.Collections;
using System.IO;
using Esp.Core.Utils.Core;

#if JSON
using Esp.VersionCheck.DataModule.Json;
#elif XML
using Esp.VersionCheck.DataModule.Xml;
#endif


namespace Esp.Core.Download
{
    /// <summary>
    /// The download item base class
    /// </summary>
    public abstract class DownLoadItemBase
    {
        /// <summary>
        /// Patch info
        /// </summary>
        protected Patch m_patch;
        /// <summary>
        /// Patch url
        /// </summary>
        protected string m_url;
        /// <summary>
        /// Patch save path
        /// </summary>
        protected string m_savePath;
        /// <summary>
        /// Path with out the extension
        /// </summary>
        protected string m_fileNameWithoutExtension;
        /// <summary>
        /// File extension
        /// </summary>
        protected string m_fileExtension;
        /// <summary>
        /// File name
        /// </summary>
        protected string m_fileName;
        /// <summary>
        /// Path to save the file
        /// </summary>
        protected string m_saveFilePath;
        /// <summary>
        /// File length
        /// </summary>
        protected long m_fileLength;
        /// <summary>
        /// Current time the file length
        /// </summary>
        protected long m_currentLength;
        /// <summary>
        /// Is start download task
        /// </summary>
        protected bool m_startDownLoad;

        public string Url { get { return m_url; } }
        public string SavePath { get { return m_savePath; } } 
        public string FileNameWithoutExtension { get { return m_fileNameWithoutExtension; } }
        public string FileExtension { get { return m_fileExtension; } }
        public string FileName { get { return m_fileName; } }
        public string SaveFilePath { get { return m_saveFilePath; } }
        public long FileLength { get { return m_fileLength; } }
        public long CurrentLength { get { return m_currentLength; } }
        public bool StartDownLoad { get { return m_startDownLoad; } }

        /// <summary>
        /// The constructor for BaseDownLoadItem, Initialize the download item info.
        /// </summary>
        /// <param name="patch">Single patch info</param>
        /// <param name="path">Target path</param>
        public DownLoadItemBase(Patch patch, string path)
        {
            m_patch = patch;
            m_url = m_patch.Url;
            m_savePath = path;
            m_startDownLoad = false;
            m_fileNameWithoutExtension = Path.GetFileNameWithoutExtension(m_url);
            m_fileExtension = Path.GetExtension(m_url);
            m_fileName = m_fileNameWithoutExtension + m_fileExtension;
            if (string.IsNullOrEmpty(m_patch.RelativePath))
                m_saveFilePath = m_savePath + "/" + m_fileNameWithoutExtension + m_fileExtension;
            else
                m_saveFilePath = m_savePath + "/" + m_patch.RelativePath + "/" + m_fileNameWithoutExtension + m_fileExtension;
        }

        public virtual IEnumerator Download(Action callback = null)
        {
            yield return null;
        }

        public abstract float GetProcess();

        public abstract long GetCurLength();

        public abstract long GetLength();

        public abstract void Destory();
    }
}