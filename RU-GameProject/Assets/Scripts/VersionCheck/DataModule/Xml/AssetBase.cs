using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Esp.VersionCheck.DataModule.Xml
{
    // TODO：改为JSON
    /// <summary>
    /// 更新文件中ServerInfo中每个Patch的基本信息
    /// </summary>
    [Serializable]
    public class AssetBase
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Md5")]
        public string Md5 { get; set; }

        [XmlAttribute("Size")]
        public float Size { get; set; }
    }
}
