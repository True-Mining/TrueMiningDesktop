using System.Collections.Generic;

namespace True_Mining_Desktop.PoolAPI
{
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

    public class XMR_nanopool
    {
        public static Dictionary<int, int> hashrateHistory_user = new Dictionary<int, int>();
        public static Dictionary<int, int> hashrateHistory_tm = new Dictionary<int, int>();
        public static approximated_earnings approximated_earnings = new approximated_earnings();
    }
}