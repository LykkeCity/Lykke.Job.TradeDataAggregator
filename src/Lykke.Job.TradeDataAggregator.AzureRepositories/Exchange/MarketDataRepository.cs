using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Job.TradeDataAggregator.Core.Domain.Exchange;

namespace Lykke.Job.TradeDataAggregator.AzureRepositories.Exchange
{
    public class MarketDataRepository : IMarketDataRepository
    {
        private readonly INoSQLTableStorage<MarketDataEntity> _tableStorage;

        public MarketDataRepository(INoSQLTableStorage<MarketDataEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task AddOrMergeMarketData(IEnumerable<IMarketData> data)
        {
            var entities = data.Select(MarketDataEntity.Create);
            return _tableStorage.InsertOrMergeBatchAsync(entities);
        }
    }
}