﻿using System;
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
        private readonly AppSettings _settings;
        private readonly IReloadingManager<AppSettings.DbSettings> _dbSettings;
        private readonly ILog _log;
        private readonly ServiceCollection _services;

        public JobModule(IReloadingManager<AppSettings.DbSettings> dbSettings, AppSettings settings, ILog log)
        {
            _settings = settings;
            _dbSettings = dbSettings;
            _log = log;
            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            _services.RegisterAssetsClient(new AssetServiceSettings
            {
                BaseUri = new Uri(_settings.Assets.ServiceUrl)
            });

            builder.RegisterAzureRepositories(_dbSettings, _log);

            builder.RegisterRabbitMq(_settings.RabbitMq);

            builder.RegisterApplicationServices(_settings.TradeDataAggregatorJob);

            builder.RegisterHandlers();

            builder.Populate(_services);
        }
    }

    public static class Extensions
    {
        public static void RegisterAzureRepositories(this ContainerBuilder container,
            IReloadingManager<AppSettings.DbSettings> dbSettings, ILog log)
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

        public static void RegisterRabbitMq(this ContainerBuilder container,
            AppSettings.RabbitMqSettings settings)
        {
            container.RegisterInstance(settings).SingleInstance();

            container.RegisterType<RabbitMqHandler>()
                .AsSelf()
                .As<IStartable>()
                .SingleInstance();
        }

        public static void RegisterApplicationServices(this ContainerBuilder container,
            AppSettings.TradeDataAggregatorSettings settings)
        {
            container.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(settings.MaxHealthyClientScanningDuration));

            container.RegisterType<StartupManager>()
                .As<IStartupManager>();

            container.RegisterType<ShutdownManager>()
                .As<IShutdownManager>();

            container.RegisterType<TradeDataAggregationService>().As<ITradeDataAggregationService>();
        }

        public static void RegisterHandlers(this ContainerBuilder container)
        {
            container.RegisterType<ScanClientsHandler>()
                .As<IScanClientsHandler>()
                .SingleInstance();
        }
    }
}