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

        /// <summary>
        /// 以APP Version 做区分的更新列表
        /// </summary>
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

        /// <summary>
        /// APP Version
        /// </summary>
        public string GameVersion;
        /// <summary>
        /// 对应ServerInfo.json中的 Patches字段
        /// </summary>
        public List<Patches> PatchInfos;

        public GameVersionInfo(JsonData data)
        {
            PatchInfos = new List<Patches>();

            GameVersion = data["GameVersion"] == null ? "" : data["GameVersion"].ToString();
            foreach (JsonData item in data["Patches"])
            {
                PatchInfos.Add(new Patches(item));
            }

            // 对每个大版本热更的小更新进行重新排序
            PatchInfos = PatchInfos.OrderBy(i => int.Parse(i.Version)).ToList();
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

        /// <summary>
        /// 对应Files
        /// </summary>
        public List<Patch> Files;

        public Patches(JsonData data)
        {
            Files = new List<Patch>();

            Version = data["Version"] == null ? "" : data["Version"].ToString();
            Des = data["Des"] == null ? "" : data["Des"].ToString();

            foreach (JsonData item in data["Files"])
            {
                Files.Add(new Patch(item));
            }
        }
    }

    /// <summary>
    /// 单个更新文件信息
    /// </summary>
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
