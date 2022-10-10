using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using TrueMiningDesktop.Core;

namespace TrueMiningDesktop.ExternalApi
{
	internal class NanopoolData
	{
		internal static HashrateHistory GetHashrateHystory(string coin, string truemining_address, string user_address = null)
		{
			return JsonConvert.DeserializeObject<HashrateHistory>(Tools.HttpGet("http://api.nanopool.org/v1/" + coin + "/history/" + truemining_address + "/" + user_address), new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture });
		}


		public class XMR_nanopool
		{
			public static Dictionary<long, decimal> hashrateHistory_user = new Dictionary<long, decimal>();
			public static Dictionary<long, decimal> hashrateHistory_tm = new Dictionary<long, decimal>();
			public static approximated_earnings approximated_earnings = new approximated_earnings();
			public static share_coefficient sharecoef = new share_coefficient();
			public static Dictionary<long, decimal> pointsHistory_user = new Dictionary<long, decimal>();
		}

		public class RVN_nanopool
		{
			public static Dictionary<long, decimal> hashrateHistory_user = new Dictionary<long, decimal>();
			public static Dictionary<long, decimal> hashrateHistory_tm = new Dictionary<long, decimal>();
			public static approximated_earnings approximated_earnings = new approximated_earnings();
			public static share_coefficient sharecoef = new share_coefficient();
			public static Dictionary<long, decimal> pointsHistory_user = new Dictionary<long, decimal>();
		}

		public class ETC_nanopool
		{
			public static Dictionary<long, decimal> hashrateHistory_user = new Dictionary<long, decimal>();
			public static Dictionary<long, decimal> hashrateHistory_tm = new Dictionary<long, decimal>();
			public static approximated_earnings approximated_earnings = new approximated_earnings();
			public static share_coefficient sharecoef = new share_coefficient();
			public static Dictionary<long, decimal> pointsHistory_user = new Dictionary<long, decimal>();
		}
	}
	public class Minute
	{
		public decimal coins { get; set; } = 0;
		public decimal dollars { get; set; } = 0;
		public decimal yuan { get; set; } = 0;
		public decimal euros { get; set; } = 0;
		public decimal rubles { get; set; } = 0;
		public decimal bitcoins { get; set; } = 0;
		public decimal pounds { get; set; } = 0;
	}

	public class Hour
	{
		public decimal coins { get; set; } = 0;
		public decimal dollars { get; set; } = 0;
		public decimal yuan { get; set; } = 0;
		public decimal euros { get; set; } = 0;
		public decimal rubles { get; set; } = 0;
		public decimal bitcoins { get; set; } = 0;
		public decimal pounds { get; set; } = 0;
	}

	public class Day
	{
		public decimal coins { get; set; } = 0;
		public decimal dollars { get; set; } = 0;
		public decimal yuan { get; set; } = 0;
		public decimal euros { get; set; } = 0;
		public decimal rubles { get; set; } = 0;
		public decimal bitcoins { get; set; } = 0;
		public decimal pounds { get; set; } = 0;
	}

	public class Week
	{
		public decimal coins { get; set; } = 0;
		public decimal dollars { get; set; } = 0;
		public decimal yuan { get; set; } = 0;
		public decimal euros { get; set; } = 0;
		public decimal rubles { get; set; } = 0;
		public decimal bitcoins { get; set; } = 0;
		public decimal pounds { get; set; } = 0;
	}

	public class Month
	{
		public decimal coins { get; set; } = 0;
		public decimal dollars { get; set; } = 0;
		public decimal yuan { get; set; } = 0;
		public decimal euros { get; set; } = 0;
		public decimal rubles { get; set; } = 0;
		public decimal bitcoins { get; set; } = 0;
		public decimal pounds { get; set; } = 0;
	}

	public class Prices
	{
		public decimal price_btc { get; set; } = 0;
		public decimal price_usd { get; set; } = 0;
		public decimal price_eur { get; set; } = 0;
		public decimal price_rur { get; set; } = 0;
		public decimal price_cny { get; set; } = 0;
		public decimal price_gbp { get; set; } = 0;
	}

	public class data_approximated_earnings
	{
		public Minute minute = new Minute();
		public Hour hour = new Hour();
		public Day day = new Day();
		public Week week = new Week();
		public Month month = new Month();
		public Prices prices = new Prices();
	}

	public class approximated_earnings
	{
		public bool status { get; set; } = true;
		public data_approximated_earnings data = new();
	}

	public class share_coefficient
	{
		public bool status { get; set; } = true;
		public decimal data { get; set; } = (decimal)52.5;
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
		public DataGeneralInfo data { get; set; }
	}

	public class DataGeneralInfo
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