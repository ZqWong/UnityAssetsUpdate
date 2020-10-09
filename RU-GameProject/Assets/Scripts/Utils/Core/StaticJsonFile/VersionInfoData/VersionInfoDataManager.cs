using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esp.Assets.Scripts.Utils.Core.StaticJsonFile;
using Esp.Assets.Scripts.Utils.Core.StaticJsonFile.VersionInfoData.DataModule;
using Esp.Scripts.Utils.Core.StaticJsonFile.DataModule;
using Esp.Scripts.Utils.Core.StaticJsonFile.Utils;
using UnityEngine;

namespace Esp.Scripts.Utils.Core.StaticJsonFile.VersionInfoData
{
    public class VersionInfoDataManager : JsonDataModule
    {
        private VersionInfoDataModule m_versionInfoData = new VersionInfoDataModule();


        public VersionInfoDataManager(StaticJsonManager manager) : base(manager)
        {

        }

        public VersionInfoDataModule VersionInfo
        {
            get { return m_versionInfoData; }
        }

        protected override void InitializeData()
        {
            m_versionInfoData.Init();
        }

        protected override void ClearData()
        {
            m_versionInfoData.Init();
        }

        protected override void LoadDataFromFile()
        {
            var readVersionInfoModule = FileUtils.DecryptJSONDataFromFile<VersionInfoDataModule>(FilePath);
            if (null != readVersionInfoModule)
            {
                m_versionInfoData = readVersionInfoModule;
            }
        }

        protected override void SaveDataToFile()
        {
            FileUtils.EncryptJSONDataInFile<VersionInfoDataModule>(m_versionInfoData, FilePath);
        }
    }
}
