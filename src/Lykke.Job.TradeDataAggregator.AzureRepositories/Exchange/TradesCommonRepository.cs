using System;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Job.TradeDataAggregator.Core.Domain.Exchange;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.TradeDataAggregator.AzureRepositories.Exchange
{
    public class TradeCommonEntity : TableEntity, ITradeCommon
    {
        public static string GenerateParitionKey(DateTime dt)
        {
            return $"{dt.Year}-{dt.Month}-{dt.Day}";
        }

        public static string GenerateRowKey(DateTime dt, string id)
        {
            return $"{DateTime.MaxValue.Ticks - dt.Ticks}_{id}";
        }

        public static TradeCommonEntity Create(ITradeCommon trade)
        {
            return new TradeCommonEntity
            {
                Amount = trade.Amount,
                AssetPair = trade.AssetPair,
                BaseAsset = trade.BaseAsset,
                Dt = trade.Dt,
                Id = trade.Id,
                LimitOrderId = trade.LimitOrderId,
                MarketOrderId = trade.MarketOrderId,
                PartitionKey = GenerateParitionKey(trade.Dt),
                Price = trade.Price,
                QuotAsset = trade.QuotAsset,
                RowKey = GenerateRowKey(trade.Dt, trade.Id)
            };
        }

        public string Id { get; set; }
        public DateTime Dt { get; set; }
        public string AssetPair { get; set; }
        public string BaseAsset { get; set; }
        public string QuotAsset { get; set; }
        public double Price { get; set; }
        public double Amount { get; set; }
        public string LimitOrderId { get; set; }
        public string MarketOrderId { get; set; }
    }

    public class TradesCommonRepository : ITradesCommonRepository
    {
        private readonly INoSQLTableStorage<TradeCommonEntity> _tableStorage;

        public TradesCommonRepository(INoSQLTableStorage<TradeCommonEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task InsertCommonTrade(TradeCommon trade)
        {
            var entity = TradeCommonEntity.Create(trade);
            return _tableStorage.InsertAsync(entity);
        }
    }
}
