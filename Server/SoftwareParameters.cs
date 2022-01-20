using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using TrueMiningDesktop.Core;

namespace TrueMiningDesktop.Server
{
    public partial class TrueMiningDesktopParameters
    {
        [JsonProperty("MiningCoins", NullValueHandling = NullValueHandling.Ignore)]
        public List<MiningCoin> MiningCoins { get; set; }

        [JsonProperty("PaymentCoins", NullValueHandling = NullValueHandling.Ignore)]
        public List<PaymentCoin> PaymentCoins { get; set; }

        [JsonProperty("DynamicFee", NullValueHandling = NullValueHandling.Ignore)]
        public decimal DynamicFee { get; set; }

        [JsonProperty("AppFiles", NullValueHandling = NullValueHandling.Ignore)]
        public List<FileInfo> AppFiles { get; set; }

        [JsonProperty("ExtraFiles", NullValueHandling = NullValueHandling.Ignore)]
        public ExtraFiles ExtraFiles { get; set; }

        [JsonProperty("RemovedFiles", NullValueHandling = NullValueHandling.Ignore)]
        public List<FileInfo> RemovedFiles { get; set; }
    }

    public partial class MiningCoin
    {
        [JsonProperty("coinTicker", NullValueHandling = NullValueHandling.Ignore)]
        public string CoinTicker { get; set; }

        [JsonProperty("coinName", NullValueHandling = NullValueHandling.Ignore)]
        public string CoinName { get; set; }

        [JsonProperty("algorithm", NullValueHandling = NullValueHandling.Ignore)]
        public string Algorithm { get; set; }

        [JsonProperty("marketDataSources", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> MarketDataSources { get; set; }

        [JsonProperty("poolName", NullValueHandling = NullValueHandling.Ignore)]
        public string PoolName { get; set; }

        [JsonProperty("poolFee", NullValueHandling = NullValueHandling.Ignore)]
        public decimal PoolFee { get; set; }

        [JsonProperty("poolHosts", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> PoolHosts { get; set; }

        [JsonProperty("stratumPort", NullValueHandling = NullValueHandling.Ignore)]
        public short? StratumPort { get; set; }

        [JsonProperty("stratumPortSSL", NullValueHandling = NullValueHandling.Ignore)]
        public short? StratumPortSsl { get; set; }

        [JsonProperty("depositAddressTrueMining", NullValueHandling = NullValueHandling.Ignore)]
        public string DepositAddressTrueMining { get; set; }

        [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
        public string Email { get; set; }

        [JsonProperty("password", NullValueHandling = NullValueHandling.Ignore)]
        public string Password { get; set; }

        [JsonProperty("shareCoef", NullValueHandling = NullValueHandling.Ignore)]
        public decimal ShareCoef { get; set; } = 1;

        [JsonProperty("shareMmc", NullValueHandling = NullValueHandling.Ignore)]
        public decimal ShareMmc { get; set; } = 1;

        [JsonProperty("defaultHashMuString", NullValueHandling = NullValueHandling.Ignore)]
        public string DefaultHashMuString { get; set; } = "H/s";

        [JsonProperty("defaultHashMuCoef", NullValueHandling = NullValueHandling.Ignore)]
        public int DefaultHashMuCoef { get; set; } = 1;
    }

    public partial class PaymentCoin
    {
        [JsonProperty("coinTicker", NullValueHandling = NullValueHandling.Ignore)]
        public string CoinTicker { get; set; }

        [JsonProperty("coinName", NullValueHandling = NullValueHandling.Ignore)]
        public string CoinName { get; set; }

        [JsonProperty("marketDataSources", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> MarketDataSources { get; set; }

        [JsonProperty("addressPatterns", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> AddressPatterns { get; set; }

        [JsonProperty("minPayout", NullValueHandling = NullValueHandling.Ignore)]
        public decimal MinPayout { get; set; }
    }

    public partial class ExtraFiles
    {
        [JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)]
        public List<FileInfo> Tools { get; set; }

        [JsonProperty("backendMiners", NullValueHandling = NullValueHandling.Ignore)]
        public BackendMiners BackendMiners { get; set; }
    }

    public partial class BackendMiners
    {
        [JsonProperty("common", NullValueHandling = NullValueHandling.Ignore)]
        public List<FileInfo> Common { get; set; }

        [JsonProperty("cpu", NullValueHandling = NullValueHandling.Ignore)]
        public List<FileInfo> Cpu { get; set; }

        [JsonProperty("opencl", NullValueHandling = NullValueHandling.Ignore)]
        public List<FileInfo> Opencl { get; set; }

        [JsonProperty("cuda", NullValueHandling = NullValueHandling.Ignore)]
        public List<FileInfo> Cuda { get; set; }
    }

    public partial class FileInfo
    {
        [JsonProperty("dlLink", NullValueHandling = NullValueHandling.Ignore)]
        public string DlLink { get; set; }

        [JsonProperty("directory")]
        public string Directory { get; set; }

        [JsonProperty("fileName", NullValueHandling = NullValueHandling.Ignore)]
        public string FileName { get; set; }

        [JsonProperty("sha256", NullValueHandling = NullValueHandling.Ignore)]
        public string Sha256 { get; set; }
    }

    public class SoftwareParameters
    {
        public static TrueMiningDesktopParameters ServerConfig;

        private static DateTime lastUpdated = DateTime.Now.AddHours(-1).AddMinutes(-1);

        public static void Update(Uri uri)
        {
            while (!Tools.IsConnected()) { Thread.Sleep(3000); }

            if (lastUpdated.AddHours(1).Ticks < DateTime.Now.Ticks)
            {
                bool trying = true;

                while (trying)
                {
                    Task updateParameters = new(() =>
                    {
                        lastUpdated = DateTime.Now;
                        try
                        {
                            SoftwareParameters.ServerConfig = JsonConvert.DeserializeObject<TrueMiningDesktopParameters>(Tools.HttpGet(uri.ToString(), Tools.UseTor), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture }); //update parameters
                            trying = false;
                        }
                        catch
                        {
                            try { Tools.AddFirewallRule("True Mining Desktop", System.Reflection.Assembly.GetExecutingAssembly().Location, true); Tools.UseTor = !Tools.UseTor; } catch { }
                        }
                    });
                    updateParameters.Start();
                    updateParameters.Wait(7000);
                }
            }
        }
    }
}