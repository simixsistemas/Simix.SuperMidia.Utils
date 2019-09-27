using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Simix.SuperMidia.Utils.Core.Adb {
    [Serializable()]
    [DesignerCategory("code")]
    [XmlRoot("SETTINGS")]
    public partial class Settings {
        [XmlElement("SUPERUSER")]
        public SuperUser SuperUser { get; set; }

        [XmlElement("EXECUTE")]
        public Execute Execute { get; set; }
    }
}
