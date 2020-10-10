using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LitJson;

namespace Esp.VersionCheck.DataModule.Json
{
    [Serializable]
    public class ServerInfoDataModule
    {
        public ServerInfoDataModule()
        {
            GameVersionInfos = new List<GameVersionInfo>();
        }

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
        public GameVersionInfo()
        {
            PatchInfos = new List<Patches>();
        }

        public string GameVersion;
        public List<Patches> PatchInfos;

        public GameVersionInfo(JsonData data)
        {
            PatchInfos = new List<Patches>();

            GameVersion = data["GameVersion"] == null ? "" : data["GameVersion"].ToString();
            foreach (JsonData item in data["Patches"])
            {
                PatchInfos.Add(new Patches(item));
            }
        }
    }

    [Serializable]
    public class Patches
    {
        public Patches()
        {
            Files = new List<Patch>();
        }

        public string Version;
        public string Des;
        public List<Patch> Files;

        public Patches(JsonData data)
        {
            Files = new List<Patch>();

            Version = data["Version"] == null ? "" : data["Version"].ToString();
            Des = data["Des"] == null ? "" : data["Des"].ToString();

            foreach (JsonData item in data["Packages"])
            {
                Files.Add(new Patch(item));
            }
        }
    }

    [Serializable]
    public class Patch
    {
        public Patch() { }

        public string Name = string.Empty;
        public string Url = string.Empty;
        public string Platform = string.Empty;
        public string Md5 = string.Empty;
        public string Size = string.Empty;
        public string RelativePath = string.Empty;

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
