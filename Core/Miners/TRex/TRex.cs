using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TrueMiningDesktop.Janelas;
using TrueMiningDesktop.Server;

namespace TrueMiningDesktop.Core.TRex
{
	public class TRex
	{
		private bool isMining = false;
		private bool isTryingStartMining = false;
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

		private List<DeviceInfo> Backends = new();
		public readonly Process TRexProcess = new();
		public readonly ProcessStartInfo TRexProcessStartInfo = new(Environment.CurrentDirectory + @"\Miners\TRex\" + @"t-rex.exe");
		private string AlgoBackendsString = null;
		public string WindowTitle = "True Mining running TRex";
		private int APIport = 20220;
		private bool IsInTRexexitEvent = false;
		private DateTime startedSince = DateTime.Now.AddYears(-1);

		public TRex(List<DeviceInfo> backends)
		{
			Backends = backends;

			MiningCoin miningCoin = SoftwareParameters.ServerConfig.MiningCoins.First(x => x.Algorithm.Equals(backends.First().MiningAlgo, StringComparison.OrdinalIgnoreCase));

			CreateConfigFile(miningCoin);
		}

		public void Start()
		{
			IsTryingStartMining = true;

			if (TRexProcess.StartInfo != TRexProcessStartInfo)
			{
				TRexProcessStartInfo.WorkingDirectory = Environment.CurrentDirectory + @"\Miners\TRex\";
				TRexProcessStartInfo.Arguments = "--config config-" + AlgoBackendsString + ".json";
				TRexProcessStartInfo.UseShellExecute = true;
				TRexProcessStartInfo.RedirectStandardError = false;
				TRexProcessStartInfo.RedirectStandardOutput = false;
				TRexProcessStartInfo.CreateNoWindow = false;
				TRexProcessStartInfo.ErrorDialog = false;
				TRexProcessStartInfo.WindowStyle = User.Settings.User.ShowCLI ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden;
				TRexProcess.StartInfo = TRexProcessStartInfo;
			}

			TRexProcess.Exited -= TRexProcess_Exited;
			TRexProcess.Exited += TRexProcess_Exited;
			TRexProcess.EnableRaisingEvents = true;

			try
			{
				TRexProcess.ErrorDataReceived -= TRexProcess_ErrorDataReceived;
				TRexProcess.ErrorDataReceived += TRexProcess_ErrorDataReceived;

				TRexProcess.Start();

				new Task(() =>
				{
					while (true)
					{
						try
						{
							Thread.Sleep(100);
							DateTime time = TRexProcess.StartTime;
							if (time.Ticks > 100) { try { Tools.SetWindowText(TRexProcess.MainWindowHandle, WindowTitle); } catch { } break; }
						}
						catch { }
					}
				}).Wait(3000);

				IsMining = true;
				IsTryingStartMining = false;

				startedSince = DateTime.UtcNow;
			}
			catch (Exception e)
			{
				Stop();

				IsTryingStartMining = true;

				if (minerBinaryChangedTimes < 2)
				{
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
								MessageBox.Show("T-Rex can't start. Try add True Mining Desktop folder in Antivirus/Windows Defender exclusions. " + e.Message);
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
						MessageBox.Show("TRex failed to start. Try add True Mining Desktop folder in Antivirus/Windows Defender exclusions. " + ee.Message);
					}
				}
			}
		}

		private void TRexProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			Tools.KillProcess(TRexProcess.ProcessName); Stop();
		}

		private void TRexProcess_Exited(object sender, EventArgs e)
		{
			if (IsMining && !IsStoppingMining)
			{
				if (!IsInTRexexitEvent)
				{
					IsInTRexexitEvent = true;

					if (startedSince < DateTime.UtcNow.AddSeconds(-30)) { Thread.Sleep(30000); } else { Thread.Sleep(10000); }

					if (IsMining && !IsStoppingMining)
					{
						Start();
					}

					IsInTRexexitEvent = false;
				}
			}
		}

		public void Stop()
		{
			try
			{
				try
				{
					IsStoppingMining = true;
				}
				catch { }

				Task tryCloseFancy = new(() =>
				{
					try
					{
						TRexProcess.CloseMainWindow();
						TRexProcess.WaitForExit();

						IsMining = false;
						IsStoppingMining = false;
					}
					catch
					{
						TRexProcess.Kill(true);
						Tools.KillProcessByName(TRexProcess.ProcessName);

						IsMining = false;
						IsStoppingMining = false;
					}
				});
				tryCloseFancy.Start();
				tryCloseFancy.Wait(4000);

				if (!tryCloseFancy.Wait(4000))
				{
					try
					{
						TRexProcess.Kill(true);
						Tools.KillProcessByName(TRexProcess.ProcessName);

						IsMining = false;
						IsStoppingMining = false;
					}
					catch { }
				}

				try
				{
					if (TRexProcess.Responding || !TRexProcess.HasExited)
					{
						TRexProcess.Kill(true);

						IsMining = false;
						IsStoppingMining = false;
					}
				}
				catch { }
			}
			catch { }
		}

