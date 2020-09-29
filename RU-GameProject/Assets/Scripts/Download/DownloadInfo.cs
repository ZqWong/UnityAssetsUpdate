using System;

namespace RU.Core.Download
{
    public class DownloadInfo
    {
        /// <summary>
        /// Download task name
        /// </summary>
        public string Name;

        /// <summary>
        /// Is need unpack zip
        /// </summary>
        public bool NeedUnpackZip;

        /// <summary>
        /// Currrent file prasing status
        /// </summary>
        public DownloadInfo.State currentState = DownloadInfo.State.NONE;

        /// <summary>
        /// Current download progress
        /// </summary>
        private float downloadProgress = 0.0f;

        /// <summary>
        /// Current unpack progress
        /// </summary>
        private float unpackZipProgress = 0.0f;

        /// <summary>
        /// Current create file progress
        /// </summary>
        private float createFileProgress = 0.0f;

        /// <summary>
        /// Current download progress value
        /// </summary>
        private Action<string, float> UpdateProgress;

        /// <summary>
        /// 
        /// </summary>
        private bool m_zipFileCreateEnable;

        public DownloadInfo(
          string name,
          bool needUnpackZip,
          Action<string, float> updateProgress,
          bool zipFileCreateEnable)
        {
            Name = name;
            NeedUnpackZip = needUnpackZip;
            UpdateProgress = updateProgress;
            m_zipFileCreateEnable = zipFileCreateEnable;
        }

        public float DownloadProgress
        {
            get { return downloadProgress; }
            set
            {
                downloadProgress = value;
                Update();
            }
        }

        public float UnpackZipProgress
        {
            get { return unpackZipProgress; }
            set
            {
                unpackZipProgress = value;
                Update();
            }
        }

        public float CreateFileProgress
        {
            get { return createFileProgress; }
            set
            {
                createFileProgress = value;
                Update();
            }
        }

        public DownloadInfo.State CurrentState
        {
            get { return currentState; }
            set
            {
                currentState = value;
                Update();
            }
        }

        public float GetProgress()
        {
            if (!NeedUnpackZip)
                return (float)((double)DownloadProgress * 0.699999988079071 + (double)CreateFileProgress * 0.300000011920929);
            return m_zipFileCreateEnable ? (float)((double)DownloadProgress * 0.400000005960464 + (double)CreateFileProgress * 0.300000011920929 + (double)UnpackZipProgress * 0.300000011920929) : (float)((double)DownloadProgress * 0.699999988079071 + (double)UnpackZipProgress * 0.300000011920929);
        }

        public string GetStateInfo()
        {
            switch (CurrentState)
            {
                case DownloadInfo.State.NONE:
                    return "正在准备开始 :" + Name;
                case DownloadInfo.State.DOWNLOAD:
                    return "正在下载 :" + Name;
                case DownloadInfo.State.UNPACK_ZIP:
                    return "正在解压 :" + Name;
                case DownloadInfo.State.CREATE_FILE:
                    return "正在创建 :" + Name;
                case DownloadInfo.State.COMPLETE:
                    return "已完成 :" + Name;
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
            NONE,
            DOWNLOAD,
            UNPACK_ZIP,
            CREATE_FILE,
            COMPLETE,
        }
    }
}
