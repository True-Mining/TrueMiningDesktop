namespace TruePayment.Nanopool.Objects
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public partial class HashrateHistory
    {
        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Status { get; set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public List<Datum> Data { get; set; }
    }

    public partial class Datum
    {
        [JsonProperty("date", NullValueHandling = NullValueHandling.Ignore)]
        public long Date { get; set; }

        [JsonProperty("hashrate", NullValueHandling = NullValueHandling.Ignore)]
        public decimal Hashrate { get; set; }
    }
}