﻿using System;
using System.Threading.Tasks;

namespace Lykke.Job.TradeDataAggregator.Core.Domain.Exchange
{
    public interface ITradeCommon
    {
        string Id { get; set; }
        DateTime Dt { get; set; }
        string BaseAsset { get; set; }
        string QuotAsset { get; set; }
        double Price { get; set; }
        double Amount { get; set; }
        string LimitOrderId { get; set; }
        string MarketOrderId { get; set; }
    }

    public class TradeCommon : ITradeCommon
    {
        public string Id { get; set; }
        public DateTime Dt { get; set; }
        public string BaseAsset { get; set; }
        public string QuotAsset { get; set; }
        public double Price { get; set; }
        public double Amount { get; set; }
        public string LimitOrderId { get; set; }
        public string MarketOrderId { get; set; }
    }

    public interface ITradesCommonRepository
    {
        Task InsertCommonTrade(TradeCommon trade);
    }
}