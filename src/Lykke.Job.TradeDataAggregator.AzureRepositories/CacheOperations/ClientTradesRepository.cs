using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<IClientTrade[]> SaveAsync(params IClientTrade[] clientTrades)
        {
            List<IClientTrade> inserted = new List<IClientTrade>();
            foreach (var trade in clientTrades)
            {
                var byMultisig = ClientTradeEntity.ByClientId.Create(trade);
                var insertByClientIdTask = _tableStorage.InsertAsync(byMultisig);
                var insertbyMultisigTask = _tableStorage.InsertAsync(ClientTradeEntity.ByMultisig.Create(trade));
                var insertbyDtTask = _tableStorage.InsertAsync(ClientTradeEntity.ByDt.Create(trade));

                await insertByClientIdTask;
                await insertbyMultisigTask;
                await insertbyDtTask;

                inserted.Add(byMultisig);
            }

            return inserted.ToArray();
        }

        public async Task<IEnumerable<IClientTrade>> GetAsync(string clientId)
        {
            var partitionKey = ClientTradeEntity.ByClientId.GeneratePartitionKey(clientId);
            return await _tableStorage.GetDataAsync(partitionKey);
        }

        public async Task<IEnumerable<IClientTrade>> GetAsync(DateTime @from, DateTime to)
        {
            // ToDo - Have to optimize according to the task: https://lykkex.atlassian.net/browse/LWDEV-131
            return await _tableStorage.GetDataAsync(itm => @from <= itm.DateTime && itm.DateTime < to);
        }

        public async Task<IClientTrade> GetAsync(string clientId, string recordId)
        {
            var partitionKey = ClientTradeEntity.ByClientId.GeneratePartitionKey(clientId);
            var rowKey = ClientTradeEntity.ByClientId.GenerateRowKey(recordId);

            return await _tableStorage.GetDataAsync(partitionKey, rowKey);
        }

        public async Task UpdateBlockChainHashAsync(string clientId, string recordId, string hash)
        {
            var partitionKey = ClientTradeEntity.ByClientId.GeneratePartitionKey(clientId);
            var rowKey = ClientTradeEntity.ByClientId.GenerateRowKey(recordId);

            var clientIdRecord = await _tableStorage.GetDataAsync(partitionKey, rowKey);

            var multisigPartitionKey = ClientTradeEntity.ByMultisig.GeneratePartitionKey(clientIdRecord.Multisig);
            var multisigRowKey = ClientTradeEntity.ByMultisig.GenerateRowKey(recordId);

            var dtPartitionKey = ClientTradeEntity.ByDt.GeneratePartitionKey();
            var dtRowKey = ClientTradeEntity.ByDt.GenerateRowKey(recordId);


            await _tableStorage.MergeAsync(partitionKey, rowKey, entity =>
            {
                entity.BlockChainHash = hash;
                entity.State = TransactionStates.SettledOnchain;
                return entity;
            });

            await _tableStorage.MergeAsync(multisigPartitionKey, multisigRowKey, entity =>
            {
                entity.BlockChainHash = hash;
                entity.State = TransactionStates.SettledOnchain;
                return entity;
            });

            await _tableStorage.MergeAsync(dtPartitionKey, dtRowKey, entity =>
            {
                entity.BlockChainHash = hash;
                entity.State = TransactionStates.SettledOnchain;
                return entity;
            });
        }

        public async Task SetDetectionTimeAndConfirmations(string clientId, string recordId, DateTime detectTime,
            int confirmations)
        {
            var partitionKey = ClientTradeEntity.ByClientId.GeneratePartitionKey(clientId);
            var rowKey = ClientTradeEntity.ByClientId.GenerateRowKey(recordId);

            var clientIdRecord = await _tableStorage.GetDataAsync(partitionKey, rowKey);

            if (clientIdRecord == null)
                return;

            var multisigPartitionKey = ClientTradeEntity.ByMultisig.GeneratePartitionKey(clientIdRecord.Multisig);
            var multisigRowKey = ClientTradeEntity.ByMultisig.GenerateRowKey(recordId);

            var dtPartitionKey = ClientTradeEntity.ByDt.GeneratePartitionKey();
            var dtRowKey = ClientTradeEntity.ByDt.GenerateRowKey(recordId);

            await _tableStorage.MergeAsync(partitionKey, rowKey, entity =>
            {
                entity.DetectionTime = detectTime;
                entity.Confirmations = confirmations;
                entity.State = TransactionStates.SettledOnchain;
                return entity;
            });

            await _tableStorage.MergeAsync(multisigPartitionKey, multisigRowKey, entity =>
            {
                entity.DetectionTime = detectTime;
                entity.Confirmations = confirmations;
                entity.State = TransactionStates.SettledOnchain;
                return entity;
            });

            await _tableStorage.MergeAsync(dtPartitionKey, dtRowKey, entity =>
            {
                entity.DetectionTime = detectTime;
                entity.Confirmations = confirmations;
                entity.State = TransactionStates.SettledOnchain;
                return entity;
            });
        }

        public async Task SetBtcTransactionAsync(string clientId, string recordId, string btcTransactionId)
        {
            var partitionKey = ClientTradeEntity.ByClientId.GeneratePartitionKey(clientId);
            var rowKey = ClientTradeEntity.ByClientId.GenerateRowKey(recordId);

            var clientIdRecord = await _tableStorage.GetDataAsync(partitionKey, rowKey);

            var multisigPartitionKey = ClientTradeEntity.ByMultisig.GeneratePartitionKey(clientIdRecord.Multisig);
            var multisigRowKey = ClientTradeEntity.ByMultisig.GenerateRowKey(recordId);

            var dtPartitionKey = ClientTradeEntity.ByDt.GeneratePartitionKey();
            var dtRowKey = ClientTradeEntity.ByDt.GenerateRowKey(recordId);

            await _tableStorage.MergeAsync(partitionKey, rowKey, entity =>
            {
                entity.TransactionId = btcTransactionId;
                return entity;
            });

            await _tableStorage.MergeAsync(multisigPartitionKey, multisigRowKey, entity =>
            {
                entity.TransactionId = btcTransactionId;
                return entity;
            });

            await _tableStorage.MergeAsync(dtPartitionKey, dtRowKey, entity =>
            {
                entity.TransactionId = btcTransactionId;
                return entity;
            });
        }

        public async Task SetIsSettledAsync(string clientId, string id, bool offchain)
        {
            var partitionKey = ClientTradeEntity.ByClientId.GeneratePartitionKey(clientId);
            var rowKey = ClientTradeEntity.ByClientId.GenerateRowKey(id);

            var clientIdRecord = await _tableStorage.GetDataAsync(partitionKey, rowKey);

            var multisigPartitionKey = ClientTradeEntity.ByMultisig.GeneratePartitionKey(clientIdRecord.Multisig);
            var multisigRowKey = ClientTradeEntity.ByMultisig.GenerateRowKey(id);

            var dtPartitionKey = ClientTradeEntity.ByDt.GeneratePartitionKey();
            var dtRowKey = ClientTradeEntity.ByDt.GenerateRowKey(id);


            await Task.WhenAll(
                _tableStorage.MergeAsync(partitionKey, rowKey, entity =>
                {
                    if (offchain)
                        entity.State = TransactionStates.SettledOffchain;
                    else
                        entity.IsSettled = true;
                    return entity;
                }),
                _tableStorage.MergeAsync(multisigPartitionKey, multisigRowKey, entity =>
                {
                    if (offchain)
                        entity.State = TransactionStates.SettledOffchain;
                    else
                        entity.IsSettled = true;
                    return entity;
                }),
                _tableStorage.MergeAsync(dtPartitionKey, dtRowKey, entity =>
                {
                    if (offchain)
                        entity.State = TransactionStates.SettledOffchain;
                    else
                        entity.IsSettled = true;
                    return entity;
                })
            );
        }

        public async Task<IEnumerable<IClientTrade>> GetByMultisigAsync(string multisig)
        {
            var partitionKey = ClientTradeEntity.ByMultisig.GeneratePartitionKey(multisig);
            return await _tableStorage.GetDataAsync(partitionKey);
        }

        public async Task<IEnumerable<IClientTrade>> GetByMultisigsAsync(string[] multisigs)
        {
            var tasks =
                multisigs.Select(x => _tableStorage.GetDataAsync(ClientTradeEntity.ByMultisig.GeneratePartitionKey(x)));

            var tradesByMultisigArr = await Task.WhenAll(tasks);

            var result = new List<IClientTrade>();

            foreach (var arr in tradesByMultisigArr)
            {
                result.AddRange(arr);
            }

            return result;
        }

        public Task ScanByDtAsync(Func<IEnumerable<IClientTrade>, Task> chunk, DateTime @from, DateTime to)
        {
            var rangeQuery = AzureStorageUtils.QueryGenerator<ClientTradeEntity>
                .RowKeyOnly.BetweenQuery(ClientTradeEntity.ByDt.GetRowKeyPart(from),
                    ClientTradeEntity.ByDt.GetRowKeyPart(to), ToIntervalOption.IncludeTo);
            return _tableStorage.ScanDataAsync(rangeQuery, chunk);
        }

        public Task GetDataByChunksAsync(Func<IEnumerable<IClientTrade>, Task> chunk)
        {
            return _tableStorage.GetDataByChunksAsync(chunk);
        }
    }
}