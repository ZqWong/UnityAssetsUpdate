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
            this.Name = name;
            this.NeedUnpackZip = needUnpackZip;
            this.UpdateProgress = updateProgress;
            this.m_ZipFileCreateEnable = zipFileCreateEnable;
        }

        public float DownloadProgress
        {
            get => this.downloadProgress;
            set
            {
                this.downloadProgress = value;
                this.Update();
            }
        }

        public float UnpackZipProgress
        {
            get => this.unpackZipProgress;
            set
            {
                this.unpackZipProgress = value;
                this.Update();
            }
        }

        public float CreateFileProgress
        {
            get => this.createFileProgress;
            set
            {
                this.createFileProgress = value;
                this.Update();
            }
        }

        public DownloadInfo.State CurrentState
        {
            get => this.currentState;
            set
            {
                this.currentState = value;
                this.Update();
            }
        }

        public float GetProgress()
        {
            if (!this.NeedUnpackZip)
                return (float)((double)this.DownloadProgress * 0.699999988079071 + (double)this.CreateFileProgress * 0.300000011920929);
            return this.m_ZipFileCreateEnable ? (float)((double)this.DownloadProgress * 0.400000005960464 + (double)this.CreateFileProgress * 0.300000011920929 + (double)this.UnpackZipProgress * 0.300000011920929) : (float)((double)this.DownloadProgress * 0.699999988079071 + (double)this.UnpackZipProgress * 0.300000011920929);
        }

        public string GetStateInfo()
        {
            switch (this.CurrentState)
            {
                case DownloadInfo.State.None:
                    return "正在准备开始  " + this.Name;
                case DownloadInfo.State.Download:
                    return "正在下载  " + this.Name;
                case DownloadInfo.State.UnpackZip:
                    return "正在解压  " + this.Name;
                case DownloadInfo.State.CreateFile:
                    return "正在创建  " + this.Name;
                case DownloadInfo.State.Completed:
                    return "已完成  " + this.Name;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Update()
        {
            if (this.UpdateProgress == null)
                return;
            this.UpdateProgress(this.GetStateInfo(), this.GetProgress());
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
