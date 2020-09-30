using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RU.Core.Utils.Core;
using RU.Scripts.Utils.Core.StaticJsonFile.VersionInfoData;
using UnityEngine;

namespace RU.Assets.Scripts.Utils.Core.StaticJsonFile
{
    public class StaticJsonManager : Singleton<StaticJsonManager>
    {
        public event Action OnQueueJsonFileEvent;

        private bool m_initialized = false;

        private const string VERSION_DATA_FILENAME = "Version.json";



        private VersionInfoDataManager m_versionInfoDataManager;

        public StaticJsonManager()
        {
            m_versionInfoDataManager = new VersionInfoDataManager(this);
        }

        public void Initialize()
        {
            m_versionInfoDataManager.Initialize(Application.dataPath + "/Resources/" + VERSION_DATA_FILENAME);
        }

        public VersionInfoDataManager VersionInfo
        {
            get { return m_versionInfoDataManager; }
        }
    }
}
