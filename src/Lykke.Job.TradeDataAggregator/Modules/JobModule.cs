using System.Linq;
using Autofac;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Lykke.Job.TradeDataAggregator.AzureRepositories.Assets;
using Lykke.Job.TradeDataAggregator.AzureRepositories.CacheOperations;
using Lykke.Job.TradeDataAggregator.AzureRepositories.Exchange;
using Lykke.Job.TradeDataAggregator.AzureRepositories.Feed;
using Lykke.Job.TradeDataAggregator.Core;
using Lykke.Job.TradeDataAggregator.Core.Domain.Assets;
using Lykke.Job.TradeDataAggregator.Core.Domain.CacheOperations;
using Lykke.Job.TradeDataAggregator.Core.Domain.Exchange;
using Lykke.Job.TradeDataAggregator.Core.Domain.Feed;

namespace Lykke.Job.TradeDataAggregator.Modules
{
    public class JobModule : Module
    {
        private readonly AppSettings.TradeDataAggregatorSettings _settings;
        private readonly ILog _log;

        public JobModule(AppSettings.TradeDataAggregatorSettings settings, ILog log)
        {
            _settings = settings;
            _log = log;
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

            RegisterAzureRepositories(builder, _settings.Db, _log);
            RegisterCachedDicts(builder);
        }

        private static void RegisterAzureRepositories(ContainerBuilder container, AppSettings.DbSettings dbSettings, ILog log)
        {
            container.RegisterInstance<IMarketDataRepository>(
                new MarketDataRepository(
                    new AzureTableStorage<MarketDataEntity>(dbSettings.HTradesConnString, "MarketsData", log)));


            container.RegisterInstance<IClientTradesRepository>(
                new ClientTradesRepository(
                    new AzureTableStorage<ClientTradeEntity>(dbSettings.HTradesConnString, "Trades", log)));

            container.RegisterInstance<IAssetPairsRepository>(
                new AssetPairsRepository(
                    new AzureTableStorage<AssetPairEntity>(dbSettings.DictsConnString, "Dictionaries", log)));

            container.RegisterInstance<IAssetPairBestPriceRepository>(
                new AssetPairBestPriceRepository(
                    new AzureTableStorage<FeedDataEntity>(dbSettings.HLiquidityConnString, "MarketProfile", log)));
        }

        private static void RegisterCachedDicts(ContainerBuilder builder)
        {
            builder.Register(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return new CachedDataDictionary<string, IAssetPair>(async () => (await ctx.Resolve<IAssetPairsRepository>().GetAllAsync()).ToDictionary(itm => itm.Id));
            }).SingleInstance();
        }
    }
}