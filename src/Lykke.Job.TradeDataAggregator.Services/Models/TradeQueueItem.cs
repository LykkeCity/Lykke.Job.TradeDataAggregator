using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lykke.Job.TradeDataAggregator.Services.Models
{
    public class TradeQueueItem
    {
        [JsonProperty("order")]
        public MarketOrder Order { get; set; }

        [JsonProperty("trades")]
        public List<TradeInfo> Trades { get; set; }

        public class MarketOrder
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("externalId")]
            public string ExternalId { get; set; }

            [JsonProperty("assetPairId")]
            public string AssetPairId { get; set; }

            [JsonProperty("clientId")]
            public string ClientId { get; set; }

            [JsonProperty("volume")]
            public double Volume { get; set; }

            [JsonProperty("price")]
            public double? Price { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("createdAt")]
            public DateTime CreatedAt { get; set; }

            [JsonProperty("registered")]
            public DateTime Registered { get; set; }

            [JsonProperty("matchedAt")]
            public DateTime? MatchedAt { get; set; }

            [JsonProperty("straight")]
            public bool Straight { get; set; }

            [JsonProperty("reservedLimitVolume")]
            public double ReservedLimitVolume { get; set; }

            [JsonProperty("dustSize")]
            public double? DustSize { get; set; }
        }

        public class TradeInfo
        {
            [JsonProperty("price")]
            public double? Price { get; set; }

            [JsonProperty("limitOrderId")]
            public string LimitOrderId { get; set; }

            [JsonProperty("limitOrderExternalId")]
            public string LimitOrderExternalId { get; set; }

            [JsonProperty("timestamp")]
            public DateTime Timestamp { get; set; }

            [JsonProperty("marketClientId")]
            public string MarketClientId { get; set; }

            [JsonProperty("marketAsset")]
            public string MarketAsset { get; set; }

            [JsonProperty("marketVolume")]
            public double MarketVolume { get; set; }

            [JsonProperty("limitClientId")]
            public string LimitClientId { get; set; }

            [JsonProperty("limitVolume")]
            public double LimitVolume { get; set; }

            [JsonProperty("limitAsset")]
            public string LimitAsset { get; set; }
        }
    }
}
