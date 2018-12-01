using System;
using System.IO;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Common;
using Lykke.JobTriggers.Triggers;
using Microsoft.AspNetCore.Hosting;

namespace Lykke.Job.TradeDataAggregator
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine($"{AppEnvironment.Name} {AppEnvironment.Version}");

            var webHostCancellationTokenSource = new CancellationTokenSource();
            TriggerHost triggerHost = null;
            Task webHostTask = null;
            Task triggerHostTask = null;
            var end = new ManualResetEvent(false);

            try
            {
                AssemblyLoadContext.Default.Unloading += ctx =>
                {
                    Console.WriteLine("SIGTERM recieved");

                    webHostCancellationTokenSource.Cancel();

                    end.WaitOne();
                };

                var hostBuilder = new WebHostBuilder()
                    .UseKestrel()
                    .UseUrls("http://*:5000")
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>();
#if !DEBUG
                hostBuilder = hostBuilder.UseApplicationInsights();
#endif
                var webHost = hostBuilder.Build();

                triggerHost = new TriggerHost(webHost.Services);

                webHostTask = webHost.RunAsync(webHostCancellationTokenSource.Token);
                triggerHostTask = triggerHost.Start();

                // WhenAny to handle any task termination with exception, 
                // or gracefully termination of webHostTask
                Task.WhenAny(webHostTask, triggerHostTask).Wait();
            }
            finally
            {
                Console.WriteLine("Terminating...");

                webHostCancellationTokenSource.Cancel();
                triggerHost?.Cancel();

                webHostTask?.Wait();
                triggerHostTask?.Wait();

                end.Set();
            }
        }
    }
}