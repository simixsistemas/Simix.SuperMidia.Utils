using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace RemoteDevices {
    public partial class Form1 : Form {
        private readonly Timer timer = new Timer();

        public Form1()
        {
            InitializeComponent();
            dataGridView1.DefaultValuesNeeded += DataGridView1_DefaultValuesNeeded;
            timer.Tick += Timer_Tick;
            timer.Interval = 1000;
            timer.Start();
        }

        #region Form Events

        private void Form1_Load(object sender, EventArgs e) {

            int x = Screen.PrimaryScreen.Bounds.Width;
            int y = Screen.PrimaryScreen.Bounds.Height;

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
                        dataGridView1.Rows.Add(name, ip, "Disconnected", "Connect");
                }
            }
        }

        #endregion

        #region Datagrid Events

        private void DataGridView1_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            e.Row.Cells["status"].Value = "Disconected";
            e.Row.Cells["connect"].Value = "Connect";
        }

        private void DataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var xmlDoc = new XDocument();
            var rootNode = new XElement("DEVICES");

            foreach (DataGridViewRow row in dataGridView1.Rows) {
                var ip = row.Cells[1].Value as string;
                var name = row.Cells[0].Value as string;
                if(!string.IsNullOrEmpty(ip))
                    rootNode.Add(new XElement("DEVICE", ip, new XAttribute("NAME", name ?? string.Empty)));
            }

            xmlDoc.Add(rootNode);
            SaveConfig(xmlDoc);
        }

        private void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) {
            var senderGrid = (DataGridView)sender;

            if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn &&
                e.RowIndex >= 0) {
                var row = dataGridView1.Rows[e.RowIndex];
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
            var p = new Process();

            p.StartInfo.WorkingDirectory = Path.GetDirectoryName(GetAndroidSdkPath());
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.Arguments = $"/c adb connect {ip}:5555";
            p.StartInfo.FileName = "cmd.exe";
            p.Start();

            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            if(output.IndexOf("connected", StringComparison.OrdinalIgnoreCase) != -1) {
                row.Cells[2].Value = "Connected";
                Task.Run(async()=> await IntallVysor(ip));
            } else {
                row.Cells[2].Value = "Disconnected";
            }
        }

        private void CheckDevices() {
            var p = new Process();

            p.StartInfo.WorkingDirectory = Path.GetDirectoryName(GetAndroidSdkPath());
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.Arguments = $"/c adb devices -l";
            p.StartInfo.FileName = "cmd.exe";
            p.Start();

            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            foreach (DataGridViewRow row in dataGridView1.Rows) {
                var ip = row.Cells[1].Value as string;
                if (!string.IsNullOrWhiteSpace(ip)) {
                    Invoke(new MethodInvoker(()=> {
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

                var p = new Process();

                p.StartInfo.WorkingDirectory = Path.GetDirectoryName(GetAndroidSdkPath());
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.Arguments = $"/c adb -s {ip}:5555 install -d -r {apkDirectory}";
                p.StartInfo.FileName = "cmd.exe";
                p.Start();

                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                if (output.IndexOf("Success", StringComparison.OrdinalIgnoreCase) == -1)
                    MessageBox.Show("Erro", $"Não foi possivel instalar vysor no dispositivo {ip}", MessageBoxButtons.OK, MessageBoxIcon.Error);

                Debug.Write(output);
            });
        }

        private static string GetAndroidSdkPath() {
            var outPutPath = string.Empty;
            var drives = DriveInfo.GetDrives();
            var found = false;
            var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var userLocal = userFolder.Substring(Path.GetPathRoot(userFolder).Length);
            var suggestedFolders = new[] { "Android", userLocal, "Program Files (x86)", "Program Files (x86)" };
            var suggestedSubFolders = new[] { "Android\\android-sdk", "android-sdk", "sdk" };

            foreach (var drive in drives) {
                foreach (var suggestedFolder in suggestedFolders) {
                    foreach (var suggestedSubFolder in suggestedSubFolders) {
                        var currentPath = Path.Combine(drive.Name, suggestedFolder, suggestedSubFolder, "platform-tools");
                        if (!Directory.Exists(currentPath)) continue;

                        var files = Directory.GetFiles(currentPath, "*.exe", SearchOption.AllDirectories);
                        foreach (var file in files) {
                            if (file.Contains("adb.exe")) {
                                outPutPath = file;
                                found = true;
                            }

                            if (found) break;
                        }
                        if (found) break;
                    }
                    if (found) break;
                }
                if (found) break;
            }

            if (string.IsNullOrEmpty(outPutPath))
                throw new NullReferenceException("Android sdk path not found");

            return outPutPath;
        }

        #endregion

        #region Click Events

        private void btnConnectAll_Click(object sender, EventArgs e) {
            for (var i = 0; i < dataGridView1.Rows.Count; i++) {
                var row = dataGridView1.Rows[i];
                var ip = row.Cells[1].Value as string;
                ConnectToDevice(ref row, ip);
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            BringToFront();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        #endregion
    }
}