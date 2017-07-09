using System;

namespace Lykke.Job.TradeDataAggregator.Core.Domain.CacheOperations
{
    /// <summary>
    /// Base cash operation
    /// </summary>
    public interface IBaseCashOperation
    {
        /// <summary>
        /// Record Id
        /// </summary>
        string Id { get; }

        string AssetId { get; }

        string ClientId { get; }

        double Amount { get; }

        DateTime DateTime { get; }

        bool IsHidden { get; }
    }
}