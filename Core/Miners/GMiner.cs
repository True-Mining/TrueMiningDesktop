﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TrueMiningDesktop.Server;
using TrueMiningDesktop.User;

namespace TrueMiningDesktop.Core.GMiner
{
    public static class GMiner
    {
        private static readonly Process GMinerProcess = new();
        private static readonly ProcessStartInfo GMinerProcessStartInfo = new(Environment.CurrentDirectory + @"\Miners\GMiner\" + @"GMiner.exe");
        private static bool inGMinerexitEvent = false;
        private static readonly DateTime holdTime = DateTime.UtcNow;
        private static DateTime startedSince = holdTime.AddTicks(-(holdTime.Ticks));

        public static void Start()
        {
            if (GMinerProcess.StartInfo != GMinerProcessStartInfo)
            {
                GMinerProcessStartInfo.WorkingDirectory = Environment.CurrentDirectory + @"\Miners\GMiner\";
                GMinerProcessStartInfo.UseShellExecute = true;
                GMinerProcessStartInfo.RedirectStandardError = false;
                GMinerProcessStartInfo.RedirectStandardOutput = false;
                GMinerProcessStartInfo.CreateNoWindow = false;
                GMinerProcessStartInfo.ErrorDialog = false;
                GMinerProcessStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                GMinerProcessStartInfo.Arguments = "--config config-" + User.Settings.Device.cuda.Algorithm.ToLowerInvariant() + ".yml";
                GMinerProcess.StartInfo = GMinerProcessStartInfo;
            }

            GMinerProcess.Exited -= GMinerminer_Exited;
            GMinerProcess.Exited += GMinerminer_Exited;
            GMinerProcess.EnableRaisingEvents = true;

            try
            {
                GMinerProcess.ErrorDataReceived -= GMinerminer_ErrorDataReceived;
                GMinerProcess.ErrorDataReceived += GMinerminer_ErrorDataReceived;
                GMinerProcess.Start();

                new Task(() =>
                {
                    while (true)
                    {
                        try
                        {
                            Thread.Sleep(100);
                            DateTime time = GMinerProcess.StartTime;
                            if (time.Ticks > 100) { break; }
                        }
                        catch { }
                    }
                }).Wait(3000);

                new Task(() =>
                {
                    try
                    {
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            DateTime initializingTask = DateTime.UtcNow;
                            while (Tools.FindWindow(null, "True Mining running GMiner").ToInt32() == 0 && initializingTask >= DateTime.UtcNow.AddSeconds(-30)) { Thread.Sleep(500); }
                            Thread.Sleep(1000);
                            Miner.ShowHideCLI();
                            Miner.ShowHideCLI();
                        });
                    }
                    catch { }
                }).Start();

                startedSince = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                Miner.StopMiner(true);
                Miner.IntentToMine = true;

                if (minerBinaryChangedTimes < 4)
                {
                    ChangeMinerBinary();
                    Thread.Sleep(3000);
                    Miner.StartMiner(true);
                }
                else
                {
                    try
                    {
                        if (!Tools.HaveADM)
                        {
                            Tools.RestartApp(true);
                        }
                        else
                        {
                            if (Tools.AddedTrueMiningDestopToWinDefenderExclusions)
                            {
                                Miner.IntentToMine = false;
                                MessageBox.Show("GMiner can't start. Try add True Mining Desktop folder in Antivirus/Windows Defender exclusions. " + e.Message);
                            }
                            else
                            {
                                Application.Current.Dispatcher.Invoke((Action)delegate
                                {
                                    Tools.AddTrueMiningDestopToWinDefenderExclusions(true);

                                    Thread.Sleep(3000);
                                    Miner.StartMiner(true);
                                });
                            }
                        }
                    }
                    catch (Exception ee)
                    {
                        Miner.IntentToMine = false;
                        MessageBox.Show("GMiner failed to start. Try add True Mining Desktop folder in Antivirus/Windows Defender exclusions. " + ee.Message);
                    }
                }
            }
        }

