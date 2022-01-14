using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TrueMiningDesktop.Janelas;
using TrueMiningDesktop.Server;
using TrueMiningDesktop.User;

namespace TrueMiningDesktop.Core.TRex
{
    public class TRex
    {
        private bool isMining = false;
        private bool isTryingStartMining = true;
        private bool isStoppingMining = false;

        public bool IsMining
        {
            get
            {
                return isMining;
            }
            set
            {
                isMining = value;
                Miner.VerifyGeneralMiningState();
            }
        }

        public bool IsTryingStartMining
        {
            get
            {
                return isTryingStartMining;
            }
            set
            {
                isTryingStartMining = value;
                Miner.VerifyGeneralMiningState();
            }
        }

        public bool IsStoppingMining
        {
            get
            {
                return isStoppingMining;
            }
            set
            {
                isStoppingMining = value;
                Miner.VerifyGeneralMiningState();
            }
        }

        private List<DeviceInfo> Backends = new();
        public readonly Process TRexProcess = new();
        public readonly ProcessStartInfo TRexProcessStartInfo = new(Environment.CurrentDirectory + @"\Miners\TRex\" + @"t-rex.exe");
        private string AlgoBackendsString = null;
        public string WindowTitle = "True Mining running TRex";
        private int APIport = 20210;
        private bool IsInTRexexitEvent = false;
        private DateTime startedSince = DateTime.Now.AddYears(-1);

        public TRex(List<DeviceInfo> backends)
        {
            Backends = backends;

            MiningCoin miningCoin = SoftwareParameters.ServerConfig.MiningCoins.First(x => x.Algorithm.Equals(backends.First().MiningAlgo, StringComparison.OrdinalIgnoreCase));

            CreateConfigFile(miningCoin);
        }

        public void Start()
        {
            IsTryingStartMining = true;

            if (TRexProcess.StartInfo != TRexProcessStartInfo)
            {
                TRexProcessStartInfo.WorkingDirectory = Environment.CurrentDirectory + @"\Miners\TRex\";
                TRexProcessStartInfo.Arguments = "--config config-" + AlgoBackendsString + ".json";
                TRexProcessStartInfo.UseShellExecute = true;
                TRexProcessStartInfo.RedirectStandardError = false;
                TRexProcessStartInfo.RedirectStandardOutput = false;
                TRexProcessStartInfo.CreateNoWindow = false;
                TRexProcessStartInfo.ErrorDialog = false;
                TRexProcessStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                TRexProcess.StartInfo = TRexProcessStartInfo;
            }

            TRexProcess.Exited -= TRexProcess_Exited;
            TRexProcess.Exited += TRexProcess_Exited;
            TRexProcess.EnableRaisingEvents = true;

            try
            {
                TRexProcess.ErrorDataReceived -= TRexProcess_ErrorDataReceived;
                TRexProcess.ErrorDataReceived += TRexProcess_ErrorDataReceived;

                TRexProcess.Start();

                new Task(() =>
                {
                    while (true)
                    {
                        try
                        {
                            Thread.Sleep(100);
                            DateTime time = TRexProcess.StartTime;
                            if (time.Ticks > 100) { break; }
                        }
                        catch { }
                    }
                }).Wait(3000);

                IsMining = true;
                IsTryingStartMining = false;

                startedSince = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                Stop();

                IsTryingStartMining = true;

                if (minerBinaryChangedTimes < 2)
                {
                    Thread.Sleep(3000);
                    Start();
                }
                else
                {
                    try
                    {
                        if (!Tools.HaveADM)
                        {
                            Tools.RestartApp(true);

                            IsTryingStartMining = false;
                        }
                        else
                        {
                            if (Tools.AddedTrueMiningDestopToWinDefenderExclusions)
                            {
                                IsTryingStartMining = false;
                                MessageBox.Show("TRex can't start. Try add True Mining Desktop folder in Antivirus/Windows Defender exclusions. " + e.Message);
                            }
                            else
                            {
                                Application.Current.Dispatcher.Invoke((Action)delegate
                                {
                                    Tools.AddTrueMiningDestopToWinDefenderExclusions(true);

                                    Thread.Sleep(3000);
                                    Start();
                                });
                            }
                        }
                    }
                    catch (Exception ee)
                    {
                        IsTryingStartMining = false;
                        MessageBox.Show("TRex failed to start. Try add True Mining Desktop folder in Antivirus/Windows Defender exclusions. " + ee.Message);
                    }
                }
            }
        }

        private void TRexProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Tools.KillProcess(TRexProcess.ProcessName); Stop();
        }

