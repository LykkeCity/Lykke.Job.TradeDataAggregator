using System;
using System.Collections.Generic;
using Lykke.Job.TradeDataAggregator.Core.Services;
using Lykke.Sdk.Health;

namespace Lykke.Job.TradeDataAggregator.Services
{
    public class HealthService : IHealthService, IHealthServiceExt
    {
        public DateTime LastClientsScanningStartedMoment { get; private set; }
        public TimeSpan LastClientsScanningDuration { get; private set; }
        public TimeSpan MaxHealthyClientsScanningDuration { get; }

        private bool WasLastClientsScanningFailed { get; set; }
        private bool WasLastClientsScanningCompleted { get; set; }
        private bool WasClientsScanningEverStarted { get; set; }

        public HealthService(TimeSpan maxHealthyClientsScanningDuration)
        {
            MaxHealthyClientsScanningDuration = maxHealthyClientsScanningDuration;

            LastClientsScanningStartedMoment = DateTime.MinValue;
            LastClientsScanningDuration = TimeSpan.Zero;
            WasLastClientsScanningFailed = false;
            WasLastClientsScanningCompleted = false;
            WasClientsScanningEverStarted = false;
        }

        public void TraceClientsScanningStarted()
        {
            LastClientsScanningStartedMoment = DateTime.UtcNow;
            WasClientsScanningEverStarted = true;
        }

        public void TraceClientsScanningCompleted()
        {
            LastClientsScanningDuration = DateTime.UtcNow - LastClientsScanningStartedMoment;
            WasLastClientsScanningFailed = false;
            WasLastClientsScanningCompleted = true;
        }

        public void TraceClientsScanningFailed()
        {
            WasLastClientsScanningFailed = true;
            WasLastClientsScanningCompleted = false;
        }

        public string GetHealthViolationMessage()
        {
            if (!WasLastClientsScanningCompleted && !WasLastClientsScanningFailed && !WasClientsScanningEverStarted)
            {
                return "Waiting for first clients scanning execution started";
            }

            if (!WasLastClientsScanningCompleted && !WasLastClientsScanningFailed && WasClientsScanningEverStarted)
            {
                return $"Waiting {DateTime.UtcNow - LastClientsScanningStartedMoment} for first clients scanning execution completed";
            }

            var lastDuration = LastClientsScanningDuration;
            if (lastDuration > MaxHealthyClientsScanningDuration)
            {
                return $"Last clients scanning was lasted for {lastDuration}, which is too long";
            }

            if (WasLastClientsScanningFailed)
            {
                return "Last clients scanning was failed";
            }

            return null;
        }

        public IEnumerable<HealthIssue> GetHealthIssues()
        {
            var issues = new List<HealthIssue>();
            var message = GetHealthViolationMessage();
            if (message != null)
                issues.Add(HealthIssue.Create(string.Empty, message));
            return issues;
        }

        public void Register(string type, string value)
        {
        }

        public void ClearAllIssues()
        {
        }

        public void ClearIssuesByType(string type)
        {
        }
    }
}