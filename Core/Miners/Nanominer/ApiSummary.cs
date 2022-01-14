using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueMiningDesktop.Core.Miners.Nanominer
{
    public partial class ApiSummary
    {
        [JsonProperty("Algorithms", NullValueHandling = NullValueHandling.Ignore)]
        public List<Dictionary<string, dynamic>> Algorithms { get; set; }

        [JsonProperty("Devices", NullValueHandling = NullValueHandling.Ignore)]
        public List<Dictionary<string, DeviceData>> Devices { get; set; }

        [JsonProperty("WorkTime", NullValueHandling = NullValueHandling.Ignore)]
        public long? WorkTimeSeconds { get; set; }
    }

    public partial class SharesInfo
    {
        [JsonProperty("Accepted", NullValueHandling = NullValueHandling.Ignore)]
        public long? Accepted { get; set; }

        [JsonProperty("Denied", NullValueHandling = NullValueHandling.Ignore)]
        public long? Denied { get; set; }

        [JsonProperty("Hashrate", NullValueHandling = NullValueHandling.Ignore)]
        public string Hashrate { get; set; }
    }

    public partial class DeviceData
    {
        [JsonProperty("Name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("Platform", NullValueHandling = NullValueHandling.Ignore)]
        public string Platform { get; set; }

        [JsonProperty("Pci", NullValueHandling = NullValueHandling.Ignore)]
        public string Pci { get; set; }

        [JsonProperty("Fan", NullValueHandling = NullValueHandling.Ignore)]
        public long? FanPercent { get; set; }

        [JsonProperty("Temperature", NullValueHandling = NullValueHandling.Ignore)]
        public long? TemperatureCelcius { get; set; }

        [JsonProperty("Power", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? PowerWatts { get; set; }
    }
}
