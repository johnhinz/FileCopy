using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FileCopy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureLogging((hostContext, logging) =>
                {
                    var serilogLogger = new LoggerConfiguration()
                    .ReadFrom.Configuration(hostContext.Configuration)
                    .CreateLogger();

                    logging.ClearProviders();
                    logging.AddSerilog(serilogLogger);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>((serviceProvider) =>
                    {
                        return new Worker(serviceProvider.GetService <ILogger<Worker>>(),
                            hostContext.Configuration.GetSection("SourceDirectory").Value,
                            hostContext.Configuration.GetSection("DestinationDirectory").Value);
                    });
                });
    }
}
