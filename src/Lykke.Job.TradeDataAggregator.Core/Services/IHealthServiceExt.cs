namespace Lykke.Job.TradeDataAggregator.Core.Services
{
    public interface IHealthServiceExt
    {
        void TraceClientsScanningStarted();

        void TraceClientsScanningCompleted();

        void TraceClientsScanningFailed();
    }
}