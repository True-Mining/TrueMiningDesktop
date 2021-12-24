using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        [JsonProperty("TrueMiningFiles", NullValueHandling = NullValueHandling.Ignore)]
        public TrueMiningFiles TrueMiningFiles { get; set; }

        [JsonProperty("ThirdPartyBinaries", NullValueHandling = NullValueHandling.Ignore)]
        public ThirdPartyBinaries ThirdPartyBinaries { get; set; }

        [JsonProperty("MarkAsOldFiles", NullValueHandling = NullValueHandling.Ignore)]
        public MarkAsOldFiles MarkAsOldFiles { get; set; }
    }

    public partial class MiningCoin
    {
        [JsonProperty("coin", NullValueHandling = NullValueHandling.Ignore)]
        public string Coin { get; set; }

        [JsonProperty("coinName", NullValueHandling = NullValueHandling.Ignore)]
        public string CoinName { get; set; }
        [JsonProperty("algorithm", NullValueHandling = NullValueHandling.Ignore)]
        public string Algorithm { get; set; }

        [JsonProperty("poolName", NullValueHandling = NullValueHandling.Ignore)]
        public string PoolName { get; set; }

        [JsonProperty("poolFee", NullValueHandling = NullValueHandling.Ignore)]
        public decimal PoolFee { get; set; }

        [JsonProperty("hosts", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Hosts { get; set; }

        [JsonProperty("stratumPort", NullValueHandling = NullValueHandling.Ignore)]
        public short? StratumPort { get; set; }

        [JsonProperty("stratumPortSSL", NullValueHandling = NullValueHandling.Ignore)]
        public short? StratumPortSsl { get; set; }

        [JsonProperty("wallet_TM", NullValueHandling = NullValueHandling.Ignore)]
        public string WalletTm { get; set; }

        [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
        public string Email { get; set; }

        [JsonProperty("password", NullValueHandling = NullValueHandling.Ignore)]
        public string Password { get; set; }
    }

    public partial class PaymentCoin
    {
        [JsonProperty("coinTicker", NullValueHandling = NullValueHandling.Ignore)]
        public string CoinTicker { get; set; }

        [JsonProperty("coinName", NullValueHandling = NullValueHandling.Ignore)]
        public string CoinName { get; set; }

        [JsonProperty("addressPatterns", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> AddressPatterns { get; set; }

        [JsonProperty("minPayout", NullValueHandling = NullValueHandling.Ignore)]
        public decimal MinPayout { get; set; }
    }

    public partial class ThirdPartyBinaries
    {
        [JsonProperty("files", NullValueHandling = NullValueHandling.Ignore)]
        public List<FileToDownload> Files { get; set; }
    }

    public partial class MarkAsOldFiles
    {
        [JsonProperty("files", NullValueHandling = NullValueHandling.Ignore)]
        public List<FileToDownload> Files { get; set; }
    }

    public partial class FileToDownload
    {
        [JsonProperty("dlLink", NullValueHandling = NullValueHandling.Ignore)]
        public string DlLink { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("fileName", NullValueHandling = NullValueHandling.Ignore)]
        public string FileName { get; set; }

        [JsonProperty("sha256", NullValueHandling = NullValueHandling.Ignore)]
        public string Sha256 { get; set; }
    }

    public partial class TrueMiningFiles
    {
        [JsonProperty("changelogLink", NullValueHandling = NullValueHandling.Ignore)]
        public Uri ChangelogLink { get; set; }

        [JsonProperty("files", NullValueHandling = NullValueHandling.Ignore)]
        public List<FileToDownload> Files { get; set; }
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
                            SoftwareParameters.ServerConfig = JsonConvert.DeserializeObject<TrueMiningDesktopParameters>(Tools.HttpGet(uri.ToString(), Tools.UseTor)); //update parameters
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