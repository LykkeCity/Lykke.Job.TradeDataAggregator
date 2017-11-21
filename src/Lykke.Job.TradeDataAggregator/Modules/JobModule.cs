using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
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
using Microsoft.Extensions.DependencyInjection;
using Lykke.Job.TradeDataAggregator.Services;
using Lykke.Service.Assets.Client;
using Lykke.SettingsReader;

namespace Lykke.Job.TradeDataAggregator.Modules
{
    public class JobModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;
        private readonly ILog _log;
        private readonly ServiceCollection _services;

        public JobModule(IReloadingManager<AppSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings.CurrentValue.TradeDataAggregatorJob)
                .SingleInstance();

            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.CurrentValue.TradeDataAggregatorJob.MaxHealthyClientScanningDuration));

            builder.RegisterType<TradeDataAggregationService>().As<ITradeDataAggregationService>();

            _services.RegisterAssetsClient(new AssetServiceSettings
            {
                BaseUri = new Uri(_settings.CurrentValue.Assets.ServiceUrl)
            });

            RegisterAzureRepositories(builder, _settings.Nested(x => x.TradeDataAggregatorJob.Db), _log);

            builder.RegisterInstance(_settings.CurrentValue.RabbitMq).SingleInstance();

            builder.RegisterType<RabbitMqHandler>()
                .AsSelf()
                .As<IStartable>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>();

            builder.Populate(_services);
        }

        private static void RegisterAzureRepositories(ContainerBuilder container, IReloadingManager<AppSettings.DbSettings> dbSettings, ILog log)
        {
            container.RegisterInstance<IMarketDataRepository>(new MarketDataRepository(
                AzureTableStorage<MarketDataEntity>.Create(dbSettings.ConnectionString(x => x.HTradesConnString),
                    "MarketsData", log)));

            container.RegisterInstance<ITradesCommonRepository>(new TradesCommonRepository(
                AzureTableStorage<TradeCommonEntity>.Create(dbSettings.ConnectionString(x => x.HTradesConnString),
                    "TradesCommon", log)));

            container.RegisterInstance<IClientTradesRepository>(new ClientTradesRepository(
                AzureTableStorage<ClientTradeEntity>.Create(dbSettings.ConnectionString(x => x.HTradesConnString),
                    "Trades", log)));

            container.RegisterInstance<IAssetPairBestPriceRepository>(new AssetPairBestPriceRepository(
                AzureTableStorage<FeedDataEntity>.Create(dbSettings.ConnectionString(x => x.HLiquidityConnString),
                    "MarketProfile", log)));
        }
    }
}