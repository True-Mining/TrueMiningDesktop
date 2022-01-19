using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using TrueMiningDesktop.Janelas;

namespace TrueMiningDesktop.Core
{
    public static class Miner
    {
        private static readonly DateTime holdTime = DateTime.UtcNow;
        public static DateTime StartedSince = holdTime.AddTicks(-holdTime.Ticks);

        public static List<XMRig.XMRig> XMRigMiners = new();
        public static List<TRex.TRex> TRexMiners = new();
        public static List<TeamRedMiner.TeamRedMiner> TeamRedMinerMiners = new();

        public static void StartMiners(bool force = false)
        {
            if (!IsMining && !IsTryingStartMining || force)
            {
                while (IsStoppingMining && !force) { System.Threading.Thread.Sleep(100); }

                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    IsTryingStartMining = true;
                });

                if (String.IsNullOrEmpty(User.Settings.User.Payment_Coin) || User.Settings.User.PayCoin == null || User.Settings.User.PayCoin.CoinName == null)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        if (Application.Current.MainWindow.WindowState != WindowState.Minimized) { MessageBox.Show("Select Payment Coin first"); }

                        IsTryingStartMining = false;
                        IsMining = false;
                    });
                    return;
                }

                if (!Tools.WalletAddressIsValid(User.Settings.User.Payment_Wallet))
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        if (Application.Current.MainWindow.WindowState != WindowState.Minimized) { MessageBox.Show("Something wrong. Check your wallet address and selected coin."); }

                        IsTryingStartMining = false;
                        IsMining = false;
                    });
                    return;
                }

                Server.SoftwareParameters.ServerConfig.MiningCoins.ForEach(miningCoin =>
                {
                    if (Device.DevicesList.Any(device => device.MiningAlgo.Equals(miningCoin.Algorithm, StringComparison.OrdinalIgnoreCase) && device.IsSelected && (!device.BackendName.Equals("cuda", StringComparison.OrdinalIgnoreCase) || device.MiningAlgo.Equals("RandomX", StringComparison.OrdinalIgnoreCase)) && (!device.BackendName.Equals("opencl", StringComparison.OrdinalIgnoreCase) || device.MiningAlgo.Equals("RandomX", StringComparison.OrdinalIgnoreCase))))
                    {
                        XMRigMiners.Add(new XMRig.XMRig(Device.DevicesList.Where(device => device.MiningAlgo.Equals(miningCoin.Algorithm, StringComparison.OrdinalIgnoreCase) && device.IsSelected && (!device.BackendName.Equals("cuda", StringComparison.OrdinalIgnoreCase) || device.MiningAlgo.Equals("RandomX", StringComparison.OrdinalIgnoreCase)) && (!device.BackendName.Equals("opencl", StringComparison.OrdinalIgnoreCase) || device.MiningAlgo.Equals("RandomX", StringComparison.OrdinalIgnoreCase))).ToList()));
                    }
                    if (Device.Cuda.MiningAlgo.Equals(miningCoin.Algorithm, StringComparison.OrdinalIgnoreCase) && Device.Cuda.IsSelected && !Device.Cuda.MiningAlgo.Equals("RandomX", StringComparison.OrdinalIgnoreCase))
                    {
                        TRexMiners.Add(new TRex.TRex(Device.DevicesList.Where(device => device.MiningAlgo.Equals(miningCoin.Algorithm, StringComparison.OrdinalIgnoreCase) && device.IsSelected && (device.BackendName.Equals("cuda", StringComparison.OrdinalIgnoreCase) && !device.MiningAlgo.Equals("RandomX", StringComparison.OrdinalIgnoreCase))).ToList()));
                    }
                    if (Device.Opencl.MiningAlgo.Equals(miningCoin.Algorithm, StringComparison.OrdinalIgnoreCase) && Device.Opencl.IsSelected && !Device.Opencl.MiningAlgo.Equals("RandomX", StringComparison.OrdinalIgnoreCase))
                    {
                        TeamRedMinerMiners.Add(new TeamRedMiner.TeamRedMiner(Device.DevicesList.Where(device => device.MiningAlgo.Equals(miningCoin.Algorithm, StringComparison.OrdinalIgnoreCase) && device.IsSelected && (device.BackendName.Equals("opencl", StringComparison.OrdinalIgnoreCase) && !device.MiningAlgo.Equals("RandomX", StringComparison.OrdinalIgnoreCase))).ToList()));
                    }
                }); // joga para listas todos os dispositivos separados por miningCoin. Possível bug: mais moedas com o mesmo algoritmo vão gerar mais moedas por dispositivo

                if (Device.DevicesList.Any(device => Server.SoftwareParameters.ServerConfig.MiningCoins.Any(miningCoin => device.MiningAlgo.Equals(miningCoin.Algorithm, StringComparison.OrdinalIgnoreCase)) && device.IsSelected))
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Tools.CheckerPopup = new CheckerPopup(CheckerPopup.ToCheck.SelectedBackendMiners);
                        Tools.CheckerPopup.ShowDialog();
                    });
                    if (!EmergencyExit || force)
                    {
                        new System.Threading.Tasks.Task(() =>
                        {
                            try
                            {
                                List<Task<Action>> startMinersTask = new();

                                startMinersTask.Add(new Task<Action>(() => { XMRigMiners.ForEach(miner => miner.Start()); return null; })); //inicia cada um dos mineradores da lista

                                startMinersTask.Add(new Task<Action>(() => { TRexMiners.ForEach(miner => miner.Start()); return null; })); //inicia cada um dos mineradores da lista

                                startMinersTask.Add(new Task<Action>(() => { TeamRedMinerMiners.ForEach(miner => miner.Start()); return null; })); //inicia cada um dos mineradores da lista

                                Parallel.ForEach(startMinersTask, task =>
                                {
                                    task.Start();
                                });

                                Task.WaitAll(startMinersTask.ToArray(), 15000);

                                ShowHideCLI();
                            }
                            catch { Application.Current.Dispatcher.Invoke((Action)delegate { IsTryingStartMining = false; }); }
                        })
                        .Start();
                    }
                }
                else
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        MessageBox.Show("select at least one device");
                        IsTryingStartMining = false;
                        IsMining = false;
                    });
                }
            }
        }

        public static void StopMiners(bool force = false)
        {
            IsStoppingMining = true;

            List<Task<Action>> stopMinersTask = new();

            DateTime dateTimeStopInit = new DateTime(DateTime.UtcNow.Ticks).AddSeconds(30);

            stopMinersTask.Add(new Task<Action>(() =>
            {
                while (DateTime.UtcNow < dateTimeStopInit && XMRigMiners.Any(miner => miner.IsTryingStartMining) && !force) { System.Threading.Thread.Sleep(100); }

                XMRigMiners.ForEach(miner => { try { miner.Stop(); } catch { } });

                XMRigMiners.Clear();

                return null;
            })); //para cada um dos mineradores da lista

            stopMinersTask.Add(new Task<Action>(() =>
            {
                while (DateTime.UtcNow < dateTimeStopInit && TRexMiners.Any(miner => miner.IsTryingStartMining) && !force) { System.Threading.Thread.Sleep(100); }

                TRexMiners.ForEach(miner => { try { miner.Stop(); } catch { } });

                TRexMiners.Clear();

                return null;
            })); //para cada um dos mineradores da lista

            stopMinersTask.Add(new Task<Action>(() =>
            {
                while (DateTime.UtcNow < dateTimeStopInit && TeamRedMinerMiners.Any(miner => miner.IsTryingStartMining) && !force) { System.Threading.Thread.Sleep(100); }

                TeamRedMinerMiners.ForEach(miner => { try { miner.Stop(); } catch { } });

                TeamRedMinerMiners.Clear();

                return null;
            })); //para cada um dos mineradores da lista

            Parallel.ForEach(stopMinersTask, task =>
            {
                task.Start();
            });

            if (IsMining || force)
            {
                isTryingStartMining = false;
                isMining = false;

                IsStoppingMining = true;
            }

            Task.WaitAll(stopMinersTask.ToArray());
        }

        public static void ShowHideCLI()
        {
            bool showCLI = User.Settings.User.ShowCLI;
            bool MainWindowFocused = Tools.ApplicationIsActivated();

            List<Task<Action>> showHideMinersTask = new();

            showHideMinersTask.Add(new Task<Action>(() =>
            {
                XMRigMiners.ForEach(miner =>
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
                                    continueWaiting = Tools.FindWindow(null, miner.WindowTitle).ToInt32() == 0 && initializingTask >= DateTime.UtcNow.AddSeconds(-30);
                                });
                            }
                            catch { }
                            if (continueWaiting)
                            {
                                Thread.Sleep(500);
                            }
                            else
                            {
                                break;
                            }
                        }

                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            IntPtr windowIdentifier = Tools.FindWindow(null, miner.WindowTitle);
                            if (showCLI)
                            {
                                if (Application.Current.MainWindow.IsVisible && MainWindowFocused)
                                {
                                    XMRigMiners.ForEach(miner => miner.Show());
                                    Tools.ShowWindow(windowIdentifier, 1);
                                    Application.Current.MainWindow.Focus();
                                }
                                else
                                {
                                    TRexMiners.ForEach(miner => miner.TRexProcessStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized);
                                    Tools.ShowWindow(windowIdentifier, 2);
                                }
                            }
                            else
                            {
                                XMRigMiners.ForEach(miner => miner.Hide());
                                Tools.ShowWindow(windowIdentifier, 0);
                            }
                        });
                    }
                    catch { }
                });
                return null;
            }));

            showHideMinersTask.Add(new Task<Action>(() =>
            {
                TRexMiners.ForEach(miner =>
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
                                    continueWaiting = Tools.FindWindow(null, miner.TRexProcess.MainWindowTitle).ToInt32() == 0 && initializingTask >= DateTime.UtcNow.AddSeconds(-30);
                                });
                            }
                            catch { }
                            if (continueWaiting)
                            {
                                Thread.Sleep(500);
                            }
                            else
                            {
                                break;
                            }
                        }

                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            IntPtr windowIdentifier = Tools.FindWindow(null, miner.TRexProcess.MainWindowTitle);
                            if (showCLI)
                            {
                                if (Application.Current.MainWindow.IsVisible && MainWindowFocused)
                                {
                                    TRexMiners.ForEach(miner => miner.Show());
                                    Tools.ShowWindow(windowIdentifier, 1);
                                    Application.Current.MainWindow.Focus();
                                }
                                else
                                {
                                    TRexMiners.ForEach(miner => miner.TRexProcessStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized);
                                    Tools.ShowWindow(windowIdentifier, 2);
                                }
                            }
                            else
                            {
                                TRexMiners.ForEach(miner => miner.Hide());
                                Tools.ShowWindow(windowIdentifier, 0);
                            }
                        });
                    }
                    catch { }
                });
                return null;
            }));

            showHideMinersTask.Add(new Task<Action>(() =>
            {
                TeamRedMinerMiners.ForEach(miner =>
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
                                    continueWaiting = Tools.FindWindow(null, miner.TeamRedMinerProcess.MainWindowTitle).ToInt32() == 0 && initializingTask >= DateTime.UtcNow.AddSeconds(-30);
                                });
                            }
                            catch { }
                            if (continueWaiting)
                            {
                                Thread.Sleep(500);
                            }
                            else
                            {
                                break;
                            }
                        }

                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            IntPtr windowIdentifier = Tools.FindWindow(null, miner.TeamRedMinerProcess.MainWindowTitle);
                            if (showCLI)
                            {
                                if (Application.Current.MainWindow.IsVisible && MainWindowFocused)
                                {
                                    TRexMiners.ForEach(miner => miner.Show());
                                    Tools.ShowWindow(windowIdentifier, 1);
                                    Application.Current.MainWindow.Focus();
                                }
                                else
                                {
                                    TRexMiners.ForEach(miner => miner.TRexProcessStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized);
                                    Tools.ShowWindow(windowIdentifier, 2);
                                }
                            }
                            else
                            {
                                TRexMiners.ForEach(miner => miner.Hide());
                                Tools.ShowWindow(windowIdentifier, 0);
                            }
                        });
                    }
                    catch { }
                });
                return null;
            }));

            Parallel.ForEach(showHideMinersTask, task =>
            {
                task.Start();
            });

            Task.WaitAll(showHideMinersTask.ToArray(), 5000);
        }

        public static decimal GetHashrate(string alias = null)
        {
            try
            {
                Dictionary<string, decimal> hashrates = new();

                XMRigMiners.ForEach(miner =>
                {
                    try
                    {
                        Dictionary<string, decimal> temp_hashrates = miner.GetHasrates();

                        if (temp_hashrates != null)
                        {
                            foreach (KeyValuePair<string, decimal> hashrate in temp_hashrates)
                            {
                                if (hashrates.ContainsKey(hashrate.Key.ToLowerInvariant()))
                                {
                                    hashrates[hashrate.Key.ToLowerInvariant()] += hashrate.Value;
                                }
                                else
                                {
                                    hashrates.Add(hashrate.Key.ToLowerInvariant(), hashrate.Value);
                                }
                            }
                        }
                    }
                    catch { }
                });

                TRexMiners.ForEach(miner =>
                {
                    try
                    {
                        Dictionary<string, decimal> temp_hashrates = miner.GetHasrates();

                        if (temp_hashrates != null)
                        {
                            foreach (KeyValuePair<string, decimal> hashrate in temp_hashrates)
                            {
                                if (hashrates.ContainsKey(hashrate.Key.ToLowerInvariant()))
                                {
                                    hashrates[hashrate.Key.ToLowerInvariant()] += hashrate.Value;
                                }
                                else
                                {
                                    hashrates.Add(hashrate.Key.ToLowerInvariant(), hashrate.Value);
                                }
                            }
                        }
                    }
                    catch { }
                });

                TeamRedMinerMiners.ForEach(miner =>
                {
                    try
                    {
                        Dictionary<string, decimal> temp_hashrates = miner.GetHasrates();

                        if (temp_hashrates != null)
                        {
                            foreach (KeyValuePair<string, decimal> hashrate in temp_hashrates)
                            {
                                if (hashrates.ContainsKey(hashrate.Key.ToLowerInvariant()))
                                {
                                    hashrates[hashrate.Key.ToLowerInvariant()] += hashrate.Value;
                                }
                                else
                                {
                                    hashrates.Add(hashrate.Key.ToLowerInvariant(), hashrate.Value);
                                }
                            }
                        }
                    }
                    catch { }
                });

                if (hashrates == null || hashrates.Count == 0)
                {
                    return -1;
                }

                Device.DevicesList.ForEach(device =>
                {
                    if (hashrates.ContainsKey(device.BackendName.ToLowerInvariant()))
                    {
                        device.HashrateValue_raw = hashrates[device.BackendName.ToLowerInvariant()];
                    }
                });

                if (alias != null && hashrates.ContainsKey(alias.ToLowerInvariant()))
                {
                    return hashrates[alias.ToLowerInvariant()];
                }
            }
            catch
            {
                return -1;
            }
            return -1;
        }

        public static bool EmergencyExit;

        private static bool isMining;
        private static bool isTryingStartMining;
        private static bool isStoppingMining;

        public static bool IsMining
        {
            get
            {
                //   VerifyGeneralMiningState();

                return isMining;
            }
            set
            {
                try
                {
                    isMining = value;
                    if (value) isTryingStartMining = false;

                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        if (Device.Cpu.IsSelected) { Device.Cpu.IsMining = true; Pages.SettingsCPU.AllContent.IsEnabled = false; Pages.SettingsCPU.LockWarning.Visibility = Visibility.Visible; }
                        if (Device.Opencl.IsSelected) { Device.Opencl.IsMining = true; Pages.SettingsOPENCL.AllContent.IsEnabled = false; Pages.SettingsOPENCL.LockWarning.Visibility = Visibility.Visible; }
                        if (Device.Cuda.IsSelected) { Device.Cuda.IsMining = true; Pages.SettingsCUDA.AllContent.IsEnabled = false; Pages.SettingsCUDA.LockWarning.Visibility = Visibility.Visible; }

                        if (isMining && !isStoppingMining && !isTryingStartMining)
                        {
                            StartedSince = DateTime.UtcNow;

                            Janelas.Pages.Home.GridUserWalletCoin.IsEnabled = false;

                            Pages.Home.StartStopButton_text.Content = "Stop Mining";
                            Pages.Home.StartStopButton_icon.Kind = MaterialDesignThemes.Wpf.PackIconKind.StopCircleOutline;

                            Pages.Home.StartStopButton.Background = Brushes.DarkOrange;
                            Pages.Home.StartStopButton.BorderBrush = Brushes.DarkOrange;

                            if (Device.Cpu.IsSelected)
                            {
                                Device.Cpu.OverviewDeviceSimplified.ovIcon.Foreground = Brushes.ForestGreen;
                            }
                            if (Device.Cuda.IsSelected)
                            {
                                Device.Cuda.OverviewDeviceSimplified.ovIcon.Foreground = Brushes.ForestGreen;
                            }
                            if (Device.Opencl.IsSelected)
                            {
                                Device.Opencl.OverviewDeviceSimplified.ovIcon.Foreground = Brushes.ForestGreen;
                            }
                        }
                        else if (!isMining && !isTryingStartMining && !isStoppingMining)
                        {
                            StartedSince = holdTime.AddTicks(-holdTime.Ticks);

                            Device.Cpu.IsMining = false;
                            Device.Opencl.IsMining = false;
                            Device.Cuda.IsMining = false;

                            Pages.SettingsCPU.AllContent.IsEnabled = true;
                            Pages.SettingsCUDA.AllContent.IsEnabled = true;
                            Pages.SettingsOPENCL.AllContent.IsEnabled = true;
                            Pages.SettingsCPU.LockWarning.Visibility = Visibility.Hidden;
                            Pages.SettingsCUDA.LockWarning.Visibility = Visibility.Hidden;
                            Pages.SettingsOPENCL.LockWarning.Visibility = Visibility.Hidden;

                            Janelas.Pages.Home.GridUserWalletCoin.IsEnabled = true;

                            Pages.Home.StartStopButton_text.Content = "Start Mining";
                            Pages.Home.StartStopButton_icon.Kind = MaterialDesignThemes.Wpf.PackIconKind.PlayOutline;

                            Pages.Home.StartStopButton.Background = (Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#5C7AEA");
                            Pages.Home.StartStopButton.BorderBrush = (Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#5C7AEA");

                            if (Device.Cpu.IsSelected)
                            {
                                Device.Cpu.OverviewDeviceSimplified.ovIcon.Foreground = Brushes.Black;
                            }
                            if (Device.Cuda.IsSelected)
                            {
                                Device.Cuda.OverviewDeviceSimplified.ovIcon.Foreground = Brushes.Black;
                            }
                            if (Device.Opencl.IsSelected)
                            {
                                Device.Opencl.OverviewDeviceSimplified.ovIcon.Foreground = Brushes.Black;
                            }
                        }
                    });
                }
                catch { }
            }
        }

        public static bool IsTryingStartMining
        {
            get
            {
                //    VerifyGeneralMiningState();

                return isTryingStartMining;
            }
            set
            {
                isTryingStartMining = value;

                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    Pages.SettingsCPU.AllContent.IsEnabled = false;
                    Pages.SettingsCUDA.AllContent.IsEnabled = false;
                    Pages.SettingsOPENCL.AllContent.IsEnabled = false;
                    Pages.SettingsCPU.LockWarning.Visibility = Visibility.Visible;
                    Pages.SettingsCUDA.LockWarning.Visibility = Visibility.Visible;
                    Pages.SettingsOPENCL.LockWarning.Visibility = Visibility.Visible;

                    if (isTryingStartMining)
                    {
                        Janelas.Pages.Home.GridUserWalletCoin.IsEnabled = false;

                        Pages.Home.StartStopButton_text.Content = "Loading";
                        Pages.Home.StartStopButton_icon.Kind = MaterialDesignThemes.Wpf.PackIconKind.AutoFix;

                        Pages.Home.StartStopButton_icon.Width = 20;

                        Pages.Home.StartStopButton.Background = Brushes.ForestGreen;
                        Pages.Home.StartStopButton.BorderBrush = Brushes.ForestGreen;

                        if (Device.Cpu.IsSelected)
                        {
                            Device.Cpu.OverviewDeviceSimplified.ovIcon.Foreground = Brushes.ForestGreen;
                        }
                        if (Device.Cuda.IsSelected)
                        {
                            Device.Cuda.OverviewDeviceSimplified.ovIcon.Foreground = Brushes.ForestGreen;
                        }
                        if (Device.Opencl.IsSelected)
                        {
                            Device.Opencl.OverviewDeviceSimplified.ovIcon.Foreground = Brushes.ForestGreen;
                        }
                    }

                    if (IsMining) { IsMining = IsMining; }
                });
            }
        }

        public static bool IsStoppingMining
        {
            get
            {
                //   VerifyGeneralMiningState();

                return isStoppingMining;
            }
            set
            {
                isStoppingMining = value;

                if (isStoppingMining)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Pages.Home.StartStopButton_text.Content = "Stopping";
                        Pages.Home.StartStopButton_icon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Power;

                        Pages.Home.StartStopButton_icon.Width = 20;

                        Pages.Home.StartStopButton.Background = Brushes.OrangeRed;
                        Pages.Home.StartStopButton.BorderBrush = Brushes.OrangeRed;
                    });
                }

                IsMining = IsMining;
            }
        }

        public static void VerifyGeneralMiningState()
        {
            Miner.IsStoppingMining = XMRigMiners.Any(miner => miner.IsStoppingMining) || TRexMiners.Any(miner => miner.IsStoppingMining) || TeamRedMinerMiners.Any(miner => miner.IsStoppingMining);

            Miner.IsMining = XMRigMiners.Any(miner => !miner.IsTryingStartMining && miner.IsMining && !miner.IsStoppingMining) || TRexMiners.Any(miner => !miner.IsTryingStartMining && miner.IsMining && !miner.IsStoppingMining) || TeamRedMinerMiners.Any(miner => !miner.IsTryingStartMining && miner.IsMining && !miner.IsStoppingMining);

            Miner.IsTryingStartMining = !Miner.IsMining && (XMRigMiners.Any(miner => miner.IsTryingStartMining) || TRexMiners.Any(miner => miner.IsTryingStartMining) || TeamRedMinerMiners.Any(miner => miner.IsTryingStartMining));
        }
    }
}