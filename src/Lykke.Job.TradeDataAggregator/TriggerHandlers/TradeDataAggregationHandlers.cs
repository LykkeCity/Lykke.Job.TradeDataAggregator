using System.Threading.Tasks;
using Lykke.Job.TradeDataAggregator.Core.Services;
using Lykke.JobTriggers.Triggers.Attributes;

namespace Lykke.Job.TradeDataAggregator.TriggerHandlers
{
    public class TradeDataAggregationHandlers
    {
        private readonly ITradeDataAggregationService _tradeDataAggregationService;
        private readonly IHealthService _healthService;

        public TradeDataAggregationHandlers(ITradeDataAggregationService tradeDataAggregationService,
            IHealthService healthService)
        {
            _tradeDataAggregationService = tradeDataAggregationService;
            _healthService = healthService;
        }

        [TimerTrigger("00:00:30")]
        public async Task ScanClients()
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