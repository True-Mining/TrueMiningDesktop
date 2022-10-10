using System.Collections.Generic;

namespace TrueMiningDesktop.ExternalApi
{
	public class ExchangeOrderbooks
	{
		public static Orderbook XMRBTC = new Orderbook();
		public static Orderbook RVNBTC = new Orderbook();
		public static Orderbook ETCBTC = new Orderbook();
		public static Orderbook PaymentCoinBTC = new Orderbook();
	}

	public class Orderbook
	{
		public List<BuyLevel> buyLevels { get; set; }
		public List<SellLevel> sellLevels { get; set; }
	}

	public class BuyLevel
	{
		public decimal price { get; set; }
		public decimal volume { get; set; }
	}

	public class SellLevel
	{
		public decimal price { get; set; }
		public decimal volume { get; set; }
	}

	public class BitcoinPrice
	{
		public static decimal BTCUSD { get; set; }
	}
}