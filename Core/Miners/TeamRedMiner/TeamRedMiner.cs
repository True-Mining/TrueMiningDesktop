using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TrueMiningDesktop.Janelas;
using TrueMiningDesktop.Server;

namespace TrueMiningDesktop.Core.TeamRedMiner
{
    public class TeamRedMiner
    {
        private bool isMining = false;
        private bool isTryingStartMining = false;
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
        public readonly ProcessStartInfo TeamRedMinerProcessStartInfo = new(Environment.CurrentDirectory + @"\Miners\TeamRedMiner\teamredminer.exe");
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
                TeamRedMinerProcessStartInfo.Arguments = Arguments;
                TeamRedMinerProcessStartInfo.UseShellExecute = false;
                TeamRedMinerProcessStartInfo.RedirectStandardError = false;
                TeamRedMinerProcessStartInfo.RedirectStandardOutput = false;
                TeamRedMinerProcessStartInfo.CreateNoWindow = !User.Settings.User.ShowCLI;
                TeamRedMinerProcessStartInfo.ErrorDialog = false;
                TeamRedMinerProcessStartInfo.WindowStyle = User.Settings.User.ShowCLI ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden;

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
                            Thread.Sleep(30);
                            DateTime time = TeamRedMinerProcess.StartTime;
                            if (time.Ticks > 10)
                            {
                                Task hideIfNecessary = new Task(() =>
                                {
                                    if (!User.Settings.User.ShowCLI) { Hide(true); }
                                });
                                hideIfNecessary.Start();
                                hideIfNecessary.Wait(3000);

                                try { Tools.SetWindowText(TeamRedMinerProcess.MainWindowHandle, WindowTitle); } catch { }

                                break;
                            }
                        }
                        catch (Exception e) { MessageBox.Show(e.Message); }
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
                try
                {
                    IsStoppingMining = true;
                }
                catch { }

                Task tryCloseFancy = new(() =>
                {
                    try
                    {
                        TeamRedMinerProcess.CloseMainWindow();
                        TeamRedMinerProcess.WaitForExit();

                        IsMining = false;
                        IsStoppingMining = false;
                    }
                    catch
                    {
                        TeamRedMinerProcess.Kill(true);
                        Tools.KillProcessByName(TeamRedMinerProcess.ProcessName);

                        IsMining = false;
                        IsStoppingMining = false;
                    }
                });
                tryCloseFancy.Start();
                tryCloseFancy.Wait(4000);

                if (!tryCloseFancy.Wait(4000))
                {
                    try
                    {
                        TeamRedMinerProcess.Kill(true);
                        Tools.KillProcessByName(TeamRedMinerProcess.ProcessName);

                        IsMining = false;
                        IsStoppingMining = false;
                    }
                    catch { }
                }

