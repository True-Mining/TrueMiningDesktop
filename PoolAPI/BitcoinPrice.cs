using Newtonsoft.Json;

namespace True_Mining_Desktop.PoolAPI
{
    public class FIAT
    {
        [JsonProperty("last")]
        public double Last { get; set; } = 0;

        [JsonProperty("buy")]
        public double Buy { get; set; } = 0;

        [JsonProperty("sell")]
        public double Sell { get; set; } = 0;

        [JsonProperty("symbol")]
        public string Symbol { get; set; } = null;
    }

    public class Coins
    {
        [JsonProperty("USD")]
        public FIAT USD = new FIAT();

        [JsonProperty("AUD")]
        public FIAT AUD = new FIAT();

        [JsonProperty("BRL")]
        public FIAT BRL = new FIAT();

        [JsonProperty("CAD")]
        public FIAT CAD = new FIAT();

        [JsonProperty("CHF")]
        public FIAT CHF = new FIAT();

        [JsonProperty("CLP")]
        public FIAT CLP = new FIAT();

        [JsonProperty("CNY")]
        public FIAT CNY = new FIAT();

        [JsonProperty("DKK")]
        public FIAT DKK = new FIAT();

        [JsonProperty("EUR")]
        public FIAT EUR = new FIAT();

        [JsonProperty("GBP")]
        public FIAT GBP = new FIAT();

        [JsonProperty("HKD")]
        public FIAT HKD = new FIAT();

        [JsonProperty("INR")]
        public FIAT INR = new FIAT();

        [JsonProperty("ISK")]
        public FIAT ISK = new FIAT();

        [JsonProperty("JPY")]
        public FIAT JPY = new FIAT();

        [JsonProperty("KRW")]
        public FIAT KRW = new FIAT();

        [JsonProperty("NZD")]
        public FIAT NZD = new FIAT();

        [JsonProperty("PLN")]
        public FIAT PLN = new FIAT();

        [JsonProperty("RUB")]
        public FIAT RUB = new FIAT();

        [JsonProperty("SEK")]
        public FIAT SEK = new FIAT();

        [JsonProperty("SGD")]
        public FIAT SGD = new FIAT();

        [JsonProperty("THB")]
        public FIAT THB = new FIAT();

        [JsonProperty("TRY")]
        public FIAT TRY = new FIAT();

        [JsonProperty("TWD")]
        public FIAT TWD = new FIAT();
    }

    public class BitcoinPrice
    {
        public static Coins FIAT_rates = new Coins();
    }
}