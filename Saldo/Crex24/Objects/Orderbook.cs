using System.Collections.Generic;

namespace TruePayment.Crex24.Objects
{
    public class Orderbook
    {
        public List<Buylevel> buyLevels { get; set; }
        public List<Selllevel> sellLevels { get; set; }
    }

    public class Buylevel
    {
        public decimal price { get; set; }
        public decimal volume { get; set; }
    }

    public class Selllevel
    {
        public decimal price { get; set; }
        public decimal volume { get; set; }
    }
}