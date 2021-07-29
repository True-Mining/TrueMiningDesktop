using System.Text.Json;
using TrueMiningDesktop.Core;
using TrueMiningDesktop.Nanopool.Objects;

namespace TrueMiningDesktop.Nanopool
{
    internal class NanopoolData
    {
        internal static HashrateHistory GetHashrateHystory(string coin, string truemining_address, string user_address = null)
        {
            return JsonSerializer.Deserialize<HashrateHistory>(Tools.HttpGet("https://api.nanopool.org/v1/" + coin + "/history/" + truemining_address + "/" + user_address));
        }

        public static AccountBalance GetConfirmedBalance(string coin, string truemining_address)
        {
            return JsonSerializer.Deserialize<AccountBalance>(Tools.HttpGet("https://api.nanopool.org/v1/" + coin + "/balance/" + truemining_address));
        }

        public static GeneralInfo GetGeneralInfo(string coin, string truemining_address, string user_address = null)
        {
            return JsonSerializer.Deserialize<GeneralInfo>(Tools.HttpGet("https://api.nanopool.org/v1/" + coin + "/user/" + truemining_address + "/" + user_address));
        }
    }
}