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

        private readonly TimeSpan _scanMaxDuration = TimeSpan.FromSeconds(30);
        private readonly IMarketDataRepository _marketDataRepository;
        private readonly IAssetsService _assetsService;
        private readonly ITradeOperationsRepositoryClient _tradeOperationsRepositoryClient;
        private readonly ILykkeMarketProfile _marketProfileService;
        private readonly ILog _log;

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
            var now = DateTime.UtcNow;

            string continuationToken = null;
            var tempDataByLimitOrderAndDtId = new Dictionary<string, TemporaryAggregatedData>();

            do
            {
                var tradesResult = await _tradeOperationsRepositoryClient.GetByDatesAsync(
                    now.Subtract(TimeSpan.FromDays(1)),
                    now,
                    continuationToken);
                HandleTradeRecords(tradesResult.Trades, tempDataByLimitOrderAndDtId);
                continuationToken = tradesResult.ContinuationToken;
            } while (continuationToken != null);

            await FillMarketDataAsync(tempDataByLimitOrderAndDtId);

            var scanDuration = DateTime.UtcNow - now;
            if (scanDuration > _scanMaxDuration)
                _log.WriteInfoAsync(nameof(TradeDataAggregationService), nameof(ScanClientsAsync), $"Scan took {scanDuration.TotalSeconds} seconds");

            GC.Collect(GC.MaxGeneration);
        }

        private async Task FillMarketDataAsync(Dictionary<string, TemporaryAggregatedData> tempDataByLimitOrderAndDtId)
        {
            var newMarketData = new Dictionary<string, IMarketData>();
            var assetPairs = await _assetsService.AssetPairGetAllAsync();
            var tempDataValues = tempDataByLimitOrderAndDtId.Values.OrderBy(x => x.Dt);

            var marketProfile = await _marketProfileService.ApiMarketProfileGetAsync();
            var assetPairsHash = marketProfile.Select(i => i.AssetPair).ToHashSet();

            foreach (var record in tempDataValues)
            {
                if (record.Volume1 <= 0)
                    continue;

                try
                {
                    var assetPair = FindPairWithAssets(assetPairs, record.Asset1, record.Asset2);
                    if (assetPair == null || !assetPairsHash.Contains(assetPair.Id))
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

        private void HandleTradeRecords(IEnumerable<ClientTrade> trades, Dictionary<string, TemporaryAggregatedData> tempDataByLimitOrderAndDtId)
        {
            foreach (var item in trades)
            {
                try
                {
                    HandleTradeRecord(item, tempDataByLimitOrderAndDtId);
                }
                catch (Exception ex)
                {
                    _log.WriteError("HandleTradeRecords", item.ToJson(), ex);
                }
            }
        }

        private void HandleTradeRecord(ClientTrade trade, Dictionary<string, TemporaryAggregatedData> tempDataByLimitOrderAndDtId)
        {
            var key = TemporaryAggregatedData.CreateKey(trade.LimitOrderId, trade.DateTime);
            if (!tempDataByLimitOrderAndDtId.ContainsKey(key))
            {
                tempDataByLimitOrderAndDtId.Add(key, new TemporaryAggregatedData
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

            var tempRecord = tempDataByLimitOrderAndDtId[key];
            if (tempRecord.ClientId == trade.ClientId)
            {
                tempRecord.Asset2 = trade.AssetId;
                tempRecord.Volume2 = Math.Abs(trade.Amount);
            }
        }
    }
}