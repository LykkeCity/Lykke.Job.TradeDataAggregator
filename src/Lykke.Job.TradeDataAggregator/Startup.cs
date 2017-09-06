using System;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Job.TradeDataAggregator.Core;
using Lykke.Job.TradeDataAggregator.Models;
using Lykke.Job.TradeDataAggregator.Modules;
using Lykke.JobTriggers.Extenstions;
using Lykke.JobTriggers.Triggers;
using Lykke.Logs;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.TradeDataAggregator
{
    public class Startup
    {
        public IHostingEnvironment Environment { get; }
        public IContainer ApplicationContainer { get; set; }
        public IConfigurationRoot Configuration { get; }

        private TriggerHost _triggerHost;
        private Task _triggerHostTask;

        private const string AppName = "Lykke.Job.TradeDataAggregator";

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            Environment = env;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver =
                        new Newtonsoft.Json.Serialization.DefaultContractResolver();
                });

            services.AddSwaggerGen(options =>
            {
                options.DefaultLykkeConfiguration("v1", "TradeDataAggregator API");
            });

            var builder = new ContainerBuilder();
            var appSettings = Environment.IsDevelopment()
                ? Configuration.Get<AppSettings>()
                : HttpSettingsLoader.Load<AppSettings>(Configuration.GetValue<string>("SettingsUrl"));
            var log = CreateLogWithSlack(services, appSettings);

            builder.RegisterModule(new JobModule(appSettings, log));
            builder.AddTriggers();
            builder.Populate(services);

            ApplicationContainer = builder.Build();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseLykkeMiddleware("TradeDataAggregator", ex => new ErrorResponse { ErrorMessage = "Technical problem" });

            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUi();
            app.UseStaticFiles();

            appLifetime.ApplicationStarted.Register(Start);
            appLifetime.ApplicationStopping.Register(StopApplication);
            appLifetime.ApplicationStopped.Register(CleanUp);
        }

        private void Start()
        {
            _triggerHost = new TriggerHost(new AutofacServiceProvider(ApplicationContainer));
            _triggerHostTask = _triggerHost.Start();
        }

        private void StopApplication()
        {
            _triggerHost?.Cancel();
            _triggerHostTask?.Wait();
        }

        private void CleanUp()
        {
            ApplicationContainer.Dispose();
        }

        private static ILog CreateLogWithSlack(IServiceCollection services, AppSettings settings)
        {
            LykkeLogToAzureStorage logToAzureStorage = null;

            var logToConsole = new LogToConsole();
            var logAggregate = new LogAggregate();

            logAggregate.AddLogger(logToConsole);

            var dbLogConnectionString = settings.TradeDataAggregatorJob.Db.LogsConnString;

            // Creating azure storage logger, which logs own messages to concole log
            if (!string.IsNullOrEmpty(dbLogConnectionString) && !(dbLogConnectionString.StartsWith("${") && dbLogConnectionString.EndsWith("}")))
            {
                var persistanceManager = new LykkeLogToAzureStoragePersistenceManager(AppName,
                    AzureTableStorage<LogEntity>.Create(() => dbLogConnectionString, "TradeDataAggregatorLog", logToConsole));

                logToAzureStorage = new LykkeLogToAzureStorage(AppName, persistanceManager, lastResortLog: logToConsole);

                logAggregate.AddLogger(logToAzureStorage);
            }

            // Creating aggregate log, which logs to console and to azure storage, if last one specified
            var log = logAggregate.CreateLogger();

            // Creating slack notification service, which logs own azure queue processing messages to aggregate log
            var slackService = services.UseSlackNotificationsSenderViaAzureQueue(new AzureQueueIntegration.AzureQueueSettings
            {
                ConnectionString = settings.SlackNotifications.AzureQueue.ConnectionString,
                QueueName = settings.SlackNotifications.AzureQueue.QueueName,

            }, log);

            // Finally, setting slack notification for azure storage log, which will forward necessary message to slack service
            logToAzureStorage?.SetSlackNotificationsManager(
                new LykkeLogToAzureSlackNotificationsManager(AppName, slackService, logToConsole));

            logToAzureStorage?.Start();

            return log;
        }
    }
}