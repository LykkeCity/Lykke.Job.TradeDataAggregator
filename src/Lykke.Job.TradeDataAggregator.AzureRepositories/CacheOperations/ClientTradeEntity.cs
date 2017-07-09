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

        public static class ByDt
        {
            public static string GetRowKeyPart(DateTime dt)
            {
                //ME rowkey format e.g. 20160812180446244_00130
                return $"{dt.Year}{dt.Month:00}{dt.Day:00}{dt.Hour:00}{dt.Minute:00}";
            }
        }
    }
}