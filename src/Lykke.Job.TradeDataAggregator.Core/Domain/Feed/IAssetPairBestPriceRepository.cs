using System.Threading.Tasks;

namespace Lykke.Job.TradeDataAggregator.Core.Domain.Feed
{
    public interface IAssetPairBestPriceRepository
    {
        Task<MarketProfile> GetAsync();
        Task<IFeedData> GetAsync(string assetPairId);

        Task SaveAsync(IFeedData feedData);
    }
}