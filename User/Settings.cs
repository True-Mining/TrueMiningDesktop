using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using True_Mining_v4.Core;

namespace True_Mining_v4.User
{
    public static class Settings
    {
        public static DeviceSettings Device = new DeviceSettings();
        public static UserPreferences User = new UserPreferences();

        public static bool loadingSettings = true;

        public static void SettingsSaver()
        {
            //    System.Windows.MessageBox.Show("saving settings");
            File.WriteAllText("configsDevices.txt", JsonConvert.SerializeObject(Device, Formatting.Indented));
            File.WriteAllText("configsUser.txt", JsonConvert.SerializeObject(User, Formatting.Indented));
        }

        public static void SettingsRecover()
        {
            if (File.Exists("configsDevices.txt"))
            {
                Device = JsonConvert.DeserializeObject<DeviceSettings>(File.ReadAllText("configsDevices.txt"));
            }

            if (File.Exists("configsUser.txt"))
            {
                UserPreferences up = JsonConvert.DeserializeObject<UserPreferences>(File.ReadAllText("configsUser.txt"));
                User.AutostartMining = up.AutostartMining;
                User.AutostartSoftwareWithWindows = up.AutostartSoftwareWithWindows;
                User.AvoidWindowsSuspend = up.AvoidWindowsSuspend;
                User.ShowCLI = up.ShowCLI;
                User.StartHide = up.StartHide;
                User.Payment_Coin = up.Payment_Coin;
                User.Payment_Wallet = up.Payment_Wallet;
            }

            loadingSettings = false;
        }
    }

    public class DeviceSettings
    {
        public CPUSettings cpu = new CPUSettings();
        public NVIDIASettings cuda = new NVIDIASettings();
        public AMDSettings opencl = new AMDSettings();
    }

    public class CPUSettings
    {
        private bool miningSelected = true;
        public bool MiningSelected { get { return miningSelected; } set { miningSelected = value; if (!Settings.loadingSettings) { Settings.SettingsSaver(); } } }
        public bool Autoconfig { get; set; } = true;
        public String Algorithm { get; set; } = "RandomX";
        public List<string> AlgorithmsList { get; set; } = new List<string>();
        public int Priority { get; set; } = 1;
        public int MaxUsageHint { get; set; } = 100;
        public bool NoYield { get; set; } = true;
    }

    public class NVIDIASettings
    {
        private bool miningSelected = true;
        public bool MiningSelected { get { return miningSelected; } set { miningSelected = value; if (!Settings.loadingSettings) { Settings.SettingsSaver(); } } }
        public bool Autoconfig { get; set; } = true;
        public String Algorithm { get; set; } = "RandomX";
        public List<string> AlgorithmsList { get; set; } = new List<string>();
        public bool NVML { get; set; } = true;
    }

    public class AMDSettings
    {
        private bool miningSelected = true;
        public bool MiningSelected { get { return miningSelected; } set { miningSelected = value; if (!Settings.loadingSettings) { Settings.SettingsSaver(); } } }
        public bool Autoconfig { get; set; } = true;
        public String Algorithm { get; set; } = "RandomX";
        public List<string> AlgorithmsList { get; set; } = new List<string>();
        public bool Cache { get; set; } = true;
    }

    public class UserPreferences
    {
        private string payment_Wallet = null;

        public string Payment_Wallet
        {
            get { return payment_Wallet; }
            set
            {
                payment_Wallet = value;
                if (payment_Wallet != null)
                {
                    payment_Wallet.Replace(" ", "");
                    if (payment_Wallet.Length == 34)
                    {
                        // linkLabel3.Visible = false;
                    }
                    else
                    {
                        // linkLabel3.Visible = true;
                    }

                    if (payment_Wallet.StartsWith("R"))
                    { Payment_Coin = "RDCT"; }
                    if (payment_Wallet.StartsWith("D"))
                    { Payment_Coin = "DOGE"; }

                    if (!Settings.loadingSettings) { Settings.SettingsSaver(); }
                }
            }
        }

        public bool LICENSE_read = false;

        private String payment_Coin;

        public String Payment_Coin
        {
            get
            {
                return payment_Coin;
            }
            set
            {
                payment_Coin = value;
                Janelas.Pages.Home.ComboBox_PaymentCoin.SelectedIndex = PaymentCoinComboBox_SelectedIndex;
            }
        }

        public List<string> Payment_CoinsList { get; set; } = new List<string>();
        private bool showCLI = false;
        public bool ShowCLI { get { return showCLI; } set { showCLI = value; Miner.ShowHideCLI(); } }
        private bool autostartSoftwareWithWindows = true;
        public bool AutostartSoftwareWithWindows { get { return autostartSoftwareWithWindows; } set { autostartSoftwareWithWindows = value; Core.Tools.AutostartSoftwareWithWindowsRegistryWriter(); if (!Settings.loadingSettings && startHide && autostartSoftwareWithWindows && autostartMining) { showCLI = false; Janelas.Pages.Settings.ShowMiningConsole_CheckBox.IsChecked = false; } } }
        private bool autostartMining = true;
        public bool AutostartMining { get { return autostartMining; } set { autostartMining = value; if (!Settings.loadingSettings && startHide && autostartSoftwareWithWindows && autostartMining) { showCLI = false; Janelas.Pages.Settings.ShowMiningConsole_CheckBox.IsChecked = false; } } }
        private bool startHide = false;
        public bool StartHide { get { return startHide; } set { startHide = value; if (!Settings.loadingSettings && startHide && autostartSoftwareWithWindows && autostartMining) { showCLI = false; Janelas.Pages.Settings.ShowMiningConsole_CheckBox.IsChecked = false; } } }
        private bool avoidWindowsSuspend = false;
        public bool AvoidWindowsSuspend { get { return avoidWindowsSuspend; } set { avoidWindowsSuspend = value; Task.Run(() => Core.Tools.KeepSystemAwake()); } }

        public int PaymentCoinComboBox_SelectedIndex
        {
            get
            {
                for (int i = 0; Payment_CoinsList.Count > i; i++)
                {
                    if (String.Equals(Payment_CoinsList[i], payment_Coin, StringComparison.OrdinalIgnoreCase)) { return i; }
                }
                return 0;
            }
            set { }
        }
    }
}