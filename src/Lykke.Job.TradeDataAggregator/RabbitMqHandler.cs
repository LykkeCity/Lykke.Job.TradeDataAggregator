﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.Job.TradeDataAggregator.Core;
using Lykke.Job.TradeDataAggregator.Core.Domain.Exchange;
using Lykke.Job.TradeDataAggregator.Services.Models;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Newtonsoft.Json;

namespace Lykke.Job.TradeDataAggregator
{
    public class RabbitMqHandler : IStartable, IDisposable
    {
        private readonly AppSettings.RabbitMqSettings _rabbitMqSettings;
        private readonly ITradesCommonRepository _tradesCommonRepository;
        private readonly IAssetsService _assetsService;
        private readonly ILog _log;
        private RabbitMqSubscriber<TradeQueueItem> _tradesSubscriber;

        public RabbitMqHandler(AppSettings.RabbitMqSettings rabbitMqSettings,
            ITradesCommonRepository tradesCommonRepository,
            IAssetsService assetsService,
            ILog log)
        {
            _rabbitMqSettings = rabbitMqSettings;
            _tradesCommonRepository = tradesCommonRepository;
            _assetsService = assetsService;
            _log = log;
        }

        public void Start()
        {
            _log.WriteInfoAsync(nameof(RabbitMqHandler), nameof(Start), string.Empty, "Starting").Wait();

            var rabbitSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString =
                    $"amqp://{_rabbitMqSettings.Username}:{_rabbitMqSettings.Password}@{_rabbitMqSettings.Host}:{_rabbitMqSettings.Port}",
                ExchangeName = _rabbitMqSettings.ExchangeSwap,
                IsDurable = false,
                QueueName = $"{_rabbitMqSettings.ExchangeSwap}-tradedataaggregator",
            };

            _tradesSubscriber = new RabbitMqSubscriber<TradeQueueItem>(rabbitSettings, new DeadQueueErrorHandlingStrategy(_log, rabbitSettings))
                .SetMessageDeserializer(new JsonDeserializer<TradeQueueItem>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                .SetLogger(_log)
                .Subscribe(ProcessTrade)
                .Start();
        }

        public void Dispose()
        {
            _tradesSubscriber.Stop();
            _log.WriteInfoAsync(nameof(RabbitMqHandler), nameof(Dispose), string.Empty, "Stopping").Wait();
        }

        private async Task ProcessTrade(TradeQueueItem message)
        {
            if (!message.Order.Status.Equals("matched", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var pair = await _assetsService.GetAssetPairAsync(message.Order.AssetPairId) as AssetPairResponseModel;

            if (pair == null) throw new ArgumentNullException(nameof(pair));

            bool isLimitAssetBase = message.Trades.First().LimitAsset == pair.BaseAssetId;

            var limitAsset = await _assetsService.AssetGetAsync(message.Trades.First().LimitAsset);
            if (limitAsset == null) throw new ArgumentNullException(nameof(limitAsset));

            var marketAsset = await _assetsService.AssetGetAsync(message.Trades.First().MarketAsset);
            if (marketAsset == null) throw new ArgumentNullException(nameof(marketAsset));

            foreach (var trade in message.Trades)
            {
                await _tradesCommonRepository.InsertCommonTrade(new TradeCommon
                {
                    Amount = isLimitAssetBase ? trade.LimitVolume : trade.MarketVolume,
                    AssetPair = message.Order.AssetPairId,
                    BaseAsset = isLimitAssetBase ? limitAsset.DisplayId : marketAsset.DisplayId,
                    BaseAssetId = isLimitAssetBase ? trade.LimitAsset : trade.MarketAsset,
                    QuotAssetId = isLimitAssetBase ? trade.MarketAsset : trade.LimitAsset,
                    Dt = trade.Timestamp,
                    Id = Guid.NewGuid().ToString(),
                    LimitOrderId = trade.LimitOrderId,
                    MarketOrderId = message.Order.Id,
                    Price = trade.Price.GetValueOrDefault(),
                    QuotAsset = isLimitAssetBase ? marketAsset.DisplayId : limitAsset.DisplayId,
                });
            }
        }
    }

    public class JsonDeserializer<T> : IMessageDeserializer<T>
    {
        public T Deserialize(byte[] data)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data));
        }
    }
}
