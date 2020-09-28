using System;
using System.Collections;
using System.IO;
using RU.Core.Utils.Scripts;

namespace RU.Core.Download
{
    public abstract class BaseDownLoadItem
    {
        protected Patch m_Patch;
        protected string m_Url;
        protected string m_SavePath;
        protected string m_FileNameWithoutExt;
        protected string m_FileExt;
        protected string m_FileName;
        protected string m_SaveFilePath;
        protected long m_FileLength;
        protected long m_CurLength;
        protected bool m_StartDownLoad;

        public string Url => m_Url;

        public string SavePath => m_SavePath;

        public string FileNameWithoutExt => m_FileNameWithoutExt;

        public string FileExt => m_FileExt;

        public string FileName => m_FileName;

        public string SaveFilePath => m_SaveFilePath;

        public long FileLength => m_FileLength;

        public long CurLength => m_CurLength;

        public bool StartDownLoad => m_StartDownLoad;

        public BaseDownLoadItem(Patch patch, string path)
        {
            m_Patch = patch;
            m_Url = m_Patch.Url;
            m_SavePath = path;
            m_StartDownLoad = false;
            m_FileNameWithoutExt = Path.GetFileNameWithoutExtension(m_Url);
            m_FileExt = Path.GetExtension(m_Url);
            m_FileName = m_FileNameWithoutExt + m_FileExt;
            if (string.IsNullOrEmpty(m_Patch.RelativePath))
                m_SaveFilePath = m_SavePath + "/" + m_FileNameWithoutExt + m_FileExt;
            else
                m_SaveFilePath = m_SavePath + "/" + m_Patch.RelativePath + "/" + m_FileNameWithoutExt + m_FileExt;
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