// Decompiled with JetBrains decompiler
// Type: ETPVersionCheck.VersionUtil.ZipMd5Data
// Assembly: ETPVersionUtil, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 5C3B3BE6-B4E9-4BFE-BB17-66B03FC0EA25
// Assembly location: E:\Main\Focus\2018Project\Assets\Plugins\ETPVersionUtil.dll

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Esp.VersionCheck.DataModule.Xml
{
  //TODO：改为json
  /// <summary>
  /// Zip包基本信息
  /// </summary>
  [Serializable]
  public class ZipMd5Data
  {
    [XmlElement("ZipMd5List")]
    public List<ZipMd5> ZipMd5List { get; set; }
  }
}
