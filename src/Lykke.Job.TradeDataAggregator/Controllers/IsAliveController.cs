﻿using System;
using System.Net;
using Lykke.Common;
using Lykke.Job.TradeDataAggregator.Core.Services;
using Lykke.Job.TradeDataAggregator.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Job.TradeDataAggregator.Controllers
{
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        private readonly IHealthService _healthService;

        public IsAliveController(IHealthService healthService)
        {
            _healthService = healthService;
        }

        /// <summary>
        /// Checks service is alive
        /// </summary>
        [HttpGet]
        [SwaggerOperation("IsAlive")]
        [ProducesResponseType(typeof(IsAliveResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public IActionResult Get()
        {
            var healthViloationMessage = _healthService.GetHealthViolationMessage();
            if (healthViloationMessage != null)
            {
                return StatusCode((int) HttpStatusCode.InternalServerError, new ErrorResponse
                {
                    ErrorMessage = $"Job is unhealthy: {healthViloationMessage}"
                });
            }

            return Ok(new IsAliveResponse
            {
                Name = AppEnvironment.Name,
                Version = AppEnvironment.Version,
                Env = Environment.GetEnvironmentVariable("ENV_INFO"),
                LastClientsScanningStartedMoment = _healthService.LastClientsScanningStartedMoment,
                LastClientsScanningDuration = _healthService.LastClientsScanningDuration,
                MaxHealthyClientsScanningDuration = _healthService.MaxHealthyClientsScanningDuration,
                HealthWarning = _healthService.GetHealthWarningMessage() ?? "No"
            });
        }
    }
}