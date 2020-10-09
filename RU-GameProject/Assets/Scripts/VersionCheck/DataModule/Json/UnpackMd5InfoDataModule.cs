using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LitJson;

namespace Esp.VersionCheck.DataModule.Json
{
	[Serializable]
	public class UnpackMd5InfoDataModule
    {
        public UnpackMd5InfoDataModule()
        {
            ZipMd5List = new List<ZipMd5>();
        }


        public List<ZipMd5> ZipMd5List;

		public UnpackMd5InfoDataModule(JsonData data)
		{
            ZipMd5List = new List<ZipMd5>();
			foreach (JsonData item in data)
			{
                ZipMd5List.Add(new ZipMd5(item));
			}
		}
	}

	[Serializable]
	public class ZipMd5
	{
        public ZipMd5()
        {
            FileList = new List<ZipBase>();
        }

        public string ZipName;
		public List<ZipBase> FileList;

		public ZipMd5(JsonData data)
        {
            ZipName = data["ZipName"].ToString();
            FileList = new List<ZipBase>();

            foreach (JsonData item in data["FileList"])
            {
                FileList.Add(new ZipBase(item));
            }
        }

	}

    [Serializable]
    public class ZipBase
	{
        public ZipBase() { }

        public string Name;
        public string Md5;
        public string PackageName;
        public string UnpackPath;

        public ZipBase(JsonData data)
        {
            Name = data["Name"].ToString();
            Md5 = data["Md5"].ToString();
            PackageName = data["ZipName"].ToString();
            UnpackPath = data["UnPackPath"].ToString();
        }
    }
}
