using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using TrueMiningDesktop.Core;

namespace TrueMiningDesktop.User
{
    public static class Settings
    {
        public static DeviceSettings Device = new();
        public static UserPreferences User = new();

        public static bool LoadingSettings { get; set; } = true;

        private static readonly Timer timerSaveSettings = new(5000);

        public static void SettingsSaver(bool now = false)
        {
            if (now) { WriteSettings(); } else { timerSaveSettings.Elapsed -= TimerSaveSettings_Elapsed; timerSaveSettings.Elapsed += TimerSaveSettings_Elapsed; timerSaveSettings.AutoReset = false; timerSaveSettings.Stop(); timerSaveSettings.Start(); }
        }

        private static void TimerSaveSettings_Elapsed(object sender, ElapsedEventArgs e)
        {
            WriteSettings();
        }

        private static void WriteSettings()
        {
            timerSaveSettings.Stop();

            string tempPath = Path.GetTempFileName();
            string backup = "configsDevices.txt" + ".backup";

            if (File.Exists(backup)) { File.Delete(backup); }

            byte[] data = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Device, Formatting.Indented));

            using (FileStream tempFile = File.Create(tempPath, 4096, FileOptions.WriteThrough))
                tempFile.Write(data, 0, data.Length);

            File.Replace(tempPath, "configsDevices.txt", backup);

            tempPath = Path.GetTempFileName();
            backup = "configsUser.txt" + ".backup";

            if (File.Exists(backup)) { File.Delete(backup); }

            data = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(User, Formatting.Indented));

            using (FileStream tempFile = File.Create(tempPath, 4096, FileOptions.WriteThrough))
                tempFile.Write(data, 0, data.Length);

            File.Replace(tempPath, "configsUser.txt", backup);
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
                User.UseAllInterfacesInsteadLocalhost = up.UseAllInterfacesInsteadLocalhost;
                User.UseTorSharpOnMining = up.UseTorSharpOnMining;
                User.ShowCLI = up.ShowCLI;
                User.StartHide = up.StartHide;
                User.ChangeTbIcon = up.ChangeTbIcon;
                User.Payment_Coin = up.Payment_Coin;
                User.Payment_Wallet = up.Payment_Wallet;
                User.LICENSE_read = up.LICENSE_read;
            }

            LoadingSettings = false;
        }
    }

    public class DeviceSettings
    {
        public CPUSettings cpu = new();
        public NVIDIASettings cuda = new();
        public AMDSettings opencl = new();
    }

    public class CPUSettings
    {
        private bool miningSelected = true;
        public bool MiningSelected { get { return miningSelected; } set { miningSelected = value; if (!Settings.LoadingSettings) { Settings.SettingsSaver(); } } }
        public bool Autoconfig { get; set; } = true;
        public string Algorithm { get; set; } = "RandomX";
        public List<string> AlgorithmsList { get; set; } = new();
        public int Priority { get; set; } = 1;
        public int MaxUsageHint { get; set; } = 100;
        public int Threads { get; set; } = 0;
        public bool Yield { get; set; } = true;
    }

    public class NVIDIASettings
    {
        private bool miningSelected = false;
        public bool MiningSelected { get { return miningSelected; } set { miningSelected = value; if (!Settings.LoadingSettings) { Settings.SettingsSaver(); } } }
        public bool Autoconfig { get; set; } = true;
        public string Algorithm { get; set; } = "RandomX";
        public List<string> AlgorithmsList { get; set; } = new List<string>();
        public bool NVML { get; set; } = true;
    }

    public class AMDSettings
    {
        private bool miningSelected = false;
        public bool MiningSelected { get { return miningSelected; } set { miningSelected = value; if (!Settings.LoadingSettings) { Settings.SettingsSaver(); } } }
        public bool Autoconfig { get; set; } = true;
        public string Algorithm { get; set; } = "RandomX";
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
                    payment_Wallet = payment_Wallet.Replace(" ", "");

                    if (payment_Wallet.StartsWith("R"))
                    { Payment_Coin = "RDCT"; }
                    if (payment_Wallet.StartsWith("D"))
                    { Payment_Coin = "DOGE"; }

                    if (!Settings.LoadingSettings) { Settings.SettingsSaver(); }
                }
            }
        }

        public bool LICENSE_read = false;

        private string payment_Coin;

        public string Payment_Coin
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
        private bool autostartSoftwareWithWindows = false;
        public bool AutostartSoftwareWithWindows { get { return autostartSoftwareWithWindows; } set { autostartSoftwareWithWindows = value; Core.Tools.AutostartSoftwareWithWindowsRegistryWriter(); if (!Settings.LoadingSettings && startHide && autostartSoftwareWithWindows && autostartMining) { showCLI = false; Janelas.Pages.Settings.ShowMiningConsole_CheckBox.IsChecked = false; } } }
        private bool autostartMining = false;
        public bool AutostartMining { get { return autostartMining; } set { autostartMining = value; if (!Settings.LoadingSettings && startHide && autostartSoftwareWithWindows && autostartMining) { showCLI = false; Janelas.Pages.Settings.ShowMiningConsole_CheckBox.IsChecked = false; } } }
        private bool startHide = false;
        public bool StartHide { get { return startHide; } set { startHide = value; if (!Settings.LoadingSettings && startHide && autostartSoftwareWithWindows && autostartMining) { showCLI = false; Janelas.Pages.Settings.ShowMiningConsole_CheckBox.IsChecked = false; } } }

        private bool changeTbIcon = false;
        public bool ChangeTbIcon { get { return changeTbIcon; } set { changeTbIcon = value; Tools.TryChangeTaskbarIconAsSettingsOrder(); } }

        private bool avoidWindowsSuspend = true;
        public bool AvoidWindowsSuspend { get { return avoidWindowsSuspend; } set { avoidWindowsSuspend = value; Task.Run(() => Core.Tools.KeepSystemAwake()); } }

        public int PaymentCoinComboBox_SelectedIndex
        {
            get
            {
                for (int i = 0; Payment_CoinsList.Count > i; i++)
                {
                    if (string.Equals(Payment_CoinsList[i], payment_Coin, StringComparison.OrdinalIgnoreCase)) { return i; }
                }
                return 0;
            }
            set { }
        }

        private bool useAllInterfacesInsteadLocalhost = false;
        public bool UseAllInterfacesInsteadLocalhost { get { return useAllInterfacesInsteadLocalhost; } set { useAllInterfacesInsteadLocalhost = value; if (Miner.IsMining) { Miner.StopMiner(); Miner.StartMiner(); }; } }

        private bool useTorSharpOnAll = false;
        public bool UseTorSharpOnMining { get { return useTorSharpOnAll; } set { useTorSharpOnAll = value; if (!User.Settings.LoadingSettings) { Tools.NotifyPropertyChanged(); } if (value) { new Task(() => _ = Tools.TorProxy).Start(); } if (Miner.IsMining) { Miner.StopMiner(); Miner.StartMiner(); }; } }
    }
}