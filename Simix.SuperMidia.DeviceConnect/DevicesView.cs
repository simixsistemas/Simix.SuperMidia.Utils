using Simix.SuperMidia.Utils.Core.Adb;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace RemoteDevices {
    public partial class DevicesView : Form {
        #region Fields

        private readonly Timer _timer = new Timer();
        private readonly AdbTerminal _adb = new AdbTerminal(new Settings());

        #endregion

        #region Constructors

        public DevicesView() {
            InitializeComponent();
            grdDevices.DefaultValuesNeeded += DataGridView1_DefaultValuesNeeded;
            _timer.Tick += Timer_Tick;
            _timer.Interval = 1000;
            _timer.Start();
        }

        #endregion

        #region Form Events

        private void Form1_Load(object sender, EventArgs e) {
            var x = Screen.PrimaryScreen.Bounds.Width;
            var y = Screen.PrimaryScreen.Bounds.Height;

            Size = new Size(Convert.ToInt32(Width), Convert.ToInt32(Height));
            Location = new Point(x - Convert.ToInt32(Width), y - Convert.ToInt32(Height) - 30);

            var fileName = GetConfigFileName();
            if (File.Exists(fileName)) {
                var xdoc = XDocument.Load(fileName);
                var devicesNodes = xdoc.Root?.Descendants();

                if (devicesNodes == null)
                    return;

                foreach (var deviceNode in devicesNodes) {
                    var ip = deviceNode?.Value;
                    var name = deviceNode?
                        .Attributes()?
                        .FirstOrDefault(a => a.Name.LocalName.Equals("NAME", StringComparison.OrdinalIgnoreCase))?
                        .Value;
                    if (!string.IsNullOrEmpty(ip))
                        grdDevices.Rows.Add(name, ip, "Disconnected", "Connect");
                }
            }
        }

        #endregion

        #region Datagrid Events

        private void DataGridView1_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e) {
            e.Row.Cells["status"].Value = "Disconected";
            e.Row.Cells["connect"].Value = "Connect";
        }

        private void DataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e) {
            var xmlDoc = new XDocument();
            var rootNode = new XElement("DEVICES");

            foreach (DataGridViewRow row in grdDevices.Rows) {
                var ip = row.Cells[1].Value as string;
                var name = row.Cells[0].Value as string;
                if (!string.IsNullOrEmpty(ip))
                    rootNode.Add(new XElement("DEVICE", ip, new XAttribute("NAME", name ?? string.Empty)));
            }

            xmlDoc.Add(rootNode);
            SaveConfig(xmlDoc);
        }

        private void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) {
            var senderGrid = (DataGridView)sender;

            if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn &&
                e.RowIndex >= 0) {
                var row = grdDevices.Rows[e.RowIndex];
                var ip = row.Cells[1].Value as string;
                ConnectToDevice(ref row, ip);
            }
        }

        #endregion

        #region Private Methods

        private void Timer_Tick(object sender, EventArgs e) => CheckDevices();

        private void SaveConfig(XDocument xmlDoc) {
            var fileName = GetConfigFileName();
            var path = Path.GetDirectoryName(fileName);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            xmlDoc.Save(fileName);
        }

        private string GetConfigFileName() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "RemoteDevices", "Preferences.Config");

        private void ConnectToDevice(ref DataGridViewRow row, string ip) {
            _adb.ConnectIfNeeded(ip);
            CheckDevices();
        }

        private void CheckDevices() {
            var output = _adb.Execute("devices -l", getOutput: true);
            foreach (DataGridViewRow row in grdDevices.Rows) {
                var ip = row.Cells[1].Value as string;
                if (!string.IsNullOrWhiteSpace(ip)) {
                    Invoke(new MethodInvoker(() => {
                        if (output.IndexOf(ip, StringComparison.OrdinalIgnoreCase) != -1)
                            row.Cells[2].Value = "Connected";
                        else
                            row.Cells[2].Value = "Disconnected";
                    }));
                }
            }
        }

        private async Task IntallVysor(string ip) {
            await Task.Run(() => {
                var currentLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var apkDirectory = Path.GetFullPath(Path.Combine(currentLocation, "Vysor-release.apk"));
                _adb.Install(apkDirectory, ip);
            });
        }

        #endregion

        #region Click Events

        private void BtnConnectAll_Click(object sender, EventArgs e) {
            for (var i = 0; i < grdDevices.Rows.Count; i++) {
                var row = grdDevices.Rows[i];
                var ip = row.Cells[1].Value as string;
                ConnectToDevice(ref row, ip);
            }
        }

        private void CloseToolStripMenuItem_Click(object sender, EventArgs e) => Close();

        private void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e) {
            Show();
            BringToFront();
        }

        private void Hide_Click(object sender, EventArgs e) => Hide();

        #endregion
    }
}