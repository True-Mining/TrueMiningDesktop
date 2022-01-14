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

namespace TrueMiningDesktop.Core.TeamRedMiner
{
    public class TeamRedMiner
    {
        private bool isMining = false;
        private bool isTryingStartMining = true;
        private bool isStoppingMining = false;

        private string Arguments = "";

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
        public readonly Process TeamRedMinerProcess = new();
        public readonly ProcessStartInfo TeamRedMinerProcessStartInfo = new(Environment.CurrentDirectory + @"\Miners\TeamRedMiner\" + @"teamredminer.exe");
        private string AlgoBackendsString = null;
        public string WindowTitle = "True Mining running TeamRedMiner";
        private int APIport = 20220;
        private bool IsInTeamRedMinerexitEvent = false;
        private DateTime startedSince = DateTime.Now.AddYears(-1);

        public TeamRedMiner(List<DeviceInfo> backends)
        {
            Backends = backends;

            MiningCoin miningCoin = SoftwareParameters.ServerConfig.MiningCoins.First(x => x.Algorithm.Equals(backends.First().MiningAlgo, StringComparison.OrdinalIgnoreCase));

            CreateConfigFile(miningCoin);
        }

        public void Start()
        {
            IsTryingStartMining = true;

            if (TeamRedMinerProcess.StartInfo != TeamRedMinerProcessStartInfo)
            {
                TeamRedMinerProcessStartInfo.WorkingDirectory = Environment.CurrentDirectory + @"\Miners\TeamRedMiner\";
                TeamRedMinerProcessStartInfo.Arguments = "";
                TeamRedMinerProcessStartInfo.UseShellExecute = true;
                TeamRedMinerProcessStartInfo.RedirectStandardError = false;
                TeamRedMinerProcessStartInfo.RedirectStandardOutput = false;
                TeamRedMinerProcessStartInfo.CreateNoWindow = false;
                TeamRedMinerProcessStartInfo.ErrorDialog = false;
                TeamRedMinerProcessStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                TeamRedMinerProcess.StartInfo = TeamRedMinerProcessStartInfo;

                if (TeamRedMinerProcessStartInfo.EnvironmentVariables.ContainsKey("GPU_MAX_ALLOC_PERCENT")) { TeamRedMinerProcessStartInfo.EnvironmentVariables["GPU_MAX_ALLOC_PERCENT"] = "100"; } else { TeamRedMinerProcessStartInfo.EnvironmentVariables.Add("GPU_MAX_ALLOC_PERCENT", "100"); }
                if (TeamRedMinerProcessStartInfo.EnvironmentVariables.ContainsKey("GPU_SINGLE_ALLOC_PERCENT")) { TeamRedMinerProcessStartInfo.EnvironmentVariables["GPU_SINGLE_ALLOC_PERCENT"] = "100"; } else { TeamRedMinerProcessStartInfo.EnvironmentVariables.Add("GPU_SINGLE_ALLOC_PERCENT", "100"); }
                if (TeamRedMinerProcessStartInfo.EnvironmentVariables.ContainsKey("GPU_MAX_HEAP_SIZE")) { TeamRedMinerProcessStartInfo.EnvironmentVariables["GPU_MAX_HEAP_SIZE"] = "100"; } else { TeamRedMinerProcessStartInfo.EnvironmentVariables.Add("GPU_MAX_HEAP_SIZE", "100"); }
                if (TeamRedMinerProcessStartInfo.EnvironmentVariables.ContainsKey("GPU_USE_SYNC_OBJECTS")) { TeamRedMinerProcessStartInfo.EnvironmentVariables["GPU_USE_SYNC_OBJECTS"] = "1"; } else { TeamRedMinerProcessStartInfo.EnvironmentVariables.Add("GPU_USE_SYNC_OBJECTS", "1"); }
            }

            TeamRedMinerProcess.Exited -= TeamRedMinerProcess_Exited;
            TeamRedMinerProcess.Exited += TeamRedMinerProcess_Exited;
            TeamRedMinerProcess.EnableRaisingEvents = true;

            try
            {
                TeamRedMinerProcess.ErrorDataReceived -= TeamRedMinerProcess_ErrorDataReceived;
                TeamRedMinerProcess.ErrorDataReceived += TeamRedMinerProcess_ErrorDataReceived;

                TeamRedMinerProcess.Start();

                new Task(() =>
                {
                    while (true)
                    {
                        try
                        {
                            Thread.Sleep(100);
                            DateTime time = TeamRedMinerProcess.StartTime;
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
                                MessageBox.Show("TeamRedMiner can't start. Try add True Mining Desktop folder in Antivirus/Windows Defender exclusions. " + e.Message);
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
                        MessageBox.Show("TeamRedMiner failed to start. Try add True Mining Desktop folder in Antivirus/Windows Defender exclusions. " + ee.Message);
                    }
                }
            }
        }

        private void TeamRedMinerProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Tools.KillProcess(TeamRedMinerProcess.ProcessName); Stop();
        }

        private void TeamRedMinerProcess_Exited(object sender, EventArgs e)
        {
            if (IsMining && !IsStoppingMining)
            {
                if (!IsInTeamRedMinerexitEvent)
                {
                    IsInTeamRedMinerexitEvent = true;

                    IsTryingStartMining = true;

                    if (startedSince < DateTime.UtcNow.AddSeconds(-30)) { Thread.Sleep(30000); } else { Thread.Sleep(10000); }

                    if (IsMining && !IsStoppingMining)
                    {
                        Start();
                    }

                    IsInTeamRedMinerexitEvent = false;
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
                        TeamRedMinerProcess.CloseMainWindow();
                        TeamRedMinerProcess.WaitForExit();

                        closed = true;
                        IsMining = false;
                        IsStoppingMining = false;
                    }
                    catch
                    {
                        TeamRedMinerProcess.Kill(true);

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
                        TeamRedMinerProcess.Kill(true);
                        Tools.KillProcessByName(TeamRedMinerProcess.ProcessName);

                        closed = true;
                        IsMining = false;
                        IsStoppingMining = false;
                    }
                    catch { }
                }

                try
                {
                    TeamRedMinerProcess.Kill(true);

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
            TeamRedMinerProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
        }

        public void Hide()
        {
            TeamRedMinerProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        }

        public Dictionary<string, decimal> GetHasrates()
        {
            if (Backends == null || Backends.Count == 0) { return null; }

            try
            {
                string backendPureData = new WebClient().DownloadString("http://localhost:" + APIport + "/summary");
                Miners.TeamRedMiner.ApiSummary backendsAPI = JsonConvert.DeserializeObject<Miners.TeamRedMiner.ApiSummary>(backendPureData, new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture });

                Dictionary<string, decimal> hashrates = new();

                Backends.ForEach(backend =>
                {

                    if (backendsAPI.Uptime < 1)
                    {
                        hashrates.TryAdd("opencl", -1);
                    }
                    else if (backendsAPI.Hashrate < 1)
                    {
                        hashrates.TryAdd("opencl", 0);
                    }
                    else
                    {
                        hashrates.TryAdd("opencl", Convert.ToDecimal(backendsAPI.Hashrate, CultureInfo.InvariantCulture.NumberFormat));
                    }
                });

                return hashrates;
            }
            catch { return null; }
        }

        public void CreateConfigFile(MiningCoin miningCoin)
        {
            APIport = 20230 + SoftwareParameters.ServerConfig.MiningCoins.IndexOf(miningCoin);

            AlgoBackendsString = miningCoin.Algorithm.ToLowerInvariant() + '-' + string.Join(null, Backends.Select(x => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(x.BackendName.ToLowerInvariant())));

            WindowTitle = "TeamRedMiner - " + miningCoin.Algorithm + " - " + string.Join(", ", Backends.Select(x => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(x.BackendName.ToLowerInvariant())));

            string Algorithm = miningCoin.Algorithm.ToString().ToLowerInvariant();

            StringBuilder args = new();
            args.AppendLine("--hardware=gpu");
            args.AppendLine("--api_listen=" + (User.Settings.User.UseAllInterfacesInsteadLocalhost ? "0.0.0.0" : "127.0.0.1") + APIport);
            args.AppendLine("--log_file=\"trm_<algo>_<yyyymmdd_hhmmss>.log\"");
            args.AppendLine("--log_interval=30");
            args.AppendLine("--log_rotate=1M,16");
            args.AppendLine("--enable_compute");

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
                args.AppendLine("-a " + Algorithm + "-o stratum+tcp://" + host + ":" + miningCoin.StratumPort + " -u " + miningCoin.WalletTm + "." + User.Settings.User.PayCoin.CoinTicker.ToLowerInvariant() + '_' + User.Settings.User.Payment_Wallet + "/" + miningCoin.Email + " -p " + miningCoin.Password);
            }

            Arguments = String.Join(args.To;

            if (!Directory.Exists(@"Miners")) { Directory.CreateDirectory(@"Miners"); }
            if (!Directory.Exists(@"Miners\TeamRedMiner")) { Directory.CreateDirectory(@"Miners\TeamRedMiner"); }
            if (!Directory.Exists(@"Miners\TeamRedMiner\logs")) { Directory.CreateDirectory(@"Miners\TeamRedMiner\logs"); }

            System.IO.File.WriteAllText(@"Miners\TeamRedMiner\config-" + AlgoBackendsString + ".json", args.ToString().NormalizeJson());

            StringBuilder cmdStart = new();
            cmdStart.AppendLine("cd /d \"%~dp0\"");
            cmdStart.AppendLine("teamredminer.exe --config " + "config-" + AlgoBackendsString + ".json");
            cmdStart.AppendLine("pause");

            System.IO.File.WriteAllText(@"Miners\TeamRedMiner\start-" + AlgoBackendsString + ".cmd", cmdStart.ToString());
        }
    }
}