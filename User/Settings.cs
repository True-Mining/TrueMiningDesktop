using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using TrueMiningDesktop.Core;
using TrueMiningDesktop.Server;

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
			while (LoadingSettings) { System.Threading.Thread.Sleep(500); }
			if (now)
			{
				WriteSettings();
			}
			else
			{
				timerSaveSettings.Elapsed -= TimerSaveSettings_Elapsed;
				timerSaveSettings.Elapsed += TimerSaveSettings_Elapsed;
				timerSaveSettings.AutoReset = false;
				timerSaveSettings.Stop();
				timerSaveSettings.Start();
			}
		}

		private static void TimerSaveSettings_Elapsed(object sender, ElapsedEventArgs e)
		{
			WriteSettings();
		}

		private static void WriteSettings()
		{
			timerSaveSettings.Stop();
			if (!File.Exists("configsDevices.txt")) { File.WriteAllText("configsDevices.txt", JsonConvert.SerializeObject(Device, Formatting.Indented)); }
			if (!File.Exists("configsUser.txt")) { File.WriteAllText("configsUser.txt", JsonConvert.SerializeObject(User, Formatting.Indented)); }

			string tempPath = Path.GetTempFileName();
			string backup = "configsDevices.txt" + ".backup";

			if (File.Exists(backup)) { File.Delete(backup); }

			byte[] data = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Device, Formatting.Indented));

			using (FileStream tempFile = File.Create(tempPath, 4096, FileOptions.WriteThrough))
			{
				tempFile.Write(data, 0, data.Length);
			}

			File.Replace(tempPath, "configsDevices.txt", backup);

			tempPath = Path.GetTempFileName();
			backup = "configsUser.txt" + ".backup";

			if (File.Exists(backup)) { File.Delete(backup); }

			data = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(User, Formatting.Indented));

			using (FileStream tempFile = File.Create(tempPath, 4096, FileOptions.WriteThrough))
			{
				tempFile.Write(data, 0, data.Length);
			}

			File.Replace(tempPath, "configsUser.txt", backup);
		}

		public static void SettingsRecover()
		{
			try
			{
				if (File.Exists("configsDevices.txt"))
				{
					Device = JsonConvert.DeserializeObject<DeviceSettings>(File.ReadAllText("configsDevices.txt"), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture });
				}

				if (File.Exists("configsUser.txt"))
				{
					UserPreferences up = JsonConvert.DeserializeObject<UserPreferences>(File.ReadAllText("configsUser.txt"), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture });
					User.LICENSE_read = up.LICENSE_read;
					User.AutostartMining = up.AutostartMining;
					User.AutostartSoftwareWithWindows = up.AutostartSoftwareWithWindows;
					User.AvoidWindowsSuspend = up.AvoidWindowsSuspend;
					User.UseAllInterfacesInsteadLocalhost = up.UseAllInterfacesInsteadLocalhost;
					User.UseTorSharpOnMining = up.UseTorSharpOnMining;
					User.ShowCLI = up.ShowCLI;
					User.StartHide = up.StartHide;
					User.ChangeTbIcon = up.ChangeTbIcon;
					User.Payment_CoinsList = up.Payment_CoinsList;
					User.PayCoin = up.PayCoin;
					User.Payment_Coin = up.Payment_Coin;
					if (up.Payment_Coin == null && up.PayCoin != null && up.PayCoin.CoinTicker != null && up.PayCoin.CoinName != null) { User.Payment_Coin = up.PayCoin.CoinTicker + " - " + up.PayCoin.CoinName; }
					User.Payment_Wallet = up.Payment_Wallet;
				}

				LoadingSettings = false;
			}
			catch
			{
				try
				{
					if (File.Exists("configsDevices.txt.backup"))
					{
						Device = JsonConvert.DeserializeObject<DeviceSettings>(File.ReadAllText("configsDevices.txt.backup"), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture });
						if (!Device.cpu.AlgorithmsList.Contains(Device.cpu.Algorithm)) { Device.cpu.Algorithm = Device.cpu.AlgorithmsList.First(); }
						if (!Device.cuda.AlgorithmsList.Contains(Device.cuda.Algorithm)) { Device.cuda.Algorithm = Device.cuda.AlgorithmsList.First(); }
						if (!Device.opencl.AlgorithmsList.Contains(Device.opencl.Algorithm)) { Device.opencl.Algorithm = Device.opencl.AlgorithmsList.First(); }
					}

					if (File.Exists("configsUser.txt.backup"))
					{
						UserPreferences up = JsonConvert.DeserializeObject<UserPreferences>(File.ReadAllText("configsUser.txt.backup"), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture });
						User.LICENSE_read = up.LICENSE_read;
						User.AutostartMining = up.AutostartMining;
						User.AutostartSoftwareWithWindows = up.AutostartSoftwareWithWindows;
						User.AvoidWindowsSuspend = up.AvoidWindowsSuspend;
						User.UseAllInterfacesInsteadLocalhost = up.UseAllInterfacesInsteadLocalhost;
						User.UseTorSharpOnMining = up.UseTorSharpOnMining;
						User.ShowCLI = up.ShowCLI;
						User.StartHide = up.StartHide;
						User.ChangeTbIcon = up.ChangeTbIcon;
						User.Payment_CoinsList = up.Payment_CoinsList;
						User.PayCoin = up.PayCoin;
						User.Payment_Coin = up.Payment_Coin;
						if (up.Payment_Coin == null && up.PayCoin != null && up.PayCoin.CoinTicker != null && up.PayCoin.CoinName != null) { User.Payment_Coin = up.PayCoin.CoinTicker + " - " + up.PayCoin.CoinName; }
						User.Payment_Wallet = up.Payment_Wallet;
					}

					LoadingSettings = false;
				}
				catch { }
			}
		}
	}

	public class DeviceSettings
	{
		public CpuSettings cpu = new();
		public CudaSettings cuda = new();
		public OpenClSettings opencl = new();
	}

	public class CpuSettings
	{
		private bool miningSelected = true;

		public bool MiningSelected
		{ get { return miningSelected; } set { miningSelected = value; if (!Settings.LoadingSettings) { Settings.SettingsSaver(); } } }

		public bool Autoconfig { get; set; } = true;

		private string algorithm = "RandomX";

		public string Algorithm
		{ get { return algorithm; } set { algorithm = value; if (!Settings.LoadingSettings) { Device.Cpu.MiningAlgo = value; Settings.SettingsSaver(); } } }

		public List<string> AlgorithmsList { get; set; } = new List<string>(new[] { "RandomX" });
		public int Priority { get; set; } = 1;
		public int MaxUsageHint { get; set; } = 100;
		public int Threads { get; set; } = 0;
		public bool Yield { get; set; } = true;
	}

	public class CudaSettings
	{
		private bool miningSelected = false;

		public bool MiningSelected
		{ get { return miningSelected; } set { miningSelected = value; if (!Settings.LoadingSettings) { Settings.SettingsSaver(); } } }

		public bool Autoconfig { get; set; } = true;

		private string algorithm = "EtcHash";

		public string Algorithm
		{ get { return algorithm; } set { algorithm = value; if (!Settings.LoadingSettings) { Device.Cuda.MiningAlgo = value; Settings.SettingsSaver(); } } }

		public List<string> AlgorithmsList { get; set; } = new List<string>(new[] { "EtcHash", "KawPow", "RandomX" });

		public bool DisableTempControl { get; set; } = false;

		public int ChipPauseMiningTemp { get; set; } = 85;
		public int ChipFansFullspeedTemp { get; set; } = 65;

		public int MemPauseMiningTemp { get; set; } = 95;
		public int MemFansFullspeedTemp { get; set; } = 75;
	}

	public class OpenClSettings
	{
		private bool miningSelected = false;

		public bool MiningSelected
		{ get { return miningSelected; } set { miningSelected = value; if (!Settings.LoadingSettings) { Settings.SettingsSaver(); } } }

		public bool Autoconfig { get; set; } = true;

		private string algorithm = "EtcHash";

		public string Algorithm
		{ get { return algorithm; } set { algorithm = value; if (!Settings.LoadingSettings) { Device.Opencl.MiningAlgo = value; Settings.SettingsSaver(); } } }

		public List<string> AlgorithmsList { get; set; } = new List<string>(new[] { "EtcHash", "KawPow", "RandomX" });

		public bool DisableTempControl { get; set; } = false;

		public int ChipPauseMiningTemp { get; set; } = 85;
		public int ChipFansFullspeedTemp { get; set; } = 65;

		public int MemPauseMiningTemp { get; set; } = 95;
		public int MemFansFullspeedTemp { get; set; } = 75;
	}

	public class UserPreferences
	{
		private string payment_Wallet = null;

		public string Payment_Wallet
		{
			get { return payment_Wallet; }
			set
			{
				if (value != null)
				{
					payment_Wallet = value;

					if (!Settings.LoadingSettings) { Settings.SettingsSaver(); }
				}
			}
		}

		public bool LICENSE_read = false;

		private string payment_Coin = null;

		public string Payment_Coin
		{
			get
			{
				return payment_Coin;
			}
			set
			{
				string newValue = null;
				bool coinIsInParamsCoins = false;

				try
				{
					if (SoftwareParameters.ServerConfig != null && SoftwareParameters.ServerConfig.PaymentCoins != null && value != null)
					{
						PayCoin = SoftwareParameters.ServerConfig.PaymentCoins.First(x => value.Split('-', ' ').Any(z => z.Equals(x.CoinName, StringComparison.OrdinalIgnoreCase) || z.Equals(x.CoinTicker, StringComparison.OrdinalIgnoreCase)));

						newValue = PayCoin.CoinTicker + " - " + PayCoin.CoinName;

						coinIsInParamsCoins = true;
					}
					else if (Payment_CoinsList.Any(x => x.Contains(value)))
					{
						newValue = Payment_CoinsList.First(x => x.Contains(value));
					}
					else
					{
						newValue = value;
					}
				}
				catch
				{
					newValue = value;
				}

				if (payment_Coin != newValue)
				{
					payment_Coin = newValue;

					if (!Settings.LoadingSettings) { Janelas.Pages.Home.PaymentInfoWasChanged = true; }
					if (!Settings.LoadingSettings && coinIsInParamsCoins) { Settings.SettingsSaver(); }
				}

				if (!Settings.LoadingSettings && coinIsInParamsCoins)
				{
					try
					{
						// Se a PaymentCoin selecionada estiver em fase de deslistagem, exibe warning na Home Page
						if (PayCoin != null && PayCoin.Unlisting)
						{
							Janelas.Pages.Home.ImportantWarningTextBlock.Text = "Warning: Payment Coin " + PayCoin.CoinTicker + " - " + PayCoin.CoinName + " is in Unlisting Phase. Please change your Payment Coin as soon as possible. After the unlisting phase ends, payments in that coin will no longer happen";
							Janelas.Pages.Home.ImportantWarningTextBlock.Visibility = System.Windows.Visibility.Visible;
						}
						else if (PayCoin != null && !PayCoin.Unlisting)
						{
							Janelas.Pages.Home.ImportantWarningTextBlock.Visibility = System.Windows.Visibility.Collapsed;
						}
					}
					catch { }
				}
			}
		}

		public PaymentCoin PayCoin { get; set; } = new();

		public List<string> Payment_CoinsList
		{
			get
			{
				return payCoinsList;
			}
			set
			{
				payCoinsList = value;

				if (SoftwareParameters.ServerConfig != null && SoftwareParameters.ServerConfig.PaymentCoins != null && payment_Coin != null)
				{
					PayCoin = SoftwareParameters.ServerConfig.PaymentCoins.First(x => payment_Coin.Split('-', ' ').Any(z => z.Equals(x.CoinName, StringComparison.OrdinalIgnoreCase) || z.Equals(x.CoinTicker, StringComparison.OrdinalIgnoreCase)));

					Payment_Coin = PayCoin.CoinTicker + " - " + PayCoin.CoinName;
				}
			}
		}

		private List<string> payCoinsList = new();

		private bool showCLI = false;

		public bool ShowCLI
		{ get { return showCLI; } set { showCLI = value; Miner.ShowHideCLI(); } }

		private bool autostartSoftwareWithWindows = false;

		public bool AutostartSoftwareWithWindows
		{ get { return autostartSoftwareWithWindows; } set { autostartSoftwareWithWindows = value; Core.Tools.AutostartSoftwareWithWindowsRegistryWriter(); if (!Settings.LoadingSettings && startHide && autostartSoftwareWithWindows && autostartMining) { showCLI = false; Janelas.Pages.Settings.ShowMiningConsole_CheckBox.IsChecked = false; } } }

		private bool autostartMining = false;

		public bool AutostartMining
		{ get { return autostartMining; } set { autostartMining = value; if (!Settings.LoadingSettings && startHide && autostartSoftwareWithWindows && autostartMining) { showCLI = false; Janelas.Pages.Settings.ShowMiningConsole_CheckBox.IsChecked = false; } } }

		private bool startHide = false;

		public bool StartHide
		{ get { return startHide; } set { startHide = value; if (!Settings.LoadingSettings && startHide && autostartSoftwareWithWindows && autostartMining) { showCLI = false; Janelas.Pages.Settings.ShowMiningConsole_CheckBox.IsChecked = false; } } }

		private bool changeTbIcon = false;

		public bool ChangeTbIcon
		{ get { return changeTbIcon; } set { changeTbIcon = value; Tools.TryChangeTaskbarIconAsSettingsOrder(); } }

		private bool avoidWindowsSuspend = true;

		public bool AvoidWindowsSuspend
		{ get { return avoidWindowsSuspend; } set { avoidWindowsSuspend = value; } }

		private bool useAllInterfacesInsteadLocalhost = false;

		public bool UseAllInterfacesInsteadLocalhost
		{ get { return useAllInterfacesInsteadLocalhost; } set { useAllInterfacesInsteadLocalhost = value; if (Miner.IsMining) { Miner.StopMiners(); Miner.StartMiners(); }; } }

		private bool useTorSharpOnMining = false;

		public bool UseTorSharpOnMining
		{ get { return useTorSharpOnMining; } set { useTorSharpOnMining = value; if (!User.Settings.LoadingSettings) { Tools.NotifyPropertyChanged(); } if (value) { new Task(() => _ = Tools.TorProxy).Start(); } if (Miner.IsMining) { Miner.StopMiners(); Miner.StartMiners(); }; } }
	}
}