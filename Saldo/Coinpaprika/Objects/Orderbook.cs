using System;

namespace TrueMiningDesktop.Coinpaprika.Objects
{
    public class OHLCV
    {
        public DateTime time_open { get; set; }
        public DateTime time_close { get; set; }
        public decimal open { get; set; }
        public decimal high { get; set; }
        public decimal low { get; set; }
        public decimal close { get; set; }
        public decimal volume { get; set; }
        public decimal market_cap { get; set; }
    }
}