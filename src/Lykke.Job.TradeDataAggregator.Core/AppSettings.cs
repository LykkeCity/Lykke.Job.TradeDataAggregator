using System;

namespace Lykke.Job.TradeDataAggregator.Core
{
    public class AppSettings
    {
        public TradeDataAggregatorSettings TradeDataAggregatorJob { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
        public AssetsSettings Assets { get; set; }
        public RabbitMqSettings RabbitMq { get; set; }

        public class TradeDataAggregatorSettings
        {
            public DbSettings Db { get; set; }
            public TimeSpan MaxHealthyClientScanningDuration { get; set; }
        }

        public class DbSettings
        {
            public string LogsConnString { get; set; }
            public string HTradesConnString { get; set; }
            public string HLiquidityConnString { get; set; }
        }

        public class AssetsSettings
        {
            public string ServiceUrl { get; set; }
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

        public class RabbitMqSettings
        {
            public string ExternalHost { get; set; }
            public string Host { get; set; }
            public int Port { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string ExchangeSwap { get; set; }
        }
    }
}