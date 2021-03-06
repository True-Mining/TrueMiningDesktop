﻿using System.Collections.Generic;

namespace TrueMiningDesktop.PoolAPI
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

    public class Crex24
    {
        public static Orderbook XMRBTC_Orderbook = new Orderbook();
        public static Orderbook MiningCoinBTC_Orderbook = new Orderbook();
    }
}