using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Esp.VersionCheck.DataModule.Json
{
    [Serializable]
    public class ItemData
    {
        public List<LocalVersionInfoDataModule> LocalVersionInfoData;

        public ItemData()
        {
            LocalVersionInfoData = new List<LocalVersionInfoDataModule>();
        }
    }

    [Serializable]
    public class LocalVersionInfoDataModule
    {
        public string GameVersion;
        public string BranchName;
        public string AssetVersion;
    }
}
