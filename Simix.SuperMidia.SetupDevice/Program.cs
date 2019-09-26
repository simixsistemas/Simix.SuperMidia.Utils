using Simix.SuperMidia.Core.Extensions;
using Simix.SuperMidia.Entities.Enums;
using Simix.SuperMidia.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SetupDevice {
    class Program {
        private static Adb adb;

        static void Main(string[] args) {
            Console.WriteLine("*****************************************************");
            Console.WriteLine("*                 Símix - SuperMídia                *");
            Console.WriteLine("*             Configuração de dispositivo           *");
            Console.WriteLine("*                       v1.0.0                      *");
            Console.WriteLine("*****************************************************");
            Console.WriteLine();

            Task.Delay(500).GetAwaiter().GetResult();
            Console.WriteLine("Iniciando setup de dispositivo...");
            if (!File.Exists("Adb.Config"))
                BuildSettingsFile();

            
            try {
                SaveDefaultFiles();
                var workingDirectory = StartAdb();
                DownloadSuperMidia();
                ExecuteDefaultCommands();

                var deviceIp = ConnectToDevice();
                if (string.IsNullOrEmpty(deviceIp)) return;

                ChangeLanguage(deviceIp);
                adb.ExecuteCustomSuperUserScripts(deviceIp);
                InstallApplications(workingDirectory, deviceIp);
                PushBackups(workingDirectory, deviceIp);
                ChangeLauncher(deviceIp);
                SetBackground(workingDirectory, deviceIp);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Pressione qualquer tecla para sair");
            Console.ReadKey();
        }

        private static void SetBackground(string workingDirectory, string deviceIp) {
            Console.WriteLine("Alterando plano de fundo");
            var background = Directory.GetFiles(workingDirectory,
                "*.jpg", SearchOption.AllDirectories).FirstOrDefault();

            if (!string.IsNullOrEmpty(background)) {
                adb.SetBackground(background, deviceIp);
            }
        }

        private static void ChangeLauncher(string deviceIp) {
            Console.WriteLine("Alterando launcher...");
            adb.Execute("shell am start -n com.teslacoilsw.launcher/com.teslacoilsw.launcher.NovaLauncher", deviceIp);
            adb.Execute("shell am start -n com.teslacoilsw.launcher/com.teslacoilsw.launcher.NovaLauncher", deviceIp);
        }

        private static void PushBackups(string workingDirectory, string deviceIp) {
            var backups = Directory.GetFiles(workingDirectory,
                                "*.novabackup", SearchOption.AllDirectories);

            foreach (var backup in backups) {
                adb.Push(backup, deviceIp);
            }
        }

        private static void InstallApplications(string workingDirectory, string deviceIp) {
            var applications = Directory.GetFiles(workingDirectory,
                "*.apk", SearchOption.AllDirectories);

            foreach (var apkToInstall in applications) {
                adb.Install(apkToInstall, deviceIp);
            }
        }

        private static void ChangeLanguage(string deviceIp) {
            Console.WriteLine("Digite o numero do idioma do dispositivo: (0 - Portugues, 1 - Espanhol, 2 - Inglês)");
            var result = Console.ReadLine();
            int.TryParse(result, out var languageInt);
            languageInt = languageInt > 2 || languageInt < 0 ? 0 : languageInt;

            adb.SetLanguage((LanguageMode)languageInt, deviceIp);
        }

        private static string ConnectToDevice() {
            Console.WriteLine("Procurando por dispositivos conectados...");
            var devices = adb.FindDevices();
            var device = devices?.FirstOrDefault();
            var deviceIp = "";

            if (!string.IsNullOrEmpty(device)) {
                deviceIp = device.Replace(device.Substring(device.IndexOf(" ")), "");
                Console.WriteLine($"{deviceIp} encontrado!");
            } else {
                Console.WriteLine("Nenhum dispositivo encontrado!");
                Console.WriteLine("Digite o IP (2 digitos se começar com 10.1.1) ou o serial(USB) do equipamento:");
                deviceIp = Console.ReadLine();

                if (string.IsNullOrEmpty(deviceIp)) {
                    Console.WriteLine("ip não pode ser nulo!");
                    Console.WriteLine("Pressione qualquer tecla para sair");
                    Console.ReadKey();
                    return null;
                }

                if (deviceIp.Length < 6)
                    deviceIp = $"10.1.1.{deviceIp}";
            }

            adb.ConnectIfNeeded(deviceIp, true);
            Task.Delay(500).GetAwaiter().GetResult();
            return deviceIp;
        }

        private static void ExecuteDefaultCommands() {
            Console.WriteLine();
            Console.WriteLine("Executando scripts do arquivo de configuração");
            adb.ExecuteCustomScripts();
        }

        private static void DownloadSuperMidia() {
            var service = new AppcenterService();
            Console.WriteLine();
            Console.WriteLine("Baixar última versão da supermidia? (S/N)");
            var response = Console.ReadLine();
            if (response.EqualsIgnorecase("S")) {
                DownloadRelease("supermidia", service);
            }

            Console.WriteLine();
            Console.WriteLine("Baixar última versão do monitor? (S/N)");
            response = Console.ReadLine();
            if (response.EqualsIgnorecase("S")) {
                DownloadRelease("supermidia.android.monitor", service);
            }

            Task.Delay(500).GetAwaiter().GetResult();
        }

        private static string StartAdb() {
            var serializer = new XmlSerializer(typeof(Settings));
            Settings settings = null;
            using (var reader = new StreamReader("Adb.Config")) {
                settings = (Settings)serializer.Deserialize(reader);
            }

            var workingDirectory = Directory.GetCurrentDirectory();
            var localAdbDirectory = Directory.GetFiles(workingDirectory, "*adb.exe", SearchOption.AllDirectories).FirstOrDefault();

            Console.WriteLine(localAdbDirectory);
            adb = new Adb(settings, localAdbDirectory);
            Task.Delay(900).GetAwaiter().GetResult();
            return workingDirectory;
        }

        private static void SaveDefaultFiles() {
            var resources = Assembly.GetExecutingAssembly()
                                .GetManifestResourceNames();

            foreach (var resource in resources) {
                Console.WriteLine($"Salvando arquivo '{resource}'");
                SaveResource(resource, resource.Substring(resource.LastIndexOf(".") + 1).Equals("su"));
            }
        }

        private static void DownloadRelease(string application, AppcenterService service) {
            var token =$"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes($"felipe.baltazar@simix.com.br:8YA?hh],)m%?UvvJ"))}";
            var release = service.GetCurrentRelease(application, "", ReleaseGroup.Release).GetAwaiter().GetResult();
            using (var client = new HttpClient()) {
                using (var result = client.GetStreamAsync(release.DownloadLink).GetAwaiter().GetResult()) {
                    using (var fileStream = new FileStream($"Applications\\com.simix.{ParseName(application)}.apk", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)) {
                        result.CopyTo(fileStream);
                        fileStream.Close();
                    }
                }
            }

            Console.WriteLine($"Versão encontrada para {application}: {release.Version}");
        }

        private static string ParseName(string application) =>
            application == "supermidia" ? "supermidia.pro" : "monitor";

        private static void BuildSettingsFile() {
            var xmlDoc = new XDocument();
            var rootNode = new XElement("SETTINGS");
            var suElement = new XElement("SUPERUSER");
            var executeEl = new XElement("EXECUTE");

            executeEl.Add(new XElement("CUSTOM", "kill-server"));
            executeEl.Add(new XElement("CUSTOM", "start-server"));

            suElement.Add(new XElement("UNINSTALL", "com.google.android.youtube"));
            suElement.Add(new XElement("UNINSTALL", "org.xbmc.kodi"));
            suElement.Add(new XElement("DISABLE", "com.android.soundrecorder"));
            suElement.Add(new XElement("DISABLE", "com.android.contacts"));
            suElement.Add(new XElement("DISABLE", "com.android.camera2"));
            suElement.Add(new XElement("DISABLE", "com.android.calendar"));
            suElement.Add(new XElement("DISABLE", "com.android.musicfx"));
            suElement.Add(new XElement("DISABLE", "com.android.gallery3d"));
            suElement.Add(new XElement("DISABLE", "com.android.calculator2"));
            suElement.Add(new XElement("DISABLE", "com.android.email"));
            suElement.Add(new XElement("DISABLE", "com.android.music"));
            suElement.Add(new XElement("DISABLE", "com.android.quicksearchbox"));
            suElement.Add(new XElement("DISABLE", "com.android.deskclock"));
            suElement.Add(new XElement("DISABLE", "android.rk.RockVideoPlayer"));
            suElement.Add(new XElement("DISABLE", "com.droidlogic.PPPoE"));
            suElement.Add(new XElement("DISABLE", "com.android.development"));

            rootNode.Add(suElement);
            rootNode.Add(executeEl);

            xmlDoc.Add(rootNode);
            xmlDoc.Save("Adb.Config");
        }

        private static void SaveResource(string resource, bool withouExtension = false) {
            var fileName = GetFileNameFromResource(resource, withouExtension);
            var completeDirectory = Path.GetDirectoryName(fileName);

            if (!string.IsNullOrEmpty(completeDirectory) && !Directory.Exists(completeDirectory))
                Directory.CreateDirectory(completeDirectory);

            if (File.Exists(fileName)) return;

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)) {
                using (var fileStream = new FileStream(fileName, FileMode.Create)) {
                    for (int i = 0; i < stream.Length; i++)
                        fileStream.WriteByte((byte)stream.ReadByte());
                }
            }
        }

        private static string GetFileNameFromResource(string resourceName, bool withoutExtension = false) {
            var sb = new StringBuilder();
            var escapeDot = false;

            for (int i = resourceName.Length - 1; i >= 0; i--) {
                if (resourceName[i] == '_') {
                    escapeDot = true;
                    continue;
                }

                if (resourceName[i] != '.') {
                    escapeDot = false;
                } else {
                    if (!escapeDot) {
                        if ((withoutExtension || (resourceName.Length - i) > 4)) {
                            sb.Append('\\');
                            continue;
                        }
                    }
                }

                sb.Append(resourceName[i]);
            }

            var fileName = Path.GetDirectoryName(sb.ToString());
            return new string(fileName.Reverse().ToArray());
        }
    }
}
