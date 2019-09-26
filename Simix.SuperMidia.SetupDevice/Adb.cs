using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SetupDevice {
    public sealed class Adb {
        private const int MAX_ATTEMPTS = 40;
        private const string PATH_DESTINY = "/sdcard/Download/";
        private readonly string _localAdbDirectory;
        private readonly Settings _settings;
        private readonly Process _process;
        private readonly ProcessStartInfo _psi;
        private readonly StreamWriter _sw;

        public Adb(Settings adbSettings, string adbDirectory = null) {
            _localAdbDirectory = adbDirectory ?? "RootKit\\adb.exe";
            _settings = adbSettings;
            _process = new Process();
            _psi = new ProcessStartInfo();
            _psi.FileName = "cmd.exe";
            _psi.RedirectStandardInput = true;
            _psi.UseShellExecute = false;
            _psi.WorkingDirectory = Path.GetDirectoryName(GetAndroidSdkPath());
            _process.StartInfo = _psi;
            _process.Start();
            _sw = _process.StandardInput;
        }

        public void ConnectIfNeeded(string deviceIp, bool validateConnection = false) {
            var output = Execute("devices -l", getOutput: true);
            var attempts = 0;
            while ((output.IndexOf(deviceIp, StringComparison.OrdinalIgnoreCase) == -1) && attempts < MAX_ATTEMPTS) {
                Execute($"connect {deviceIp}:5555");
                output = Execute("devices -l", getOutput: true);
                Task.Delay(1000).GetAwaiter().GetResult();
            }

            if (output.IndexOf(deviceIp, StringComparison.OrdinalIgnoreCase) == -1)
                throw new Exception("Não foi possível conectar ao dispositivo");

            if (!validateConnection) return;

            Console.WriteLine("Validando conexão...");
            ExecuteWithSuperUser(GetDefaultCommands(), deviceIp);
        }

        public void ExecuteCustomScripts() {
            var customScripts = _settings.Execute.Custom;

            foreach (var customScript in customScripts) {
                Execute(customScript);
            }
        }

        public void ExecuteCustomSuperUserScripts(string deviceIp) {
            var commandList = GetSuperUserCommands();
            ExecuteWithSuperUser(GetDefaultCommands(), deviceIp);
            ExecuteWithSuperUser(commandList, deviceIp);
        }

        public void Install(string apkToInstall, string deviceIp) {
            Console.WriteLine($"Instalando {apkToInstall}");
            ConnectIfNeeded(deviceIp);
            Execute($"install -r {apkToInstall}", deviceIp);
            Push(apkToInstall, deviceIp);
        }

        public void Push(string fileToPush, string deviceIp) {
            Console.WriteLine($"Enviando {fileToPush} para pasta downloads");
            ConnectIfNeeded(deviceIp);
            Execute($"push {fileToPush} \"{PATH_DESTINY}\"", deviceIp);
        }

        public void SetBackground(string file, string deviceIp) {
            Push(file, deviceIp);
            Execute($"shell am start " +
                $"-a android.intent.action.ATTACH_DATA " +
                $"-c android.intent.category.DEFAULT " +
                $"-d file://{PATH_DESTINY}SuperMidia.jpg " +
                $"-t 'image/*' -e mimeType 'image/*'");
        }

        public void SetLanguage(LanguageMode language, string deviceIp) {
            var languageLocale = language.ToString().Split("_");
            var commands = new List<string>() {
                $"setprop persist.sys.language  {languageLocale[0]}",
                $"setprop persist.sys.country {languageLocale[1]}",
                "stop",
                "sleep 2",
                "start"
            };

            ExecuteWithSuperUser(GetDefaultCommands(), deviceIp);
            ExecuteWithSuperUser(commands, deviceIp);
        }

        public void ExecuteWithSuperUser(IEnumerable<string> commandList, string deviceIp) {
            _sw.WriteLine($"adb connect {deviceIp}:5555");

            foreach (var cmd in commandList) {
                Task.Delay(900).GetAwaiter().GetResult();
                _sw.WriteLine(cmd);
                Console.WriteLine(cmd);
            }
        }

        public string Execute(string command, string deviceIp = null, bool getOutput = false) {
            var parsedCommand = string.IsNullOrEmpty(deviceIp)
                ? $"/c adb {command}" : $"/c adb -s {deviceIp} {command}";

            var p = new Process();
            p.StartInfo.WorkingDirectory = Path.GetDirectoryName(GetAndroidSdkPath());
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.Arguments = parsedCommand;
            p.StartInfo.FileName = "cmd.exe";
            p.ErrorDataReceived += new DataReceivedEventHandler((s, e) => {
                Console.WriteLine(e.Data);
                if (e.Data?.ToLower()?.Contains("cannot connect to daemon") ?? false) {
                    try {
                        var proc = Process.GetProcessesByName("adb");
                        foreach (var adbProc in proc) {
                            adbProc?.Kill();
                        }
                    } catch (Exception) {
                    }
                }
            });

            p.OutputDataReceived += new DataReceivedEventHandler((s, e) => {
                Console.WriteLine(e.Data);
            });

            p.Start();
            p.BeginErrorReadLine();
            var output = "";

            if (!getOutput) {
                p.BeginOutputReadLine();
            } else {
                output = p.StandardOutput.ReadToEnd();
            }

            p.WaitForExit();
            return output;
        }

        public IEnumerable<string> FindDevices() {
            var p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.Arguments = "devices -l";

            var sdkOutput = GetAndroidSdkPath();

            p.StartInfo.FileName = sdkOutput;
            p.Start();

            var sOutput = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            var lines = TakeLastLines(sOutput, 15);
            var result = new List<string>();

            foreach (var line in lines) {
                if (line.ToLower().IndexOf("device:") > -1)
                    result.Add(line);
            }

            return result;
        }

        private string GetAndroidSdkPath() {
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
                        var Files = Directory.GetFiles(currentPath, "*.exe", SearchOption.AllDirectories);

                        foreach (var file in Files) {
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

            if (string.IsNullOrEmpty(outPutPath)) {
                outPutPath = _localAdbDirectory;
            }

            Console.WriteLine($"Using adb path '{outPutPath}'");
            return outPutPath;
        }

        private IEnumerable<string> GetDefaultCommands() {
            yield return "adb root";
            yield return "adb shell";
            yield return "su";
        }

        private List<string> TakeLastLines(string text, int count) {
            var lines = new List<string>();
            var match = Regex.Match(text, "^.*$", RegexOptions.Multiline | RegexOptions.RightToLeft);

            while (match.Success && lines.Count < count) {
                lines.Insert(0, match.Value);
                match = match.NextMatch();
            }

            return lines;
        }

        private IEnumerable<string> GetSuperUserCommands() {
            var superUser = _settings.SuperUser;

            if (superUser.Disable != null) {
                foreach (var unistall in superUser.Uninstall)
                    yield return $"uninstall {unistall}";
            }

            if (superUser.Disable != null) {
                foreach (var disable in superUser.Disable)
                    yield return $"pm disable {disable}";
            }

            if (superUser.Custom != null) {
                foreach (var custom in superUser.Custom)
                    yield return $"{custom}";
            }

            yield return "exit";
            yield return "exit";
        }
    }

    public enum LanguageMode {
        Pt_BR = 0,
        Es_ES = 1,
        En_US = 2
    }

    [Serializable()]
    [DesignerCategory("code")]
    [XmlRoot("SETTINGS")]
    public partial class Settings {
        [XmlElement("SUPERUSER")]
        public SuperUser SuperUser { get; set; }

        [XmlElement("EXECUTE")]
        public Execute Execute { get; set; }
    }

    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class SuperUser : AdbCommand {
    }

    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class Execute : AdbCommand {
    }

    public abstract class AdbCommand {
        [XmlElement("UNINSTALL", IsNullable = true)]
        public string[] Uninstall { get; set; }

        [XmlElement("DISABLE", IsNullable = true)]
        public string[] Disable { get; set; }

        [XmlElement("CUSTOM", IsNullable = true)]
        public string[] Custom { get; set; }
    }
}
