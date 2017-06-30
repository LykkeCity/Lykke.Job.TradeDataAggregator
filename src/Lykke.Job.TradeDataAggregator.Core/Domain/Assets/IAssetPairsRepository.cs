using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Job.TradeDataAggregator.Core.Domain.Assets
{
    public interface IAssetPairsRepository
    {
        Task<IEnumerable<IAssetPair>> GetAllAsync();
        Task<IAssetPair> GetAsync(string id);
        Task AddAsync(IAssetPair assetPair);
        Task EditAsync(string id, IAssetPair assetPair);

    }
}