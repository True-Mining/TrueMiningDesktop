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
        public static List<Nanominer.Nanominer> NanominerMiners = new();
        public static List<TRex.TRex> TRexMiners = new();

        public static void StartMiner(bool force = false)
        {
            if (!IsMining && !IsTryingStartMining || force)
            {
                IsTryingStartMining = true;

                while (IsStoppingMining && !force) { System.Threading.Thread.Sleep(100); }
                IsTryingStartMining = true;

                if (String.IsNullOrEmpty(User.Settings.User.Payment_Coin) || User.Settings.User.PayCoin == null || User.Settings.User.PayCoin.CoinName == null)
                {
                    IsTryingStartMining = false;
                    if (Application.Current.MainWindow.IsVisible) { MessageBox.Show("Select Payment Coin first"); }
                    IsMining = false;
                    IsTryingStartMining = false;
                    return;
                }

                if (!Tools.WalletAddressIsValid(User.Settings.User.Payment_Wallet))
                {
                    Miner.IsTryingStartMining = false;
                    if (Application.Current.MainWindow.IsVisible) { MessageBox.Show("Something wrong. Check your wallet address and selected coin."); }
                    IsMining = false;
                    IsTryingStartMining = false;
                    return;
                }

                Server.SoftwareParameters.ServerConfig.MiningCoins.ForEach(miningCoin =>
                {
                    if (Device.DevicesList.Any(device => device.MiningAlgo.Equals(miningCoin.Algorithm, StringComparison.OrdinalIgnoreCase) && device.IsSelected && (!device.BackendName.Equals("cuda", StringComparison.OrdinalIgnoreCase) || device.MiningAlgo.Equals("RandomX", StringComparison.OrdinalIgnoreCase))))
                    {
                        XMRigMiners.Add(new XMRig.XMRig(Device.DevicesList.Where(device => device.MiningAlgo.Equals(miningCoin.Algorithm, StringComparison.OrdinalIgnoreCase) && device.IsSelected && (!device.BackendName.Equals("cuda", StringComparison.OrdinalIgnoreCase) || device.MiningAlgo.Equals("RandomX", StringComparison.OrdinalIgnoreCase))).ToList()));
                    }
                    if (Device.cuda.MiningAlgo.Equals(miningCoin.Algorithm, StringComparison.OrdinalIgnoreCase) && Device.cuda.IsSelected && !Device.cuda.MiningAlgo.Equals("RandomX", StringComparison.OrdinalIgnoreCase))
                    {
                        TRexMiners.Add(new TRex.TRex(Device.DevicesList.Where(device => device.MiningAlgo.Equals(miningCoin.Algorithm, StringComparison.OrdinalIgnoreCase) && device.IsSelected && (device.BackendName.Equals("cuda", StringComparison.OrdinalIgnoreCase) && !device.MiningAlgo.Equals("RandomX", StringComparison.OrdinalIgnoreCase))).ToList()));
                    }
                }); // joga para listas todos os dispositivos separados por miningCoin. Possível bug: mais moedas com o mesmo algoritmo vão gerar mais moedas por dispositivo

                if (Device.DevicesList.Any(device => Server.SoftwareParameters.ServerConfig.MiningCoins.Any(miningCoin => device.MiningAlgo.Equals(miningCoin.Algorithm, StringComparison.OrdinalIgnoreCase)) && device.IsSelected))
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        //     IsTryingStartMining = true;
                        Tools.CheckerPopup = new CheckerPopup("all");
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

                                startMinersTask.Add(new Task<Action>(() => { TRexMiners.ForEach(miner => miner.Start()); return null; }));//inicia cada um dos mineradores da lista

                                startMinersTask.Add(new Task<Action>(() => { NanominerMiners.ForEach(miner => miner.Start()); return null; }));//inicia cada um dos mineradores da lista

                                foreach (Task task in startMinersTask)
                                {
                                    task.Start();
                                }

                                Task.WaitAll(startMinersTask.ToArray());

                                ShowHideCLI();
                            }
                            catch (Exception e) { MessageBox.Show(e.Message); isTryingStartMining = false; }
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

        public static void StopMiner(bool force = false)
        {
            IsStoppingMining = true;

            List<Task<Action>> stopMinersTask = new();

            stopMinersTask.Add(new Task<Action>(() =>
            {
                while (XMRigMiners.Any(miner => miner.IsTryingStartMining) && !force) { System.Threading.Thread.Sleep(100); }

                XMRigMiners.ForEach(miner => { try { miner.Stop(); } catch { } });

                XMRigMiners.Clear();

                return null;
            })); //para cada um dos mineradores da lista

            stopMinersTask.Add(new Task<Action>(() =>
            {
                while (TRexMiners.Any(miner => miner.IsTryingStartMining) && !force) { System.Threading.Thread.Sleep(100); }

                TRexMiners.ForEach(miner => { try { miner.Stop(); } catch { } });

                TRexMiners.Clear();

                return null;
            })); //para cada um dos mineradores da lista

            stopMinersTask.Add(new Task<Action>(() =>
            {
                while (NanominerMiners.Any(miner => miner.IsTryingStartMining) && !force) { System.Threading.Thread.Sleep(100); }

                NanominerMiners.ForEach(miner => { try { miner.Stop(); } catch { } });

                NanominerMiners.Clear();

                return null;
            })); //para cada um dos mineradores da lista

            foreach (Task task in stopMinersTask)
            {
                task.Start();
            }

            if (IsMining || force)
            {
                isTryingStartMining = false;
                isMining = false;

                IsStoppingMining = true;
            }

         //   Task.WaitAll(stopMinersTask.ToArray());
        }

        public static void ShowHideCLI()
        {
            bool showCLI = User.Settings.User.ShowCLI;
            bool MainWindowFocused = Tools.ApplicationIsActivated();

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

            NanominerMiners.ForEach(miner =>
            {
                try
                {
                    try
                    {
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            DateTime initializingTask = DateTime.UtcNow;
                            while (Tools.FindWindow(null, miner.WindowTitle).ToInt32() == 0 && initializingTask >= DateTime.UtcNow.AddSeconds(-30)) { Thread.Sleep(500); }
                            //    Thread.Sleep(1000);
                        });
                    }
                    catch { }

                    IntPtr windowIdentifier = Tools.FindWindow(null, miner.WindowTitle);
                    if (showCLI)
                    {
                        if (Application.Current.MainWindow.IsVisible)
                        {
                            NanominerMiners.ForEach(miner => miner.Show());
                            Tools.ShowWindow(windowIdentifier, 1);
                            Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                Application.Current.MainWindow.Focus();
                            });
                        }
                        else
                        {
                            NanominerMiners.ForEach(miner => miner.Show());
                            Tools.ShowWindow(windowIdentifier, 2);
                        }
                    }
                    else
                    {
                        NanominerMiners.ForEach(miner => miner.Hide());
                        Tools.ShowWindow(windowIdentifier, 0);
                    }
                }
                catch { }
            });
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

                NanominerMiners.ForEach(miner =>
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
                        if (Device.cpu.IsSelected) { Device.cpu.IsMining = true; Pages.SettingsCPU.AllContent.IsEnabled = false; Pages.SettingsCPU.LockWarning.Visibility = Visibility.Visible; }
                        if (Device.opencl.IsSelected) { Device.opencl.IsMining = true; Pages.SettingsOPENCL.AllContent.IsEnabled = false; Pages.SettingsOPENCL.LockWarning.Visibility = Visibility.Visible; }
                        if (Device.cuda.IsSelected) { Device.cuda.IsMining = true; Pages.SettingsCUDA.AllContent.IsEnabled = false; Pages.SettingsCUDA.LockWarning.Visibility = Visibility.Visible; }

                        if (isMining && !isStoppingMining && !isTryingStartMining)
                        {
                            StartedSince = DateTime.UtcNow;

                            Janelas.Pages.Home.GridUserWalletCoin.IsEnabled = false;

                            Pages.Home.StartStopButton_text.Content = "Stop Mining";
                            Pages.Home.StartStopButton_icon.Kind = MaterialDesignThemes.Wpf.PackIconKind.StopCircleOutline;

                            Pages.Home.StartStopButton.Background = Brushes.DarkOrange;
                            Pages.Home.StartStopButton.BorderBrush = Brushes.DarkOrange;

                            if (Device.cpu.IsSelected)
                            {
                                Device.cpu.OverviewDeviceSimplified.ovIcon.Foreground = Brushes.ForestGreen;
                            }
                            if (Device.cuda.IsSelected)
                            {
                                Device.cuda.OverviewDeviceSimplified.ovIcon.Foreground = Brushes.ForestGreen;
                            }
                            if (Device.opencl.IsSelected)
                            {
                                Device.opencl.OverviewDeviceSimplified.ovIcon.Foreground = Brushes.ForestGreen;
                            }
                        }
                        else if (!isMining && !isTryingStartMining && !isStoppingMining)
                        {
                            StartedSince = holdTime.AddTicks(-holdTime.Ticks);

                            Device.cpu.IsMining = false;
                            Device.opencl.IsMining = false;
                            Device.cuda.IsMining = false;

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

                            if (Device.cpu.IsSelected)
                            {
                                Device.cpu.OverviewDeviceSimplified.ovIcon.Foreground = Brushes.Black;
                            }
                            if (Device.cuda.IsSelected)
                            {
                                Device.cuda.OverviewDeviceSimplified.ovIcon.Foreground = Brushes.Black;
                            }
                            if (Device.opencl.IsSelected)
                            {
                                Device.opencl.OverviewDeviceSimplified.ovIcon.Foreground = Brushes.Black;
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

                        if (Device.cpu.IsSelected)
                        {
                            Device.cpu.OverviewDeviceSimplified.ovIcon.Foreground = Brushes.ForestGreen;
                        }
                        if (Device.cuda.IsSelected)
                        {
                            Device.cuda.OverviewDeviceSimplified.ovIcon.Foreground = Brushes.ForestGreen;
                        }
                        if (Device.opencl.IsSelected)
                        {
                            Device.opencl.OverviewDeviceSimplified.ovIcon.Foreground = Brushes.ForestGreen;
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
            if (XMRigMiners.Any(miner => miner.IsStoppingMining) || TRexMiners.Any(miner => miner.IsStoppingMining) || NanominerMiners.Any(miner => miner.IsStoppingMining))
            {
                Miner.IsStoppingMining = true;
            }
            else
            {
                Miner.IsStoppingMining = false;
            }
            if (XMRigMiners.Any(miner => miner.IsTryingStartMining) && !XMRigMiners.Any(miner => miner.IsMining) || TRexMiners.Any(miner => miner.IsTryingStartMining) && !TRexMiners.Any(miner => miner.IsMining) || NanominerMiners.Any(miner => miner.IsTryingStartMining) && !NanominerMiners.Any(miner => miner.IsMining))
            {
                Miner.IsTryingStartMining = true;
            }
            else
            {
                Miner.IsTryingStartMining = false;
            }
            if ((!XMRigMiners.Any(miner => miner.IsTryingStartMining) && XMRigMiners.Any(miner => miner.IsMining)) || (!TRexMiners.Any(miner => miner.IsTryingStartMining) && TRexMiners.Any(miner => miner.IsMining)) || (!NanominerMiners.Any(miner => miner.IsTryingStartMining) && NanominerMiners.Any(miner => miner.IsMining)))
            {
                Miner.IsMining = true;
            }
            else
            {
                Miner.IsMining = false;
            }
        }
    }
}