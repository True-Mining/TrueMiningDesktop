namespace TrueMiningDesktop.Nanopool.Objects
{
    public class HashrateHistory
    {
        public bool status { get; set; }
        public Datum[] data { get; set; }
    }

    public class Datum
    {
        public int date { get; set; }
        public int hashrate { get; set; }
    }
}