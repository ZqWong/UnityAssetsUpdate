using System;

namespace RU.Core.Download
{
    public class DownloadInfo
    {
        public string Name;
        public bool NeedUnpackZip;
        private float downloadProgress = 0.0f;
        private float unpackZipProgress = 0.0f;
        private float createFileProgress = 0.0f;
        public DownloadInfo.State currentState = DownloadInfo.State.None;
        private Action<string, float> UpdateProgress;
        private bool m_ZipFileCreateEnable;

        public DownloadInfo(
          string name,
          bool needUnpackZip,
          Action<string, float> updateProgress,
          bool zipFileCreateEnable)
        {
            Name = name;
            NeedUnpackZip = needUnpackZip;
            UpdateProgress = updateProgress;
            m_ZipFileCreateEnable = zipFileCreateEnable;
        }

        public float DownloadProgress
        {
            get => downloadProgress;
            set
            {
                downloadProgress = value;
                Update();
            }
        }

        public float UnpackZipProgress
        {
            get => unpackZipProgress;
            set
            {
                unpackZipProgress = value;
                Update();
            }
        }

        public float CreateFileProgress
        {
            get => createFileProgress;
            set
            {
                createFileProgress = value;
                Update();
            }
        }

        public DownloadInfo.State CurrentState
        {
            get => currentState;
            set
            {
                currentState = value;
                Update();
            }
        }

        public float GetProgress()
        {
            if (!NeedUnpackZip)
                return (DownloadProgress * 0.699999988079071 + CreateFileProgress * 0.300000011920929);
            return m_ZipFileCreateEnable ? (DownloadProgress * 0.400000005960464 + CreateFileProgress * 0.300000011920929 + UnpackZipProgress * 0.300000011920929) : (DownloadProgress * 0.699999988079071 + UnpackZipProgress * 0.300000011920929);
        }

        public string GetStateInfo()
        {
            switch (CurrentState)
            {
                case DownloadInfo.State.None:
                    return "正在准备开始  " + Name;
                case DownloadInfo.State.Download:
                    return "正在下载  " + Name;
                case DownloadInfo.State.UnpackZip:
                    return "正在解压  " + Name;
                case DownloadInfo.State.CreateFile:
                    return "正在创建  " + Name;
                case DownloadInfo.State.Completed:
                    return "已完成  " + Name;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Update()
        {
            if (UpdateProgress == null)
                return;
            UpdateProgress(GetStateInfo(), GetProgress());
        }

        public enum State
        {
            None,
            Download,
            UnpackZip,
            CreateFile,
            Completed,
        }
    }
}
