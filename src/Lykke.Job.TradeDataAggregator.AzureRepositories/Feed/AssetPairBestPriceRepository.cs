using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Job.TradeDataAggregator.Core.Domain.Feed;

namespace Lykke.Job.TradeDataAggregator.AzureRepositories.Feed
{
    public class AssetPairBestPriceRepository : IAssetPairBestPriceRepository
    {
        private readonly INoSQLTableStorage<FeedDataEntity> _tableStorage;

        public AssetPairBestPriceRepository(INoSQLTableStorage<FeedDataEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<MarketProfile> GetAsync()
        {
            var result = await
                _tableStorage.GetDataAsync();

            var profilePartitionKey = FeedDataEntity.Profile.GeneratePartitionKey();

            return new MarketProfile
            {
                Profile = result.Where(itm => itm.PartitionKey == profilePartitionKey).ToArray()
            };

        }

        public async Task<IFeedData> GetAsync(string assetPairId)
        {
            var partitionKey = FeedDataEntity.Profile.GeneratePartitionKey();
            var rowKey = FeedDataEntity.Profile.GenerateRowKey(assetPairId);

            return await _tableStorage.GetDataAsync(partitionKey, rowKey);

        }

        public async Task SaveAsync(IFeedData feedData)
        {
            var newEntity = FeedDataEntity.Profile.CreateNew(feedData);
            await _tableStorage.InsertOrReplaceAsync(newEntity);
        }
    }
}