                try
                {
                    if (TeamRedMinerProcess.Responding || !TeamRedMinerProcess.HasExited)
                    {
                        TeamRedMinerProcess.Kill(true);

                        IsMining = false;
                        IsStoppingMining = false;
                    }
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

        public void Hide(bool self = false)
        {
            bool showCLI = User.Settings.User.ShowCLI;
            bool MainWindowFocused = Tools.ApplicationIsActivated();

            if (self)
            {
                try
                {
                    DateTime initializingTask = DateTime.UtcNow;

                    while (true)
                    {
                        bool continueWaiting = true;
                        try
                        {
                            Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                continueWaiting = Tools.FindWindow(null, TeamRedMinerProcess.MainWindowTitle).ToInt32() == 0 && initializingTask >= DateTime.UtcNow.AddSeconds(-30);
                            });
                        }
                        catch { }
                        if (continueWaiting)
                        {
                            Thread.Sleep(50);
                        }
                        else
                        {
                            break;
                        }
                    }

                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        IntPtr windowIdentifier = Tools.FindWindow(null, TeamRedMinerProcess.MainWindowTitle);
                        if (showCLI)
                        {
                            if (Application.Current.MainWindow.IsVisible && MainWindowFocused)
                            {
                                Tools.ShowWindow(windowIdentifier, 1);
                            }
                            else
                            {
                                Tools.ShowWindow(windowIdentifier, 2);
                            }
                        }
                        else
                        {
                            Tools.ShowWindow(windowIdentifier, 0);
                        }
                    });
                }
                catch { }
            }

            TeamRedMinerProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        }

        public Dictionary<string, decimal> GetHasrates()
        {
            if (Backends == null || Backends.Count == 0) { return null; }

            try
            {
                string backendPureData = Tools.TcpPost("127.0.0.1", APIport, "{\"command\":\"devs\"}").Result;

                Dictionary<string, decimal> hashrates = new();

                double hashrate = 0;

                var devsJson = JToken.Parse(backendPureData)["DEVS"].Children().ToList();
                if (devsJson.Count == 1)
                {
                    hashrate = devsJson[0].Value<double>("KHS 30s");
                }
                else if (devsJson.Count > 1)
                {
                    foreach (JToken devJson in devsJson)
                    {
                        hashrate += devJson.Value<double>("KHS 30s");
                    }
                }
                hashrate *= 1000;

                hashrates.TryAdd("opencl", Convert.ToDecimal(hashrate, CultureInfo.InvariantCulture.NumberFormat));

                return hashrates;
            }
            catch { return null; }
        }

        public void CreateConfigFile(MiningCoin miningCoin)
        {
            APIport = 20300 + SoftwareParameters.ServerConfig.MiningCoins.IndexOf(miningCoin) + Device.DevicesList.IndexOf(this.Backends.Last());

            AlgoBackendsString = miningCoin.Algorithm.ToLowerInvariant() + '-' + string.Join(null, Backends.Select(x => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(x.BackendName.ToLowerInvariant())));

            WindowTitle = "TeamRedMiner - " + miningCoin.Algorithm + " - " + string.Join(", ", Backends.Select(x => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(x.BackendName.ToLowerInvariant())));

            string Algorithm = miningCoin.Algorithm.ToString().ToLowerInvariant();

            StringBuilder args = new();
            args.AppendLine("--hardware=gpu");
            args.AppendLine("--api_listen=" + (User.Settings.User.UseAllInterfacesInsteadLocalhost ? "0.0.0.0" : "127.0.0.1") + ':' + APIport);
            args.AppendLine("--log_file=\"logs/trm_" + AlgoBackendsString + ".log\"");
            args.AppendLine("--log_interval=30");
            args.AppendLine("--log_rotate=1M,4");
            args.AppendLine("--enable_compute");
            args.AppendLine("--eth_micro_delay=32");
            args.AppendLine("--eth_no_4gb_kernels");
            args.AppendLine("--eth_stagger");
            args.AppendLine("--prog_stagger");
            args.AppendLine("--watchdog_disabled");

            if (User.Settings.Device.opencl.ChipFansFullspeedTemp > 0 || User.Settings.Device.opencl.MemFansFullspeedTemp > 0)
            {
                args.AppendLine("--fan_control=" + (User.Settings.Device.opencl.ChipFansFullspeedTemp > 0 ? User.Settings.Device.opencl.ChipFansFullspeedTemp : "") + "::" + (User.Settings.Device.opencl.MemFansFullspeedTemp > 0 ? User.Settings.Device.opencl.MemFansFullspeedTemp : "") + "::40:100");
            }

            if (User.Settings.Device.opencl.ChipPauseMiningTemp > 0)
            {
                args.AppendLine("--temp_limit=" + User.Settings.Device.opencl.ChipPauseMiningTemp);
                args.AppendLine("--temp_resume=" + (User.Settings.Device.opencl.ChipPauseMiningTemp - 30));
            }

            if (User.Settings.Device.opencl.MemPauseMiningTemp > 0)
            {
                args.AppendLine("--mem_temp_limit=" + User.Settings.Device.opencl.MemPauseMiningTemp);
                args.AppendLine("--mem_temp_resume=" + (User.Settings.Device.opencl.MemPauseMiningTemp - 30));
            }

            List<string> addresses = miningCoin.PoolHosts;

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

            miningCoin.PoolHosts = pingHosts.OrderBy((KeyValuePair<string, long> value) => value.Value).ToDictionary(x => x.Key, x => x.Value).Keys.ToList();

            if (User.Settings.User.UseTorSharpOnMining)
            {
                new Task(() => _ = Tools.TorProxy).Start();
            }

            foreach (string host in miningCoin.PoolHosts)
            {
                args.AppendLine("-a " + Algorithm + " -o stratum+tcp://" + host + ":" + miningCoin.StratumPortSsl + " -u " + miningCoin.DepositAddressTrueMining + "." + User.Settings.User.PayCoin.CoinTicker.ToLowerInvariant() + '_' + User.Settings.User.Payment_Wallet + "/" + miningCoin.Email + " -p " + miningCoin.Password);
                args.AppendLine("-a " + Algorithm + " -o stratum+tcp://" + host + ":" + miningCoin.StratumPort + " -u " + miningCoin.DepositAddressTrueMining + "." + User.Settings.User.PayCoin.CoinTicker.ToLowerInvariant() + '_' + User.Settings.User.Payment_Wallet + "/" + miningCoin.Email + " -p " + miningCoin.Password);
            }

            args.Remove(args.Length - 1, 1);

            Arguments = args.ToString().Replace(Environment.NewLine, " ");
            TeamRedMinerProcessStartInfo.Arguments = Arguments;

            if (!Directory.Exists(@"Miners")) { Directory.CreateDirectory(@"Miners"); }
            if (!Directory.Exists(@"Miners\TeamRedMiner")) { Directory.CreateDirectory(@"Miners\TeamRedMiner"); }
            if (!Directory.Exists(@"Miners\TeamRedMiner\logs")) { Directory.CreateDirectory(@"Miners\TeamRedMiner\logs"); }

            StringBuilder cmdStart = new();
            cmdStart.AppendLine("cd /d \"%~dp0\"");
            cmdStart.AppendLine();
            cmdStart.AppendLine("set GPU_MAX_ALLOC_PERCENT=100");
            cmdStart.AppendLine("set GPU_SINGLE_ALLOC_PERCENT=100");
            cmdStart.AppendLine("set GPU_MAX_HEAP_SIZE=100");
            cmdStart.AppendLine("set GPU_USE_SYNC_OBJECTS=1");
            cmdStart.AppendLine();
            cmdStart.AppendLine("teamredminer.exe ^\n" + args.ToString().Replace(Environment.NewLine, " ^\n"));
            cmdStart.Append("pause");

            System.IO.File.WriteAllText(@"Miners\TeamRedMiner\start-" + AlgoBackendsString + ".cmd", cmdStart.ToString());
        }
    }
}