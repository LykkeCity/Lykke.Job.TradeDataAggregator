using System;

namespace Lykke.Job.TradeDataAggregator.Core.Domain.Feed
{
    public class FeedData : IFeedData
    {
        public string Asset { get; set; }
        public DateTime DateTime { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }

        public static FeedData Create(string asset, double bid, double ask, DateTime? dt = null)
        {
            return new FeedData
            {
                Asset = asset,
                Ask = ask,
                Bid = bid,
                DateTime = dt ?? DateTime.UtcNow
            };
        }
    }
}