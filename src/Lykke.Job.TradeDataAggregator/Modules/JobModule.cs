using System;
using Autofac;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Job.TradeDataAggregator.AzureRepositories.Exchange;
using Lykke.Job.TradeDataAggregator.Core.Domain.Exchange;
using Lykke.Job.TradeDataAggregator.Core.Services;
using Lykke.Job.TradeDataAggregator.Services;
using Lykke.Sdk;
using Lykke.Sdk.Health;
using Lykke.Service.Assets.Client;
using Lykke.Service.MarketProfile.Client;
using Lykke.Service.OperationsRepository.Client;
using Lykke.SettingsReader;
using HealthService = Lykke.Job.TradeDataAggregator.Services.HealthService;

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
                .As<IHealthServiceExt>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.TradeDataAggregatorJob.MaxHealthyClientScanningDuration));

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>()
                .SingleInstance();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>()
                .AutoActivate()
                .SingleInstance();

            builder.RegisterType<TradeDataAggregationService>().As<ITradeDataAggregationService>();

            builder.RegisterAssetsClient(_settings.Assets.ServiceUrl);

            builder.RegisterOperationsRepositoryClients(_settings.OperationsRepositoryServiceClient);

            builder.RegisterType<LykkeMarketProfile>()
                .As<ILykkeMarketProfile>()
                .WithParameter("baseUri", new Uri(_settings.MarketProfileServiceClient.ServiceUrl));

            RegisterAzureRepositories(builder, _settingsManager.Nested(s => s.TradeDataAggregatorJob.Db), _log);

            builder.RegisterInstance(_settings.RabbitMq).SingleInstance();

            builder.RegisterType<RabbitMqHandler>()
                .AsSelf()
                .As<IStartStop>()
                .SingleInstance();
        }

        private static void RegisterAzureRepositories(ContainerBuilder container, IReloadingManager<DbSettings> dbSettings, ILog log)
        {
            container.RegisterInstance<IMarketDataRepository>(
                new MarketDataRepository(
                    AzureTableStorage<MarketDataEntity>.Create(dbSettings.ConnectionString(s => s.DataConnString), "MarketsData", log)));

            container.RegisterInstance<ITradesCommonRepository>(
                new TradesCommonRepository(
                    AzureTableStorage<TradeCommonEntity>.Create(dbSettings.ConnectionString(s => s.DataConnString), "TradesCommon", log)));
        }
    }
}