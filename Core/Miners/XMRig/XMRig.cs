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
using TrueMiningDesktop.User;

namespace TrueMiningDesktop.Core.XMRig
{
	public class XMRig
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
		public readonly Process XMRigProcess = new();
		public readonly ProcessStartInfo XMRigProcessStartInfo = new(Environment.CurrentDirectory + @"\Miners\XMRig\" + @"xmrig_zerofee-msvc.exe");
		private string AlgoBackendsString = null;
		public string WindowTitle = "True Mining running XMRig";
		private int APIport = 20210;
		private bool IsInXMRIGexitEvent = false;
		private DateTime startedSince = DateTime.Now.AddYears(-1);

		public XMRig(List<DeviceInfo> backends)
		{
			Backends = backends;

			MiningCoin miningCoin = SoftwareParameters.ServerConfig.MiningCoins.First(x => x.Algorithm.Equals(backends.First().MiningAlgo, StringComparison.OrdinalIgnoreCase));

			CreateConfigFile(miningCoin);
		}

		public void Start()
		{
			IsTryingStartMining = true;

			if (XMRigProcess.StartInfo != XMRigProcessStartInfo)
			{
				XMRigProcessStartInfo.WorkingDirectory = Environment.CurrentDirectory + @"\Miners\XMRig\";
				XMRigProcessStartInfo.Arguments = "--config=config-" + AlgoBackendsString + ".json";
				XMRigProcessStartInfo.UseShellExecute = true;
				XMRigProcessStartInfo.RedirectStandardError = false;
				XMRigProcessStartInfo.RedirectStandardOutput = false;
				XMRigProcessStartInfo.CreateNoWindow = false;
				XMRigProcessStartInfo.ErrorDialog = false;
				XMRigProcessStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				XMRigProcess.StartInfo = XMRigProcessStartInfo;
			}

			XMRigProcess.Exited -= XMRigProcess_Exited;
			XMRigProcess.Exited += XMRigProcess_Exited;
			XMRigProcess.EnableRaisingEvents = true;

			try
			{
				XMRigProcess.ErrorDataReceived -= XMRigProcess_ErrorDataReceived;
				XMRigProcess.ErrorDataReceived += XMRigProcess_ErrorDataReceived;

				XMRigProcess.Start();

				new Task(() =>
				{
					while (true)
					{
						try
						{
							Thread.Sleep(100);
							DateTime time = XMRigProcess.StartTime;
							if (time.Ticks > 100) { break; }
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

				if (minerBinaryChangedTimes < 4)
				{
					ChangeMinerBinary();
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
								MessageBox.Show("XMRig can't start. Try add True Mining Desktop folder in Antivirus/Windows Defender exclusions. " + e.Message);
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
						MessageBox.Show("XMRig failed to start. Try add True Mining Desktop folder in Antivirus/Windows Defender exclusions. " + ee.Message);
					}
				}
			}
		}

		private void XMRigProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			Tools.KillProcess(XMRigProcess.ProcessName); Stop();
		}

		private void XMRigProcess_Exited(object sender, EventArgs e)
		{
			if (IsMining && !IsStoppingMining)
			{
				if (!IsInXMRIGexitEvent)
				{
					IsInXMRIGexitEvent = true;

					if (startedSince < DateTime.UtcNow.AddSeconds(-30)) { Thread.Sleep(7000); }
					else { ChangeMinerBinary(); }

					if (IsMining && !IsStoppingMining)
					{
						Start();
					}

					IsInXMRIGexitEvent = false;
				}
			}
		}

		public void Stop(bool spawnCommandTaskkill = false)
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
						XMRigProcess.CloseMainWindow();
						XMRigProcess.WaitForExit();

						IsMining = false;
						IsStoppingMining = false;
					}
					catch
					{
						XMRigProcess.Kill(true);
						Tools.KillProcessByName(XMRigProcess.ProcessName);

						IsMining = false;
						IsStoppingMining = false;
					}
				});
				tryCloseFancy.Start();

				try
				{
					if (spawnCommandTaskkill)
					{
						XMRigProcess.Kill(true);
						Tools.KillProcessByName(XMRigProcess.ProcessName);
						Tools.KillProcess(XMRigProcess.ProcessName);
					}
				}
				catch { }

				if (!tryCloseFancy.Wait(4000))
				{
					try
					{
						XMRigProcess.Kill(true);
						Tools.KillProcessByName(XMRigProcess.ProcessName);

						IsMining = false;
						IsStoppingMining = false;
					}
					catch { }
				}

