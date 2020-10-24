using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using True_Mining_Desktop.User;

namespace True_Mining_Desktop.Core.XMRig
{
    public static class XMRig
    {
        public static Process XMRIGminer = new Process();
        private static ProcessStartInfo XMRigProcessStartInfo = new ProcessStartInfo(Environment.CurrentDirectory + @"\Miners\xmrig\" + @"xmrig-gcc.exe");
        private static bool inXMRIGexitEvent = false;
        private static DateTime holdTime = DateTime.UtcNow;
        private static DateTime startedSince = holdTime.AddTicks(-(holdTime.Ticks));

        public static void Start()
        {
            if (XMRIGminer.StartInfo != XMRigProcessStartInfo)
            {
                XMRigProcessStartInfo.WorkingDirectory = Environment.CurrentDirectory + @"\Miners\xmrig\";
                XMRigProcessStartInfo.UseShellExecute = false;
                XMRigProcessStartInfo.RedirectStandardError = true;
                XMRigProcessStartInfo.RedirectStandardOutput = false;
                XMRigProcessStartInfo.CreateNoWindow = false;
                XMRigProcessStartInfo.ErrorDialog = false;
                XMRIGminer.StartInfo = XMRigProcessStartInfo;
            }

            XMRIGminer.Exited -= XMRIGminer_Exited;
            XMRIGminer.Exited += XMRIGminer_Exited;
            XMRIGminer.EnableRaisingEvents = true;

            try
            {
                XMRIGminer.ErrorDataReceived -= XMRIGminer_ErrorDataReceived;
                XMRIGminer.ErrorDataReceived += XMRIGminer_ErrorDataReceived;
                XMRIGminer.Start();

                new Task(() =>
                {
                    try
                    {
                        DateTime initializingTask = DateTime.UtcNow;
                        while (Tools.FindWindow(null, "True Mining running XMRig").ToInt32() == 0 && Miner.IsMining && initializingTask >= DateTime.UtcNow.AddSeconds(-30)) { Thread.Sleep(1); }
                        Miner.ShowHideCLI();
                    }
                    catch { }
                }).Start();

                startedSince = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                Miner.IsMining = false;
                MessageBox.Show("XMRig can't start. Try add True Mining's folder in Antivirus exclusions." + e.Message);
            }
        }

        private static void XMRIGminer_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Tools.KillProcess(XMRIGminer.ProcessName); Stop();
        }

        public static void Stop()
        {
            try { XMRIGminer.Kill(); } catch { }
            Thread.Sleep(200);
        }

