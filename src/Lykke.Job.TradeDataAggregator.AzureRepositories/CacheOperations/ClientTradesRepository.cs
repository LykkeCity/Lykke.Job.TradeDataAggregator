using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Job.TradeDataAggregator.Core.Domain.CacheOperations;

namespace Lykke.Job.TradeDataAggregator.AzureRepositories.CacheOperations
{
    public class ClientTradesRepository : IClientTradesRepository
    {
        private readonly INoSQLTableStorage<ClientTradeEntity> _tableStorage;

        public ClientTradesRepository(INoSQLTableStorage<ClientTradeEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }
        
        public Task ScanByDtAsync(Func<IEnumerable<IClientTrade>, Task> chunk, DateTime @from, DateTime to)
        {
            var rangeQuery = AzureStorageUtils.QueryGenerator<ClientTradeEntity>.BetweenQuery(
                "dt",
                ClientTradeEntity.ByDt.GetRowKeyPart(from),
                ClientTradeEntity.ByDt.GetRowKeyPart(to),
                ToIntervalOption.IncludeTo);
            return _tableStorage.ScanDataAsync(rangeQuery, chunk);
        }
    }
}