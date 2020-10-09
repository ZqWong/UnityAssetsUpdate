using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esp.Assets.Scripts.Utils.Core.StaticJsonFile;
using UnityEngine;

namespace Esp.Scripts.Utils.Core.StaticJsonFile.DataModule
{
    public abstract class JsonDataModule
    {
        public bool Initialized { get; private set; }
        protected string FilePath { get; private set; }

        private StaticJsonManager m_manager = null;

        protected JsonDataModule(StaticJsonManager manager)
        {
            Debug.Assert(null != manager, "Unexpected null manager");
            m_manager = manager;
        }

        public virtual void Initialize(string filePath)
        {
            Debug.Assert(!Initialized, "Invalid UserProfileDataModule control flow");
            Debug.Assert(!string.IsNullOrEmpty(filePath) && Directory.Exists(Path.GetDirectoryName(filePath)), "Directory doesn't exist: " + filePath);
            FilePath = filePath;
            InitializeData();
            //if (m_manager.ShouldSyncUserData)
            //{
                LoadDataFromFile();
            //}
            Initialized = true;
        }

        public void Deinitialize()
        {
            if (Initialized)
            {
                //if (m_manager.ShouldSyncUserData)
                //{
                    SaveDataToFile();
                //}
                ClearData();
                FilePath = null;
                Initialized = false;

                InitializeData();
                LoadDataFromFile();
            }
        }


        protected abstract void InitializeData();
        protected abstract void ClearData();
        protected abstract void LoadDataFromFile();
        protected abstract void SaveDataToFile();


    }
}
