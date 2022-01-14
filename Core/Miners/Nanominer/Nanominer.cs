using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TrueMiningDesktop.Janelas;
using TrueMiningDesktop.Server;
using TrueMiningDesktop.User;

namespace TrueMiningDesktop.Core.Nanominer
{
    public class Nanominer
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

        private List<Janelas.DeviceInfo> Backends = new();
        public readonly Process MinerProcess = new();
        public readonly ProcessStartInfo MinerProcessStartInfo = new(Environment.CurrentDirectory + @"\Miners\Nanominer\" + @"nanominer-cuda11.exe");
        private string AlgoBackendsString = null;
        public string WindowTitle = "True Mining running Nanominer";
        private int APIport = 20310;
        private bool IsInMinerProcessExitEvent = false;
        private DateTime StartedSince = DateTime.Now.AddYears(-1);

        public Nanominer(List<Janelas.DeviceInfo> backends)
        {
            Backends = backends;

            MiningCoin miningCoin = SoftwareParameters.ServerConfig.MiningCoins.First(x => x.Algorithm.Equals(backends.First().MiningAlgo, StringComparison.OrdinalIgnoreCase));

            CreateConfigFile(miningCoin);
        }

        public void Start()
        {
            IsTryingStartMining = true;

            if (MinerProcess.StartInfo != MinerProcessStartInfo)
            {
                MinerProcessStartInfo.WorkingDirectory = Environment.CurrentDirectory + @"\Miners\Nanominer\";
                MinerProcessStartInfo.Arguments = "config-" + AlgoBackendsString + ".ini";
                MinerProcessStartInfo.UseShellExecute = true;
                MinerProcessStartInfo.RedirectStandardError = false;
                MinerProcessStartInfo.RedirectStandardOutput = false;
                MinerProcessStartInfo.CreateNoWindow = false;
                MinerProcessStartInfo.ErrorDialog = false;
                MinerProcessStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                MinerProcess.StartInfo = MinerProcessStartInfo;
            }

            MinerProcess.Exited -= MinerProcess_Exited;
            MinerProcess.Exited += MinerProcess_Exited;
            MinerProcess.EnableRaisingEvents = true;

            try
            {
                MinerProcess.ErrorDataReceived -= MinerProcess_ErrorDataReceived;
                MinerProcess.ErrorDataReceived += MinerProcess_ErrorDataReceived;

                MinerProcess.Start();

                new Task(() =>
                {
                    while (true)
                    {
                        try
                        {
                            Thread.Sleep(100);
                            DateTime time = MinerProcess.StartTime;
                            if (time.Ticks > 100) { break; }
                        }
                        catch { }
                    }
                }).Wait(3000);

                IsMining = true;
                IsTryingStartMining = false;

                StartedSince = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                Stop();

                IsTryingStartMining = true;

                if (minerBinaryChangedTimes < 4)
                {
                    ChangeMinerBinary();
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
                                MessageBox.Show("Nanominer can't start. Try add True Mining Desktop folder in Antivirus/Windows Defender exclusions. " + e.Message);
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
                        MessageBox.Show("Nanominer failed to start. Try add True Mining Desktop folder in Antivirus/Windows Defender exclusions. " + ee.Message);
                    }
                }
            }
        }

