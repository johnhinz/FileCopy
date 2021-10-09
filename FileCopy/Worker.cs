using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace FileCopy
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly RetryPolicy _fileAccessRetryPolicy;
        private readonly string _sourcePath;
        private readonly string _destPath;

        public Worker(ILogger<Worker> logger, string sourcePath, string destPath)
        {
            _logger = logger;
            _sourcePath = sourcePath;
            _destPath = destPath;
            _fileAccessRetryPolicy = Policy
                .Handle<FileLoadException>()
                .Or<FileNotFoundException>()
                .Or<ArgumentException>()
                .Or<OutOfMemoryException>().WaitAndRetry(retryCount: 5, retryNumber => TimeSpan.FromMilliseconds(500));
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
                using (MemoryStream ms = new MemoryStream()) 
                {
                    new FileStream(e.FullPath, FileMode.Open).CopyTo(ms);
                    HttpResponseMessage output;
                    using (var _client = new HttpClient())
                    {
                        var request = new MultipartFormDataContent();
                        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", "XXX");
                        request.Add(new StreamContent(ms),"document");//, "document", Path.GetFileName(e.FullPath));
                        output = _client.PostAsync("http://192.168.0.80/api/documents/post_document/", request).Result;
                    }
                    //return JsonConvert.DeserializeObject<Predictions>(await output.Content.ReadAsStringAsync());
                }
                //_fileAccessRetryPolicy.Execute(() => { File.Copy(e.FullPath, $"{_destPath}\\{e.Name}", true); });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
            }
            
        }
    }
}
