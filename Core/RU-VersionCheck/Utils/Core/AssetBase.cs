using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace RU.Core.Utils.Core
{
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
