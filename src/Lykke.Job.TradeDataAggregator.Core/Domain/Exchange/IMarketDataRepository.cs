using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Job.TradeDataAggregator.Core.Domain.Exchange
{
    public interface IMarketDataRepository
    {
        Task AddOrMergeMarketData(IEnumerable<IMarketData> data);
    }
}