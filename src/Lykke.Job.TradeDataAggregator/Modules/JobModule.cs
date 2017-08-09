﻿using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Job.TradeDataAggregator.AzureRepositories.Exchange;
using Lykke.Job.TradeDataAggregator.AzureRepositories.Feed;
using Lykke.Job.TradeDataAggregator.Core;
using Lykke.Job.TradeDataAggregator.Core.Domain.Exchange;
using Lykke.Job.TradeDataAggregator.Core.Domain.Feed;
using Lykke.Job.TradeDataAggregator.Core.Services;
using Lykke.Service.Assets.Client.Custom;
using Microsoft.Extensions.DependencyInjection;
using Lykke.Job.TradeDataAggregator.Services;
using Lykke.Service.OperationsRepository.Client;

namespace Lykke.Job.TradeDataAggregator.Modules
{
    public class JobModule : Module
    {
        private readonly AppSettings _settings;
        private readonly ILog _log;
        private readonly ServiceCollection _services;

        public JobModule(AppSettings settings, ILog log)
        {
            _settings = settings;
            _log = log;
            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings.TradeDataAggregatorJob)
                .SingleInstance();

            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            // NOTE: You can implement your own poison queue notifier. See https://github.com/LykkeCity/JobTriggers/blob/master/readme.md
            // builder.Register<PoisionQueueNotifierImplementation>().As<IPoisionQueueNotifier>();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.TradeDataAggregatorJob.MaxHealthyClientScanningDuration));

            builder.RegisterType<TradeDataAggregationService>().As<ITradeDataAggregationService>();

            _services.UseAssetsClient(new AssetServiceSettings
            {
                BaseUri = new Uri(_settings.Assets.ServiceUrl)
            });

            RegisterAzureRepositories(builder, _settings.TradeDataAggregatorJob.Db, _log);

            builder.RegisterOperationsRepositoryClients(_settings.OperationsRepositoryClient.ServiceUrl, _log,
                _settings.OperationsRepositoryClient.RequestTimeout);

            builder.Populate(_services);
        }

        private static void RegisterAzureRepositories(ContainerBuilder container, AppSettings.DbSettings dbSettings, ILog log)
        {
            container.RegisterInstance<IMarketDataRepository>(
                new MarketDataRepository(
                    new AzureTableStorage<MarketDataEntity>(dbSettings.HTradesConnString, "MarketsData", log)));

            container.RegisterInstance<IAssetPairBestPriceRepository>(
                new AssetPairBestPriceRepository(
                    new AzureTableStorage<FeedDataEntity>(dbSettings.HLiquidityConnString, "MarketProfile", log)));
        }
    }
}