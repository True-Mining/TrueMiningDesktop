using System;
using System.Windows;
using System.Windows.Media;
using True_Mining_v4.Janelas;

namespace True_Mining_v4.Core
{
    public static class Miner
    {
        public static void StartMiner()
        {
            if (!Tools.WalletAddressIsValid(User.Settings.User.Payment_Wallet))
            {
                MessageBox.Show("Your wallet address is not correct. Check it.");
                Miner.IsMining = false;
                return;
            }

            if (User.Settings.User.StartHide) { new Janelas.CheckerPopup("TrueMining"); } else { new Janelas.CheckerPopup("TrueMining").ShowDialog(); }

            if ((Device.cpu.IsSelected || Device.opencl.IsSelected || Device.cuda.IsSelected) && (String.Equals(Device.cpu.MiningAlgo, "RandomX", StringComparison.OrdinalIgnoreCase) || String.Equals(Device.opencl.MiningAlgo, "RandomX", StringComparison.OrdinalIgnoreCase) || String.Equals(Device.cuda.MiningAlgo, "RandomX", StringComparison.OrdinalIgnoreCase)))
            {
                IsMining = true;
                XMRig.XMRig.CreateConfigFile();
                XMRig.XMRig.Start();
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