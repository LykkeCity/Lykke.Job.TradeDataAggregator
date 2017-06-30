using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Job.TradeDataAggregator.Core.Domain.CacheOperations
{
    public interface IClientTradesRepository
    {
        Task<IClientTrade[]> SaveAsync(params IClientTrade[] clientTrades);
        Task<IEnumerable<IClientTrade>> GetAsync(string clientId);

        Task<IEnumerable<IClientTrade>> GetAsync(DateTime from, DateTime to);

        Task<IClientTrade> GetAsync(string clientId, string recordId);
        Task UpdateBlockChainHashAsync(string clientId, string recordId, string hash);
        Task SetDetectionTimeAndConfirmations(string clientId, string recordId, DateTime detectTime, int confirmations);
        Task SetBtcTransactionAsync(string clientId, string recordId, string btcTransactionId);
        Task SetIsSettledAsync(string clientId, string id, bool offchain);
        Task<IEnumerable<IClientTrade>> GetByMultisigAsync(string multisig);
        Task<IEnumerable<IClientTrade>> GetByMultisigsAsync(string[] multisigs);

        Task ScanByDtAsync(Func<IEnumerable<IClientTrade>, Task> chunk, DateTime from, DateTime to);
        Task GetDataByChunksAsync(Func<IEnumerable<IClientTrade>, Task> chunk);
    }
}