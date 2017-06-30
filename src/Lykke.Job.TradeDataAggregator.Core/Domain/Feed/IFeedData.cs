using System;

namespace Lykke.Job.TradeDataAggregator.Core.Domain.Feed
{
    public interface IFeedData
    {
        string Asset { get; }
        DateTime DateTime { get; }
        double Bid { get; }
        double Ask { get; }
    }
}