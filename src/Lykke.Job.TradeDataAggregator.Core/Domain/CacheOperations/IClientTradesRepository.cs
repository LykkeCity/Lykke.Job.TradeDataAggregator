using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Job.TradeDataAggregator.Core.Domain.CacheOperations
{
    public interface IClientTradesRepository
    {
        Task ScanByDtAsync(Func<IEnumerable<IClientTrade>, Task> chunk, DateTime from, DateTime to);
    }
}