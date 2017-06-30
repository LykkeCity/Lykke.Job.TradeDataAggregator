using Lykke.Job.TradeDataAggregator.Core.Domain.Assets;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.TradeDataAggregator.AzureRepositories.Assets
{
    public class AssetPairEntity : TableEntity, IAssetPair
    {
        public static string GeneratePartitionKey()
        {
            return "AssetPair";
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }

        public string Id => RowKey;
        public string Name { get; set; }

        public string BaseAssetId { get; set; }
        public string QuotingAssetId { get; set; }
        public int Accuracy { get; set; }
        public int InvertedAccuracy { get; set; }
        public string Source { get; set; }
        public string Source2 { get; set; }
        public bool IsDisabled { get; set; }


        public static AssetPairEntity Create(IAssetPair src)
        {
            return new AssetPairEntity
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(src.Id),
                Accuracy = src.Accuracy,
                BaseAssetId = src.BaseAssetId,
                QuotingAssetId = src.QuotingAssetId,
                Name = src.Name,
                Source = src.Source,
                Source2 = src.Source2,
                IsDisabled = src.IsDisabled,
                InvertedAccuracy = src.InvertedAccuracy
            };
        }
    }
}