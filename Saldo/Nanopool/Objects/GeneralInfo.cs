namespace TruePayment.Nanopool.Objects
{
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
}