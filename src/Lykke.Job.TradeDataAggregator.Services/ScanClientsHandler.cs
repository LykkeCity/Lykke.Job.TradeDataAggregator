using Common;
using Lykke.Job.TradeDataAggregator.Core.Services;
using System;
using System.Threading.Tasks;
using Common.Log;

namespace Lykke.Job.TradeDataAggregator.Services
{
    public class ScanClientsHandler : TimerPeriod, IScanClientsHandler
    {
        private readonly ITradeDataAggregationService _tradeDataAggregationService;
        private readonly IHealthService _healthService;

        public ScanClientsHandler(ITradeDataAggregationService tradeDataAggregationService,
            IHealthService healthService, ILog log) : base(nameof(ScanClientsHandler),
            (int)TimeSpan.FromSeconds(30).TotalMilliseconds, log)
        {
            _tradeDataAggregationService = tradeDataAggregationService;
            _healthService = healthService;
        }

        public override async Task Execute()
        {
            try
            {
                _healthService.TraceClientsScanningStarted();

                await _tradeDataAggregationService.ScanClientsAsync();

                _healthService.TraceClientsScanningCompleted();
            }
            catch
            {
                _healthService.TraceClientsScanningFailed();
            }
        }
    }
}