using System.Threading.Tasks;

namespace Lykke.Job.TradeDataAggregator.Core.Services
{
    public interface IStartupManager
    {
        Task StartAsync();
    }
}