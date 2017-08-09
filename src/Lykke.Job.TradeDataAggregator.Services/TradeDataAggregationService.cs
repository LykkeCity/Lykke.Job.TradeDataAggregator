using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Job.TradeDataAggregator.Core.Domain.Exchange;
using Lykke.Job.TradeDataAggregator.Core.Domain.Feed;
using Lykke.Job.TradeDataAggregator.Core.Services;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Custom;
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

        private readonly ITradeOperationsRepositoryClient _clientTradesRepositoryClient;
        private readonly IMarketDataRepository _marketDataRepository;
        private readonly IAssetsservice _assetsService;
        private readonly ILog _log;
        private readonly MarketProfile _marketProfile;

        private readonly Dictionary<string, TemporaryAggregatedData> _tempDataByLimitOrderAndDtId = new Dictionary<string, TemporaryAggregatedData>();

        public TradeDataAggregationService(
            ITradeOperationsRepositoryClient clientTradesRepositoryClient,
            IMarketDataRepository marketDataRepository,
            IAssetsservice assetsService,
            IAssetPairBestPriceRepository assetPairBestPriceRepository,
            ILog log)
        {
            _clientTradesRepositoryClient = clientTradesRepositoryClient;
            _marketDataRepository = marketDataRepository;
            _assetsService = assetsService;
            _log = log;
            _marketProfile = assetPairBestPriceRepository.GetAsync().Result;
        }

        public async Task ScanClientsAsync()
        {
            var trades = await _clientTradesRepositoryClient.ScanByDtAsync(
                DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)),
                DateTime.UtcNow);

            await HandleTradeRecords(trades);

            await FillMarketData();
        }

        private async Task FillMarketData()
        {
            var newMarketData = new Dictionary<string, IMarketData>();
            var assetPairs = await _assetsService.GetAssetPairsAsync();
            var tempDataValues = _tempDataByLimitOrderAndDtId.Values.OrderBy(x => x.Dt);

            foreach (var record in tempDataValues)
            {
                try
                {
                    var assetPair = FindPairWithAssets(assetPairs, record.Asset1, record.Asset2);
                    if (assetPair != null && record.Volume1 > 0 &&
                        _marketProfile.Profile.Any(x => x.Asset == assetPair.Id))
                    {
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
                }
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync("TradeDataAggregator", "FillMarketData", record.ToJson(), ex);
                }
            }

            await _marketDataRepository.AddOrMergeMarketData(newMarketData.Values);
        }

        private static IAssetPair FindPairWithAssets(IEnumerable<IAssetPair> src, string assetId1, string assetId2)
        {
            return src.FirstOrDefault(assetPair =>
                assetPair.BaseAssetId == assetId1 && assetPair.QuotingAssetId == assetId2 ||
                assetPair.BaseAssetId == assetId2 && assetPair.QuotingAssetId == assetId1
            );
        }

        private static bool IsInvertedTarget(IAssetPair assetPair, string targetAsset)
        {
            return assetPair.QuotingAssetId == targetAsset;
        }

        private async Task HandleTradeRecords(IEnumerable<ClientTrade> trades)
        {
            foreach (var item in trades)
            {
                try
                {
                    HandleTradeRecord(item);
                }
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync("TradeDataAggregator", "HandleTradeRecords", item.ToJson(), ex);
                }
            }
        }

        private void HandleTradeRecord(ClientTrade trade)
        {
            var key = TemporaryAggregatedData.CreateKey(trade.LimitOrderId, trade.DateTime ?? default(DateTime));
            if (!_tempDataByLimitOrderAndDtId.ContainsKey(key))
            {
                _tempDataByLimitOrderAndDtId.Add(key, new TemporaryAggregatedData
                {
                    Asset1 = trade.AssetId,
                    ClientId = trade.ClientId,
                    LimitOrderAndDateTimeKey = key,
                    Volume1 = Math.Abs(trade.Amount ?? default(double)),
                    Price = trade.Price ?? default(double),
                    Dt = trade.DateTime ?? default(DateTime)
                });
                return;
            }

            var tempRecord = _tempDataByLimitOrderAndDtId[key];
            if (tempRecord.ClientId == trade.ClientId)
            {
                tempRecord.Asset2 = trade.AssetId;
                tempRecord.Volume2 = Math.Abs(trade.Amount ?? default(double));
            }
        }
    }
}