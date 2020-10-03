using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace True_Mining_v4.Core
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
                return true;
            }
            catch { return false; }
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

            switch (User.Settings.User.Payment_Coin)
            {
                case "RDCT":
                    {
                        if (!address.StartsWith("R"))
                        {
                            return false;
                        }
                        break;
                    }
                case "DOGE":
                    {
                        if (!address.StartsWith("D"))
                        {
                            return false;
                        }
                        break;
                    }
            }

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

        public static void KeepSystemAwake()
        {
            while (User.Settings.User.AvoidWindowsSuspend)
            {
                SetThreadExecutionState(ES_SYSTEM_REQUIRED);
                Thread.Sleep(50000);
            }
        }

        // Import SetThreadExecutionState Win32 API and necessary flags
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
                    Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                    if ((string)Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true).GetValue("True Mining") != '"' + System.AppDomain.CurrentDomain.BaseDirectory + System.AppDomain.CurrentDomain.BaseDirectory + System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe" + '"')
                    {
                        key.SetValue("True Mining", '"' + System.AppDomain.CurrentDomain.BaseDirectory + System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe" + '"');
                    }
                }
                else
                {
                    if (Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true).GetValue("True Mining") != null)
                    {
                        Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                        key.DeleteValue("True Mining");
                    }
                }
            }
            catch { }
        }

        public static void KillMiners()
        {
            Process mataminers = new Process();
            mataminers.StartInfo = new ProcessStartInfo("taskkill", "/F /IM xmrig.exe /IM xmrig-gcc.exe /IM xmrig-msvc.exe");
            mataminers.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            mataminers.Start();
            mataminers.WaitForExit();
        }

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr windowIdentifier, int nCmdShow);

        /*
        private void ChecarArquiteturaSO()
        {
            if (Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "x86")
            {
                arquiteturaCPU = "win32";
                nvidiaMiningCheck.CheckState = CheckState.Unchecked;
                amdMiningCheck.CheckState = CheckState.Unchecked;
                nvidiaMiningCheck.Enabled = false;
                amdMiningCheck.Enabled = false;

                MessageBox.Show("True Mining não oferece suporte a sistemas operacionais de 32 bits. Caso você tenha um processador 64 bits, instale um sistema operacional de 64 bits. Caso seu processador seja 32 bits, o True Mining é simplesmente incompatível com seu computador. O software será fechado", "Problema detectado", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            else if (Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "AMD64")
            {
                arquiteturaCPU = "win64";
                nvidiaMiningCheck.Enabled = true;
                amdMiningCheck.Enabled = true;
            }
            else if (Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "AI64")
            {
                arquiteturaCPU = "win64";
                nvidiaMiningCheck.Enabled = true;
                amdMiningCheck.Enabled = true;
            }
            else
            {
                nvidiaMiningCheck.Enabled = true;
                amdMiningCheck.Enabled = true;
            }
        }

        private void ChecarIntegridade()
        {
            Dictionary<string, FileInfo> arquivosMiner = new Dictionary<string, FileInfo>
            {
               { "xmrig00", new FileInfo(AppDomain.CurrentDomain.BaseDirectory + @"\bin\bin.b64") },
               { "xmrig01", new FileInfo(AppDomain.CurrentDomain.BaseDirectory + @"\bin\xmrig\nvrtc-builtins64_101.dll") },
               { "xmrig02", new FileInfo(AppDomain.CurrentDomain.BaseDirectory + @"\bin\xmrig\nvrtc-builtins64_100.dll") },
               { "xmrig03", new FileInfo(AppDomain.CurrentDomain.BaseDirectory + @"\bin\xmrig\nvrtc-builtins64_90.dll") },
               { "xmrig04", new FileInfo(AppDomain.CurrentDomain.BaseDirectory + @"\bin\xmrig\nvrtc64_101_0.dll") },
               { "xmrig05", new FileInfo(AppDomain.CurrentDomain.BaseDirectory + @"\bin\xmrig\nvrtc64_100_0.dll") },
               { "xmrig06", new FileInfo(AppDomain.CurrentDomain.BaseDirectory + @"\bin\xmrig\nvrtc64_90.dll") },
               { "xmrig07", new FileInfo(AppDomain.CurrentDomain.BaseDirectory + @"\bin\xmrig\WinRing0x64.sys") },
               { "xmrig08", new FileInfo(AppDomain.CurrentDomain.BaseDirectory + @"\bin\xmrig\xmrig-cuda_101.dll") },
               { "xmrig09", new FileInfo(AppDomain.CurrentDomain.BaseDirectory + @"\bin\xmrig\xmrig-cuda_100.dll") },
               { "xmrig10", new FileInfo(AppDomain.CurrentDomain.BaseDirectory + @"\bin\xmrig\xmrig-cuda_90.dll") },
               { "xmrig11", new FileInfo(AppDomain.CurrentDomain.BaseDirectory + @"\bin\xmrig\xmrig-msvc-win64.exe") },
               { "xmrig12", new FileInfo(AppDomain.CurrentDomain.BaseDirectory + @"\bin\xmrig\xmrig-gcc-win64.exe") }
            };

            bool erroEncontrado = false;

            foreach (KeyValuePair<string, FileInfo> arquivo in arquivosMiner)
            {
                if (!File.Exists(arquivo.Value.ToString())) { erroEncontrado = true; }
            }

            if (!erroEncontrado && !HashIsOK())
            {
                erroEncontrado = true;
            }

            Mataminers(true);

            while (atualizandoTM) { Esperar(10000); }

            if (erroEncontrado && !atualizandoTM) { Corrigir(); }
        }

        private bool HashIsOK()
        {
            if (System.IO.File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\bin\bin.b64"))
            {
                var arquivo = AppDomain.CurrentDomain.BaseDirectory + @"\bin\bin.b64";
                var hashBin = CalcularHash(arquivo);
                var hashBinString = BitConverter.ToString(hashBin).Replace("-", "").ToUpper();

                if (dynamicParametros.bin64.sha256 == hashBinString.ToUpper())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        static Thread Esperar(int time)
        {
            Thread.Sleep(time);
            return null;
        }
    }
    */
    }
}