using Newtonsoft.Json;
using System.Globalization;
using System.Text.Json;
using TrueMiningDesktop.Core;
using TruePayment.Nanopool.Objects;

namespace TruePayment.Nanopool
{
    internal class NanopoolData
    {
        internal static HashrateHistory GetHashrateHystory(string coin, string truemining_address, string user_address = null)
        {
            return JsonConvert.DeserializeObject<HashrateHistory>(Tools.HttpGet("http://api.nanopool.org/v1/" + coin + "/history/" + truemining_address + "/" + user_address), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture });
        }
    }
}