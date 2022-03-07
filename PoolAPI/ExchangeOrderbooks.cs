using System.Collections.Generic;

namespace TrueMiningDesktop.ExternalApi
{
    public class BuyLevel
    {
        public decimal price { get; set; } = 1;
        public decimal volume { get; set; } = 0;
    }

    public class SellLevel
    {
        public decimal price { get; set; } = 1;
        public decimal volume { get; set; } = 0;
    }

    public class Orderbook
    {
        public List<BuyLevel> buyLevels = new List<BuyLevel>();
        public List<SellLevel> sellLevels = new List<SellLevel>();
    }

    public class ExchangeOrderbooks
    {
        public static Orderbook XMRBTC = new Orderbook();
        public static Orderbook RVNBTC = new Orderbook();
        public static Orderbook ETCBTC = new Orderbook();
        public static Orderbook PaymentCoinBTC = new Orderbook();
    }
}