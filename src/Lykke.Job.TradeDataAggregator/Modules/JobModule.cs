using Autofac;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Job.TradeDataAggregator.AzureRepositories.CacheOperations;
using Lykke.Job.TradeDataAggregator.AzureRepositories.Exchange;
using Lykke.Job.TradeDataAggregator.AzureRepositories.Feed;
using Lykke.Job.TradeDataAggregator.Core;
using Lykke.Job.TradeDataAggregator.Core.Domain.CacheOperations;
using Lykke.Job.TradeDataAggregator.Core.Domain.Exchange;
using Lykke.Job.TradeDataAggregator.Core.Domain.Feed;
using Lykke.Job.TradeDataAggregator.Core.Services;
using Lykke.Job.TradeDataAggregator.Services;
using Lykke.Service.Assets.Client;
using Lykke.SettingsReader;

namespace Lykke.Job.TradeDataAggregator.Modules
{
    public class JobModule : Module
    {
        private readonly AppSettings _settings;
        private readonly IReloadingManager<AppSettings> _settingsManager;
        private readonly ILog _log;

        public JobModule(IReloadingManager<AppSettings> settingsManager, ILog log)
        {
            _settingsManager = settingsManager;
            _settings = settingsManager.CurrentValue;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings.TradeDataAggregatorJob)
                .SingleInstance();

            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.TradeDataAggregatorJob.MaxHealthyClientScanningDuration));

            builder.RegisterType<TradeDataAggregationService>().As<ITradeDataAggregationService>();

            builder.RegisterAssetsClient(_settings.Assets.ServiceUrl);

            RegisterAzureRepositories(builder, _settingsManager.Nested(s => s.TradeDataAggregatorJob.Db), _log);

            builder.RegisterInstance(_settings.RabbitMq).SingleInstance();

            builder.RegisterType<RabbitMqHandler>()
                .AsSelf()
                .As<IStartable>()
                .SingleInstance();
        }

        private static void RegisterAzureRepositories(ContainerBuilder container, IReloadingManager<AppSettings.DbSettings> dbSettings, ILog log)
        {
            container.RegisterInstance<IMarketDataRepository>(
                new MarketDataRepository(
                    AzureTableStorage<MarketDataEntity>.Create(dbSettings.ConnectionString(s => s.HTradesConnString), "MarketsData", log)));

            container.RegisterInstance<ITradesCommonRepository>(
                new TradesCommonRepository(
                    AzureTableStorage<TradeCommonEntity>.Create(dbSettings.ConnectionString(s => s.HTradesConnString), "TradesCommon", log)));

            container.RegisterInstance<IClientTradesRepository>(
                new ClientTradesRepository(
                    AzureTableStorage<ClientTradeEntity>.Create(dbSettings.ConnectionString(s =>s.HTradesConnString), "Trades", log)));

            container.RegisterInstance<IAssetPairBestPriceRepository>(
                new AssetPairBestPriceRepository(
                    AzureTableStorage<FeedDataEntity>.Create(dbSettings.ConnectionString(s => s.HLiquidityConnString), "MarketProfile", log)));
        }
    }
}