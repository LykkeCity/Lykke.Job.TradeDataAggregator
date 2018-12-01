using System;

namespace Lykke.Job.TradeDataAggregator.Models
{
    public class IsAliveResponse
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Env { get; set; }
        public DateTime LastClientsScanningStartedMoment { get; set; }
        public TimeSpan LastClientsScanningDuration { get; set; }
        public TimeSpan MaxHealthyClientsScanningDuration { get; set; }
        public string HealthWarning { get; set; }
    }
}