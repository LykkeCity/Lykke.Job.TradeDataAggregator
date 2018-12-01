using Autofac;
using Common;

namespace Lykke.Job.TradeDataAggregator.Core.Services
{
    public interface IStartStop : IStartable, IStopable
    {
    }
}
