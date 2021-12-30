using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using MQTTnet.Client.Publishing;
using System.Collections.Generic;

namespace FileCopy
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly RetryPolicy _fileAccessRetryPolicy;
        private readonly string _sourcePath;
        private readonly string _destPath;
        private readonly IPublishDetections<MqttClientPublishResult> _mqttQueue;

        public Worker(ILogger<Worker> logger, IPublishDetections<MqttClientPublishResult> mqttQueue, string sourcePath, string destPath, int retryDelay, int retryCount)
        {
            _logger = logger;
            _sourcePath = sourcePath;
            _destPath = destPath;
            _mqttQueue = mqttQueue;
            _fileAccessRetryPolicy = Policy
                .Handle<FileLoadException>()
                .Or<FileNotFoundException>()
                .Or<ArgumentException>()
                .Or<OutOfMemoryException>()
                .Or<IOException>()
                .WaitAndRetry(retryCount: retryCount, retryNumber => TimeSpan.FromMilliseconds(retryDelay));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string[] filters = { "*.pdf", "*.jpg", "*.jpeg" };
            List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();

            foreach (string extension in filters)
            {
                FileSystemWatcher watcher = new FileSystemWatcher();
                watcher.Path = _sourcePath;
                watcher.Filter = extension;
                watcher.NotifyFilter = NotifyFilters.FileName;
                watcher.Created += OnChanged;
                watcher.EnableRaisingEvents = true;
                watchers.Add(watcher);
            }
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation($"File found:{e.FullPath}");
            try
            {
                _fileAccessRetryPolicy.Execute(() => { File.Copy(e.FullPath, $"{_destPath}\\{e.Name}", true); });

                Message message = new Message()
                {
                    FileName = e.FullPath
                };

                _mqttQueue.PublishAsync(message, "File", CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
            }
        }
    }
}