        public static void ChangeCompiler()
        {
            if (XMRigProcessStartInfo.FileName == Environment.CurrentDirectory + @"\Miners\xmrig\" + @"xmrig-gcc.exe")
            { XMRigProcessStartInfo.FileName = Environment.CurrentDirectory + @"\Miners\xmrig\" + @"xmrig-msvc.exe"; }
            else
            { XMRigProcessStartInfo.FileName = Environment.CurrentDirectory + @"\Miners\xmrig\" + @"xmrig-gcc.exe"; }
        }

        public static void Show()
        {
            XMRIGminer.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
        }

        public static void Hide()
        {
            XMRIGminer.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        }

        public static decimal GetHasrate(string backend = null)
        {
            if (backend == null) { return -1; }

            try
            {
                string backendPureData = new WebClient().DownloadString("http://127.0.0.1:" + APIport + "/2/backends");
                dynamic backendsAPI = JsonConvert.DeserializeObject(backendPureData);

                decimal hashrate = -1;

                if (String.Equals(backend, "CPU", StringComparison.OrdinalIgnoreCase))
                {
                    hashrate = 0;

                    foreach (dynamic backendLoop in backendsAPI)
                    {
                        if (backendLoop.type == "cpu")
                        {
                            hashrate += Convert.ToDecimal(backendLoop.hashrate[0], CultureInfo.InvariantCulture.NumberFormat);
                        }
                    }
                }
                else if (String.Equals(backend, "opencl", StringComparison.OrdinalIgnoreCase) || String.Equals(backend, "AMD", StringComparison.OrdinalIgnoreCase))
                {
                    hashrate = 0;

                    foreach (dynamic backendLoop in backendsAPI)
                    {
                        if (backendLoop.type == "opencl")
                        {
                            hashrate += Convert.ToDecimal(backendLoop.hashrate[0], CultureInfo.InvariantCulture.NumberFormat);
                        }
                    }
                }
                else if (String.Equals(backend, "cuda", StringComparison.OrdinalIgnoreCase) || String.Equals(backend, "NVIDIA", StringComparison.OrdinalIgnoreCase))
                {
                    hashrate = 0;

                    foreach (dynamic backendLoop in backendsAPI)
                    {
                        if (backendLoop.type == "cuda")
                        {
                            hashrate += Convert.ToDecimal(backendLoop.hashrate[0], CultureInfo.InvariantCulture.NumberFormat);
                        }
                    }
                }

                if (hashrate >= 0) { return hashrate; }
            }
            catch
            {
                return -1;
            }

            return -1;
        }

        private static void XMRIGminer_Exited(object sender, EventArgs e)
        {
            if (Miner.IsMining)
            {
                if (!inXMRIGexitEvent)
                {
                    inXMRIGexitEvent = true;

                    if (startedSince < DateTime.UtcNow.AddSeconds(-30)) { Thread.Sleep(7000); }
                    else { ChangeCompiler(); }

                    Start();

                    inXMRIGexitEvent = false;
                }
            }
        }

        public static void CreateConfigFile()
        {
            StringBuilder conf = new StringBuilder();

            conf.AppendLine("{");
            conf.AppendLine("   \"api\": {");
            conf.AppendLine("       \"id\": null,");
            conf.AppendLine("       \"worker-id\": null");
            conf.AppendLine("   },");
            conf.AppendLine("   \"http\": {");
            conf.AppendLine("       \"enabled\": true,");
            conf.AppendLine("       \"host\": \"127.0.0.1\",");
            conf.AppendLine("       \"port\": " + APIport + ",");
            conf.AppendLine("       \"access-token\": null,");
            conf.AppendLine("       \"restricted\": true");
            conf.AppendLine("   },");
            conf.AppendLine("   \"autosave\": false,");
            conf.AppendLine("   \"colors\": true,");
            conf.AppendLine("   \"title\": \"True Mining running XMRig\",");
            conf.AppendLine("   \"cpu\": {");
            conf.AppendLine("       \"enabled\": " + Settings.Device.cpu.MiningSelected.ToString().ToLowerInvariant() + ",");
            conf.AppendLine("       \"huge-pages\": true,");
            conf.AppendLine("       \"hw-aes\": null,");
            if (!Settings.Device.cpu.Autoconfig) { conf.AppendLine("        \"priority\": " + Settings.Device.cpu.Priority + ","); }
            conf.AppendLine("       \"memory-pool\": true,");
            if (!Settings.Device.cpu.Autoconfig) { conf.AppendLine("        \"yield\": " + Settings.Device.cpu.Yield.ToString().ToLowerInvariant() + ","); }
            conf.AppendLine("       \"asm\": true,");
            if (!Settings.Device.cpu.Autoconfig) { conf.AppendLine("        \"max-threads-hint\": " + Settings.Device.cpu.MaxUsageHint + ","); }
            conf.AppendLine("       \"rx\": {");
            conf.AppendLine("           \"init\": -1,");
            conf.AppendLine("           \"mode\": \"auto\",");
            conf.AppendLine("           \"1gb-pages\": true,");
            conf.AppendLine("           \"rdmsr\": true,");
            conf.AppendLine("           \"wrmsr\": true,");
            conf.AppendLine("           \"cache_qos\": true,");
            conf.AppendLine("           \"numa\": true,");
            if (!Settings.Device.cpu.Autoconfig && Settings.Device.cpu.Threads > 0) { conf.AppendLine("           \"threads\": " + Settings.Device.cpu.Threads + ","); }
            conf.AppendLine("       }");
            conf.AppendLine("   },");
            conf.AppendLine("   \"opencl\": {");
            conf.AppendLine("       \"enabled\": " + Settings.Device.opencl.MiningSelected.ToString().ToLowerInvariant() + ",");
            if (!Settings.Device.opencl.Autoconfig) { conf.AppendLine("     \"cache\": " + Settings.Device.opencl.Cache + ","); }
            conf.AppendLine("       \"loader\": null,");
            conf.AppendLine("       \"platform\": \"AMD\",");
            conf.AppendLine("       \"adl\": true,");
            conf.AppendLine("       \"cn/0\": false,");
            conf.AppendLine("       \"cn-lite/0\": false");
            conf.AppendLine("   },");
            conf.AppendLine("   \"cuda\": {");
            conf.AppendLine("       \"enabled\": " + Settings.Device.cuda.MiningSelected.ToString().ToLowerInvariant() + ",");
            conf.AppendLine("       \"loader\": null,");
            if (!Settings.Device.opencl.Autoconfig) { conf.AppendLine("     \"nvml\": " + Settings.Device.cuda.NVML.ToString().ToLowerInvariant()); }
            conf.AppendLine("   },");
            conf.AppendLine("   \"donate-level\": 1,");
            conf.AppendLine("   \"donate-over-proxy\": 1,");
            conf.AppendLine("   \"log-file\": \"XMRig-log.txt\",");
            conf.AppendLine("   \"pools\": [");
            conf.AppendLine("       {");
            conf.AppendLine("           \"algo\": null,");
            conf.AppendLine("           \"coin\": \"monero\",");
            conf.AppendLine("           \"url\": \"xmr-us-west1.nanopool.org:14433\",");
            conf.AppendLine("           \"user\": \"43gouirChAZdJKELkBfm15McQxszLiFXYMEAtQa4g7vYEK2rBd73XhXjNn8Q4KkdkCSe6YceTiLpkWpMz11ZfwE5DWwsLFr." + Settings.User.Payment_Wallet + "\",");
            conf.AppendLine("           \"pass\": \"x\",");
            conf.AppendLine("           \"rig-id\": null,");
            conf.AppendLine("           \"nicehash\": false,");
            conf.AppendLine("           \"keepalive\": false,");
            conf.AppendLine("           \"enabled\": true,");
            conf.AppendLine("           \"tls\": true,");
            conf.AppendLine("           \"tls-fingerprint\": null,");
            conf.AppendLine("           \"daemon\": false,");
            conf.AppendLine("           \"socks5\": null,");
            conf.AppendLine("           \"self-select\": null");
            conf.AppendLine("       }");
            conf.AppendLine("   ]");
            conf.AppendLine("}");

            File.WriteAllText(@"Miners\xmrig\config.json", conf.ToString());
        }

        private static int APIport { get; } = 20202;
    }
}