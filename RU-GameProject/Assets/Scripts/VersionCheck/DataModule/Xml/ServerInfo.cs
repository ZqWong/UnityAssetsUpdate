using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Serialization;

namespace Esp.VersionCheck.DataModule.Xml
{

    //TODO：改为json
    /// <summary>
    /// ServerInfo.xml反序列化
    /// </summary>
    [System.Serializable]
    public class ServerInfo
    {
        [XmlElement("GameVersion")] public VersionInfo[] GameVersion;
    }

    //当前游戏版本对应的所有补丁
    [System.Serializable]
    public class VersionInfo
    {
        [XmlAttribute] public string Version;
        [XmlElement] public Patches[] Patches;
    }

    //一个总补丁包
    [System.Serializable]
    public class Patches
    {
        [XmlAttribute] public int Version;

        [XmlAttribute] public string Des;

        [XmlElement] public List<Patch> Files;
    }

    /// <summary>
    /// 单个补丁包
    /// </summary>
    [System.Serializable]
    public class Patch
    {
        [XmlAttribute] public string Name;

        [XmlAttribute] public string Url;

        [XmlAttribute] public string Platform;

        [XmlAttribute] public string Md5;

        [XmlAttribute] public float Size;

        [XmlAttribute] public string RelativePath;
    }
}