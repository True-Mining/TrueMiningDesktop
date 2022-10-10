namespace TrueMiningDesktop.ExternalApi
{
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;
	using System.Globalization;

	public partial class CoinData
	{
		[JsonProperty("btc", NullValueHandling = NullValueHandling.Ignore)]
		public decimal Btc { get; set; }

		[JsonProperty("btc_24h_change", NullValueHandling = NullValueHandling.Ignore)]
		public decimal? Btc24HChange { get; set; }

		[JsonProperty("last_updated_at", NullValueHandling = NullValueHandling.Ignore)]
		public long LastUpdatedAt { get; set; }
	}

	internal static class Converter
	{
		public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
		{
			MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
			DateParseHandling = DateParseHandling.None,
			Converters =
			{
				new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
			},
		};
	}
}