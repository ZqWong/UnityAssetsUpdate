// Decompiled with JetBrains decompiler
// Type: ETPVersionCheck.VersionUtil.AssetsMd5
// Assembly: ETPVersionUtil, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 5C3B3BE6-B4E9-4BFE-BB17-66B03FC0EA25
// Assembly location: E:\Main\Focus\2018Project\Assets\Plugins\ETPVersionUtil.dll

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace RU.Core.Utils.Core
{
  
  // TODO：改为JSON
  /// <summary>
  /// AssetBase中的所有文件信息
  /// </summary>
  [Serializable]
  public class AssetsMd5
  {
    [XmlElement("ABMD5List")]
    public List<AssetBase> ABMD5List { get; set; }
  }
}
