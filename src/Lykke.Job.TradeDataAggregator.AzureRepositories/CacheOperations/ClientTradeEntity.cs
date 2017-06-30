using System;
using Lykke.Job.TradeDataAggregator.Core.Domain.CacheOperations;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.TradeDataAggregator.AzureRepositories.CacheOperations
{
    public class ClientTradeEntity : TableEntity, IClientTrade
    {
        public string Id => RowKey;

        public DateTime DateTime { get; set; }
        public bool IsHidden { get; set; }
        public string LimitOrderId { get; set; }
        public string MarketOrderId { get; set; }
        public double Price { get; set; }
        public DateTime? DetectionTime { get; set; }
        public int Confirmations { get; set; }
        public double Amount => Volume;
        public string AssetId { get; set; }
        public string BlockChainHash { get; set; }
        public string Multisig { get; set; }
        public string TransactionId { get; set; }
        public string AddressFrom { get; set; }
        public string AddressTo { get; set; }
        public bool? IsSettled { get; set; }
        public string StateField { get; set; }
        public TransactionStates State
        {
            get
            {
                TransactionStates type = TransactionStates.InProcessOnchain;
                if (!string.IsNullOrEmpty(StateField))
                {
                    Enum.TryParse(StateField, out type);
                }
                return type;
            }
            set { StateField = value.ToString(); }
        }
        public double Volume { get; set; }
        public string ClientId { get; set; }

        public static class ByClientId
        {
            public static string GeneratePartitionKey(string clientId)
            {
                return clientId;
            }

            public static string GenerateRowKey(string tradeId)
            {
                return tradeId;
            }

            public static ClientTradeEntity Create(IClientTrade src)
            {
                var entity = CreateNew(src);
                entity.RowKey = GenerateRowKey(src.Id);
                entity.PartitionKey = GeneratePartitionKey(src.ClientId);
                return entity;
            }
        }

        public static class ByMultisig
        {
            public static string GeneratePartitionKey(string multisig)
            {
                return multisig;
            }

            public static string GenerateRowKey(string tradeId)
            {
                return tradeId;
            }

            public static ClientTradeEntity Create(IClientTrade src)
            {
                var entity = CreateNew(src);
                entity.RowKey = GenerateRowKey(src.Id);
                entity.PartitionKey = GeneratePartitionKey(src.Multisig);
                return entity;
            }
        }

        public static class ByDt
        {
            public static string GeneratePartitionKey()
            {
                return "dt";
            }

            public static string GenerateRowKey(string tradeId)
            {
                return tradeId;
            }

            public static string GetRowKeyPart(DateTime dt)
            {
                //ME rowkey format e.g. 20160812180446244_00130
                return $"{dt.Year}{dt.Month.ToString("00")}{dt.Day.ToString("00")}{dt.Hour.ToString("00")}{dt.Minute.ToString("00")}";
            }

            public static ClientTradeEntity Create(IClientTrade src)
            {
                var entity = CreateNew(src);
                entity.RowKey = GenerateRowKey(src.Id);
                entity.PartitionKey = GeneratePartitionKey();
                return entity;
            }
        }

        public static ClientTradeEntity CreateNew(IClientTrade src)
        {
            return new ClientTradeEntity
            {
                ClientId = src.ClientId,
                AssetId = src.AssetId,
                DateTime = src.DateTime,
                LimitOrderId = src.LimitOrderId,
                MarketOrderId = src.MarketOrderId,
                Volume = src.Amount,
                BlockChainHash = src.BlockChainHash,
                Price = src.Price,
                IsHidden = src.IsHidden,
                AddressFrom = src.AddressFrom,
                AddressTo = src.AddressTo,
                Multisig = src.Multisig,
                DetectionTime = src.DetectionTime,
                Confirmations = src.Confirmations,
                IsSettled = src.IsSettled,
                State = src.State,
                TransactionId = src.TransactionId
            };
        }
    }
}