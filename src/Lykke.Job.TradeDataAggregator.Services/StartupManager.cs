using System.Threading.Tasks;
using Common.Log;
using Lykke.Job.TradeDataAggregator.Core.Services;

namespace Lykke.Job.TradeDataAggregator.Services
{
    // NOTE: Sometimes, startup process which is expressed explicitly is not just better, 
    // but the only way. If this is your case, use this class to manage startup.
    // For example, sometimes some state should be restored before any periodical handler will be started, 
    // or any incoming message will be processed and so on.
    // Do not forget to remove As<IStartable>() and AutoActivate() from DI registartions of services, 
    // which you want to startup explicitly.

    public class StartupManager : IStartupManager
    {
        private readonly ILog _log;
        private readonly IScanClientsHandler _scanClientsHandler;

        public StartupManager(ILog log, IScanClientsHandler scanClientsHandler)
        {
            _log = log;
            _scanClientsHandler = scanClientsHandler;
        }

        public async Task StartAsync()
        {
            await _log.WriteInfoAsync(nameof(StartAsync), "", "Starting scan clients handler");

            _scanClientsHandler.Start();

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Started up");
        }
    }
}