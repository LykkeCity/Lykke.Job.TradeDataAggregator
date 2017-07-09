using System;

namespace Lykke.Job.TradeDataAggregator.Core.Domain.Exchange
{
    public interface IMarketData
    {
        string AssetPairId { get; set; }
        double Volume { get; set; }
        double LastPrice { get; set; }
        DateTime Dt { get; set; }
    }
}