using System.Collections.Generic;
using System.Xml.Serialization;

[System.Serializable]
public class AssetsMd5
{
    [XmlElement("ABMD5List")]
    public List<AssetBase> ABMD5List { get; set; }
}

[System.Serializable]
public class AssetBase
{
    [XmlAttribute("Name")]
    public string Name { get; set; }
    [XmlAttribute("Md5")]
    public string Md5 { get; set; }
    [XmlAttribute("Size")]
    public float Size { get; set; }
    //[XmlAttribute("ZipName")]
    //public string ZipName { get; set; }
    //[XmlAttribute("UnpackPath")]
    //public string UnpackPath { get; set; }
}

[System.Serializable]
public class ZipMd5Data
{
    [XmlElement("ZipMd5List")]
    public List<ZipMd5> ZipMd5List { get; set; }
}

[System.Serializable]
public class ZipMd5
{
    [XmlAttribute("ZipName")]
    public string ZipName { get; set; }
    [XmlElement("FileList")]
    public List<ZipBase> FileList { get; set; }
}

[System.Serializable]
public class ZipBase
{
    [XmlAttribute("Name")]
    public string Name { get; set; }
    [XmlAttribute("Md5")]
    public string Md5 { get; set; }

    [XmlAttribute("ZipName")]
    public string ZipName { get; set; }
    [XmlAttribute("UnpackPath")]
    public string UnpackPath { get; set; }
}