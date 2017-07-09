using System;
using Lykke.Job.TradeDataAggregator.Core.Domain.Exchange;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.TradeDataAggregator.AzureRepositories.Exchange
{
    public class MarketDataEntity : TableEntity, IMarketData
    {
        public string AssetPairId { get; set; }
        public double Volume { get; set; }
        public double LastPrice { get; set; }
        public DateTime Dt { get; set; }

        public static string GeneratePartition()
        {
            return "md";
        }

        public static string GenerateRowKey(string assetPairId)
        {
            return assetPairId;
        }

        public static MarketDataEntity Create(IMarketData md)
        {
            return new MarketDataEntity
            {
                AssetPairId = md.AssetPairId,
                Dt = md.Dt,
                LastPrice = md.LastPrice,
                Volume = md.Volume,
                PartitionKey = GeneratePartition(),
                RowKey = GenerateRowKey(md.AssetPairId)
            };
        }
    }
}