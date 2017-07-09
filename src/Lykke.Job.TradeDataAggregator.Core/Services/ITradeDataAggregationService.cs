using System.Threading.Tasks;

namespace Lykke.Job.TradeDataAggregator.Core.Services
{
    public interface ITradeDataAggregationService
    {
        Task ScanClientsAsync();
    }
}