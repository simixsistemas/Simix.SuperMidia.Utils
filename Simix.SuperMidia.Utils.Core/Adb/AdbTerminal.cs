using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Simix.SuperMidia.Utils.Core.Adb {
    public sealed class AdbTerminal : IDisposable {
        #region Fields

        private const int MAX_ATTEMPTS = 40;
        private const string PATH_DESTINY = "/sdcard/Download/";

        private readonly string _localAdbDirectory;
        private readonly ProcessStartInfo _psi;
        private readonly Settings _settings;
        private readonly Process _process;
        private StreamWriter sw;

        #endregion

        #region Public Methods

        public AdbTerminal(Settings adbSettings, string adbDirectory = null) {
            _localAdbDirectory = adbDirectory ?? "RootKit\\adb.exe";
            _settings = adbSettings;
            _process = new Process();
            _psi = new ProcessStartInfo();
            StartSuperUserProcess();
        }

        public void ConnectIfNeeded(string deviceIp, bool validateConnection = false) {
            Console.WriteLine("Validando conexão...");
            var output = Execute("devices -l", getOutput: true);
            var attempts = 0;
            while ((output.IndexOf(deviceIp, StringComparison.OrdinalIgnoreCase) == -1) && attempts < MAX_ATTEMPTS) {
                Execute($"connect {deviceIp}:5555");
                output = Execute("devices -l", getOutput: true);
                Task.Delay(1000).GetAwaiter().GetResult();
            }

            if (output.IndexOf(deviceIp, StringComparison.OrdinalIgnoreCase) == -1)
                throw new Exception("Não foi possível conectar ao dispositivo");

            if (!validateConnection || !(_settings?.SuperUser?.HasCommands() ?? false)) return;

            Console.WriteLine("Conceda acesso ao ADB no dispositivo...");
            Console.WriteLine("Caso ja tenha feito, precione qualquer tecla para continuar.");
            Console.ReadKey();
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
            var languageLocale = language.ToString().Split('_');
            var commands = BuildLanguageCommands(languageLocale[0], languageLocale[1]);

            ExecuteWithSuperUser(GetDefaultCommands(), deviceIp);
            ExecuteWithSuperUser(commands, deviceIp);
        }

        public void ExecuteWithSuperUser(IEnumerable<string> commandList, string deviceIp) {
            sw.WriteLine($"adb connect {deviceIp}:5555");

            foreach (var cmd in commandList) {
                Task.Delay(900).GetAwaiter().GetResult();
                sw.WriteLine(cmd);
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

        #endregion

        #region Private Methods

        private string GetAndroidSdkPath() {
            var drives = DriveInfo.GetDrives();
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
                                Console.WriteLine($"Using adb path '{_localAdbDirectory}'");
                                return file;
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"Using adb path '{_localAdbDirectory}'");
            return _localAdbDirectory;
        }

        private void StartSuperUserProcess() {
            _psi.FileName = "cmd.exe";
            _psi.RedirectStandardInput = true;
            _psi.UseShellExecute = false;
            _psi.WorkingDirectory = Path.GetDirectoryName(GetAndroidSdkPath());
            _process.StartInfo = _psi;

            if (_settings?.SuperUser?.HasCommands() ?? false) {
                _process.Start();
                sw = _process.StandardInput;
            }
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

        private IEnumerable<string> BuildLanguageCommands(string language, string country) {
            yield return $"setprop persist.sys.language  {language}";
            yield return $"setprop persist.sys.country {country}";
            yield return "stop";
            yield return "sleep 2";
            yield return "start";
        }

        private IEnumerable<string> GetDefaultCommands() {
            yield return "adb root";
            yield return "adb shell";
            yield return "su";
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

        private List<string> TakeLastLines(string text, int count) {
            var lines = new List<string>();
            var match = Regex.Match(text, "^.*$", RegexOptions.Multiline | RegexOptions.RightToLeft);

            while (match.Success && lines.Count < count) {
                lines.Insert(0, match.Value);
                match = match.NextMatch();
            }

            return lines;
        }

        #endregion

        #region IDisposable

        public void Dispose() {
            sw.Dispose();
            _process.Dispose();
        }

        #endregion
    }
}
