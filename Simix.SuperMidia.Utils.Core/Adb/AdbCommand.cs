using System.Xml.Serialization;

namespace Simix.SuperMidia.Utils.Core.Adb {
    public abstract class AdbCommand {
        [XmlElement("UNINSTALL", IsNullable = true)]
        public string[] Uninstall { get; set; }

        [XmlElement("DISABLE", IsNullable = true)]
        public string[] Disable { get; set; }

        [XmlElement("CUSTOM", IsNullable = true)]
        public string[] Custom { get; set; }

        public bool HasCommands() =>
            Custom?.Length > 0 || Uninstall?.Length > 0 || Disable?.Length > 0;
    }
}
