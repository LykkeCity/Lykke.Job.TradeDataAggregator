using System;

namespace Lykke.Job.TradeDataAggregator.Core.Domain.Exchange
{
    public class MarketData : IMarketData
    {
        public string AssetPairId { get; set; }
        public double Volume { get; set; }
        public double LastPrice { get; set; }
        public DateTime Dt { get; set; }
    }
}