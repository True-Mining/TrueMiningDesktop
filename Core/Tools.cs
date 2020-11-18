using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows;
using True_Mining_Desktop.Janelas;

namespace True_Mining_Desktop.Core
{
    public class Tools
    {
        public static bool IsConnected()
        {
            StringBuilder str = new StringBuilder();

            Ping p = new Ping();
            try
            {
                PingReply pr = p.Send("8.8.8.8", 3000);
                if (pr.Status == IPStatus.Success) { return true; } else { if (HaveADM && !firewallRuleAdded) { AddFirewallPingRule(); }  return false; }
            }
            catch { if (HaveADM && !firewallRuleAdded) { AddFirewallPingRule(); } return false; }
        }

        public static string FileSHA256(string filePath)
        {
            using (var hashAlgorithm = System.Security.Cryptography.SHA256.Create())
            {
                using (var stream = System.IO.File.OpenRead(filePath))
                {
                    return BitConverter.ToString(hashAlgorithm.ComputeHash(stream)).Replace("-", "").ToUpper();
                }
            }
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

        static bool firewallRuleAdded = false;
        public static void AddFirewallPingRule()
        {
            Process addfwrule = new Process();
            addfwrule.StartInfo.FileName = "netsh";
            addfwrule.StartInfo.UseShellExecute = false;
            addfwrule.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            addfwrule.StartInfo.CreateNoWindow = true;
            addfwrule.StartInfo.Arguments = "advfirewall firewall del rule name=\"ping\"";
            addfwrule.Start();
            addfwrule.WaitForExit();
            addfwrule.StartInfo.Arguments = "advfirewall firewall add rule name=\"ping\" protocol=ICMPV4 dir=in action=allow";
            addfwrule.Start();
            addfwrule.WaitForExit();
            addfwrule.StartInfo.Arguments = "advfirewall firewall add rule name=\"ping\" protocol=ICMPV4 dir=out action=allow";
            addfwrule.Start();
            addfwrule.WaitForExit();

            firewallRuleAdded = true;
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
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                return true;
            }

            return false;
        }

        public static Collection<Version> InstalledDotNetVersions()
        {
            Collection<Version> versions = new Collection<Version>();
            RegistryKey NDPKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP");
            if (NDPKey != null)
            {
                string[] subkeys = NDPKey.GetSubKeyNames();
                foreach (string subkey in subkeys)
                {
                    GetDotNetVersion(NDPKey.OpenSubKey(subkey), subkey, versions);
                    GetDotNetVersion(NDPKey.OpenSubKey(subkey).OpenSubKey("Client"), subkey, versions);
                    GetDotNetVersion(NDPKey.OpenSubKey(subkey).OpenSubKey("Full"), subkey, versions);
                }
            }
            return versions;
        }

        public static bool WalletAddressIsValid(string address = "null")
        {
            if (String.IsNullOrEmpty(address))
            {
                return false;
            }
            if (address.Length != 34)
            {
                return false;
            }

            if (!address.StartsWith('D') && !address.StartsWith('R')) { return false; }

            //switch (User.Settings.User.Payment_Coin)
            //{
            //    case "RDCT":
            //        {
            //            if (!address.StartsWith("R"))
            //            {
            //                return false;
            //            }
            //            break;
            //        }
            //    case "DOGE":
            //        {
            //            if (!address.StartsWith("D"))
            //            {
            //                return false;
            //            }
            //            break;
            //        }
            //}

            return true;
        }

        private static void GetDotNetVersion(RegistryKey parentKey, string subVersionName, Collection<Version> versions)
        {
            if (parentKey != null)
            {
                string installed = Convert.ToString(parentKey.GetValue("Install"));
                if (installed == "1")
                {
                    string version = Convert.ToString(parentKey.GetValue("Version"));
                    if (string.IsNullOrEmpty(version))
                    {
                        if (subVersionName.StartsWith("v"))
                            version = subVersionName.Substring(1);
                        else
                            version = subVersionName;
                    }

                    Version ver = new Version(version);

                    if (!versions.Contains(ver))
                        versions.Add(ver);
                }
            }
        }

        public static void OpenLinkInBrowser(string link)
        {
            try
            {
                Process.Start(new ProcessStartInfo(link) { UseShellExecute = true, Verb = "open" });
            }
            catch
            {
                Clipboard.SetText(link);
                MessageBox.Show("Acess >>" + link + "<< in your browser. This is in your clipboard now.");
            }
        }

        public static void KeepSystemAwake()
        {
            while (User.Settings.User.AvoidWindowsSuspend)
            {
                SetThreadExecutionState(ES_SYSTEM_REQUIRED);
                Thread.Sleep(50000);
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern uint SetThreadExecutionState(uint esFlags);

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
                    Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                    if ((string)Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true).GetValue("True Mining") != '"' + System.AppDomain.CurrentDomain.BaseDirectory + System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe" + '"')
                    {
                        key.SetValue("True Mining", '"' + System.AppDomain.CurrentDomain.BaseDirectory + System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe" + '"');
                    }
                }
                else
                {
                    if (Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true).GetValue("True Mining") != null)
                    {
                        Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                        key.DeleteValue("True Mining");
                    }
                }
            }
            catch { }
        }

        public static void AddTrueMiningDestopToWinDefenderExclusions()
        {
            try
            {
                if (Tools.HaveADM)
                {
                    var command = @"Add-MpPreference -ExclusionPath " + '"' + System.AppDomain.CurrentDomain.BaseDirectory + '"' + " -Force";
                    var commandBytes = System.Text.Encoding.Unicode.GetBytes(command);
                    var commandBase64 = Convert.ToBase64String(commandBytes);

                    var startInfo = new ProcessStartInfo()
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy unrestricted -EncodedCommand {commandBase64}",
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true
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
            Process mataminers = new Process();
            mataminers.StartInfo = new ProcessStartInfo("taskkill", "/F /IM " + processName);
            mataminers.StartInfo.UseShellExecute = false;
            mataminers.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            mataminers.StartInfo.CreateNoWindow = true;
            mataminers.Start();
            mataminers.WaitForExit();
        }

        public static bool HaveADM => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        public static CheckerPopup CheckerPopup;

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr windowIdentifier, int nCmdShow);
    }
}