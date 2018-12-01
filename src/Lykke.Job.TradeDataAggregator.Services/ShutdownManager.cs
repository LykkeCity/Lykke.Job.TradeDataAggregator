using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Lykke.Job.TradeDataAggregator.Core.Services;
using Lykke.Sdk;

namespace Lykke.Job.TradeDataAggregator.Services
{
    public class ShutdownManager : IShutdownManager
    {
        private readonly List<IStopable> _stopables = new List<IStopable>();

        public ShutdownManager(IEnumerable<IStartStop> stopables)
        {
            _stopables.AddRange(stopables);
        }

        public Task StopAsync()
        {
            Parallel.ForEach(_stopables, item => item.Stop());

            return Task.CompletedTask;
        }
    }
}
