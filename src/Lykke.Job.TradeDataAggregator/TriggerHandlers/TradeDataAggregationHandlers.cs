using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Job.TradeDataAggregator.Core.Domain.Assets;
using Lykke.Job.TradeDataAggregator.Core.Domain.CacheOperations;
using Lykke.Job.TradeDataAggregator.Core.Domain.Exchange;
using Lykke.Job.TradeDataAggregator.Core.Domain.Feed;
using Lykke.JobTriggers.Triggers.Attributes;

namespace Lykke.Job.TradeDataAggregator.TriggerHandlers
{
    public class TradeDataAggregationHandlers
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

        private readonly IClientTradesRepository _clientTradesRepository;
        private readonly IMarketDataRepository _marketDataRepository;
        private readonly CachedDataDictionary<string, IAssetPair> _assetPairsDict;
        private readonly ILog _log;
        private readonly MarketProfile _marketProfile;

        private readonly Dictionary<string, TemporaryAggregatedData> _tempDataByLimitOrderAndDtId = new Dictionary<string, TemporaryAggregatedData>();

        public TradeDataAggregationHandlers(IClientTradesRepository clientTradesRepository,
            IMarketDataRepository marketDataRepository,
            CachedDataDictionary<string, IAssetPair> assetPairsDict,
            IAssetPairBestPriceRepository assetPairBestPriceRepository,
            ILog log)
        {
            _clientTradesRepository = clientTradesRepository;
            _marketDataRepository = marketDataRepository;
            _assetPairsDict = assetPairsDict;
            _log = log;
            _marketProfile = assetPairBestPriceRepository.GetAsync().Result;
        }

        [TimerTrigger("00:30:00")]
        public async Task ScanClients()
        {
            try
            {
                await _clientTradesRepository.ScanByDtAsync(HandleTradeRecords, DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)), DateTime.UtcNow);

                await FillMarketData();
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync("TradeDataAggregator", "ScanClients", "", ex);
            }
        }

        private async Task FillMarketData()
        {
            var assetPairsDict = await _assetPairsDict.GetDictionaryAsync();

            var newMarketData = new Dictionary<string, IMarketData>();
            var assetPairs = assetPairsDict.Values.ToArray();

            var tempDataValues = _tempDataByLimitOrderAndDtId.Values.OrderBy(x => x.Dt);

            foreach (var record in tempDataValues)
            {
                try
                {
                    var assetPair = assetPairs.PairWithAssets(record.Asset1, record.Asset2);
                    if (assetPair != null && record.Volume1 > 0 &&
                        _marketProfile.Profile.Any(x => x.Asset == assetPair.Id))
                    {
                        bool isInverted = assetPair.IsInverted(record.Asset1);


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

        private async Task HandleTradeRecords(IEnumerable<IClientTrade> trades)
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

        private void HandleTradeRecord(IClientTrade trade)
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