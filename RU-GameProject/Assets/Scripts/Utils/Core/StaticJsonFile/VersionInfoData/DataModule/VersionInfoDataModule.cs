using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LitJson;

namespace Esp.Assets.Scripts.Utils.Core.StaticJsonFile.VersionInfoData.DataModule
{
    [Serializable]
    public class VersionInfoDataModule
    {
        private const string VERSION_FIELD = "Version";
        private const string PACKAGE_NAME_FIELD = "PackageName";



        public string Version;
        public string PackageName;


        public VersionInfoDataModule()
        {
            Init(null);
        }

        public VersionInfoDataModule(string memberGUID = null)
        {
            Init(memberGUID);
        }

        public VersionInfoDataModule(JsonData data)
        {
            Version = data["Version"] == null ? "" : data["Version"].ToString();
            PackageName = data["PackageName"] == null ? "" : data["PackageName"].ToString();
        }

        public void Init(string memberGUID = null)
        {
            Version = string.Empty;
            PackageName = string.Empty;
        }

        public string ToJson()
        {
            JsonData data = new JsonData();
            data["Version"] = Version;
            data["PackageName"] = PackageName;
            return data.ToJson();
        }
    }
}
