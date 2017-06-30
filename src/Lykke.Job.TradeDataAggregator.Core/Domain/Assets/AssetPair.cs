namespace Lykke.Job.TradeDataAggregator.Core.Domain.Assets
{
    public class AssetPair : IAssetPair
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string BaseAssetId { get; set; }
        public string QuotingAssetId { get; set; }
        public int Accuracy { get; set; }
        public int InvertedAccuracy { get; set; }
        public string Source { get; set; }
        public string Source2 { get; set; }
        public bool IsDisabled { get; set; }


        public static AssetPair CreateDefault()
        {
            return new AssetPair
            {
                Accuracy = 5,
                IsDisabled = false
            };
        }

    }
}