        private void MinerProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Tools.KillProcess(MinerProcess.ProcessName); Stop();
        }

        private void MinerProcess_Exited(object sender, EventArgs e)
        {
            if (IsMining && !IsStoppingMining)
            {
                if (!IsInMinerProcessExitEvent)
                {
                    IsInMinerProcessExitEvent = true;

                    IsTryingStartMining = true;

                    if (StartedSince < DateTime.UtcNow.AddSeconds(-30)) { Thread.Sleep(7000); }
                    else { ChangeMinerBinary(); }

                    if (IsMining && !IsStoppingMining)
                    {
                        Start();
                    }

                    IsInMinerProcessExitEvent = false;
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
                        MinerProcess.CloseMainWindow();
                        MinerProcess.WaitForExit();

                        closed = true;
                        IsMining = false;
                        IsStoppingMining = false;
                    }
                    catch
                    {
                        MinerProcess.Kill(true);

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
                        MinerProcess.Kill(true);
                        Tools.KillProcessByName(MinerProcess.ProcessName);

                        closed = true;
                        IsMining = false;
                        IsStoppingMining = false;
                    }
                    catch { }
                }

                try
                {
                    MinerProcess.Kill(true);

                    closed = true;
                    IsMining = false;
                    IsStoppingMining = false;
                }
                catch { }
            }
            catch { }
        }

        private int minerBinaryChangedTimes = 0;

        public void ChangeMinerBinary()
        {
            if (MinerProcessStartInfo.FileName == Environment.CurrentDirectory + @"\Miners\Nanominer\" + @"nanominer-cuda11.exe")
            {
                MinerProcessStartInfo.FileName = Environment.CurrentDirectory + @"\Miners\Nanominer\" + @"nanominer-cuda10.exe";
            }
            else if (MinerProcessStartInfo.FileName == Environment.CurrentDirectory + @"\Miners\Nanominer\" + @"nanominer-cuda10.exe")
            {
                MinerProcessStartInfo.FileName = Environment.CurrentDirectory + @"\Miners\Nanominer\" + @"nanominer-cuda11.exe";
            }
            else
            {
                MinerProcessStartInfo.FileName = Environment.CurrentDirectory + @"\Miners\Nanominer\" + @"nanominer-cuda11.exe";
            }

            if (minerBinaryChangedTimes < 100) { minerBinaryChangedTimes++; }
        }

        public void Show()
        {
            MinerProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
        }

        public void Hide()
        {
            MinerProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        }

        public Dictionary<string, decimal> GetHasrates()
        {
            if (Backends == null || Backends.Count == 0) { return null; }

            try
            {
                string apiPureData = new WebClient().DownloadString("http://localhost:" + APIport + "/stats");
                Miners.Nanominer.ApiSummary apiObj = JsonConvert.DeserializeObject<Miners.Nanominer.ApiSummary>(apiPureData);

                Dictionary<string, decimal> hashrates = new();

                Backends.ForEach(backend =>
                {
                    if (backend.BackendName.Equals("cpu", StringComparison.OrdinalIgnoreCase))
                    {
                        List<string> listDeviceNames = new();

                        apiObj.Devices.ForEach(devices =>
                        {
                            try
                            {
                                foreach (var device in devices)
                                {
                                    try
                                    {
                                        if (device.Value.Platform.Equals("CPU", StringComparison.OrdinalIgnoreCase)) { listDeviceNames.Add(device.Key); }
                                    }
                                    catch { }
                                }
                            }
                            catch { }
                        });

                        decimal thisDeviceHashrate = 0;

                        listDeviceNames.ForEach(deviceName =>
                        {
                            try
                            {
                                apiObj.Algorithms.ForEach(algorithms =>
                            {
                                foreach (var algorithm in algorithms)
                                {
                                    try
                                    {
                                        if (algorithm.Key.Equals(deviceName, StringComparison.OrdinalIgnoreCase)) { thisDeviceHashrate += decimal.Parse(algorithm.Value.Hashrate, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat); }
                                    }
                                    catch { }
                                }
                            });
                            }
                            catch { }
                        });

                        hashrates.TryAdd("cpu", thisDeviceHashrate);
                    }

                    if (backend.BackendName.Equals("opencl", StringComparison.OrdinalIgnoreCase))
                    {
                        List<string> listDeviceNames = new();

                        apiObj.Devices.ForEach(devices =>
                        {
                            try
                            {
                                foreach (var device in devices)
                                {
                                    try
                                    {
                                        if (device.Value.Platform.Equals("OpenCL", StringComparison.OrdinalIgnoreCase)) { listDeviceNames.Add(device.Key); }
                                    }
                                    catch { }
                                }
                            }
                            catch { }
                        });

                        decimal thisDeviceHashrate = 0;

                        listDeviceNames.ForEach(deviceName =>
                        {
                            try
                            {
                                apiObj.Algorithms.ForEach(algorithms =>
                                {
                                    foreach (var algorithm in algorithms)
                                    {
                                        try
                                        {
                                            if (algorithm.Key.Equals(deviceName, StringComparison.OrdinalIgnoreCase)) { thisDeviceHashrate += decimal.Parse(algorithm.Value.Hashrate, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat); }
                                        }
                                        catch { }
                                    }
                                });
                            }
                            catch { }
                        });

                        hashrates.TryAdd("opencl", thisDeviceHashrate);
                    }

                    if (backend.BackendName.Equals("cuda", StringComparison.OrdinalIgnoreCase))
                    {
                        List<string> listDeviceNames = new();

                        apiObj.Devices.ForEach(devices =>
                        {
                            try
                            {
                                foreach (var device in devices)
                                {
                                    try
                                    {
                                        if (device.Value.Platform.Equals("CUDA", StringComparison.OrdinalIgnoreCase)) { listDeviceNames.Add(device.Key); }
                                    }
                                    catch { }
                                }
                            }
                            catch { }
                        });

                        decimal thisDeviceHashrate = 0;

                        listDeviceNames.ForEach(deviceName =>
                        {
                            try
                            {
                                apiObj.Algorithms.ForEach(algorithms =>
                                {
                                    foreach (var algorithm in algorithms)
                                    {
                                        try
                                        {
                                            if (algorithm.Key.Equals(deviceName, StringComparison.OrdinalIgnoreCase)) { thisDeviceHashrate += decimal.Parse(algorithm.Value.Hashrate, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat); }
                                        }
                                        catch { }
                                    }
                                });
                            }
                            catch { }
                        });

                        hashrates.TryAdd("cuda", thisDeviceHashrate);
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

            WindowTitle = "Nanominer - " + miningCoin.Algorithm + " - " + string.Join(", ", Backends.Select(x => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(x.BackendName.ToLowerInvariant())));

            string Algorithm = miningCoin.Algorithm.ToString().ToLowerInvariant();

            StringBuilder conf = new();
            conf.AppendLine("; nanominer configuration file by True Mining");
            conf.AppendLine("; This config file is generated automatically by True Mining Desktop");
            conf.AppendLine("; This file can be modified if you want, but is recomended to set configurations in True Mining Desktop software");
            conf.AppendLine();
            conf.AppendLine();
            conf.AppendLine("watchdog = false");
            conf.AppendLine();
            conf.AppendLine("checkForUpdates = false");
            conf.AppendLine("autoUpdate = false");
            conf.AppendLine();
            conf.AppendLine("webPort = " + APIport);
            conf.AppendLine("mport = 0");



            conf.AppendLine(";================================================================================================================");
            conf.AppendLine();
            conf.AppendLine("[" + miningCoin.Algorithm.ToLowerInvariant() + "]");
            conf.AppendLine("; Wallet from True Mining Platform");
            conf.AppendLine("wallet = " + miningCoin.WalletTm );
            conf.AppendLine();
            conf.AppendLine("; rigName is coin prefix + user payment wallet. ex: doge_D6sdTZ8F1DiPtDFfKyJnDiMCkb9wsHsFry");
            conf.AppendLine("rigName = " + User.Settings.User.PayCoin.CoinTicker.ToLowerInvariant() + '_' + User.Settings.User.Payment_Wallet);
            conf.AppendLine();
            conf.AppendLine("; e-mail of True Mining Platform. Just for notification service");
            conf.AppendLine("email = " + miningCoin.Email);
            conf.AppendLine();
            conf.AppendLine("; miningCoin from Pool");
            conf.AppendLine("coin = " + miningCoin.CoinTicker);
            conf.AppendLine();

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

            conf.AppendLine("useSSL = true");
            conf.AppendLine();
            conf.AppendLine("sortPools = true");
            conf.AppendLine();

            foreach (string host in miningCoin.Hosts)
            {
                conf.AppendLine("pool" + (miningCoin.Hosts.IndexOf(host) + 1).ToString() + " = " + host + ":" + miningCoin.StratumPort);
            }

            System.IO.File.WriteAllText(@"Miners\Nanominer\config-" + AlgoBackendsString + ".ini", conf.ToString());

            StringBuilder cmdStart = new();
            cmdStart.AppendLine("cd /d \"%~dp0\"");
            cmdStart.AppendLine("nanominer.exe config-" + AlgoBackendsString + ".ini");
            cmdStart.AppendLine("pause");

            System.IO.File.WriteAllText(@"Miners\Nanominer\start-" + AlgoBackendsString + ".cmd", cmdStart.ToString());
        }
    }
}