using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LitJson;

namespace Esp.VersionCheck.DataModule.Json
{
    [Serializable]
    public class VersionInfoDataModule
    {
        public string Version;
        public string PackageName;


        public VersionInfoDataModule()
        {
          
        }

        public VersionInfoDataModule(JsonData data)
        {
            Version = data["Version"] == null ? "" : data["Version"].ToString();
            PackageName = data["PackageName"] == null ? "" : data["PackageName"].ToString();
        }
    }
}
