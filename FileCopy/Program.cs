using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;
using MQTTnet.Client.Publishing;

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
                    int retryDelay;
                    if (!int.TryParse(hostContext.Configuration.GetSection("RetryDelay").Value, out retryDelay))
                    {
                        retryDelay = 1000;
                    }

                    int retryCount;
                    if (!int.TryParse(hostContext.Configuration.GetSection("RetryCount").Value, out retryCount))
                    {
                        retryCount = 5;
                    }

                    services.AddTransient<IPublishDetections<MqttClientPublishResult>>((ServiceProvider) =>
                    {
                        return new MqttAIPublish(
                            ServiceProvider.GetService<ILogger<MqttAIPublish>>(),
                            hostContext.Configuration.GetSection("RepositoryEndpoint").Value,
                            hostContext.Configuration.GetSection("PublisherName").Value,
                            hostContext.Configuration.GetSection("TopicParser").Value,
                            int.Parse(hostContext.Configuration.GetSection("TopicPosition").Value),
                            hostContext.Configuration.GetSection("QueueName").Value
                            );
                    });

                    services.AddHostedService<Worker>((serviceProvider) =>
                    {
                        return new Worker(serviceProvider.GetService <ILogger<Worker>>(),
                            serviceProvider.GetService<IPublishDetections<MqttClientPublishResult>>(),
                            hostContext.Configuration.GetSection("SourceDirectory").Value,
                            hostContext.Configuration.GetSection("DestinationDirectory").Value,
                            retryDelay,
                            retryCount
                            );
                    });
                });
    }
}
