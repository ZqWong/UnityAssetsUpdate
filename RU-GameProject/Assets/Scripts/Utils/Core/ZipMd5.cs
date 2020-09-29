// Decompiled with JetBrains decompiler
// Type: ETPVersionCheck.VersionUtil.ZipMd5
// Assembly: ETPVersionUtil, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 5C3B3BE6-B4E9-4BFE-BB17-66B03FC0EA25
// Assembly location: E:\Main\Focus\2018Project\Assets\Plugins\ETPVersionUtil.dll

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace RU.Core.Utils.Core
{
  //TODO：改为json
  /// <summary>
  /// Zip包基本信息
  /// </summary>
  [Serializable]
  public class ZipMd5
  {
    [XmlAttribute("ZipName")]
    public string ZipName { get; set; }

    [XmlElement("FileList")]
    public List<ZipBase> FileList { get; set; }
  }
}
