using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

namespace FileCopy
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly RetryPolicy _fileAccessRetryPolicy;
        private readonly string _sourcePath;
        private readonly string _destPath;

        public Worker(ILogger<Worker> logger, string sourcePath, string destPath, int retryDelay, int retryCount)
        {
            _logger = logger;
            _sourcePath = sourcePath;
            _destPath = destPath;
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
            using (FileSystemWatcher dirWatcher = new FileSystemWatcher())
            {
                dirWatcher.Path = _sourcePath;
                dirWatcher.NotifyFilter = NotifyFilters.FileName;
                dirWatcher.Created += OnChanged;
                dirWatcher.EnableRaisingEvents = true;

                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation($"File found:{e.FullPath}");
            try
            {
                _fileAccessRetryPolicy.Execute(() => { File.Copy(e.FullPath, $"{_destPath}\\{e.Name}", true); });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
            }
        }
    }
}
