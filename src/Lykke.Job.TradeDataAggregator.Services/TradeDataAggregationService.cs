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
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.OperationsHistory.Client;
using Lykke.Service.OperationsHistory.Client.Models;
using Lykke.Service.OperationsRepository.Contract.Cash;
using Newtonsoft.Json;

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

        private readonly IOperationsHistoryClient _operationsHistoryClient;
        private readonly IMarketDataRepository _marketDataRepository;
        private readonly IAssetsServiceWithCache _assetsService;
        private readonly ILog _log;
        private readonly MarketProfile _marketProfile;

        private readonly Dictionary<string, TemporaryAggregatedData> _tempDataByLimitOrderAndDtId = new Dictionary<string, TemporaryAggregatedData>();

        public TradeDataAggregationService(
            IOperationsHistoryClient operationsHistoryClient,
            IMarketDataRepository marketDataRepository,
            IAssetsServiceWithCache assetsService,
            IAssetPairBestPriceRepository assetPairBestPriceRepository,
            ILog log)
        {
            _operationsHistoryClient = operationsHistoryClient ?? throw new ArgumentNullException(nameof(operationsHistoryClient));
            _marketDataRepository = marketDataRepository ?? throw new ArgumentNullException(nameof(marketDataRepository));
            _assetsService = assetsService ?? throw new ArgumentNullException(nameof(assetsService));
            _log = log ?? throw new ArgumentNullException(nameof(log));

            _marketProfile = assetPairBestPriceRepository
                .GetAsync()
                .GetAwaiter()
                .GetResult();
        }

        public async Task ScanClientsAsync()
        {
            var historyResponse = await _operationsHistoryClient.GetByDateRange(DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)), DateTime.UtcNow, "ClientTrade");

            if (historyResponse.Error != null)
            {
                throw new Exception(historyResponse.Error.Message);
            }
            
            await HandleTradeRecords(historyResponse.Records);

            await FillMarketData();
        }

        private async Task FillMarketData()
        {
            var newMarketData = new Dictionary<string, IMarketData>();
            var assetPairs = await _assetsService.GetAllAssetPairsAsync();
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

        private async Task HandleTradeRecords(IEnumerable<HistoryRecordModel> trades)
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

        private void HandleTradeRecord(HistoryRecordModel historyTradeRecord)
        {
            var clienTrade = JsonConvert.DeserializeObject<ClientTradeDto>(historyTradeRecord.CustomData);

            if (!string.IsNullOrWhiteSpace(clienTrade.LimitOrderId))
            {
                // TODO: use clientId instead of trade.WalletId once ME is updated to write walletId for trade operations

                var key = TemporaryAggregatedData.CreateKey(clienTrade.LimitOrderId, historyTradeRecord.DateTime);
                if (!_tempDataByLimitOrderAndDtId.ContainsKey(key))
                {
                    _tempDataByLimitOrderAndDtId.Add(key, new TemporaryAggregatedData
                    {
                        Asset1 = clienTrade.AssetId,
                        ClientId = historyTradeRecord.WalletId,
                        LimitOrderAndDateTimeKey = key,
                        Volume1 = Math.Abs(historyTradeRecord.Amount),
                        Price = clienTrade.Price,
                        Dt = historyTradeRecord.DateTime
                    });
                    return;
                }

                var tempRecord = _tempDataByLimitOrderAndDtId[key];
                if (tempRecord.ClientId == historyTradeRecord.WalletId)
                {
                    tempRecord.Asset2 = clienTrade.AssetId;
                    tempRecord.Volume2 = Math.Abs(historyTradeRecord.Amount);
                }
            }
        }
    }
}