				try
				{
					if (XMRigProcess.Responding || !XMRigProcess.HasExited)
					{
						XMRigProcess.Kill(true);

						IsMining = false;
						IsStoppingMining = false;
					}
				}
				catch { }
			}
			catch { }
		}

		private int minerBinaryChangedTimes = 0;

		public void ChangeMinerBinary()
		{
			if (XMRigProcessStartInfo.FileName == Environment.CurrentDirectory + @"\Miners\XMRig\" + @"xmrig-gcc.exe")
			{
				XMRigProcessStartInfo.FileName = Environment.CurrentDirectory + @"\Miners\XMRig\" + @"xmrig_zerofee-msvc.exe";
			}
			else if (XMRigProcessStartInfo.FileName == Environment.CurrentDirectory + @"\Miners\XMRig\" + @"xmrig_zerofee-msvc.exe")
			{
				XMRigProcessStartInfo.FileName = Environment.CurrentDirectory + @"\Miners\XMRig\" + @"xmrig-gcc.exe";
			}
			else
			{
				XMRigProcessStartInfo.FileName = Environment.CurrentDirectory + @"\Miners\XMRig\" + @"xmrig-gcc.exe";
			}

			if (minerBinaryChangedTimes < 100) { minerBinaryChangedTimes++; }
		}

		public void Show()
		{
			XMRigProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
		}

		public void Hide()
		{
			XMRigProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		}

		public Dictionary<string, decimal> GetHasrates()
		{
			if (Backends == null || Backends.Count == 0) { return null; }

			try
			{
				string backendPureData = new WebClient().DownloadString("http://localhost:" + APIport + "/2/backends");
				dynamic backendsAPI = JsonConvert.DeserializeObject(backendPureData, new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture });

				Dictionary<string, decimal> hashrates = new();

				Backends.ForEach(backend =>
				{
					if (backend.BackendName.Equals("cpu", StringComparison.OrdinalIgnoreCase))
					{
						foreach (dynamic backendLoop in backendsAPI)
						{
							if (backendLoop.type == "cpu")
							{
								if (backendLoop.enabled == false)
								{
									hashrates.TryAdd("cpu", -1);
								}
								else if (backendLoop.hashrate[0] == null)
								{
									hashrates.TryAdd("cpu", 0);
								}
								else
								{
									hashrates.TryAdd("cpu", Convert.ToDecimal(backendLoop.hashrate[0], CultureInfo.InvariantCulture.NumberFormat));
								}
							}
						}
					}

					if (backend.BackendName.Equals("opencl", StringComparison.OrdinalIgnoreCase))
					{
						foreach (dynamic backendLoop in backendsAPI)
						{
							if (backendLoop.type == "opencl")
							{
								if (backendLoop.enabled == false)
								{
									hashrates.TryAdd("opencl", -1);
								}
								else if (backendLoop.hashrate[0] == null)
								{
									hashrates.TryAdd("opencl", 0);
								}
								else
								{
									hashrates.TryAdd("opencl", Convert.ToDecimal(backendLoop.hashrate[0], CultureInfo.InvariantCulture.NumberFormat));
								}
							}
						}
					}

					if (backend.BackendName.Equals("cuda", StringComparison.OrdinalIgnoreCase))
					{
						foreach (dynamic backendLoop in backendsAPI)
						{
							if (backendLoop.type == "cuda")
							{
								if (backendLoop.enabled == false)
								{
									hashrates.TryAdd("cuda", -1);
								}
								else if (backendLoop.hashrate[0] == null)
								{
									hashrates.TryAdd("cuda", 0);
								}
								else
								{
									hashrates.TryAdd("cuda", Convert.ToDecimal(backendLoop.hashrate[0], CultureInfo.InvariantCulture.NumberFormat));
								}
							}
						}
					}
				});

				return hashrates;
			}
			catch { return null; }
		}

		public void CreateConfigFile(MiningCoin miningCoin)
		{
			APIport = 20100 + SoftwareParameters.ServerConfig.MiningCoins.IndexOf(miningCoin) + Device.DevicesList.IndexOf(this.Backends.Last());

			AlgoBackendsString = miningCoin.Algorithm.ToLowerInvariant() + '-' + string.Join(null, Backends.Select(x => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(x.BackendName.ToLowerInvariant())));

			WindowTitle = "XMRig - " + miningCoin.Algorithm + " - " + string.Join(", ", Backends.Select(x => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(x.BackendName.ToLowerInvariant())));

			string Algorithm = miningCoin.Algorithm.ToString().ToLowerInvariant();
			if (Algorithm.Equals("RandomX", StringComparison.OrdinalIgnoreCase)) { Algorithm = "rx/0"; }

			StringBuilder conf = new();
			conf.AppendLine("{");
			conf.AppendLine("    \"api\": {");
			conf.AppendLine("        \"id\": null,");
			conf.AppendLine("        \"worker-id\": null");
			conf.AppendLine("    },");
			conf.AppendLine("    \"http\": {");
			conf.AppendLine("        \"enabled\": true,");
			conf.AppendLine("        \"host\": \"" + (User.Settings.User.UseAllInterfacesInsteadLocalhost ? "0.0.0.0" : "127.0.0.1") + "\",");
			conf.AppendLine("        \"port\": " + APIport + ",");
			conf.AppendLine("        \"access-token\": null,");
			conf.AppendLine("        \"restricted\": true");
			conf.AppendLine("    },");
			conf.AppendLine("    \"autosave\": false,");
			conf.AppendLine("    \"colors\": true,");
			conf.AppendLine("    \"title\": \"" + WindowTitle + "\",");

			Device.DevicesList.ForEach(backend =>
			{
				if (backend.BackendName.Equals("cpu", StringComparison.OrdinalIgnoreCase))
				{
					CpuSettings cpuSettings = User.Settings.Device.cpu;

					if (!cpuSettings.MiningSelected || !Backends.Any(x => x.BackendName.Equals(backend.BackendName, StringComparison.OrdinalIgnoreCase)))
					{
						conf.AppendLine("    \"cpu\": {");
						conf.AppendLine("        \"enabled\": false,");
						conf.AppendLine("    },");
					}
					else
					{
						conf.AppendLine("    \"cpu\": {");
						conf.AppendLine("        \"enabled\": true,");
						conf.AppendLine("        \"huge-pages\": true,");
						conf.AppendLine("        \"hw-aes\": null,");
						if (!cpuSettings.Autoconfig) { conf.AppendLine("        \"priority\": " + cpuSettings.Priority + ","); } else { conf.AppendLine("        \"priority\": 1,"); }
						conf.AppendLine("        \"memory-pool\": true,");
						if (!cpuSettings.Autoconfig) { conf.AppendLine("        \"yield\": " + cpuSettings.Yield.ToString().ToLowerInvariant() + ","); }
						conf.AppendLine("        \"asm\": true,");
						if (!cpuSettings.Autoconfig && cpuSettings.Threads == 0) { conf.AppendLine("        \"max-threads-hint\": " + cpuSettings.MaxUsageHint + ","); }
						if (!cpuSettings.Autoconfig && cpuSettings.Threads > 0) { conf.AppendLine("        \"rx\": {\"threads\": " + cpuSettings.Threads + "},"); }
						conf.AppendLine("    },");
					}
				}

				if (backend.BackendName.Equals("opencl", StringComparison.OrdinalIgnoreCase))
				{
					DeviceInfo openclBackend = Backends.FirstOrDefault(backend => backend.BackendName.Equals("opencl"));
					OpenClSettings openclSettings = User.Settings.Device.opencl;

					if (!openclSettings.MiningSelected || !Backends.Any(x => x.BackendName.Equals(backend.BackendName, StringComparison.OrdinalIgnoreCase)))
					{
						conf.AppendLine("    \"opencl\": {");
						conf.AppendLine("        \"enabled\": false,");
						conf.AppendLine("    },");
					}
					else
					{
						conf.AppendLine("    \"opencl\": {");
						conf.AppendLine("        \"enabled\": true,");
						if (!openclSettings.Autoconfig) { conf.AppendLine("     \"cache\": true,"); }
						conf.AppendLine("        \"loader\": null,");
						conf.AppendLine("        \"platform\": \"AMD\",");
						conf.AppendLine("        \"adl\": true,");
						conf.AppendLine("    },");
					}
				}

				if (backend.BackendName.Equals("cuda", StringComparison.OrdinalIgnoreCase))
				{
					DeviceInfo cudaBackend = Backends.FirstOrDefault(backend => backend.BackendName.Equals("cuda"));
					CudaSettings cudaSettings = User.Settings.Device.cuda;

					if (!cudaSettings.MiningSelected || !Backends.Any(x => x.BackendName.Equals(backend.BackendName, StringComparison.OrdinalIgnoreCase)))
					{
						conf.AppendLine("    \"cuda\": {");
						conf.AppendLine("        \"enabled\": false,");
						conf.AppendLine("    },");
					}
					else
					{
						conf.AppendLine("    \"cuda\": {");
						conf.AppendLine("        \"enabled\": true,");//
						conf.AppendLine("        \"loader\": null,");
						if (!cudaSettings.Autoconfig) { conf.AppendLine("        \"nvml\": true"); }
						conf.AppendLine("    },");
					}
				}
			});

			if (Algorithm.Equals("rx/0", StringComparison.OrdinalIgnoreCase) || Algorithm.Equals("RandomX", StringComparison.OrdinalIgnoreCase))
			{
				conf.AppendLine("    \"randomx\": {");
				conf.AppendLine("        \"init\": -1,");
				conf.AppendLine("        \"init-avx2\": -1,");
				conf.AppendLine("        \"mode\": \"auto\",");
				conf.AppendLine("        \"1gb-pages\": true,");
				conf.AppendLine("        \"rdmsr\": true,");
				conf.AppendLine("        \"wrmsr\": true,");
				conf.AppendLine("        \"cache_qos\": true,");
				conf.AppendLine("        \"numa\": true,");
				conf.AppendLine("        \"scratchpad_prefetch_mode\": true");
				conf.AppendLine("    },");
			}
			conf.AppendLine("    \"donate-level\": 0,");
			conf.AppendLine("    \"donate-over-proxy\": 0,");
			conf.AppendLine("    \"log-file\": \"logs/XMRig-" + AlgoBackendsString + ".log\",");
			conf.AppendLine("    \"retries\": 2,");
			conf.AppendLine("    \"retry-pause\": 3,");

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

			conf.AppendLine("    \"pools\": [");

			foreach (string host in miningCoin.PoolHosts)
			{
				if (User.Settings.User.UseTorSharpOnMining)
				{
					conf.AppendLine("        {");
					conf.AppendLine("            \"algo\": \"" + Algorithm + "\",");
					conf.AppendLine("            \"url\": \"" + host + ":" + miningCoin.StratumPort + "\",");
					conf.AppendLine("            \"user\": \"" + miningCoin.DepositAddressTrueMining + "." + User.Settings.User.PayCoin.CoinTicker.ToLowerInvariant() + '_' + User.Settings.User.Payment_Wallet + "/" + miningCoin.Email + "\", ");
					conf.AppendLine("            \"pass\": \"" + miningCoin.Password + "\",");
					conf.AppendLine("            \"rig-id\": null,");
					conf.AppendLine("            \"nicehash\": false,");
					conf.AppendLine("            \"keepalive\": false,");
					conf.AppendLine("            \"enabled\": true,");
					conf.AppendLine("            \"tls\": true,");
					conf.AppendLine("            \"tls-fingerprint\": null,");
					conf.AppendLine("            \"daemon\": false,");
					conf.AppendLine("            \"socks5\": \"127.0.0.1:8428\",");
					conf.AppendLine("            \"self-select\": null");
					conf.AppendLine("        },");
				}

				conf.AppendLine("        {");
				conf.AppendLine("            \"algo\": \"" + Algorithm + "\",");
				conf.AppendLine("            \"url\": \"" + host + ":" + miningCoin.StratumPort + "\",");
				conf.AppendLine("            \"user\": \"" + miningCoin.DepositAddressTrueMining + "." + User.Settings.User.PayCoin.CoinTicker.ToLowerInvariant() + '_' + User.Settings.User.Payment_Wallet + "/" + miningCoin.Email + "\", ");
				conf.AppendLine("            \"pass\": \"" + miningCoin.Password + "\",");
				conf.AppendLine("            \"rig-id\": null,");
				conf.AppendLine("            \"nicehash\": false,");
				conf.AppendLine("            \"keepalive\": false,");
				conf.AppendLine("            \"enabled\": true,");
				conf.AppendLine("            \"tls\": true,");
				conf.AppendLine("            \"tls-fingerprint\": null,");
				conf.AppendLine("            \"daemon\": false,");
				conf.AppendLine("            \"socks5\": null,");
				conf.AppendLine("            \"self-select\": null");
				conf.AppendLine("        },");
			}

			conf.AppendLine("   ]");
			conf.AppendLine("}");

			if (!Directory.Exists(@"Miners")) { Directory.CreateDirectory(@"Miners"); }
			if (!Directory.Exists(@"Miners\XMRig")) { Directory.CreateDirectory(@"Miners\XMRig"); }
			if (!Directory.Exists(@"Miners\XMRig\logs")) { Directory.CreateDirectory(@"Miners\XMRig\logs"); }

			System.IO.File.WriteAllText(@"Miners\XMRig\config-" + AlgoBackendsString + ".json", conf.ToString());

			StringBuilder cmdStart = new();
			cmdStart.AppendLine("cd /d \"%~dp0\"");
			cmdStart.AppendLine("xmrig.exe --config=" + "config-" + AlgoBackendsString + ".json");
			cmdStart.Append("pause");

			System.IO.File.WriteAllText(@"Miners\XMRig\start-" + AlgoBackendsString + ".cmd", cmdStart.ToString());
		}
	}
}