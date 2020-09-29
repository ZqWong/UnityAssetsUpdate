// Decompiled with JetBrains decompiler
// Type: ETPVersionCheck.VersionUtil.ZipBase
// Assembly: ETPVersionUtil, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 5C3B3BE6-B4E9-4BFE-BB17-66B03FC0EA25
// Assembly location: E:\Main\Focus\2018Project\Assets\Plugins\ETPVersionUtil.dll

using System;
using System.Xml.Serialization;

namespace RU.Core.Utils.Core
{
  //TODO：改为json
  /// <summary>
  /// Zip包基本信息
  /// </summary>
  [Serializable]
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
}
