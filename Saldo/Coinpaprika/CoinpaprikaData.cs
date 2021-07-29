using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using TruePayment.Coinpaprika.Objects;

namespace TruePayment.Coinpaprika
{
    internal class CoinpaprikaData
    {
        public static List<OHLCV> XMR_BTC_ohlcv = new List<OHLCV>();
        public static List<OHLCV> COIN_BTC_ohlcv = new List<OHLCV>();
    }
}