		private int minerBinaryChangedTimes = 0;

		public void Show()
		{
			TRexProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
		}

		public void Hide()
		{
			TRexProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		}

		public Dictionary<string, decimal> GetHasrates()
		{
			if (Backends == null || Backends.Count == 0) { return null; }

			try
			{
				string backendPureData = new WebClient().DownloadString("http://localhost:" + APIport + "/summary");
				Miners.TRex.ApiSummary backendsAPI = JsonConvert.DeserializeObject<Miners.TRex.ApiSummary>(backendPureData, new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture });

				Dictionary<string, decimal> hashrates = new();

				Backends.ForEach(backend =>
				{
					if (backendsAPI.Uptime < 1)
					{
						hashrates.TryAdd("cuda", -1);
					}
					else if (backendsAPI.Hashrate < 1)
					{
						hashrates.TryAdd("cuda", 0);
					}
					else
					{
						hashrates.TryAdd("cuda", Convert.ToDecimal(backendsAPI.Hashrate, CultureInfo.InvariantCulture.NumberFormat));
					}
				});

				return hashrates;
			}
			catch { return null; }
		}

		public void CreateConfigFile(MiningCoin miningCoin)
		{
			APIport = 20200 + SoftwareParameters.ServerConfig.MiningCoins.IndexOf(miningCoin) + Device.DevicesList.IndexOf(this.Backends.Last());

			AlgoBackendsString = miningCoin.Algorithm.ToLowerInvariant() + '-' + string.Join(null, Backends.Select(x => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(x.BackendName.ToLowerInvariant())));

			WindowTitle = "TRex - " + miningCoin.Algorithm + " - " + string.Join(", ", Backends.Select(x => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(x.BackendName.ToLowerInvariant())));

			string Algorithm = miningCoin.Algorithm.ToString().ToLowerInvariant();

			StringBuilder conf = new();
			conf.AppendLine("{");
			conf.AppendLine("  \"algo\": \"" + Algorithm + "\",");
			conf.AppendLine("  \"coin\" : \"" + miningCoin.CoinTicker.ToLowerInvariant() + "\",");
			if (User.Settings.User.UseTorSharpOnMining) { conf.AppendLine("  \"proxy\": \"127.0.0.1:8428\","); }
			conf.AppendLine("  \"pci-indexing\" : false,");
			conf.AppendLine("  \"ab-indexing\" : false,");
			conf.AppendLine("  \"gpu-init-mode\" : 0,");
			conf.AppendLine("  \"keep-gpu-busy\" : false,");
			conf.AppendLine("  \"api-bind-http\": \"127.0.0.1:" + APIport + "\",");
			conf.AppendLine("  \"api-https\": false,");
			conf.AppendLine("  \"api-key\": \"\",");
			conf.AppendLine("  \"api-webserver-cert\" : \"\",");
			conf.AppendLine("  \"api-webserver-pkey\" : \"\",");
			conf.AppendLine("  \"kernel\" : 0,");
			conf.AppendLine("  \"retries\": 3,");
			conf.AppendLine("  \"retry-pause\": 10,");
			conf.AppendLine("  \"timeout\": 150,");
			conf.AppendLine("  \"intensity\": 20,");
			conf.AppendLine("  \"dag-build-mode\": 0,");
			conf.AppendLine("  \"dataset-mode\": 0,");
			conf.AppendLine("  \"extra-dag-epoch\": -1,");
			conf.AppendLine("  \"low-load\": 0,");
			conf.AppendLine("  \"lhr-autotune-mode\": \"down\",");
			conf.AppendLine("  \"lhr-autotune-step-size\": 0.1,");
			conf.AppendLine("  \"lhr-autotune-interval\": 5,");
			conf.AppendLine("  \"lhr-low-power\": false,");
			conf.AppendLine("  \"hashrate-avr\": 60,");
			conf.AppendLine("  \"sharerate-avr\": 600,");
			conf.AppendLine("  \"gpu-report-interval\": 30,");
			conf.AppendLine("  \"log-path\": \"logs/t-rex.log\",");
			conf.AppendLine("  \"cpu-priority\": 2,");
			conf.AppendLine("  \"autoupdate\": false,");
			conf.AppendLine("  \"exit-on-cuda-error\": true,");
			conf.AppendLine("  \"exit-on-connection-lost\": false,");
			conf.AppendLine("  \"reconnect-on-fail-shares\": 5,");
			conf.AppendLine("  \"protocol-dump\": false,");
			conf.AppendLine("  \"no-color\": false,");
			conf.AppendLine("  \"hide-date\": false,");
			conf.AppendLine("  \"send-stales\": false,");
			conf.AppendLine("  \"validate-shares\": false,");
			conf.AppendLine("  \"no-strict-ssl\": true,");
			conf.AppendLine("  \"no-sni\": false,");
			conf.AppendLine("  \"no-hashrate-report\": false,");
			conf.AppendLine("  \"no-watchdog\": true,");
			conf.AppendLine("  \"quiet\": false,");
			conf.AppendLine("  \"time-limit\": 0,");
			conf.AppendLine("  \"temperature-color\": \"67,77\",");
			conf.AppendLine("  \"temperature-color-mem\": \"80,100\",");

			if (!User.Settings.Device.cuda.DisableTempControl)
			{
				if (User.Settings.Device.cuda.ChipPauseMiningTemp > 0)
				{
					conf.AppendLine("  \"no-nvml\": false,");

					conf.AppendLine("  \"temperature-limit\": " + (string)User.Settings.Device.cuda.ChipPauseMiningTemp.ToString() + ",");
					conf.AppendLine("  \"temperature-start\": " + (string)(User.Settings.Device.cuda.ChipPauseMiningTemp - 30).ToString() + ",");
				}
			}
			else
			{
				conf.AppendLine("  \"no-nvml\": true,");
			}

			if (User.Settings.Device.cuda.ChipFansFullspeedTemp > 0)
			{
				conf.AppendLine("  \"fan\": \"t:" + (string)User.Settings.Device.cuda.ChipFansFullspeedTemp.ToString() + "\",");
			}

			conf.AppendLine("  \"back-to-main-pool-sec\": 6000,");
			conf.AppendLine("  \"script-start\": \"\",");
			conf.AppendLine("  \"script-exit\": \"\",");
			conf.AppendLine("  \"script-epoch-change\": \"\",");
			conf.AppendLine("  \"script-crash\": \"\",");
			conf.AppendLine("  \"script-low-hash\": \"\",");
			conf.AppendLine("  \"monitoring-page\" : {");
			conf.AppendLine("     \"graph_interval_sec\" : 3600,");
			conf.AppendLine("     \"update_timeout_sec\" : 10");
			conf.AppendLine("  },");

			List<string> addresses = miningCoin.PoolHosts;

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

			miningCoin.PoolHosts = pingHosts.OrderBy((KeyValuePair<string, long> value) => value.Value).ToDictionary(x => x.Key, x => x.Value).Keys.ToList();

			if (User.Settings.User.UseTorSharpOnMining)
			{
				new Task(() => _ = Tools.TorProxy).Start();
			}

			conf.AppendLine("  \"pools\": [");

			foreach (string host in miningCoin.PoolHosts)
			{
				conf.AppendLine("    {");
				conf.AppendLine("      \"user\": \"" + miningCoin.DepositAddressTrueMining + "." + User.Settings.User.PayCoin.CoinTicker.ToLowerInvariant() + '_' + User.Settings.User.Payment_Wallet + "/" + miningCoin.Email + "\",");
				conf.AppendLine("      \"url\": \"" + host + ":" + miningCoin.StratumPortSsl + "\",");
				conf.AppendLine("      \"pass\": \"" + miningCoin.Password + "\",");
				conf.AppendLine("    },");
			}

			conf.AppendLine("  ]");
			conf.AppendLine("}");

			if (!Directory.Exists(@"Miners")) { Directory.CreateDirectory(@"Miners"); }
			if (!Directory.Exists(@"Miners\TRex")) { Directory.CreateDirectory(@"Miners\TRex"); }
			if (!Directory.Exists(@"Miners\TRex\logs")) { Directory.CreateDirectory(@"Miners\TRex\logs"); }

			System.IO.File.WriteAllText(@"Miners\TRex\config-" + AlgoBackendsString + ".json", conf.ToString().NormalizeJson());

			StringBuilder cmdStart = new();
			cmdStart.AppendLine("cd /d \"%~dp0\"");
			cmdStart.AppendLine("t-rex.exe --config " + "config-" + AlgoBackendsString + ".json");
			cmdStart.Append("pause");

			System.IO.File.WriteAllText(@"Miners\TRex\start-" + AlgoBackendsString + ".cmd", cmdStart.ToString());
		}
	}
}