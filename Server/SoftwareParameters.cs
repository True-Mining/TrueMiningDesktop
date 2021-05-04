using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using True_Mining_Desktop.Core;

namespace True_Mining_Desktop.Server
{
    public partial class TrueMiningDesktopParameters
    {
        [JsonProperty("Pools", NullValueHandling = NullValueHandling.Ignore)]
        public List<Pool> Pools { get; set; }

        [JsonProperty("MiningCoins", NullValueHandling = NullValueHandling.Ignore)]
        public List<MiningCoin> MiningCoins { get; set; }

        [JsonProperty("hosts", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Hosts { get; set; }

        [JsonProperty("DynamicFee", NullValueHandling = NullValueHandling.Ignore)]
        public decimal DynamicFee { get; set; }

        [JsonProperty("TrueMiningFiles", NullValueHandling = NullValueHandling.Ignore)]
        public TrueMiningFiles TrueMiningFiles { get; set; }

        [JsonProperty("ThirdPartyBinaries", NullValueHandling = NullValueHandling.Ignore)]
        public ThirdPartyBinaries ThirdPartyBinaries { get; set; }
    }

    public partial class MiningCoin
    {
        [JsonProperty("coin", NullValueHandling = NullValueHandling.Ignore)]
        public string Coin { get; set; }

        [JsonProperty("coinName", NullValueHandling = NullValueHandling.Ignore)]
        public string CoinName { get; set; }

        [JsonProperty("poolName", NullValueHandling = NullValueHandling.Ignore)]
        public string PoolName { get; set; }

        [JsonProperty("hosts", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Hosts { get; set; }

        [JsonProperty("stratumPort", NullValueHandling = NullValueHandling.Ignore)]
        public Int16? StratumPort { get; set; }

        [JsonProperty("stratumPortSSL", NullValueHandling = NullValueHandling.Ignore)]
        public Int16? StratumPortSsl { get; set; }

        [JsonProperty("wallet_TM", NullValueHandling = NullValueHandling.Ignore)]
        public string WalletTm { get; set; }

        [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
        public string Email { get; set; }

        [JsonProperty("password", NullValueHandling = NullValueHandling.Ignore)]
        public string Password { get; set; }
    }

    public partial class Pool
    {
        [JsonProperty("mineableCoin", NullValueHandling = NullValueHandling.Ignore)]
        public string MineableCoin { get; set; }

        [JsonProperty("pool", NullValueHandling = NullValueHandling.Ignore)]
        public string PoolPool { get; set; }

        [JsonProperty("host1", NullValueHandling = NullValueHandling.Ignore)]
        public string Host1 { get; set; }

        [JsonProperty("host2", NullValueHandling = NullValueHandling.Ignore)]
        public string Host2 { get; set; }

        [JsonProperty("stratumPort", NullValueHandling = NullValueHandling.Ignore)]
        public long? StratumPort { get; set; }

        [JsonProperty("stratumPortSSl", NullValueHandling = NullValueHandling.Ignore)]
        public long? StratumPortSSl { get; set; }

        [JsonProperty("wallet_TM", NullValueHandling = NullValueHandling.Ignore)]
        public string WalletTm { get; set; }

        [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
        public string Email { get; set; }

        [JsonProperty("password", NullValueHandling = NullValueHandling.Ignore)]
        public string Password { get; set; }
    }

    public partial class ThirdPartyBinaries
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
                    lastUpdated = DateTime.Now;
                    try
                    {
                        SoftwareParameters.ServerConfig = JsonConvert.DeserializeObject<TrueMiningDesktopParameters>(new WebClient().DownloadString(uri)); //update parameters
                        trying = false;
                    }
                    catch { }
                }
            }
        }
    }
}