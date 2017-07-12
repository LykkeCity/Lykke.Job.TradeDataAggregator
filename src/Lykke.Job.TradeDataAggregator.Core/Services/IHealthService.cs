using System;

namespace Lykke.Job.TradeDataAggregator.Core.Services
{
    public interface IHealthService
    {
        DateTime LastClientsScanningStartedMoment { get; }
        TimeSpan LastClientsScanningDuration { get; }
        TimeSpan MaxHealthyClientsScanningDuration { get; }

        void TraceClientsScanningStarted();
        void TraceClientsScanningCompleted();
        void TraceClientsScanningFailed();
        string GetHealthViolationMessage();
        string GetHealthWarningMessage();
    }
}