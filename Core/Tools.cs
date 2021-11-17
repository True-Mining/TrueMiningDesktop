using Knapcode.TorSharp;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TrueMiningDesktop.Janelas;

namespace TrueMiningDesktop.Core
{
    public class Tools
    {
        public static bool IsConnected()
        {
            Ping p = new();

            try
            {
                PingReply pr = p.Send("8.8.8.8", 3000);
                if (pr.Status == IPStatus.Success) { return true; }
            }
            catch { }

            try
            {
                if (HttpGet("https://truemining.online/ping") == "pong") { return true; }
            }
            catch { }

            try
            {
                if (HttpGet("http://truemining.online/ping") == "pong") { return true; }
            }
            catch { }

            try
            {
                if (HttpGet("https://www.utivirtual.com.br/Truemining/ping") == "pong") { return true; }
            }
            catch { }

            if (HaveADM && !firewallRuleAdded) { try { AddFirewallRule("ping", Path.Combine(Environment.SystemDirectory, "ping.exe")); } catch { } }

            return false;
        }

        public static KeyValuePair<string, long> ReturnPing(string address)
        {
            try
            {
                PingReply ping = new Ping().Send(address, 1000, Encoding.ASCII.GetBytes("ping by True Mining Desktop Client"));

                return new KeyValuePair<string, long>(address, (ping.Status == IPStatus.Success) ? ping.RoundtripTime : 2000);
            }
            catch { return new KeyValuePair<string, long>(address, 10000); }
        }

        public static WebHeaderCollection WebRequestHeaders()
        {
            WebHeaderCollection headers = new()
            {
                //    headers[HttpRequestHeader.Accept] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                //    headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate, br";
                //     headers[HttpRequestHeader.AcceptLanguage] = "pt-BR,pt;q=0.8,en-US;q=0.5,en;q=0.3";
                [HttpRequestHeader.CacheControl] = "max-age=0",
                [HttpRequestHeader.KeepAlive] = "1",
                [HttpRequestHeader.Allow] = "1",
                // headers[HttpRequestHeader.Via] = "http://127.0.0.1:8427";
                [HttpRequestHeader.ProxyAuthorization] = "Basic " + new TorSharpTorSettings().ControlPassword,
                [HttpRequestHeader.Trailer] = "1",
                [HttpRequestHeader.Upgrade] = "1",
                [HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:88.0) Gecko/20100101 Firefox/88.0"
            };

            return headers;
        }

        public static string HttpGet(string uri, bool forceUseTor = false)
        {
            bool useTor = false;

            for (int i = 0; i < 4; i++)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                    request.AutomaticDecompression = DecompressionMethods.All;
                    request.Proxy = useTor || forceUseTor ? Tools.TorProxy : null;
                    request.Headers = Tools.WebRequestHeaders();
                    request.Credentials = System.Net.CredentialCache.DefaultCredentials;

                    using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    using Stream stream = response.GetResponseStream();
                    using StreamReader reader = new(stream);
                    UseTor = false;
                    return reader.ReadToEnd();
                }
                catch { if (i > 1) useTor = true; UseTor = true; }
            }