        private static void GMinerminer_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Tools.KillProcess(GMinerProcess.ProcessName); Stop();
        }

        public static void Stop()
        {
            try
            {
                bool closed = false;

                Task tryCloseFancy = new(() =>
                {
                    try
                    {
                        GMinerProcess.CloseMainWindow();
                        GMinerProcess.WaitForExit();
                        closed = true;
                    }
                    catch
                    {
                        GMinerProcess.Kill();
                        GMinerProcess.WaitForExit();
                        closed = true;
                    }
                });
                tryCloseFancy.Start();
                tryCloseFancy.Wait(4000);

                if (!closed)
                {
                    Tools.KillProcessByName(GMinerProcess.ProcessName);
                }
            }
            catch { }
        }

        private static int minerBinaryChangedTimes = 0;

        public static void ChangeMinerBinary()
        {
            if (GMinerProcessStartInfo.FileName == Environment.CurrentDirectory + @"\Miners\GMiner\" + @"GMiner-gcc.exe")
            {
                GMinerProcessStartInfo.FileName = Environment.CurrentDirectory + @"\Miners\GMiner\" + @"GMiner_zerofee-msvc.exe";
            }
            else if (GMinerProcessStartInfo.FileName == Environment.CurrentDirectory + @"\Miners\GMiner\" + @"GMiner_zerofee-msvc.exe")
            {
                GMinerProcessStartInfo.FileName = Environment.CurrentDirectory + @"\Miners\GMiner\" + @"GMiner-msvc.exe";
            }
            else
            {
                GMinerProcessStartInfo.FileName = Environment.CurrentDirectory + @"\Miners\GMiner\" + @"GMiner-gcc.exe";
            }

            if (minerBinaryChangedTimes < 100) { minerBinaryChangedTimes++; }
        }

        public static void Show()
        {
            GMinerProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
        }

        public static void Hide()
        {
            GMinerProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        }

        public static decimal GetHasrate(string backend = null)
        {
            if (backend == null) { return -1; }

            try
            {
                string backendPureData = new WebClient().DownloadString("http://localhost:" + APIport + "/2/backends");
                dynamic backendsAPI = JsonConvert.DeserializeObject(backendPureData);

                decimal hashrate = -1;

                if (string.Equals(backend, "CPU", StringComparison.OrdinalIgnoreCase))
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
                else if (string.Equals(backend, "opencl", StringComparison.OrdinalIgnoreCase) || string.Equals(backend, "AMD", StringComparison.OrdinalIgnoreCase))
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
                else if (string.Equals(backend, "cuda", StringComparison.OrdinalIgnoreCase) || string.Equals(backend, "NVIDIA", StringComparison.OrdinalIgnoreCase))
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

                if (hashrate >= 0 && Miner.IsMining) { return hashrate; }
            }
            catch
            {
                return -1;
            }

            return -1;
        }

        private static void GMinerminer_Exited(object sender, EventArgs e)
        {
            if (Miner.IsMining && !Miner.StoppingMining)
            {
                if (!inGMinerexitEvent)
                {
                    inGMinerexitEvent = true;

                    if (startedSince < DateTime.UtcNow.AddSeconds(-30)) { Thread.Sleep(7000); }
                    else { ChangeMinerBinary(); }

                    if (Miner.IsMining && !Miner.StoppingMining)
                    {
                        Start();
                    }

                    inGMinerexitEvent = false;
                }
            }
        }

        public static void CreateConfigFile()
        {
            StringBuilder conf = new();
            Server.MiningCoin miningCoin = SoftwareParameters.ServerConfig.MiningCoins.Find(x => x.Coin.Equals("xmr", StringComparison.OrdinalIgnoreCase));

            conf.AppendLine("{");
            conf.AppendLine("    \"api\": {");
            conf.AppendLine("        \"id\": null,");
            conf.AppendLine("        \"worker-id\": null");
            conf.AppendLine("    },");
            conf.AppendLine("    \"http\": {");
            conf.AppendLine("        \"enabled\": true,");
            conf.AppendLine("        \"host\": \"" + (User.Settings.User.UseAllInterfacesInsteadLocalhost ? "0.0.0.0" : "127.0.0.1") + "\",");
            conf.AppendLine("        \"port\": " + APIport + ",");
            conf.AppendLine("        \"access-token\": null,");
            conf.AppendLine("        \"restricted\": true");
            conf.AppendLine("    },");
            conf.AppendLine("    \"autosave\": false,");
            conf.AppendLine("    \"colors\": true,");
            conf.AppendLine("    \"title\": \"True Mining running GMiner\",");
            conf.AppendLine("    \"cpu\": {");
            conf.AppendLine("        \"enabled\": " + Settings.Device.cpu.MiningSelected.ToString().ToLowerInvariant() + ",");
            conf.AppendLine("        \"huge-pages\": true,");
            conf.AppendLine("        \"hw-aes\": null,");
            if (!Settings.Device.cpu.Autoconfig) { conf.AppendLine("        \"priority\": " + Settings.Device.cpu.Priority + ","); } else { conf.AppendLine("        \"priority\": 1,"); }
            conf.AppendLine("        \"memory-pool\": true,");
            if (!Settings.Device.cpu.Autoconfig) { conf.AppendLine("        \"yield\": " + (Settings.Device.cpu.Yield).ToString().ToLowerInvariant() + ","); }
            conf.AppendLine("        \"asm\": true,");
            if (!Settings.Device.cpu.Autoconfig && Settings.Device.cpu.Threads == 0) { conf.AppendLine("        \"max-threads-hint\": " + Settings.Device.cpu.MaxUsageHint + ","); }
            if (!Settings.Device.cpu.Autoconfig && Settings.Device.cpu.Threads > 0) { conf.AppendLine("        \"rx\": {\"threads\": " + Settings.Device.cpu.Threads + "},"); }
            conf.AppendLine("    },");
            conf.AppendLine("    \"randomx\": {");
            conf.AppendLine("        \"init\": -1,");
            conf.AppendLine("        \"init-avx2\": -1,");
            conf.AppendLine("        \"mode\": \"auto\",");
            conf.AppendLine("        \"1gb-pages\": true,");
            conf.AppendLine("        \"rdmsr\": true,");
            conf.AppendLine("        \"wrmsr\": true,");
            conf.AppendLine("        \"cache_qos\": true,");
            conf.AppendLine("        \"numa\": true,");
            conf.AppendLine("        \"scratchpad_prefetch_mode\": true");
            conf.AppendLine("    },");
            conf.AppendLine("    \"opencl\": {");
            conf.AppendLine("        \"enabled\": " + Settings.Device.opencl.MiningSelected.ToString().ToLowerInvariant() + ",");
            if (!Settings.Device.opencl.Autoconfig) { conf.AppendLine("     \"cache\": " + Settings.Device.opencl.Cache.ToString().ToLowerInvariant() + ","); }
            conf.AppendLine("        \"loader\": null,");
            conf.AppendLine("        \"platform\": \"AMD\",");
            conf.AppendLine("        \"adl\": true,");
            conf.AppendLine("    },");
            conf.AppendLine("    \"cuda\": {");
            conf.AppendLine("        \"enabled\": " + Settings.Device.cuda.MiningSelected.ToString().ToLowerInvariant() + ",");
            conf.AppendLine("        \"loader\": null,");
            if (!Settings.Device.cuda.Autoconfig) { conf.AppendLine("        \"nvml\": " + Settings.Device.cuda.NVML.ToString().ToLowerInvariant()); }
            conf.AppendLine("    },");
            conf.AppendLine("    \"donate-level\": 0,");
            conf.AppendLine("    \"donate-over-proxy\": 0,");
            conf.AppendLine("    \"log-file\": \"GMiner-log.txt\",");
            conf.AppendLine("    \"retries\": 2,");
            conf.AppendLine("    \"retry-pause\": 3,");
            conf.AppendLine("    \"pools\": [");

            List<string> addresses = miningCoin.Hosts;

            List<Task<KeyValuePair<string, long>>> pingReturnTasks = new();
            foreach (string address in addresses)
            {
                pingReturnTasks.Add(new Task<KeyValuePair<string, long>>(() => Tools.ReturnPing(address)));
            }
            foreach (Task task in pingReturnTasks)
            {
                task.Start();
            }

            Task.WaitAll(pingReturnTasks.ToArray());

            Dictionary<string, long> pingHosts = new();

            foreach (Task<KeyValuePair<string, long>> pingTask in pingReturnTasks)
            {
                pingHosts.TryAdd(pingTask.Result.Key, pingTask.Result.Value);
            }

            bool useTor = pingHosts.Count < pingHosts.Where((KeyValuePair<string, long> pair) => pair.Value == 2000).Count() * 2;

            miningCoin.Hosts = pingHosts.OrderBy((KeyValuePair<string, long> value) => value.Value).ToDictionary(x => x.Key, x => x.Value).Keys.ToList();

            if (User.Settings.User.UseTorSharpOnMining)
            {
                new Task(() => _ = Tools.TorProxy).Start();
            }

            foreach (string host in miningCoin.Hosts)
            {
                if (User.Settings.User.UseTorSharpOnMining)
                {
                    conf.AppendLine("        {");
                    conf.AppendLine("            \"algo\": \"rx/0\",");
                    conf.AppendLine("            \"url\": \"" + host + ":" + miningCoin.StratumPort + "\",");
                    conf.AppendLine("            \"user\": \"" + miningCoin.WalletTm + "." + Settings.User.PayCoin.CoinTicker.ToLowerInvariant() + '_' + Settings.User.Payment_Wallet + "/" + miningCoin.Email + "\", ");
                    conf.AppendLine("            \"pass\": \"" + miningCoin.Password + "\",");
                    conf.AppendLine("            \"rig-id\": null,");
                    conf.AppendLine("            \"nicehash\": false,");
                    conf.AppendLine("            \"keepalive\": false,");
                    conf.AppendLine("            \"enabled\": true,");
                    conf.AppendLine("            \"tls\": true,");
                    conf.AppendLine("            \"tls-fingerprint\": null,");
                    conf.AppendLine("            \"daemon\": false,");
                    conf.AppendLine("            \"socks5\": \"127.0.0.1:8428\",");
                    conf.AppendLine("            \"self-select\": null");
                    conf.AppendLine("        },");
                }

                conf.AppendLine("        {");
                conf.AppendLine("            \"algo\": \"rx/0\",");
                conf.AppendLine("            \"url\": \"" + host + ":" + miningCoin.StratumPort + "\",");
                conf.AppendLine("            \"user\": \"" + miningCoin.WalletTm + "." + User.Settings.User.PayCoin.CoinTicker.ToLowerInvariant() + '_' + Settings.User.Payment_Wallet + "/" + miningCoin.Email + "\", ");
                conf.AppendLine("            \"pass\": \"" + miningCoin.Password + "\",");
                conf.AppendLine("            \"rig-id\": null,");
                conf.AppendLine("            \"nicehash\": false,");
                conf.AppendLine("            \"keepalive\": false,");
                conf.AppendLine("            \"enabled\": true,");
                conf.AppendLine("            \"tls\": true,");
                conf.AppendLine("            \"tls-fingerprint\": null,");
                conf.AppendLine("            \"daemon\": false,");
                conf.AppendLine("            \"socks5\": null,");
                conf.AppendLine("            \"self-select\": null");
                conf.AppendLine("        },");
            }

            conf.AppendLine("   ]");
            conf.AppendLine("}");

            System.IO.File.WriteAllText(@"Miners\GMiner\config.json", conf.ToString());
        }

        private static int APIport { get; } = 20202;
    }
}