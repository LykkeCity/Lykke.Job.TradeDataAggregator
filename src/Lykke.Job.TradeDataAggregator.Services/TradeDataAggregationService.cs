using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Job.TradeDataAggregator.Core.Domain.Exchange;
using Lykke.Job.TradeDataAggregator.Core.Services;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.MarketProfile.Client;
using Lykke.Service.OperationsRepository.AutorestClient.Models;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;

namespace Lykke.Job.TradeDataAggregator.Services
{
    public class TradeDataAggregationService : ITradeDataAggregationService
    {
        public class TemporaryAggregatedData
        {
            public string LimitOrderAndDateTimeKey { get; set; }
            public double Price { get; set; }
            public string ClientId { get; set; }
            public string Asset1 { get; set; }
            public string Asset2 { get; set; }
            public double Volume1 { get; set; }
            public double Volume2 { get; set; }
            public DateTime Dt { get; set; }

            public static string CreateKey(string limitOrder, DateTime dt)
            {
                return $"{limitOrder}_{dt}";
            }
        }

        private readonly IMarketDataRepository _marketDataRepository;
        private readonly IAssetsService _assetsService;
        private readonly ITradeOperationsRepositoryClient _tradeOperationsRepositoryClient;
        private readonly ILykkeMarketProfile _marketProfileService;
        private readonly ILog _log;

        private readonly Dictionary<string, TemporaryAggregatedData> _tempDataByLimitOrderAndDtId = new Dictionary<string, TemporaryAggregatedData>();

        public TradeDataAggregationService(
            IMarketDataRepository marketDataRepository,
            IAssetsService assetsService,
            ITradeOperationsRepositoryClient tradeOperationsRepositoryClient,
            ILykkeMarketProfile marketProfileService,
            ILog log)
        {
            _marketDataRepository = marketDataRepository;
            _assetsService = assetsService;
            _tradeOperationsRepositoryClient = tradeOperationsRepositoryClient;
            _marketProfileService = marketProfileService;
            _log = log;
        }

        public async Task ScanClientsAsync()
        {
            string continuationToken = null;
            var now = DateTime.UtcNow;
            do
            {
                var tradesResult = await _tradeOperationsRepositoryClient.GetByDatesAsync(
                    now.Subtract(TimeSpan.FromDays(1)),
                    now,
                    continuationToken);
                HandleTradeRecords(tradesResult.Trades);
                continuationToken = tradesResult.ContinuationToken;
            } while (continuationToken != null);

            await FillMarketDataAsync();

            _tempDataByLimitOrderAndDtId.Clear();
        }

        private async Task FillMarketDataAsync()
        {
            var newMarketData = new Dictionary<string, IMarketData>();
            var assetPairs = await _assetsService.AssetPairGetAllAsync();
            var tempDataValues = _tempDataByLimitOrderAndDtId.Values.OrderBy(x => x.Dt);

            var marketProfile = await _marketProfileService.ApiMarketProfileGetAsync();
            var assetPairsHash = marketProfile.Select(i => i.AssetPair).ToHashSet();

            foreach (var record in tempDataValues)
            {
                try
                {
                    var assetPair = FindPairWithAssets(assetPairs, record.Asset1, record.Asset2);
                    if (assetPair == null || record.Volume1 <= 0 || !assetPairsHash.Contains(assetPair.Id))
                        continue;

                    var isInverted = IsInvertedTarget(assetPair, record.Asset1);
                    var volume = isInverted ? record.Volume1 : record.Volume2;

                    if (newMarketData.ContainsKey(assetPair.Id))
                    {
                        newMarketData[assetPair.Id].LastPrice = record.Price;
                        newMarketData[assetPair.Id].Volume += volume;
                    }
                    else
                    {
                        newMarketData.Add(assetPair.Id, new MarketData
                        {
                            AssetPairId = assetPair.Id,
                            LastPrice = record.Price,
                            Volume = volume,
                            Dt = DateTime.UtcNow
                        });
                    }
                }
                catch (Exception ex)
                {
                    _log.WriteError("FillMarketDataAsync", record.ToJson(), ex);
                }
            }

            await _marketDataRepository.AddOrMergeMarketData(newMarketData.Values);
        }

        private static AssetPair FindPairWithAssets(IEnumerable<AssetPair> src, string assetId1, string assetId2)
        {
            return src.FirstOrDefault(assetPair =>
                assetPair.BaseAssetId == assetId1 && assetPair.QuotingAssetId == assetId2 ||
                assetPair.BaseAssetId == assetId2 && assetPair.QuotingAssetId == assetId1
            );
        }

        private static bool IsInvertedTarget(AssetPair assetPair, string targetAsset)
        {
            return assetPair.QuotingAssetId == targetAsset;
        }

        private void HandleTradeRecords(IEnumerable<ClientTrade> trades)
        {
            foreach (var item in trades)
            {
                try
                {
                    HandleTradeRecord(item);
                }
                catch (Exception ex)
                {
                    _log.WriteError("HandleTradeRecords", item.ToJson(), ex);
                }
            }
        }

        private void HandleTradeRecord(ClientTrade trade)
        {
            var key = TemporaryAggregatedData.CreateKey(trade.LimitOrderId, trade.DateTime);
            if (!_tempDataByLimitOrderAndDtId.ContainsKey(key))
            {
                _tempDataByLimitOrderAndDtId.Add(key, new TemporaryAggregatedData
                {
                    Asset1 = trade.AssetId,
                    ClientId = trade.ClientId,
                    LimitOrderAndDateTimeKey = key,
                    Volume1 = Math.Abs(trade.Amount),
                    Price = trade.Price,
                    Dt = trade.DateTime
                });
                return;
            }

            var tempRecord = _tempDataByLimitOrderAndDtId[key];
            if (tempRecord.ClientId == trade.ClientId)
            {
                tempRecord.Asset2 = trade.AssetId;
                tempRecord.Volume2 = Math.Abs(trade.Amount);
            }
        }
    }
}