        private void TRexProcess_Exited(object sender, EventArgs e)
        {
            if (IsMining && !IsStoppingMining)
            {
                if (!IsInTRexexitEvent)
                {
                    IsInTRexexitEvent = true;

                    IsTryingStartMining = true;

                    if (startedSince < DateTime.UtcNow.AddSeconds(-30)) { Thread.Sleep(7000); }

                    if (IsMining && !IsStoppingMining)
                    {
                        Start();
                    }

                    IsInTRexexitEvent = false;
                }
            }
        }

        public void Stop()
        {
            try
            {
                bool closed = false;

                IsStoppingMining = true;

                Task tryCloseFancy = new(() =>
                {
                    try
                    {
                        TRexProcess.CloseMainWindow();
                        TRexProcess.WaitForExit();

                        closed = true;
                        IsMining = false;
                        IsStoppingMining = false;
                    }
                    catch
                    {
                        TRexProcess.Kill(true);

                        closed = true;
                        IsMining = false;
                        IsStoppingMining = false;
                    }
                });
                tryCloseFancy.Start();
                tryCloseFancy.Wait(4000);

                if (!closed)
                {
                    try
                    {
                        TRexProcess.Kill(true);
                        Tools.KillProcessByName(TRexProcess.ProcessName);

                        closed = true;
                        IsMining = false;
                        IsStoppingMining = false;
                    }
                    catch { }
                }

                try
                {
                    TRexProcess.Kill(true);

                    closed = true;
                    IsMining = false;
                    IsStoppingMining = false;
                }
                catch { }
            }
            catch { }
        }

        private int minerBinaryChangedTimes = 0;

        public void Show()
        {
            TRexProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
        }

        public void Hide()
        {
            TRexProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        }

