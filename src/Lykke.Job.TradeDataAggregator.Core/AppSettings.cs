namespace Lykke.Job.TradeDataAggregator.Core
{
    public class AppSettings
    {
        public TradeDataAggregatorSettings TradeDataAggregatorJob { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }

        public class TradeDataAggregatorSettings
        {
            public DbSettings Db { get; set; }
        }

        public class DbSettings
        {
            public string LogsConnString { get; set; }
            public string HTradesConnString { get; set; }
            public string DictsConnString { get; set; }
            public string HLiquidityConnString { get; set; }
        }

        public class SlackNotificationsSettings
        {
            public AzureQueueSettings AzureQueue { get; set; }

            public int ThrottlingLimitSeconds { get; set; }
        }

        public class AzureQueueSettings
        {
            public string ConnectionString { get; set; }

            public string QueueName { get; set; }
        }
    }
}