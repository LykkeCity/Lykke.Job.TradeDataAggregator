using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Job.TradeDataAggregator.Core.Domain.Assets
{
    public static class AssetPairExt
    {
        public static int Multiplier(this IAssetPair assetPair)
        {
            return (int)Math.Pow(10, assetPair.Accuracy);
        }

        public static string RateToString(this double src, IAssetPair assetPair)
        {
            var mask = "0." + new string('#', assetPair.Accuracy);
            return src.ToString(mask);
        }

        public static IEnumerable<IAssetPair> WhichHaveAssets(this IEnumerable<IAssetPair> src, params string[] assetIds)
        {
            return src.Where(assetPair => assetIds.Contains(assetPair.BaseAssetId) || assetIds.Contains(assetPair.QuotingAssetId));
        }

        public static IEnumerable<IAssetPair> WhichConsistsOfAssets(this IEnumerable<IAssetPair> src, params string[] assetIds)
        {
            return src.Where(assetPair => assetIds.Contains(assetPair.BaseAssetId) && assetIds.Contains(assetPair.QuotingAssetId));
        }

        public static IAssetPair PairWithAssets(this IEnumerable<IAssetPair> src, string assetId1, string assetId2)
        {
            return src.FirstOrDefault(assetPair =>
                (assetPair.BaseAssetId == assetId1 && assetPair.QuotingAssetId == assetId2) ||
                (assetPair.BaseAssetId == assetId2 && assetPair.QuotingAssetId == assetId1)
            );
        }

        public static async Task<IAssetPair> GetAsync(this IAssetPairsRepository assetPairsRepository, string assetId1, string assetId2)
        {
            var assetPairs = await assetPairsRepository.GetAllAsync();
            return assetPairs.FirstOrDefault(itm =>
                (itm.BaseAssetId == assetId1 && itm.QuotingAssetId == assetId2) ||
                (itm.BaseAssetId == assetId2 && itm.QuotingAssetId == assetId1));
        }

        public static bool IsInverted(this IAssetPair assetPair, string targetAsset)
        {
            return assetPair.QuotingAssetId == targetAsset;
        }
    }
}