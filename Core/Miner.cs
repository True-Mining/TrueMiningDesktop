using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
                    if (Device.DevicesList.Any(device => device.MiningAlgo.Equals(miningCoin.Algorithm, StringComparison.OrdinalIgnoreCase) && device.IsSelected))
                    {
                        XMRigMiners.Add(new XMRig.XMRig(Device.DevicesList.Where(device => device.MiningAlgo.Equals(miningCoin.Algorithm, StringComparison.OrdinalIgnoreCase) && device.IsSelected).ToList()));
                    }
                }); // joga para uma List<XMRig.XMRig> todos os dispositivos separados por miningCoin. Possível bug: mais moedas com o mesmo algoritmo vão gerar mais moedas por dispositivo

                if ((Device.cpu.IsSelected || Device.opencl.IsSelected || Device.cuda.IsSelected) && (string.Equals(Device.cpu.MiningAlgo, "RandomX", StringComparison.OrdinalIgnoreCase) || string.Equals(Device.opencl.MiningAlgo, "RandomX", StringComparison.OrdinalIgnoreCase) || string.Equals(Device.cuda.MiningAlgo, "RandomX", StringComparison.OrdinalIgnoreCase)))
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
                                XMRigMiners.ForEach(miner => miner.Start()); //inicia cada um dos mineradores da lista

                                //          IsMining = true;
                                //          isTryingStartMining = false;

                                Application.Current.Dispatcher.Invoke((Action)delegate
                                {
                                    ShowHideCLI();
                                });
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

            System.Threading.Thread.Sleep(200);

            while (XMRigMiners.Any(miner => miner.IsTryingStartMining) && !force) { System.Threading.Thread.Sleep(100); }

            if (IsMining || force)
            {
                isTryingStartMining = false;
                isMining = false;

                IsStoppingMining = true;
            }

            new System.Threading.Tasks.Task(() =>
            {
                XMRigMiners.ForEach(miner => { try { miner.Stop(); } catch { } });

                XMRigMiners.Clear();
            })
            .Start();
        }

        public static void ShowHideCLI()
        {
            bool showCLI = User.Settings.User.ShowCLI;

            XMRigMiners.ForEach(miner =>
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
                            XMRigMiners.ForEach(miner => miner.Show());
                            Tools.ShowWindow(windowIdentifier, 1);
                            Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                Application.Current.MainWindow.Focus();
                            });
                        }
                        else
                        {
                            XMRigMiners.ForEach(miner => miner.Show());
                            Tools.ShowWindow(windowIdentifier, 2);
                        }
                    }
                    else
                    {
                        XMRigMiners.ForEach(miner => miner.Hide());
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

                if (hashrates == null || hashrates.Count == 0)
                {
                    return -1;
                }

                Device.DevicesList.ForEach(device =>
                {
                    if (hashrates.ContainsKey(device.BackendName.ToLowerInvariant()))
                    {
                        device.Hashrate = hashrates[device.BackendName.ToLowerInvariant()];
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
            if (XMRigMiners.Any(miner => miner.IsStoppingMining))
            {
                Miner.IsStoppingMining = true;
            }
            else
            {
                Miner.IsStoppingMining = false;
            }
            if (XMRigMiners.Any(miner => miner.IsTryingStartMining) && !XMRigMiners.Any(miner => miner.IsMining))
            {
                Miner.IsTryingStartMining = true;
            }
            else
            {
                Miner.IsTryingStartMining = false;
            }
            if (XMRigMiners.Any(miner => miner.IsMining))
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