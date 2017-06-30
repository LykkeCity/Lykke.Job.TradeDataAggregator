using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Job.TradeDataAggregator.Core.Domain.Assets;

namespace Lykke.Job.TradeDataAggregator.AzureRepositories.Assets
{
    public class AssetPairsRepository : IAssetPairsRepository
    {
        private readonly INoSQLTableStorage<AssetPairEntity> _tableStorage;

        public AssetPairsRepository(INoSQLTableStorage<AssetPairEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IEnumerable<IAssetPair>> GetAllAsync()
        {
            var partitionKey = AssetPairEntity.GeneratePartitionKey();
            return await _tableStorage.GetDataAsync(partitionKey);
        }

        public async Task<IAssetPair> GetAsync(string id)
        {
            var partitionKey = AssetPairEntity.GeneratePartitionKey();
            var rowKey = AssetPairEntity.GenerateRowKey(id);
            return await _tableStorage.GetDataAsync(partitionKey, rowKey);
        }

        public Task AddAsync(IAssetPair assetPair)
        {
            var newEntity = AssetPairEntity.Create(assetPair);
            return _tableStorage.InsertOrReplaceAsync(newEntity);
        }

        public async Task EditAsync(string id, IAssetPair assetPair)
        {
            await _tableStorage.DeleteAsync(AssetPairEntity.GeneratePartitionKey(), AssetPairEntity.GenerateRowKey(id));
            await AddAsync(assetPair);
        }

    }
}