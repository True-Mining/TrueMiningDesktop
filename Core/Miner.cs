using System;
using System.Windows;
using System.Windows.Media;
using TrueMiningDesktop.Janelas;

namespace TrueMiningDesktop.Core
{
    public static class Miner
    {
        private static readonly DateTime holdTime = DateTime.UtcNow;
        public static DateTime StartedSince = holdTime.AddTicks(-holdTime.Ticks);

        public static void StartMiner(bool force = false)
        {
            if (!IsMining && !IntentToMine || force)
            {
                IntentToMine = true;

                while (StoppingMining && !force) { System.Threading.Thread.Sleep(100); }
                IntentToMine = true;

                if (!Tools.WalletAddressIsValid(User.Settings.User.Payment_Wallet))
                {
                    Miner.IntentToMine = false;
                    if (Application.Current.MainWindow.IsVisible) { MessageBox.Show("Your wallet address is not correct. Check it."); }
                    return;
                }

                if ((Device.cpu.IsSelected || Device.opencl.IsSelected || Device.cuda.IsSelected) && (string.Equals(Device.cpu.MiningAlgo, "RandomX", StringComparison.OrdinalIgnoreCase) || string.Equals(Device.opencl.MiningAlgo, "RandomX", StringComparison.OrdinalIgnoreCase) || string.Equals(Device.cuda.MiningAlgo, "RandomX", StringComparison.OrdinalIgnoreCase)))
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        IntentToMine = true;
                        Tools.CheckerPopup = new CheckerPopup("all");
                        Tools.CheckerPopup.ShowDialog();
                    });
                    if (!EmergencyExit || force)
                    {
                        new System.Threading.Tasks.Task(() =>
                        {
                            try
                            {
                                IsMining = true;

                                XMRig.XMRig.CreateConfigFile();
                                XMRig.XMRig.Start();

                                intentToMine = false;
                            }
                            catch (Exception e) { MessageBox.Show(e.Message); intentToMine = false; }
                        })
                        .Start();
                    }
                }
                else
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        MessageBox.Show("select at least one device");
                        IntentToMine = false;
                        IsMining = false;
                    });
                }
            }
        }

        public static void StopMiner(bool force = false)
        {
            StoppingMining = true;

            System.Threading.Thread.Sleep(200);

            while (IntentToMine && !force) { System.Threading.Thread.Sleep(100); }

            if (IsMining)
            {

                intentToMine = false;
                isMining = false;

                StoppingMining = true;
            }

            new System.Threading.Tasks.Task(() =>
            {
                try { XMRig.XMRig.Stop(); } catch { }

                StoppingMining = false;
            })
            .Start();
        }

        public static void ShowHideCLI(string miner = "all")
        {
            bool showCLI = User.Settings.User.ShowCLI;

            if (string.Equals(miner, "XMRig", StringComparison.OrdinalIgnoreCase) || string.Equals(miner, "all", StringComparison.OrdinalIgnoreCase))
            {
                IntPtr windowIdentifier = Tools.FindWindow(null, "True Mining running XMRig");
                if (showCLI)
                {
                    XMRig.XMRig.Show();
                    Tools.ShowWindow(windowIdentifier, 1);
                }
                else
                {
                    XMRig.XMRig.Hide();
                    Tools.ShowWindow(windowIdentifier, 0);
                }
            }
        }

        public static decimal GetHashrate(string alias, string algo)
        {
            try
            {
                if (Device.DevicesList.Find(x => x.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase)).IsSelected)
                {
                    if (string.Equals(algo, "RandomX", StringComparison.OrdinalIgnoreCase))
                    {
                        return XMRig.XMRig.GetHasrate(alias);
                    }
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
        private static bool intentToMine;
        private static bool stoppingMining;

        public static bool IsMining
        {
            get
            {
                return isMining;
            }
            set
            {
                try
                {
                    isMining = value;
                    if (value) intentToMine = false;

                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        if (Device.cpu.IsSelected) { Device.cpu.IsMining = true; Pages.SettingsCPU.AllContent.IsEnabled = false; Pages.SettingsCPU.LockWarning.Visibility = Visibility.Visible; }
                        if (Device.opencl.IsSelected) { Device.opencl.IsMining = true; Pages.SettingsOPENCL.AllContent.IsEnabled = false; Pages.SettingsOPENCL.LockWarning.Visibility = Visibility.Visible; }
                        if (Device.cuda.IsSelected) { Device.cuda.IsMining = true; Pages.SettingsCUDA.AllContent.IsEnabled = false; Pages.SettingsCUDA.LockWarning.Visibility = Visibility.Visible; }

                        if (isMining && !stoppingMining && !intentToMine)
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
                        else if (!isMining && !intentToMine && !stoppingMining)
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

                            Pages.Home.StartStopButton.Background = Brushes.DodgerBlue;
                            Pages.Home.StartStopButton.BorderBrush = Brushes.DodgerBlue;

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
                catch (Exception e) { MessageBox.Show(e.Message); }
            }
        }

        public static bool IntentToMine
        {
            get
            {
                return intentToMine;
            }
            set
            {
                intentToMine = value;

                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    Pages.SettingsCPU.AllContent.IsEnabled = false;
                    Pages.SettingsCUDA.AllContent.IsEnabled = false;
                    Pages.SettingsOPENCL.AllContent.IsEnabled = false;
                    Pages.SettingsCPU.LockWarning.Visibility = Visibility.Visible;
                    Pages.SettingsCUDA.LockWarning.Visibility = Visibility.Visible;
                    Pages.SettingsOPENCL.LockWarning.Visibility = Visibility.Visible;

                    if (intentToMine)
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

        public static bool StoppingMining
        {
            get
            {
                return stoppingMining;
            }
            set
            {
                stoppingMining = value;

                if (stoppingMining)
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
    }
}