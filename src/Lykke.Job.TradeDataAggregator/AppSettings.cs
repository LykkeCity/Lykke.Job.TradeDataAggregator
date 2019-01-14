using System;
using Lykke.Service.OperationsRepository.Client;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.TradeDataAggregator
{
    public class AppSettings
    {
        public TradeDataAggregatorSettings TradeDataAggregatorJob { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
        public MarketProfileServiceClientSettings MarketProfileServiceClient { get; set; }
        public OperationsRepositoryServiceClientSettings OperationsRepositoryServiceClient { get; set; }
        public AssetsSettings Assets { get; set; }
        public RabbitMqSettings RabbitMq { get; set; }
    }

    public class TradeDataAggregatorSettings
    {
        public DbSettings Db { get; set; }
        public TimeSpan MaxHealthyClientScanningDuration { get; set; }
    }

    public class DbSettings
    {
        public string LogsConnString { get; set; }
        public string DataConnString { get; set; }
        public string HTradesConnString { get; set; }
    }

    public class AssetsSettings
    {
        public string ServiceUrl { get; set; }
    }

    public class SlackNotificationsSettings
    {
        public AzureQueueSettings AzureQueue { get; set; }
    }

    public class AzureQueueSettings
    {
        public string ConnectionString { get; set; }

        public string QueueName { get; set; }
    }

    public class RabbitMqSettings
    {
        public string Host { get; set; }

        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public string ExchangeSwap { get; set; }
    }

    public class MarketProfileServiceClientSettings
    {
        [HttpCheck("api/isalive")]
        public string ServiceUrl { get; set; }
    }
}