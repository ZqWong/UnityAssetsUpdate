using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LitJson;

namespace Assets.Scripts.VersionCheck.ServerInfoDataModule
{
    [Serializable]
    public class ServerInfoDataModule
    {

        public List<GameVersionInfo> GameVersionInfos;

        public ServerInfoDataModule(JsonData data)
        {
            GameVersionInfos = new List<GameVersionInfo>();
            foreach (JsonData item in data)
            {
                GameVersionInfos.Add(new GameVersionInfo(item));
            }
        }
    }

    [Serializable]
    public class GameVersionInfo
    {
        public string GameVersion;
        public List<PatchInfos> PatchInfos;

        public GameVersionInfo(JsonData data)
        {
            PatchInfos = new List<PatchInfos>();

            GameVersion = data["GameVersion"] == null ? "" : data["GameVersion"].ToString();
            foreach (JsonData item in data["Patches"])
            {
                PatchInfos.Add(new PatchInfos(item));
            }
        }
    }

    [Serializable]
    public class PatchInfos
    {
        public string Version;
        public string Des;
        public List<Patch> Patches;

        public PatchInfos(JsonData data)
        {
            Patches = new List<Patch>();

            Version = data["Version"] == null ? "" : data["Version"].ToString();
            Des = data["Des"] == null ? "" : data["Des"].ToString();

            foreach (JsonData item in data["Packages"])
            {
                Patches.Add(new Patch(item));
            }
        }
    }

    [Serializable]
    public class Patch
    {
        public string Name;
        public string Url;
        public string Platform;
        public string Md5;
        public string Size;
        public string RelativePath;

        public Patch(JsonData data)
        {
            Name = data["Name"] == null ? "" : data["Name"].ToString();
            Url = data["Url"] == null ? "" : data["Url"].ToString();
            Platform = data["Platform"] == null ? "" : data["Platform"].ToString();
            Md5 = data["Md5"] == null ? "" : data["Md5"].ToString();
            Size = data["Size"] == null ? "" : data["Size"].ToString();
            RelativePath = data["RelativePath"] == null ? "" : data["RelativePath"].ToString();
        }
    }
}