        public Dictionary<string, decimal> GetHasrates()
        {
            if (Backends == null || Backends.Count == 0) { return null; }

            try
            {
                string backendPureData = new WebClient().DownloadString("http://localhost:" + APIport + "/2/backends");
                dynamic backendsAPI = JsonConvert.DeserializeObject(backendPureData, new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture });

                Dictionary<string, decimal> hashrates = new();

                Backends.ForEach(backend =>
                {
                    if (backend.BackendName.Equals("cpu", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (dynamic backendLoop in backendsAPI)
                        {
                            if (backendLoop.type == "cpu")
                            {
                                if (backendLoop.enabled == false)
                                {
                                    hashrates.TryAdd("cpu", -1);
                                }
                                else if (backendLoop.hashrate[0] == null)
                                {
                                    hashrates.TryAdd("cpu", 0);
                                }
                                else
                                {
                                    hashrates.TryAdd("cpu", Convert.ToDecimal(backendLoop.hashrate[0], CultureInfo.InvariantCulture.NumberFormat));
                                }
                            }
                        }
                    }

                    if (backend.BackendName.Equals("opencl", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (dynamic backendLoop in backendsAPI)
                        {
                            if (backendLoop.type == "opencl")
                            {
                                if (backendLoop.enabled == false)
                                {
                                    hashrates.TryAdd("opencl", -1);
                                }
                                else if (backendLoop.hashrate[0] == null)
                                {
                                    hashrates.TryAdd("opencl", 0);
                                }
                                else
                                {
                                    hashrates.TryAdd("opencl", Convert.ToDecimal(backendLoop.hashrate[0], CultureInfo.InvariantCulture.NumberFormat));
                                }
                            }
                        }
                    }

                    if (backend.BackendName.Equals("cuda", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (dynamic backendLoop in backendsAPI)
                        {
                            if (backendLoop.type == "cuda")
                            {
                                if (backendLoop.enabled == false)
                                {
                                    hashrates.TryAdd("cuda", -1);
                                }
                                else if (backendLoop.hashrate[0] == null)
                                {
                                    hashrates.TryAdd("cuda", 0);
                                }
                                else
                                {
                                    hashrates.TryAdd("cuda", Convert.ToDecimal(backendLoop.hashrate[0], CultureInfo.InvariantCulture.NumberFormat));
                                }
                            }
                        }
                    }
                });

                return hashrates;
            }
            catch { return null; }
        }

        public void CreateConfigFile(MiningCoin miningCoin)
        {
            APIport = 20210 + SoftwareParameters.ServerConfig.MiningCoins.IndexOf(miningCoin);

            AlgoBackendsString = miningCoin.Algorithm.ToLowerInvariant() + '-' + string.Join(null, Backends.Select(x => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(x.BackendName.ToLowerInvariant())));

            WindowTitle = "TRex - " + miningCoin.Algorithm + " - " + string.Join(", ", Backends.Select(x => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(x.BackendName.ToLowerInvariant())));

            string Algorithm = miningCoin.Algorithm.ToString().ToLowerInvariant();
            if (Algorithm.Equals("RandomX", StringComparison.OrdinalIgnoreCase)) { Algorithm = "rx/0"; }

            StringBuilder conf = new();
            conf.AppendLine("{");
            conf.AppendLine("  \"algo\": \"" + Algorithm + "\",");
            conf.AppendLine("  \"coin\" : \"" + miningCoin.CoinTicker.ToLowerInvariant() + "\",");
            if (User.Settings.User.UseTorSharpOnMining) { conf.AppendLine("  \"proxy\": \"127.0.0.1:8428\","); }
            conf.AppendLine("  \"pci-indexing\" : false,");
            conf.AppendLine("  \"ab-indexing\" : false,");
            conf.AppendLine("  \"gpu-init-mode\" : 0,");
            conf.AppendLine("  \"keep-gpu-busy\" : false,");
            conf.AppendLine("  \"api-bind-http\": \"127.0.0.1:" + APIport + "\",");
            conf.AppendLine("  \"api-https\": false,");
            conf.AppendLine("  \"api-key\": \"\",");
            conf.AppendLine("  \"api-webserver-cert\" : \"\",");
            conf.AppendLine("  \"api-webserver-pkey\" : \"\",");
            conf.AppendLine("  \"kernel\" : 0,");
            conf.AppendLine("  \"retries\": 3,");
            conf.AppendLine("  \"retry-pause\": 10,");
            conf.AppendLine("  \"timeout\": 150,");
            conf.AppendLine("  \"intensity\": 20,");
            conf.AppendLine("  \"dag-build-mode\": 0,");
            conf.AppendLine("  \"dataset-mode\": 0,");
            conf.AppendLine("  \"extra-dag-epoch\": -1,");
            conf.AppendLine("  \"low-load\": 0,");
            conf.AppendLine("  \"lhr-tune\": -1,");
            conf.AppendLine("  \"lhr-autotune-mode\": \"full\",");
            conf.AppendLine("  \"lhr-autotune-step-size\": 0.25,");
            conf.AppendLine("  \"lhr-autotune-interval\": 15,");
            conf.AppendLine("  \"lhr-low-power\": false,");
            conf.AppendLine("  \"hashrate-avr\": 60,");
            conf.AppendLine("  \"sharerate-avr\": 600,");
            conf.AppendLine("  \"gpu-report-interval\": 30,");
            conf.AppendLine("  \"log-path\": \"logs/t-rex.log\",");
            conf.AppendLine("  \"cpu-priority\": 2,");
            conf.AppendLine("  \"autoupdate\": false,");
            conf.AppendLine("  \"exit-on-cuda-error\": true,");
            conf.AppendLine("  \"exit-on-connection-lost\": false,");
            conf.AppendLine("  \"reconnect-on-fail-shares\": 5,");
            conf.AppendLine("  \"protocol-dump\": false,");
            conf.AppendLine("  \"no-color\": false,");
            conf.AppendLine("  \"hide-date\": false,");
            conf.AppendLine("  \"send-stales\": false,");
            conf.AppendLine("  \"validate-shares\": false,");
            conf.AppendLine("  \"no-nvml\": " + (!User.Settings.Device.cuda.NVML).ToString().ToLowerInvariant() + ",");
            conf.AppendLine("  \"no-strict-ssl\": true,");
            conf.AppendLine("  \"no-sni\": false,");
            conf.AppendLine("  \"no-hashrate-report\": false,");
            conf.AppendLine("  \"no-watchdog\": true,");
            conf.AppendLine("  \"quiet\": false,");
            conf.AppendLine("  \"time-limit\": 0,");
            conf.AppendLine("  \"temperature-color\": \"67,77\",");
            conf.AppendLine("  \"temperature-color-mem\": \"80,100\",");
            conf.AppendLine("  \"temperature-limit\": 0,");
            conf.AppendLine("  \"temperature-start\": 0,");
            conf.AppendLine("  \"back-to-main-pool-sec\": 6000,");
            conf.AppendLine("  \"script-start\": \"\",");
            conf.AppendLine("  \"script-exit\": \"\",");
            conf.AppendLine("  \"script-epoch-change\": \"\",");
            conf.AppendLine("  \"script-crash\": \"\",");
            conf.AppendLine("  \"script-low-hash\": \"\",");
            conf.AppendLine("  \"monitoring-page\" : {");
            conf.AppendLine("     \"graph_interval_sec\" : 3600,");
            conf.AppendLine("     \"update_timeout_sec\" : 10");
            conf.AppendLine("  },");

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

            conf.AppendLine("  \"pools\": [");

            foreach (string host in miningCoin.Hosts)
            {
                conf.AppendLine("    {");
                conf.AppendLine("      \"user\": \"" + miningCoin.WalletTm + "." + User.Settings.User.PayCoin.CoinTicker.ToLowerInvariant() + '_' + User.Settings.User.Payment_Wallet + "/" + miningCoin.Email + "\",");
                conf.AppendLine("      \"url\": \"" + host + ":" + miningCoin.StratumPortSsl + "\",");
                conf.AppendLine("      \"pass\": \"" + miningCoin.Password + "\",");
                conf.AppendLine("    },");
            }

            conf.AppendLine("  ]");
            conf.AppendLine("}");

            if (!Directory.Exists(@"Miners")) { Directory.CreateDirectory(@"Miners"); }
            if (!Directory.Exists(@"Miners\TRex")) { Directory.CreateDirectory(@"Miners\TRex"); }
            if (!Directory.Exists(@"Miners\TRex\logs")) { Directory.CreateDirectory(@"Miners\TRex\logs"); }

            System.IO.File.WriteAllText(@"Miners\TRex\config-" + AlgoBackendsString + ".json", conf.ToString().NormalizeJson());

            StringBuilder cmdStart = new();
            cmdStart.AppendLine("cd /d \"%~dp0\"");
            cmdStart.AppendLine("t-rex.exe --config " + "config-" + AlgoBackendsString + ".json");
            cmdStart.AppendLine("pause");

            System.IO.File.WriteAllText(@"Miners\TRex\start-" + AlgoBackendsString + ".cmd", cmdStart.ToString());
        }
    }
}