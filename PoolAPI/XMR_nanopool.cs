namespace True_Mining_v4.PoolAPI
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Minute
    {
        public double coins { get; set; } = 0;
        public double dollars { get; set; } = 0;
        public double yuan { get; set; } = 0;
        public double euros { get; set; } = 0;
        public double rubles { get; set; } = 0;
        public double bitcoins { get; set; } = 0;
        public double pounds { get; set; } = 0;
    }

    public class Hour
    {
        public double coins { get; set; } = 0;
        public double dollars { get; set; } = 0;
        public double yuan { get; set; } = 0;
        public double euros { get; set; } = 0;
        public double rubles { get; set; } = 0;
        public double bitcoins { get; set; } = 0;
        public double pounds { get; set; } = 0;
    }

    public class Day
    {
        public double coins { get; set; } = 0;
        public double dollars { get; set; } = 0;
        public double yuan { get; set; } = 0;
        public double euros { get; set; } = 0;
        public double rubles { get; set; } = 0;
        public double bitcoins { get; set; } = 0;
        public double pounds { get; set; } = 0;
    }

    public class Week
    {
        public double coins { get; set; } = 0;
        public double dollars { get; set; } = 0;
        public double yuan { get; set; } = 0;
        public double euros { get; set; } = 0;
        public double rubles { get; set; } = 0;
        public double bitcoins { get; set; } = 0;
        public double pounds { get; set; } = 0;
    }

    public class Month
    {
        public double coins { get; set; } = 0;
        public double dollars { get; set; } = 0;
        public double yuan { get; set; } = 0;
        public double euros { get; set; } = 0;
        public double rubles { get; set; } = 0;
        public double bitcoins { get; set; } = 0;
        public double pounds { get; set; } = 0;
    }

    public class Prices
    {
        public double price_btc { get; set; } = 0;
        public double price_usd { get; set; } = 0;
        public double price_eur { get; set; } = 0;
        public double price_rur { get; set; } = 0;
        public double price_cny { get; set; } = 0;
        public double price_gbp { get; set; } = 0;
    }

    public class Data
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
        public Data data = new Data();
    }

    public class XMR_nanopool_avghashratelimited
    {
        public bool status { get; set; } = true;
        private double datA = 0;

        public double data
        {
            get { return datA; }
            set { datA = System.Convert.ToDouble(value); }
        }
    }

    public class XMR_nanopool
    {
        public static XMR_nanopool_avghashratelimited AvghashratelimitedAll = new XMR_nanopool_avghashratelimited();
        public static XMR_nanopool_avghashratelimited AvghashratelimitedThisworker = new XMR_nanopool_avghashratelimited();
        public static approximated_earnings approximated_earnings = new approximated_earnings();
    }
}