using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueMiningDesktop.Core.Miners.TRex
{
    public partial class ApiSummary
    {
        [JsonProperty("accepted_count", NullValueHandling = NullValueHandling.Ignore)]
        public long? AcceptedCount { get; set; }

        [JsonProperty("active_pool", NullValueHandling = NullValueHandling.Ignore)]
        public ActivePool ActivePool { get; set; }

        [JsonProperty("algorithm", NullValueHandling = NullValueHandling.Ignore)]
        public string Algorithm { get; set; }

        [JsonProperty("api", NullValueHandling = NullValueHandling.Ignore)]
        public string Api { get; set; }

        [JsonProperty("build_date", NullValueHandling = NullValueHandling.Ignore)]
        public string BuildDate { get; set; }

        [JsonProperty("coin", NullValueHandling = NullValueHandling.Ignore)]
        public string Coin { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("driver", NullValueHandling = NullValueHandling.Ignore)]
        public string Driver { get; set; }

        [JsonProperty("gpu_total", NullValueHandling = NullValueHandling.Ignore)]
        public long? GpuTotal { get; set; }

        [JsonProperty("gpus", NullValueHandling = NullValueHandling.Ignore)]
        public List<Gpus> Gpus { get; set; }

        [JsonProperty("hashrate", NullValueHandling = NullValueHandling.Ignore)]
        public long? Hashrate { get; set; }

        [JsonProperty("hashrate_day", NullValueHandling = NullValueHandling.Ignore)]
        public long? HashrateDay { get; set; }

        [JsonProperty("hashrate_hour", NullValueHandling = NullValueHandling.Ignore)]
        public long? HashrateHour { get; set; }

        [JsonProperty("hashrate_minute", NullValueHandling = NullValueHandling.Ignore)]
        public long? HashrateMinute { get; set; }

        [JsonProperty("invalid_count", NullValueHandling = NullValueHandling.Ignore)]
        public long? InvalidCount { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("os", NullValueHandling = NullValueHandling.Ignore)]
        public string Os { get; set; }

        [JsonProperty("paused", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Paused { get; set; }

        [JsonProperty("rejected_count", NullValueHandling = NullValueHandling.Ignore)]
        public long? RejectedCount { get; set; }

        [JsonProperty("revision", NullValueHandling = NullValueHandling.Ignore)]
        public string Revision { get; set; }

        [JsonProperty("sharerate", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Sharerate { get; set; }

        [JsonProperty("sharerate_average", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? SharerateAverage { get; set; }

        [JsonProperty("solved_count", NullValueHandling = NullValueHandling.Ignore)]
        public long? SolvedCount { get; set; }

        [JsonProperty("success", NullValueHandling = NullValueHandling.Ignore)]
        public long? Success { get; set; }

        [JsonProperty("time", NullValueHandling = NullValueHandling.Ignore)]
        public long? Time { get; set; }

        [JsonProperty("uptime", NullValueHandling = NullValueHandling.Ignore)]
        public long? Uptime { get; set; }

        [JsonProperty("validate_shares", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ValidateShares { get; set; }

        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string Version { get; set; }

        [JsonProperty("watchdog_stat", NullValueHandling = NullValueHandling.Ignore)]
        public WatchdogStat WatchdogStat { get; set; }
    }

    public partial class ActivePool
    {
        [JsonProperty("difficulty", NullValueHandling = NullValueHandling.Ignore)]
        public string Difficulty { get; set; }

        [JsonProperty("dns_https_server", NullValueHandling = NullValueHandling.Ignore)]
        public string DnsHttpsServer { get; set; }

        [JsonProperty("last_submit_ts", NullValueHandling = NullValueHandling.Ignore)]
        public long? LastSubmitTs { get; set; }

        [JsonProperty("ping", NullValueHandling = NullValueHandling.Ignore)]
        public long? Ping { get; set; }

        [JsonProperty("proxy", NullValueHandling = NullValueHandling.Ignore)]
        public string Proxy { get; set; }

        [JsonProperty("retries", NullValueHandling = NullValueHandling.Ignore)]
        public long? Retries { get; set; }

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public string Url { get; set; }

        [JsonProperty("user", NullValueHandling = NullValueHandling.Ignore)]
        public string User { get; set; }

        [JsonProperty("worker", NullValueHandling = NullValueHandling.Ignore)]
        public string Worker { get; set; }
    }

    public partial class Gpus
    {
        [JsonProperty("cclock", NullValueHandling = NullValueHandling.Ignore)]
        public long? Cclock { get; set; }

        [JsonProperty("dag_build_mode", NullValueHandling = NullValueHandling.Ignore)]
        public long? DagBuildMode { get; set; }

        [JsonProperty("device_id", NullValueHandling = NullValueHandling.Ignore)]
        public long? DeviceId { get; set; }

        [JsonProperty("efficiency", NullValueHandling = NullValueHandling.Ignore)]
        public string Efficiency { get; set; }

        [JsonProperty("fan_speed", NullValueHandling = NullValueHandling.Ignore)]
        public long? FanSpeed { get; set; }

        [JsonProperty("gpu_id", NullValueHandling = NullValueHandling.Ignore)]
        public long? GpuId { get; set; }

        [JsonProperty("gpu_user_id", NullValueHandling = NullValueHandling.Ignore)]
        public long? GpuUserId { get; set; }

        [JsonProperty("hashrate", NullValueHandling = NullValueHandling.Ignore)]
        public long? Hashrate { get; set; }

        [JsonProperty("hashrate_day", NullValueHandling = NullValueHandling.Ignore)]
        public long? HashrateDay { get; set; }

        [JsonProperty("hashrate_hour", NullValueHandling = NullValueHandling.Ignore)]
        public long? HashrateHour { get; set; }

        [JsonProperty("hashrate_instant", NullValueHandling = NullValueHandling.Ignore)]
        public long? HashrateInstant { get; set; }

        [JsonProperty("hashrate_minute", NullValueHandling = NullValueHandling.Ignore)]
        public long? HashrateMinute { get; set; }

        [JsonProperty("intensity", NullValueHandling = NullValueHandling.Ignore)]
        public long? Intensity { get; set; }

        [JsonProperty("lhr_tune", NullValueHandling = NullValueHandling.Ignore)]
        public long? LhrTune { get; set; }

        [JsonProperty("low_load", NullValueHandling = NullValueHandling.Ignore)]
        public bool? LowLoad { get; set; }

        [JsonProperty("mclock", NullValueHandling = NullValueHandling.Ignore)]
        public long? Mclock { get; set; }

        [JsonProperty("mtweak", NullValueHandling = NullValueHandling.Ignore)]
        public long? Mtweak { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("paused", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Paused { get; set; }

        [JsonProperty("pci_bus", NullValueHandling = NullValueHandling.Ignore)]
        public long? PciBus { get; set; }

        [JsonProperty("pci_domain", NullValueHandling = NullValueHandling.Ignore)]
        public long? PciDomain { get; set; }

        [JsonProperty("pci_id", NullValueHandling = NullValueHandling.Ignore)]
        public long? PciId { get; set; }

        [JsonProperty("potentially_unstable", NullValueHandling = NullValueHandling.Ignore)]
        public bool? PotentiallyUnstable { get; set; }

        [JsonProperty("power", NullValueHandling = NullValueHandling.Ignore)]
        public long? Power { get; set; }

        [JsonProperty("power_avr", NullValueHandling = NullValueHandling.Ignore)]
        public long? PowerAvr { get; set; }

        [JsonProperty("shares", NullValueHandling = NullValueHandling.Ignore)]
        public Shares Shares { get; set; }

        [JsonProperty("temperature", NullValueHandling = NullValueHandling.Ignore)]
        public long? Temperature { get; set; }

        [JsonProperty("uuid", NullValueHandling = NullValueHandling.Ignore)]
        public string Uuid { get; set; }

        [JsonProperty("vendor", NullValueHandling = NullValueHandling.Ignore)]
        public string Vendor { get; set; }
    }

    public partial class Shares
    {
        [JsonProperty("accepted_count", NullValueHandling = NullValueHandling.Ignore)]
        public long? AcceptedCount { get; set; }

        [JsonProperty("invalid_count", NullValueHandling = NullValueHandling.Ignore)]
        public long? InvalidCount { get; set; }

        [JsonProperty("last_share_diff", NullValueHandling = NullValueHandling.Ignore)]
        public long? LastShareDiff { get; set; }

        [JsonProperty("last_share_submit_ts", NullValueHandling = NullValueHandling.Ignore)]
        public long? LastShareSubmitTs { get; set; }

        [JsonProperty("max_share_diff", NullValueHandling = NullValueHandling.Ignore)]
        public long? MaxShareDiff { get; set; }

        [JsonProperty("max_share_submit_ts", NullValueHandling = NullValueHandling.Ignore)]
        public long? MaxShareSubmitTs { get; set; }

        [JsonProperty("rejected_count", NullValueHandling = NullValueHandling.Ignore)]
        public long? RejectedCount { get; set; }

        [JsonProperty("solved_count", NullValueHandling = NullValueHandling.Ignore)]
        public long? SolvedCount { get; set; }
    }

    public partial class WatchdogStat
    {
        [JsonProperty("built_in", NullValueHandling = NullValueHandling.Ignore)]
        public bool? BuiltIn { get; set; }

        [JsonProperty("startup_ts", NullValueHandling = NullValueHandling.Ignore)]
        public long? StartupTs { get; set; }

        [JsonProperty("total_restarts", NullValueHandling = NullValueHandling.Ignore)]
        public long? TotalRestarts { get; set; }

        [JsonProperty("uptime", NullValueHandling = NullValueHandling.Ignore)]
        public long? Uptime { get; set; }

        [JsonProperty("wd_version", NullValueHandling = NullValueHandling.Ignore)]
        public string WdVersion { get; set; }
    }
}
