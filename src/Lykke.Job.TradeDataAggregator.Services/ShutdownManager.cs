using System.Threading.Tasks;
using Common.Log;
using Lykke.Job.TradeDataAggregator.Core.Services;

namespace Lykke.Job.TradeDataAggregator.Services
{
    // NOTE: Sometimes, shutdown process should be expressed explicitly. 
    // If this is your case, use this class to manage shutdown.
    // For example, sometimes some state should be saved only after all incoming message processing and 
    // all periodical handler was stopped, and so on.
    
    public class ShutdownManager : IShutdownManager
    {
        private readonly ILog _log;
        private readonly IScanClientsHandler _scanClientsHandler;

        public ShutdownManager(ILog log, IScanClientsHandler scanClientsHandler)
        {
            _log = log;
            _scanClientsHandler = scanClientsHandler;
        }

        public async Task StopAsync()
        {
            await _log.WriteInfoAsync(nameof(StopAsync), "", "Stopping scan clients handler");

            _scanClientsHandler.Stop();

            await _log.WriteInfoAsync(nameof(StopAsync), "", "Stopped");
        }
    }
}