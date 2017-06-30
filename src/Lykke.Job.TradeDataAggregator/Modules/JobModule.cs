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
using Lykke.Service.Assets.Client.Custom;
using Microsoft.Extensions.DependencyInjection;
using Lykke.Job.TradeDataAggregator.Services;

namespace Lykke.Job.TradeDataAggregator.Modules
{
    public class JobModule : Module
    {
        private readonly AppSettings.TradeDataAggregatorSettings _settings;
        private readonly ILog _log;
        private ServiceCollection _services;

        public JobModule(AppSettings.TradeDataAggregatorSettings settings, ILog log)
        {
            _settings = settings;
            _log = log;
            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings)
                .SingleInstance();

            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            // NOTE: You can implement your own poison queue notifier. See https://github.com/LykkeCity/JobTriggers/blob/master/readme.md
            // builder.Register<PoisionQueueNotifierImplementation>().As<IPoisionQueueNotifier>();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.MaxHealthyClientScanningDuration));

            _services.UseAssetsClient(new AssetServiceSettings
            {
                BaseUri = new Uri(_settings.Assets.ServiceUri)
            });

            RegisterAzureRepositories(builder, _settings.Db, _log);

            builder.Populate(_services);
        }

        private static void RegisterAzureRepositories(ContainerBuilder container, AppSettings.DbSettings dbSettings, ILog log)
        {
            container.RegisterInstance<IMarketDataRepository>(
                new MarketDataRepository(
                    new AzureTableStorage<MarketDataEntity>(dbSettings.HTradesConnString, "MarketsData", log)));


            container.RegisterInstance<IClientTradesRepository>(
                new ClientTradesRepository(
                    new AzureTableStorage<ClientTradeEntity>(dbSettings.HTradesConnString, "Trades", log)));

            container.RegisterInstance<IAssetPairBestPriceRepository>(
                new AssetPairBestPriceRepository(
                    new AzureTableStorage<FeedDataEntity>(dbSettings.HLiquidityConnString, "MarketProfile", log)));
        }
    }
}