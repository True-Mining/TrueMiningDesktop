using System;
using System.Windows;
using System.Windows.Media;
using True_Mining_Desktop.Janelas;

namespace True_Mining_Desktop.Core
{
    public static class Miner
    {
        public static void StartMiner()
        {
            if (!Tools.WalletAddressIsValid(User.Settings.User.Payment_Wallet))
            {
                Miner.IsMining = false;
                if (Application.Current.MainWindow.IsVisible) { MessageBox.Show("Your wallet address is not correct. Check it."); }
                return;
            }

            if ((Device.cpu.IsSelected || Device.opencl.IsSelected || Device.cuda.IsSelected) && (String.Equals(Device.cpu.MiningAlgo, "RandomX", StringComparison.OrdinalIgnoreCase) || String.Equals(Device.opencl.MiningAlgo, "RandomX", StringComparison.OrdinalIgnoreCase) || String.Equals(Device.cuda.MiningAlgo, "RandomX", StringComparison.OrdinalIgnoreCase)))
            {
                Tools.CheckerPopup = new Janelas.CheckerPopup("all");
                Tools.CheckerPopup.ShowDialog();

                if (!EmergencyExit)
                {
                    IsMining = true;
                    XMRig.XMRig.CreateConfigFile();
                    XMRig.XMRig.Start();
                }
            }
        }

        public static void StopMiner()
        {
            if ((User.Settings.Device.cpu.MiningSelected | User.Settings.Device.opencl.MiningSelected | User.Settings.Device.cuda.MiningSelected) && (String.Equals(User.Settings.Device.cpu.Algorithm, "RandomX", StringComparison.OrdinalIgnoreCase) | String.Equals(User.Settings.Device.opencl.Algorithm, "RandomX", StringComparison.OrdinalIgnoreCase) | String.Equals(User.Settings.Device.cuda.Algorithm, "RandomX", StringComparison.OrdinalIgnoreCase)))
            {
                IsMining = false;
                XMRig.XMRig.Stop();
            }
        }

        public static void ShowHideCLI(string miner = "all")
        {
            bool showCLI = User.Settings.User.ShowCLI;

            if (String.Equals(miner, "XMRig", StringComparison.OrdinalIgnoreCase) || String.Equals(miner, "all", StringComparison.OrdinalIgnoreCase))
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
            if (Core.Device.cpu.IsSelected)
            {
                if (String.Equals(algo, "RandomX", StringComparison.OrdinalIgnoreCase))
                {
                    return XMRig.XMRig.GetHasrate(alias);
                }
            }

            return -1;
        }

        public static bool EmergencyExit = false;

        private static bool isMining = false;

        public static bool IsMining
        {
            get
            {
                return isMining;
            }
            set
            {
                isMining = value;

                if (Device.cpu.IsSelected) { Device.cpu.IsMining = true; }
                if (Device.opencl.IsSelected) { Device.opencl.IsMining = true; }
                if (Device.cuda.IsSelected) { Device.cuda.IsMining = true; }

                Pages.SettingsCPU.AllContent.IsEnabled = false;
                Pages.SettingsCUDA.AllContent.IsEnabled = false;
                Pages.SettingsOPENCL.AllContent.IsEnabled = false;
                Pages.SettingsCPU.LockWarning.Visibility = Visibility.Visible;
                Pages.SettingsCUDA.LockWarning.Visibility = Visibility.Visible;
                Pages.SettingsOPENCL.LockWarning.Visibility = Visibility.Visible;

                if (isMining)
                {
                    Janelas.Pages.Home.GridUserWalletCoin.IsEnabled = false;

                    Pages.Home.StartStopButton.Content = "Stop Mining";

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
                else
                {
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

                    Pages.Home.StartStopButton.Content = "Start Mining";
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
            }
        }
    }
}