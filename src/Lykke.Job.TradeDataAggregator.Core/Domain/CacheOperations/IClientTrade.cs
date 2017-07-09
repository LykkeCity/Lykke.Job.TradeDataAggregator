using System;

namespace Lykke.Job.TradeDataAggregator.Core.Domain.CacheOperations
{
    public interface IClientTrade : IBaseCashBlockchainOperation
    {
        string LimitOrderId { get; }
        string MarketOrderId { get; }
        double Price { get; }
        DateTime? DetectionTime { get; set; }
        int Confirmations { get; set; }
    }
}