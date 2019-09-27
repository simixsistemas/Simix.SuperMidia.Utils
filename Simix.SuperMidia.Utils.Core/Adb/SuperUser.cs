using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Simix.SuperMidia.Utils.Core.Adb {
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class SuperUser : AdbCommand {
    }
}
