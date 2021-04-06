using System.Net;
using System.Text.Json;
using TruePayment.Crex24.Objects;

namespace TruePayment.Crex24
{
    internal class Crex24Data
    {
        public Orderbook GetHashrateHistory(string coin, string pair, int levels = 100)
        {
            return JsonSerializer.Deserialize<Orderbook>(new WebClient().DownloadString("https://api.crex24.com/v2/public/orderBook?instrument=" + coin + "-" + pair + "&limit=" + levels));
        }
    }
}