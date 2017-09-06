using System;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Lykke.JobTriggers.Triggers;
using Microsoft.AspNetCore.Hosting;

namespace Lykke.Job.TradeDataAggregator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"TradeDataAggregator version: { Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion}");
#if DEBUG
            Console.WriteLine("Is DEBUG");
#else
            Console.WriteLine("Is RELEASE");
#endif

            var webHost = new WebHostBuilder()
                    .UseKestrel()
                    .UseUrls("http://*:5000")
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>()
                    .UseApplicationInsights()
                    .Build();

            webHost.Run();

            Console.WriteLine("Terminated");
        }
    }
}