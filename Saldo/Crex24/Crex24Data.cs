using System.Net;
using System.Text.Json;
using TrueMiningDesktop.Core;
using TrueMiningDesktop.PoolAPI;

namespace TrueMiningDesktop.Crex24
{
    internal class Crex24Data
    {
        public static Orderbook GetHashrateHistory(string coin, string pair, int levels = 100)
        {
            return JsonSerializer.Deserialize<Orderbook>(new WebClient() { Proxy = TrueMiningDesktop.User.Settings.User.UseTorSharpOnMining ? Tools.TorProxy : null, }.DownloadString("https://api.crex24.com/v2/public/orderBook?instrument=" + coin + "-" + pair + "&limit=" + levels));
        }
    }
}