            throw new Exception();
        }

        private static readonly TorSharpSettings TorSharpSettings = new()
        {
            ZippedToolsDirectory = Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name, "Knapcode.TorSharp", "ZippedTools"),
            ExtractedToolsDirectory = Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name, "Knapcode.TorSharp", "ExtractedTools"),
            PrivoxySettings = { Port = 8427 },
            WaitForConnect = new TimeSpan(0),
            TorSettings =
            {
                SocksPort = 8428,
                ControlPort = 8429,
                ControlPassword = "TrueMining"
            },
        };

        public static readonly TorSharpProxy TorSharpProxy = new(TorSharpSettings);

        private static bool useTor = false;
        public static bool UseTor
        { get { return useTor; } set { useTor = value; if (!User.Settings.LoadingSettings) { NotifyPropertyChanged(); } } }

        public static bool TorSharpProcessesRunning
        {
            get
            {
                bool torProcessRunning = false;
                bool privoxyProcessRunning = false;
                foreach (Process process in Process.GetProcessesByName("tor")) { try { if (process.MainModule.FileName.Contains(TorSharpSettings.ExtractedToolsDirectory, StringComparison.OrdinalIgnoreCase)) { torProcessRunning = true; break; } } catch { } }
                foreach (Process process in Process.GetProcessesByName("privoxy")) { try { if (process.MainModule.FileName.Contains(TorSharpSettings.ExtractedToolsDirectory, StringComparison.OrdinalIgnoreCase)) { privoxyProcessRunning = true; break; } } catch { } }

                if (privoxyProcessRunning && torProcessRunning)
                {
                    return true;
                }
                else { return false; }
            }
            set { }
        }

        private static bool torSharpEnabled = false;
        public static bool TorSharpEnabled
        { get { return torSharpEnabled; } set { torSharpEnabled = value; if (!User.Settings.LoadingSettings) { NotifyPropertyChanged(); } } }

        public static event PropertyChangedEventHandler PropertyChanged;

        private static bool generatingTorProxy = false;

        public static WebProxy TorProxy
        {
            get
            {
                bool waitWorkForReturn = false;

                while (generatingTorProxy) { Thread.Sleep(100); if (!waitWorkForReturn) { waitWorkForReturn = true; } }

                if (!waitWorkForReturn)
                {
                    generatingTorProxy = true;

                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            if (!TorSharpProcessesRunning)
                            {
                                TorSharpEnabled = false;
                                try { TorSharpProxy.Stop(); } catch { }
                                foreach (Process process in Process.GetProcessesByName("tor")) { try { process.Kill(); } catch { } }
                                foreach (Process process in Process.GetProcessesByName("privoxy")) { try { process.Kill(); } catch { } }
                            }

                            try
                            {
                                string ip = new WebClient() { Proxy = new WebProxy() { Address = new Uri("http://localhost:" + TorSharpSettings.PrivoxySettings.Port), BypassProxyOnLocal = true, UseDefaultCredentials = true } }.DownloadString("https://api.ipify.org/");
                                if (System.Net.IPAddress.TryParse(ip, out IPAddress addressValidation))
                                {
                                    TorSharpEnabled = true;

                                    i = 4;
                                }
                                else { throw new Exception(); }
                            }
                            catch
                            {
                                TorSharpEnabled = false;

                                new TorSharpToolFetcher(TorSharpSettings, new System.Net.Http.HttpClient()).FetchAsync().Wait();
                                TorSharpProxy.ConfigureAndStartAsync().Wait();
                                if (!User.Settings.LoadingSettings)
                                {
                                    NotifyPropertyChanged();
                                }
                                TorSharpProxy.GetNewIdentityAsync().Wait();
                                TorSharpEnabled = true;

                                i = 4;
                            }
                        }
                        catch
                        {
                            TorSharpEnabled = false;

                            try { TorSharpProxy.Stop(); } catch { }
                            foreach (Process process in Process.GetProcessesByName("tor")) { try { process.Kill(); } catch { } }
                            foreach (Process process in Process.GetProcessesByName("privoxy")) { try { process.Kill(); } catch { } }

                            if (Directory.Exists(Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name, "Knapcode.TorSharp", "ZippedTools"))) { try { Directory.Delete(Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name, "Knapcode.TorSharp", "ZippedTools"), true); } catch { } };
                            if (Directory.Exists(Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name, "Knapcode.TorSharp", "ExtractedTools"))) { try { Directory.Delete(Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name, "Knapcode.TorSharp", "ExtractedTools"), true); } catch { } };
                        }
                    }

                    generatingTorProxy = false;
                }

                return new WebProxy()
                {
                    Address = new Uri("http://localhost:" + TorSharpSettings.PrivoxySettings.Port),
                    BypassProxyOnLocal = true,
                    UseDefaultCredentials = true
                };
            }
            set { }
        }

        public static void NotifyPropertyChanged()
        {
            PropertyChanged(null, null);
        }

        public static string FileSHA256(string filePath)
        {
            using System.Security.Cryptography.SHA256 hashAlgorithm = System.Security.Cryptography.SHA256.Create();
            using FileStream stream = System.IO.File.OpenRead(filePath);
            return BitConverter.ToString(hashAlgorithm.ComputeHash(stream)).Replace("-", "").ToUpper();
        }

        public static string FormatPath(string path)
        {
            if (path != null)
            {
                if (path.Contains(Directory.GetCurrentDirectory())) { path = path.Replace(Directory.GetCurrentDirectory(), null); }
                if (!path.StartsWith(@"\")) { path = @"\" + path; }
                if (!path.EndsWith(@"\")) { path += @"\"; }

                path = Directory.GetCurrentDirectory() + path;
                if (!path.EndsWith(@"\")) { path += @"\"; }
            }
            else
            {
                path = Directory.GetCurrentDirectory();
                if (!path.EndsWith(@"\")) { path += @"\"; }
            }
            path = path.Replace("/", @"\");
            path = path.Replace("//", @"\");
            path = path.Replace(@"\\", @"\");
            CreateMissingPatch(path);

            return path;
        }

        private static bool firewallRuleAdded = false;

        public static void AddFirewallRule(string name, string filePatch, bool forceAdmin = false)
        {
            StreamWriter wr = new(Path.Combine(Path.GetTempPath(), "addfirewallrule.cmd"));
            wr.Write("netsh advfirewall firewall del rule name=\"" + name + "\"\nnetsh advfirewall firewall add rule name=\"" + name + "\" program=\"" + filePatch + "\" dir=in action=allow\nnetsh advfirewall firewall add rule name=\"" + name + "\" program=\"" + filePatch + "\" dir=out action=allow");
            wr.Close();

            Process addfwrule = new();
            addfwrule.StartInfo.FileName = Path.Combine(Path.GetTempPath(), "addfirewallrule.cmd");
            addfwrule.StartInfo.UseShellExecute = true;
            addfwrule.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            addfwrule.StartInfo.CreateNoWindow = true;
            if (forceAdmin) { addfwrule.StartInfo.Verb = "runas"; }
            addfwrule.Start();
            addfwrule.WaitForExit();

            firewallRuleAdded = true;
        }

        public static void TryChangeTaskbarIconAsSettingsOrder()
        {
            if (User.Settings.User.ChangeTbIcon)
            {
                try { MainWindow.NotifyIcon.Icon = new System.Drawing.Icon("Resources/iconeTaskbar2.ico"); } catch { try { MainWindow.NotifyIcon.Icon = new System.Drawing.Icon("Resources/icone.ico"); } catch { } }
            }
            else
            {
                try { MainWindow.NotifyIcon.Icon = new System.Drawing.Icon("Resources/icone.ico"); }
                catch { }
            }
        }

        public static void RestartApp(bool asAdministrator = true)
        {
            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
            {
                NextStart.Actions.Save(new NextStart.Instructions() { useThisInstructions = true, ignoreUpdates = false, startHiden = System.Windows.Application.Current.MainWindow.Visibility != Visibility.Visible, startMining = Miner.IsMining || Miner.IntentToMine });

                Process TrueMiningNewProcess = new()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = Process.GetCurrentProcess().MainModule.FileName,
                        UseShellExecute = true,
                    }
                };
                if (asAdministrator) { TrueMiningNewProcess.StartInfo.Verb = "runas"; }

                try { TrueMiningNewProcess.Start(); Miner.StopMiner(true); } catch (Exception e) { System.Windows.MessageBox.Show(e.Message); NextStart.Actions.DeleteInstructions(); }

                Process thisProcess = Process.GetCurrentProcess();

                new Task(() =>
                {
                    while (!TrueMiningNewProcess.HasExited)
                    {
                        Thread.Sleep(100);

                        Process[] processesWithTrueMiningName = Process.GetProcessesByName(thisProcess.ProcessName);

                        foreach (Process process in processesWithTrueMiningName)
                        {
                            if (process.Id != thisProcess.Id && process.Responding)
                            {
                                MainWindow.NotifyIcon.Visible = false;
                                thisProcess.Kill();
                            }
                        }
                    }
                }).Start();
            });
        }

        public static string GetAssemblyVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetEntryAssembly();
            FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersion.FileVersion.TrimEnd('.', '0');
        }

        public static void CreateMissingPatch(string path)
        {
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
        }

        public static bool IsFileLocked(FileInfo file)
        {
            if (!file.Exists) { return false; }
            try
            {
                using FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                stream.Close();
            }
            catch (IOException)
            {
                return true;
            }

            return false;
        }

        public static bool WalletAddressIsValid(string address = "null")
        {
            if (string.IsNullOrEmpty(address))
            {
                return false;
            }
            if (User.Settings.User.PayCoin.AddressPatterns.Any(x => System.Text.RegularExpressions.Regex.IsMatch(address, x)))
            {
                return true;
            }

            return false;
        }

        public static void OpenLinkInBrowser(string link)
        {
            try
            {
                Process.Start(new ProcessStartInfo(link) { UseShellExecute = true, Verb = "open" });
            }
            catch
            {
                System.Windows.Clipboard.SetText(link);
                System.Windows.MessageBox.Show("Acess >>" + link + "<< in your browser. This is in your clipboard now.");
            }
        }

        public static System.Timers.Timer timerSystemAwake = new(30000);

        public static void AwakeSystem(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (User.Settings.User.AvoidWindowsSuspend && (Miner.IsMining || Miner.IntentToMine))
            {
                SetThreadExecutionState(ES_CONTINUOUS);
                SetThreadExecutionState(ES_AWAYMODE_REQUIRED);
                SetThreadExecutionState(ES_SYSTEM_REQUIRED);
                SetThreadExecutionState(ES_SYSTEM_REQUIRED);
            }
        }

        [DllImport("kernel32.dll")]
        private static extern uint SetThreadExecutionState(uint esFlags);

        public const uint ES_CONTINUOUS = 0x80000000;
        public const uint ES_SYSTEM_REQUIRED = 0x00000001;
        public const uint ES_AWAYMODE_REQUIRED = 0x00000040;
        public const uint ES_DISPLAY_REQUIRED = 0x00000002;

        public static void AutostartSoftwareWithWindowsRegistryWriter()
        {
            try
            {
                if (User.Settings.User.AutostartSoftwareWithWindows)
                {
                    RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                    if ((string)Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true).GetValue("True Mining") != '"' + System.AppDomain.CurrentDomain.BaseDirectory + System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe" + '"')
                    {
                        key.SetValue("True Mining", '"' + System.AppDomain.CurrentDomain.BaseDirectory + System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe" + '"');
                    }
                }
                else
                {
                    if (Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true).GetValue("True Mining") != null)
                    {
                        RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                        key.DeleteValue("True Mining");
                    }
                }
            }
            catch { }
        }

        public static bool AddedTrueMiningDestopToWinDefenderExclusions;

        public static void AddTrueMiningDestopToWinDefenderExclusions(bool forceAdmin = false)
        {
            try
            {
                if (Tools.HaveADM || forceAdmin)
                {
                    string command = @"Add-MpPreference -ExclusionPath " + '"' + System.AppDomain.CurrentDomain.BaseDirectory + '"' + " -Force";
                    byte[] commandBytes = System.Text.Encoding.Unicode.GetBytes(command);
                    string commandBase64 = Convert.ToBase64String(commandBytes);

                    ProcessStartInfo startInfo = new()
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy unrestricted -EncodedCommand {commandBase64}",
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        Verb = "runas"
                    };
                    Process.Start(startInfo).WaitForExit();
                }
            }
            catch { }
        }

        public static void KillMiners()
        {
            KillProcess("xmrig-gcc.exe");
            KillProcess("xmrig-msvc.exe");
        }

        public static void KillProcess(string processName)
        {
            Process mataminers = new()
            {
                StartInfo = new ProcessStartInfo("taskkill", "/F /IM " + processName)
                {
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                }
            };
            mataminers.Start();
            mataminers.WaitForExit();
        }

        public static bool HaveADM => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        public static CheckerPopup CheckerPopup;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool ShowWindow(IntPtr windowIdentifier, int nCmdShow);
    }
}