using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using TrueMiningDesktop.Core;

namespace TrueMiningDesktop.Saldo.ExternalApi
{
    internal class NanopoolData
    {
        internal static HashrateHistory GetHashrateHystory(string coin, string truemining_address, string user_address = null)
        {
            return JsonConvert.DeserializeObject<HashrateHistory>(Tools.HttpGet("http://api.nanopool.org/v1/" + coin + "/history/" + truemining_address + "/" + user_address), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture });
        }

		public partial class HashrateHistory
		{
			[JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
			public bool? Status { get; set; }

			[JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
			public List<Datum> Data { get; set; }
		}

		public partial class Datum
		{
			[JsonProperty("date", NullValueHandling = NullValueHandling.Ignore)]
			public long Date { get; set; }

			[JsonProperty("hashrate", NullValueHandling = NullValueHandling.Ignore)]
			public decimal Hashrate { get; set; }
		}

		public class GeneralInfo
		{
			public bool status { get; set; }
			public Data data { get; set; }
		}

		public class Data
		{
			public string account { get; set; }
			public decimal unconfirmed_balance { get; set; }
			public decimal balance { get; set; }
			public decimal hashrate { get; set; }
			public Avghashrate avgHashrate { get; set; }
			public Worker[] workers { get; set; }
		}

		public class Avghashrate
		{
			public decimal h1 { get; set; }
			public decimal h3 { get; set; }
			public decimal h6 { get; set; }
			public decimal h12 { get; set; }
			public decimal h24 { get; set; }
		}

		public class Worker
		{
			public string id { get; set; }
			public int uid { get; set; }
			public decimal hashrate { get; set; }
			public int lastshare { get; set; }
			public int rating { get; set; }
			public decimal h1 { get; set; }
			public decimal h3 { get; set; }
			public decimal h6 { get; set; }
			public decimal h12 { get; set; }
			public decimal h24 { get; set; }
		}

		public class AccountBalance
		{
			public bool status { get; set; }
			public decimal data { get; set; }
		}
	}
}