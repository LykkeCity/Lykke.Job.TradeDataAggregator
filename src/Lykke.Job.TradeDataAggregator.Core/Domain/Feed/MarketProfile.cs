using System.Collections.Generic;

namespace Lykke.Job.TradeDataAggregator.Core.Domain.Feed
{
    public class MarketProfile
    {
        public IEnumerable<IFeedData> Profile { get; set; }
    }
}