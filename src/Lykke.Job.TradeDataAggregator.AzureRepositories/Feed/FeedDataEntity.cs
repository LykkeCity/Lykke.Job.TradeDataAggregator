using System;
using Lykke.Job.TradeDataAggregator.Core.Domain.Feed;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.TradeDataAggregator.AzureRepositories.Feed
{
    public class FeedDataEntity : TableEntity, IFeedData
    {
        public static class Profile
        {
            public static string GeneratePartitionKey()
            {
                return "Feed";
            }
        }

        public string Asset => RowKey;
        public DateTime DateTime { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }
    }
}